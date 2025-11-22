using Api2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Rpc.Server;

var builder = WebApplication.CreateBuilder(args);

// Register the actual implementation
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddRpcServer();

var app = builder.Build();

app.UseWebSockets();

// Map the RPC endpoint
app.MapRpcService<IUserService>();

app.Urls.Add("http://localhost:5002");

app.Run();
