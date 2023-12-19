using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PactNet.Matchers;
using Xunit;
using Xunit.Abstractions;

namespace PactNet.Learning;

public abstract record KeyValuePair(string Key);
public record IntegerKeyValuePair(string Key, int IntegerValue) : KeyValuePair(Key);
public record DoubleKeyValuePair(string Key, double DoubleValue) : KeyValuePair(Key);

public record Message(KeyValuePair[] KeyValuePairs);

public class StackOverflowConsumerTest
{
    private readonly IMessagePactBuilderV3 _pact;

    public StackOverflowConsumerTest(ITestOutputHelper output)
    {
        var pact = Pact.V3(
            "StackOverflowConsumer",
            "StackOverflowProvider",
            new PactConfig
            {
                PactDir = "./pacts/",
                DefaultJsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Converters = new List<JsonConverter> { new MyExpandoObjectConverter() }
                }
            });

        _pact = pact.WithMessageInteractions();

        var pactPath = Path.GetFullPath(Path.Combine(pact.Config.PactDir, $"{pact.Consumer}-{pact.Provider}.json"));
        output.WriteLine($"Writing pact to: {pactPath}");
    }

    [Fact]
    public void ShouldReceiveDoubleKeyValuePair()
    {
        var message = new
        {
            Key = Match.Type("SomeValue"),
            DoubleValue = Match.Decimal(12.3)
        };

        _pact
            .ExpectsToReceive(nameof(DoubleKeyValuePair))
            .WithJsonContent(message)
            .Verify<DoubleKeyValuePair>(msg =>
            {
                Assert.Equal("SomeValue", msg.Key);
                Assert.Equal(12.3, msg.DoubleValue);
            });
    }

    [Fact]
    public void ShouldReceiveExpectedMessage()
    {
        var message = new
        {
            KeyValuePairs = Match.MinType(
                new
                {
                    Key = Match.Type("SomeValue"),
                    DoubleValue = Match.Decimal(12.3)
                }, 1)
        };

        _pact
            .ExpectsToReceive(nameof(Message))
            .WithJsonContent(message)
            .Verify<Message>(msg =>
            {
                Assert.Equal(new DoubleKeyValuePair("SomeValue", 12.3), msg.KeyValuePairs.Single());
            });
    }

    [Fact]
    public void ShouldReceiveExpectedMessageUsingCustomConverter()
    {
        var message = new MatcherWrapper<Message>
        {
            ["KeyValuePairs"] = Match.MinType(
                new MatcherWrapper<DoubleKeyValuePair>
                {
                    ["Key"] = Match.Type("SomeValue"),
                    // shortcoming with naive implementation:
                    // provider will need to put DoubleKeyValuePair into the array!
                    ["DoubleValue"] = Match.Decimal(12.3)
                }, 1)
        };

        _pact
            .ExpectsToReceive(nameof(Message))
            .WithJsonContent(message)
            .Verify<Message>(msg =>
            {
                Assert.Equal(new DoubleKeyValuePair("SomeValue", 12.3), msg.KeyValuePairs.Single());
            });
    }

    public interface IMatcherWrapper : IDictionary<string, object>
    {
        Type ContentType { get; }
    }

    public sealed class MatcherWrapper<TContent> : Dictionary<string, object>, IMatcherWrapper
    {
        public Type ContentType => typeof(TContent);
    }

    public class MyExpandoObjectConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var json = new JObject();

            var matcherWrapper = (IMatcherWrapper)value;

            if (serializer.TypeNameHandling != TypeNameHandling.None)
            {
                var type = matcherWrapper.ContentType;
                // $type needs to be first key!
                json["$type"] = $"{type}, {type.Assembly.GetName().Name}";
            }

            foreach ((string key, object obj) in matcherWrapper)
            {
                // property names need to match the used CamelCasePropertyNamesContractResolver 
                var k = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(key);
                json[k] = JToken.FromObject(obj, serializer);
            }

            json.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanConvert(Type objectType) =>
            objectType
                .GetInterfaces()
                .Contains(typeof(IMatcherWrapper));
    }

    public class MyExpandoObjectBinder : ISerializationBinder
    {
        private readonly DefaultSerializationBinder _defaultBinder;

        public MyExpandoObjectBinder()
        {
            _defaultBinder = new DefaultSerializationBinder();
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return this._defaultBinder.BindToType(assemblyName, typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var type = serializedType.GetInterfaces().Contains(typeof(IMatcherWrapper))
                ? serializedType.GetGenericArguments().Single()
                : serializedType;

            this._defaultBinder.BindToName(type, out assemblyName, out typeName);
        }
    }
}
