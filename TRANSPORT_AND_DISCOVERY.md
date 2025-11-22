# Conduit Transport & Discovery Architecture

This document details the internal mechanics of the Conduit transport layer, service discovery, and node registration process.

## Interaction Flow (Sequence Diagram)

This diagram illustrates the lifecycle: from a Service Node registering itself, to a Client Node discovering it and establishing a persistent WebSocket transport connection.

```mermaid
sequenceDiagram
    participant Client as Client Node
    participant Directory as Directory Node
    participant Service as Service Node (Api2)

    Note over Service, Directory: 1. Registration Phase (ConduitNodeRegistrationService)
    Service->>Service: Startup (ConduitNode.Run)
    Service->>Directory: RegisterAsync(NodeInfo)
    Note right of Service: Sends ID, Address, and<br/>List of Services (e.g. "IUserService")
    Directory-->>Service: Acknowledge

    Note over Client, Service: 2. Discovery & Transport Phase
    Client->>Client: Code calls IUserService.GetUser()
    Client->>Directory: DiscoverAsync("IUserService")
    Directory-->>Client: Returns List<NodeInfo> (contains Service Node URL)
    
    Client->>Client: ServiceDiscoveryFilter selects Node<br/>& sets "Target-Url" Header
    
    Note over Client, Service: 3. Transport (ConduitTransport)
    alt Connection Exists
        Client->>Client: Reuse existing WebSocket
    else New Connection
        Client->>Service: WebSocket Connect
        Service-->>Client: Connected
    end
    
    Client->>Service: SendAsync(ConduitMessage)
    Note right of Client: Serialized via MessagePack<br/>Sent over WebSocket
    
    Service->>Service: ConduitServer routes to<br/>UserService implementation
    Service-->>Client: Response (ConduitMessage)
    Client->>Client: Deserialize & Return Result
```

## Component Architecture (Class Diagram)

This diagram shows the relationships between the core classes involved in the transport and discovery mechanisms.

```mermaid
classDiagram
    class ConduitNode {
        +Run()
        #RegisterConduitService~T~()
        -ConfigureCoreServices()
    }
    
    class ConduitNodeRegistrationService {
        +StartAsync()
        +StopAsync()
    }
    
    class ConduitTransport {
        -ConcurrentDictionary connections
        +SendAsync(ConduitMessage)
        -EnsureConnectedAsync()
    }
    
    class IConduitDirectory {
        <<interface>>
        +RegisterAsync(NodeInfo)
        +DiscoverAsync(serviceName)
    }
    
    class ConduitDirectoryService {
        -ConcurrentDictionary registry
        +RegisterAsync()
        +DiscoverAsync()
    }

    class NodeInfo {
        +String Id
        +String Address
        +List~String~ Services
    }

    ConduitNode --> ConduitNodeRegistrationService : Hosts
    ConduitNodeRegistrationService ..> IConduitDirectory : Registers with
    ConduitDirectoryService ..|> IConduitDirectory : Implements
    ConduitTransport ..> NodeInfo : Uses Address from
    ConduitNode ..> ConduitTransport : Uses for Client calls
```

## Key Concepts

1.  **Registration**: Every `ConduitNode` automatically registers its services with the Directory upon startup via the `ConduitNodeRegistrationService`.
2.  **Discovery**: Clients don't know where services live. They ask the `IConduitDirectory` (which is just another RPC service) to find providers.
3.  **Transport**: `ConduitTransport` manages persistent WebSocket connections. It multiplexes multiple RPC calls over a single WebSocket connection using `MessagePack` serialization and a custom framing protocol (Length Prefix + Payload).
