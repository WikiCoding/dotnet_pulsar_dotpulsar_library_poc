# Dependency
1. DotPulsar

# Local service
```yaml
services:
  pulsar:
    image: apachepulsar/pulsar:latest
    container_name: pulsar
    ports:
      - "6650:6650"
      - "8080:8080"
    command: >
      bin/pulsar standalone
    environment:
      PULSAR_MEM: "-Xms512m -Xmx512m -XX:MaxDirectMemorySize=1g"
```

# Program.cs

1. We need to inject the Pulsar client, producer and consumer.
```csharp
var pulsarSettings = builder.Configuration.GetSection("Pulsar").Get<PulsarSettings>() ?? new PulsarSettings();

builder.Services.AddSingleton(pulsarSettings);

var pulsarClient = PulsarClient
    .Builder()
    .ServiceUrl(new Uri(pulsarSettings.Url))
    .VerifyCertificateAuthority(false)
    .Authentication(AuthenticationFactory.Token(pulsarSettings.Token)) // we can pass a token without problems even if Pulsar isn't configured to ask for it
    .Build();

builder.Services.AddSingleton(pulsarClient);
builder.Services.AddScoped<PulsarProducer>();
builder.Services.AddHostedService<PulsarConsumer>();
```

# Producer
```csharp
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
        
        logger.LogInformation("Sending message {message}", message);

        await producer.NewMessage().Send(msg);
    }
}
```

# Consumer
```csharp
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

        await foreach (var message in consumer.Messages().WithCancellation(stoppingToken))
        {
            var msgString = Encoding.UTF8.GetString(message.Value()); // converting from byte[]
            
            logger.LogInformation("Received message: {msgString}", msgString);
            
            await consumer.Acknowledge(message.MessageId, stoppingToken);
        }
    }
```

# Reference docs
1. See here: https://github.com/apache/pulsar-dotpulsar