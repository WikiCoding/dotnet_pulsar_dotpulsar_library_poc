using System.Text;
using dotnet_pulsar_poc.Config;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

namespace dotnet_pulsar_poc.Producer;

public class PulsarProducer(IPulsarClient pulsarClient, ILogger<PulsarProducer> logger, PulsarSettings pulsarSettings)
{
    public async Task ProduceAsync(string message)
    {
        var producer = pulsarClient.NewProducer()
            .Topic(pulsarSettings.Topic)
            .CompressionType(CompressionType.None) // not required, I'm setting the default value anyway
            .ProducerName($"Producer-{Guid.NewGuid()}") // not required
            .StateChangedHandler((stateChanged, _) =>
            {
                logger.LogInformation("The reader for topic '{ProducerTopic}' changed state to '{StateChangedProducerState}'",
                    stateChanged.Producer.Topic,
                    stateChanged.ProducerState);
            }) // not required
            .Create();

        var msg = Encoding.UTF8.GetBytes(message);
        
        logger.LogInformation("Sending message {}", message);

        await producer.NewMessage().Send(msg);
    }
}