# Conduit TypeScript Client

A TypeScript client for the Conduit WebSocket RPC protocol.

## Installation

```bash
npm install conduit-ts-client
```

## Usage

```typescript
import { ConduitClient } from 'conduit-ts-client';

// 1. Connect to a Conduit Node
const client = new ConduitClient('ws://localhost:5000/conduit');
await client.connect();

// 2. Invoke a Remote Method
// Option A: Direct Invocation
// const user = await client.invoke<UserDto>('IUserService', 'GetUserAsync', [123]);

// Option B: Transparent Proxy (Recommended)
interface IUserService {
    GetUserAsync(id: number): Promise<UserDto>;
}

const userService = client.createProxy<IUserService>('IUserService');

try {
    const user = await userService.GetUserAsync(123);
    console.log('User:', user);
} catch (err) {
    console.error('RPC Error:', err);
}
```

## Features

- **Transparent RPC**: Calls remote methods by name.
- **Dynamic Proxies**: Create TypeScript objects that mirror .NET interfaces.
- **MessagePack**: Uses efficient binary serialization.
- **Zero Dependencies**: (Except `@msgpack/msgpack`).
