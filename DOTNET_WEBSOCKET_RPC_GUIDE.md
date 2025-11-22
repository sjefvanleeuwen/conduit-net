# High-Performance Transparent WebSocket Conduit in .NET

This guide outlines how to connect two .NET APIs via WebSockets to achieve a transparent, high-performance distributed messaging experience. The goal is to abstract away all networking complexity, allowing developers to interact with remote services using standard .NET interfaces.

## Architecture Overview

*   **Transport**: WebSockets (Persistent, bidirectional).
*   **Data Handling**: `System.IO.Pipelines` (High-performance, zero-copy I/O).
*   **Serialization**: `MessagePack` (Compact binary format, faster and smaller than JSON).
*   **Abstraction**: `System.Reflection.DispatchProxy` (Creates transparent proxies from interfaces).
*   **Extensibility**: Pipeline/Middleware pattern for filters (Logging, Auth, Compression).

---

## Step 1: Shared Contracts

Create a shared class library (e.g., `ConduitNet.Contracts`) referenced by both APIs. This contains only interfaces and DTOs.

```csharp
// IUserService.cs
public interface IUserService
{
    Task<UserDto> GetUserAsync(int id);
    Task SaveUserAsync(UserDto user);
}

// UserDto.cs
[MessagePackObject]
public record UserDto
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string Name { get; set; }
    [Key(2)] public string Email { get; set; }
}
```

---

## Step 2: The Conduit Core (Infrastructure)

This layer handles the "plumbing" and should be in a separate library (e.g., `ConduitNet.Core`).

### 2.1 The Message Envelope
We need a standard wrapper for requests and responses.

```csharp
[MessagePackObject]
public class RpcMessage
{
    [Key(0)] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Key(1)] public string MethodName { get; set; }
    [Key(2)] public byte[] Payload { get; set; } // Serialized arguments or result
    [Key(3)] public bool IsError { get; set; }
    [Key(4)] public Dictionary<string, string> Headers { get; set; } = new();
    [Key(5)] public string InterfaceName { get; set; } // Added for discovery
}
```

### 2.2 The Pipeline Filter Interface
This allows chaining behaviors like logging or compression without modifying the core transport.

```csharp
public delegate ValueTask<RpcMessage> RpcRequestDelegate(RpcMessage message);

public interface IRpcFilter
{
    ValueTask<RpcMessage> InvokeAsync(RpcMessage message, RpcRequestDelegate next);
}
```

---

## Step 3: Client-Side Abstraction

The client API will use a `DispatchProxy` to intercept method calls and send them over the wire.

### 3.1 The Transparent Proxy
This class intercepts calls to `IUserService.GetUserAsync(1)`, serializes the `1`, sends it, waits for the response, and deserializes the `UserDto`.

```csharp
public class RpcClientProxy<T> : DispatchProxy
{
    private Func<RpcMessage, Task<RpcMessage>> _sendFunc;

    public void Initialize(Func<RpcMessage, Task<RpcMessage>> sendFunc)
    {
        _sendFunc = sendFunc;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        // 1. Create Message
        var message = new RpcMessage
        {
            InterfaceName = typeof(T).Name, // Capture interface name for discovery
            MethodName = targetMethod.Name,
            Payload = MessagePackSerializer.Serialize(args) // Serialize arguments
        };

        // 2. Send via Transport (Abstracted)
        var responseTask = _sendFunc(message);

        // 3. Handle Return Type (Task<T> or Task)
        var returnType = targetMethod.ReturnType;
        
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            return ConvertToGenericTask(responseTask, resultType);
        }
        
        return responseTask.ContinueWith(t => 
        {
            if (t.Result.IsError) throw new Exception(MessagePackSerializer.Deserialize<string>(t.Result.Payload));
        });
    }

    // Helper to convert Task<RpcMessage> to Task<T>
    private async Task<TResult> ConvertToGenericTask<TResult>(Task<RpcMessage> task, Type resultType)
    {
        var response = await task;
        if (response.IsError) 
            throw new Exception(MessagePackSerializer.Deserialize<string>(response.Payload));
            
        return MessagePackSerializer.Deserialize<TResult>(response.Payload);
    }
}
```

### 3.2 Dependency Injection Setup
This is the only code the Client API developer sees.

```csharp
public static IServiceCollection AddRpcClient<TInterface>(this IServiceCollection services) 
    where TInterface : class
{
    services.AddSingleton<TInterface>(sp => 
    {
        // Resolve the Pipeline Executor
        var pipeline = sp.GetRequiredService<RpcPipelineExecutor>(); 
        
        var proxy = DispatchProxy.Create<TInterface, RpcClientProxy<TInterface>>();
        // No URL needed here; the pipeline handles discovery
        ((RpcClientProxy<TInterface>)proxy).Initialize(msg => pipeline.ExecuteAsync(msg));
        
        return proxy;
    });
    return services;
}
```

---

## Step 4: Server-Side Abstraction

The server API needs to listen for WebSocket connections and dispatch requests to the actual implementation.

### 4.1 The Middleware
This sits in the ASP.NET Core pipeline.

```csharp
public class RpcWebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = _serviceProvider.GetRequiredService<RpcRequestHandler>();
        await handler.HandleConnectionAsync(webSocket);
    }
}
```

### 4.2 The Request Handler
This component deserializes the request, finds the matching service in the DI container, invokes the method, and returns the result.

```csharp
public class RpcRequestHandler
{
    private readonly IServiceProvider _provider;

    public async Task<RpcMessage> ProcessRequestAsync(RpcMessage request, Type serviceInterface)
    {
        // 1. Resolve the actual implementation (e.g., UserService)
        var service = _provider.GetRequiredService(serviceInterface);

        // 2. Find Method
        var method = serviceInterface.GetMethod(request.MethodName);

        // 3. Deserialize Arguments
        var args = MessagePackSerializer.Deserialize<object[]>(request.Payload);

        // 4. Invoke
        var task = (Task)method.Invoke(service, args);
        await task.ConfigureAwait(false);

        // 5. Get Result
        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);

        // 6. Return Response
        return new RpcMessage 
        { 
            Id = request.Id,
            Payload = MessagePackSerializer.Serialize(result) 
        };
    }
}
```

---

## Step 5: Pipeline & Filters (The "Middleware" Chain)

To support chaining (logging, compression, auth), we implement a builder pattern.

```csharp
public class RpcPipelineBuilder
{
    private readonly List<Func<RpcRequestDelegate, RpcRequestDelegate>> _components = new();

    public RpcPipelineBuilder Use(Func<RpcMessage, Func<Task<RpcMessage>>, Task<RpcMessage>> middleware)
    {
        _components.Add(next => 
        {
            return msg => new ValueTask<RpcMessage>(middleware(msg, () => next(msg).AsTask()));
        });
        return this;
    }

    public RpcRequestDelegate Build(RpcRequestDelegate finalHandler)
    {
        RpcRequestDelegate app = finalHandler;
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            app = _components[i](app);
        }
        return app;
    }
}
```

**Example: Service Discovery Filter**

This filter inspects the `InterfaceName` and resolves the destination URL, setting it in the headers for the transport layer to use.

```csharp
public class ServiceDiscoveryFilter : IRpcFilter
{
    public ValueTask<RpcMessage> InvokeAsync(RpcMessage message, RpcRequestDelegate next)
    {
        // Simple convention-based discovery
        // IUserService -> wss://user-service/conduit
        // IOrderService -> wss://order-service/conduit
        
        var serviceName = message.InterfaceName.Substring(1).ToLower(); // Remove 'I'
        var url = $"wss://{serviceName}-service/conduit";
        
        message.Headers["Target-Url"] = url;
        
        return next(message);
    }
}
```

---

## Step 6: Final Usage Experience

This is how the developer uses the system. Notice the complete lack of networking code.

### API 1 (Client) - `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the RPC Client for IUserService
// The URL is now resolved dynamically by the pipeline (ServiceDiscoveryFilter)
builder.Services.AddRpcClient<IUserService>();

var app = builder.Build();

// Usage in any class (Controller, BackgroundService, Domain Service)
public class MyBusinessLogic
{
    private readonly IUserService _userService;

    public MyBusinessLogic(IUserService userService)
    {
        _userService = userService;
    }

    public async Task DoWorkAsync()
    {
        // PURE .NET EXPERIENCE
        // No HTTP clients, no JSON serialization, no URLs, no Paths
        var user = await _userService.GetUserAsync(42);
    }
}

app.Run();
```

### API 2 (Server) - `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the actual implementation
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

app.UseWebSockets();

// Map the RPC endpoint
// The path is abstracted away by convention (e.g. defaults to /conduit or derived from interface)
app.MapRpcService<IUserService>();

app.Run();
```
