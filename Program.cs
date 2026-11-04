using Microsoft.EntityFrameworkCore;
using PoolerBotCS;
using PoolerBotCS.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IrcBotService>();
builder.Services.AddSingleton<IIrcBotService>(sp => sp.GetRequiredService<IrcBotService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<IrcBotService>());

builder.Services.AddSingleton<DiscordBotService>();
builder.Services.AddSingleton<IDiscordBotService>(sp => sp.GetRequiredService<DiscordBotService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DiscordBotService>());

builder.Services.AddSingleton<IBanchoLobbyService, BanchoLobbyService>();
builder.Services.AddSingleton<IBanchoPlayerService, BanchoPlayerService>();

builder.Services.AddDbContext<PoolerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/lobby", (string lobbyName, IBanchoLobbyService service) =>
{
    service.Make(lobbyName);
    return Results.Ok();
});

app.MapPost("/pooler", (string[] playerNames, IBanchoLobbyService service) =>
{
    service.Create(playerNames);
    return Results.Ok();
});

app.MapGet("/lobby/{lobbyId}", (string lobbyId) =>
{
    
});

app.Run();
