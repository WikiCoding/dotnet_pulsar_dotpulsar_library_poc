using dotnet_pulsar_poc.Config;
using dotnet_pulsar_poc.Producer;
using dotnet_pulsar_poc.PulsarConsumer;
using DotPulsar;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var pulsarSettings = builder.Configuration.GetSection("Pulsar").Get<PulsarSettings>() ?? new PulsarSettings();

builder.Services.AddSingleton(pulsarSettings);

var pulsarClient = PulsarClient
    .Builder()
    .ServiceUrl(new Uri(pulsarSettings.Url))
    .VerifyCertificateAuthority(false)
    .Authentication(AuthenticationFactory.Token(pulsarSettings.Token))
    .Build();

builder.Services.AddSingleton(pulsarClient);
builder.Services.AddScoped<PulsarProducer>();
// builder.Services.AddScoped<PulsarConsumer>();
builder.Services.AddHostedService<PulsarConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();