namespace PactNetAbstactMessages;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PactNet;
using PactNet.Matchers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

public partial class ConsumerTest
{
    private readonly IMessagePactBuilderV3 _pact;

    public ConsumerTest(ITestOutputHelper output)
    {
        var pact = Pact.V3(
            "Consumer",
            "Producer",
            new PactConfig
            {
                PactDir = "../../../pacts/",
                DefaultJsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Converters = new List<JsonConverter> { new MatcherWrapperConverter() }
                }
            });

        _pact = pact.WithMessageInteractions();
        output.WriteLine($"Writing pact to: {Path.GetFullPath(Path.Combine(pact.Config.PactDir, $"{pact.Consumer}-{pact.Provider}.json"))}");
    }

    [Fact]
    public void ShouldReceiveExpectedMessage()
    {
        var message = new MatcherWrapper<Message>
        {
            ["KeyValuePairs"] = Match.MinType(
                new MatcherWrapper<DoubleKeyValuePair>
                {
                    ["Key"] = Match.Type("SomeValue"),
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
}
