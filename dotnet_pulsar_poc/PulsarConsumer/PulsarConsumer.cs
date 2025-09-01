using System.Text;
using dotnet_pulsar_poc.Config;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

namespace dotnet_pulsar_poc.PulsarConsumer;

public class PulsarConsumer(ILogger<PulsarConsumer> logger, IPulsarClient pulsarClient, PulsarSettings pulsarSettings)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consuming pulsar events from {} position", 
            Enum.Parse<SubscriptionInitialPosition>(pulsarSettings.InitialPosition));
        
        var consumer = pulsarClient.NewConsumer()
            .Topic(pulsarSettings.Topic)
            .SubscriptionName(pulsarSettings.SubscriptionName)
            .SubscriptionType(SubscriptionType.Exclusive)
            .ConsumerName($"Consumer-{Guid.NewGuid()}") // not required
            .InitialPosition(Enum.Parse<SubscriptionInitialPosition>(pulsarSettings.InitialPosition)) // not required, default is Earliest
            .StateChangedHandler((stateChanged, _) =>
            {
                logger.LogInformation("The reader for topic '{ConsumerTopic}' changed state to '{StateChangedConsumerState}'",
                    stateChanged.Consumer.Topic,
                    stateChanged.ConsumerState);
            }, cancellationToken: stoppingToken) // not required
            .Create();

        await foreach (var message in consumer.Messages(stoppingToken))
        {
            var msgString = Encoding.UTF8.GetString(message.Value()); // converting from byte[]
            
            logger.LogInformation("Received message: {msgString}", msgString);
            
            await consumer.Acknowledge(message.MessageId, stoppingToken);
        }
    }
}