using Grootscape.Server.Health;
using Grootscape.Server.Network;
using Grootscape.Server.Observability;
using Grootscape.Server.World;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddObservability();
builder.Services.AddGrootscapeHealthChecks();
builder.Services.AddWorld(builder.Configuration);
builder.Services.AddNetwork(builder.Configuration);

WebApplication app = builder.Build();
app.MapHealthEndpoints();
app.MapPlayEndpoint();

await app.RunAsync();
