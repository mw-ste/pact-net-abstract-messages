namespace PactNetAbstactMessages;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PactNet;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

public class ProducerTest : IDisposable
{
    private readonly PactVerifier _pactVerifier;

    public ProducerTest(ITestOutputHelper output)
    {
        _pactVerifier = new PactVerifier(new PactVerifierConfig
        {
            Outputters = new[] { new XunitOutput(output) },
            LogLevel = PactLogLevel.Information
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _pactVerifier.Dispose();
    }

    [Fact]
    public void ShouldSendExpectedMessage()
    {
        var pactDir = "../../../pacts/";
        string pactPath = Path.Combine(pactDir, "Consumer-Producer.json");

        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.Objects,
            Converters = new List<JsonConverter> { new MatcherWrapperConverter() }
        };

        _pactVerifier
            .MessagingProvider("Producer", serializerSettings)
            .WithProviderMessages(scenarios =>
            {
                scenarios.Add(
                    nameof(Message),
                    () => new Message(new[] { new DoubleKeyValuePair("Key", 12.3) }));
            })
            .WithFileSource(new FileInfo(pactPath))
            .Verify();
    }
}
