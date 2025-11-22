using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Rpc.Client;

var builder = WebApplication.CreateBuilder(args);

// Register the RPC Client for IUserService
builder.Services.AddRpcClient<IUserService>();
builder.Services.AddTransient<MyBusinessLogic>();

var app = builder.Build();

app.MapGet("/trigger", async (MyBusinessLogic logic) => 
{
    await logic.DoWorkAsync();
    return Results.Ok("RPC Call Completed. Check Console/Logs.");
});

app.Urls.Add("http://localhost:5001");

app.Run();

// Usage in any class
public class MyBusinessLogic
{
    private readonly IUserService _userService;

    public MyBusinessLogic(IUserService userService)
    {
        _userService = userService;
    }

    public async Task DoWorkAsync()
    {
        Console.WriteLine("Calling GetUserAsync(42)...");
        var user = await _userService.GetUserAsync(42);
        Console.WriteLine($"Received User: {user.Name} ({user.Email})");
    }
}
