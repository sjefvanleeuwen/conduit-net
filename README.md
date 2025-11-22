# ConduitNet

ConduitNet is a high-performance distributed communication fabric for .NET 9, featuring Just-In-Time Topology Learning and zero-configuration service discovery.

## System Topology

The system uses a decentralized architecture with a lightweight Directory for discovery. Nodes are homogeneous and self-register upon startup.

```mermaid
graph TD
    subgraph "Infrastructure"
        Directory[Directory Node<br/>(Port 5000)]
        style Directory fill:#f9f,stroke:#333,stroke-width:4px
    end

    subgraph "Service Mesh"
        UserNode1[User Node 1<br/>(Port 5001)]
        UserNode2[User Node 2<br/>(Port 5002)]
        UserNode3[User Node 3<br/>(Port 5003)]
    end

    subgraph "Clients"
        Gateway[Gateway / API Client]
        WebClient[Web Frontend]
    end

    %% Registration Flow
    UserNode1 -- "1. Register (IUserService)" --> Directory
    UserNode2 -- "1. Register (IUserService)" --> Directory
    UserNode3 -- "1. Register (IUserService)" --> Directory

    %% Discovery Flow
    Gateway -- "2. Discover(IUserService)" --> Directory
    Directory -- "3. Returns [Node1, Node2...]" --> Gateway

    %% RPC Flow
    Gateway -- "4. Connect & RPC" --> UserNode1
    Gateway -.-> UserNode2
    Gateway -.-> UserNode3

    %% Styling
    classDef node fill:#ececff,stroke:#ccccff,stroke-width:2px;
    class UserNode1,UserNode2,UserNode3 node;
```

## Key Components

- **ConduitNet.Node**: Abstract base for creating self-registering nodes.
- **ConduitNet.Directory**: Centralized (but optional) registry for service discovery.
- **ConduitNet.Client**: Smart client that queries the Directory and routes traffic.
- **ConduitNet.Server**: High-performance WebSocket RPC server using `System.IO.Pipelines`.

## Getting Started

1. **Start the Directory**:
   ```bash
   cd ConduitNet/Api1
   dotnet run --urls http://localhost:5000
   ```

2. **Start a Service Node**:
   ```bash
   cd ConduitNet/Api2
   dotnet run --urls http://localhost:5001
   ```

3. **Run the Frontend**:
   ```bash
   cd www
   npm run dev
   ```
