# Securing Conduit: mTLS over WebSockets

## Introduction

As **Conduit** evolves into a distributed service mesh, security becomes paramount. The default WebSocket protocol (`ws://`) transmits data in plaintext, making it vulnerable to eavesdropping and Man-in-the-Middle (MITM) attacks. To secure the network, we must upgrade to **Secure WebSockets (`wss://`)**.

This guide explores implementing **Mutual TLS (mTLS)** over WebSockets to ensure that not only does the client verify the server (standard TLS), but the server also verifies the client.

---

## What is mTLS?

**TLS (Transport Layer Security)** is the standard for secure communication on the web.
*   **Standard TLS (One-way):** The client verifies the server's identity using a certificate (like seeing the lock icon in a browser). The server doesn't know who the client is.
*   **Mutual TLS (mTLS):** Both parties exchange certificates. The server proves its identity to the client, AND the client proves its identity to the server.

In the context of Conduit, mTLS provides a **Zero Trust** network layer where every node and client must possess a valid cryptographic certificate signed by a trusted Certificate Authority (CA) to connect.

---

## Why mTLS for WebSockets?

WebSockets create persistent, stateful connections. Unlike short-lived HTTP requests, a compromised WebSocket connection allows an attacker to intercept a continuous stream of real-time commands and data.

### Benefits
1.  **Strong Authentication:** Only clients with a valid certificate can connect. Passwords or tokens are not strictly required for the transport layer handshake.
2.  **Encryption:** All traffic is encrypted (WSS), preventing eavesdropping.
3.  **Identity Assurance:** The certificate Common Name (CN) can be used to identify the specific node or service connecting (e.g., `CN=UserService`).

### The Browser Challenge
**Important:** Browsers do not allow JavaScript to programmatically attach client certificates to a WebSocket connection.
*   **Browser Clients:** If you use mTLS with a browser, the browser itself will prompt the user to select a certificate installed in their OS/Browser store. You cannot manage this purely in code.
*   **Server-to-Server:** mTLS is perfect for Node-to-Node communication (e.g., User Service talking to Directory Service).

---

## Architecture Strategy

For Conduit, a **Hybrid Security Model** is often best:

### 1. Internal Network (Node-to-Node) -> **mTLS**
Services running in the backend (User Service, ACL, Directory) should communicate using mTLS. This ensures that rogue services cannot join the mesh without a valid certificate.

### 2. External Clients (Browser/Admin UI) -> **TLS + Application Auth**
Since managing client certificates for every web user is impractical, external clients should use:
*   **Standard TLS (`wss://`)**: Encrypts the channel.
*   **Application Level Auth**: JWT (JSON Web Tokens) or API Keys sent in the initial WebSocket handshake (Headers or first packet).

---

## Implementation Guide

### Step 1: Generate Certificates
You need a Root CA and certificates for your nodes.

```bash
# 1. Create a self-signed Root CA
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout conduit-ca.key -out conduit-ca.crt

# 2. Create a Server Private Key and CSR
openssl req -newkey rsa:2048 -nodes -keyout node.key -out node.csr

# 3. Sign the Server Certificate with the CA
openssl x509 -req -in node.csr -CA conduit-ca.crt -CAkey conduit-ca.key -CAcreateserial -out node.crt
```

### Step 2: Configure ASP.NET Core (Kestrel)
Modify `Program.cs` or `launchSettings.json` to enforce HTTPS and require client certificates.

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps(httpsOptions =>
        {
            httpsOptions.ServerCertificate = LoadCertificate("node.pfx");
            
            // Enforce mTLS
            httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            
            // Validate Client Certificate
            httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
            {
                // Verify issuer is our internal CA
                return cert.Issuer == "CN=ConduitCA";
            };
        });
    });
});
```

### Step 3: Configure .NET Client (Node-to-Node)
When one Conduit node connects to another, it must present its certificate.

```csharp
var ws = new ClientWebSocket();
ws.Options.ClientCertificates.Add(LoadCertificate("client.pfx"));
await ws.ConnectAsync(new Uri("wss://localhost:5001/conduit"), CancellationToken.None);
```

### Step 4: Configure TypeScript Client (Browser)
For the Admin UI, you rely on the browser.
1.  Install the Client Certificate (`client.pfx`) into the User's OS (Windows Certificate Store or macOS Keychain).
2.  When `new WebSocket('wss://...')` is called, the browser will detect the server requires a certificate and prompt the user to select one.

---

## Implementation Guide (v0.2)

We have implemented a pure .NET solution for mTLS that works on Windows and Linux without external dependencies like OpenSSL.

### 1. Certificate Generation
We created a tool `ConduitNet.Tools.CertGen` that uses the .NET `System.Security.Cryptography` library to generate a self-signed CA and issue certificates.

To generate certificates:
```bash
dotnet run --project ConduitNet/ConduitNet.Tools.CertGen/ConduitNet.Tools.CertGen.csproj
```
This will create a `certs/` directory in the root with:
- `ca.crt`: The Certificate Authority public key.
- `node.pfx`: The Node's certificate (with private key), signed by CA.
- `client.pfx`: The Client's certificate (with private key), signed by CA.

### 2. Running the Stack
The `run-backend-stack.ps1` script has been updated to:
1. Build the solution.
2. Run the CertGen tool if needed.
3. Launch all services using `wss://` (Secure WebSockets).

```powershell
.\run-backend-stack.ps1
```

### 3. Verification
You can verify the mTLS connection using the tester tool:
```bash
dotnet run --project ConduitNet/ConduitNet.Tools.MtlsTester/ConduitNet.Tools.MtlsTester.csproj
```
This tool attempts to connect to the Directory Service using the generated client certificate.

### 4. Linux Support
The solution is fully cross-platform.
- Certificates are generated as standard `.crt` and `.pfx` files.
- The backend loads them from the filesystem.
- No Windows Certificate Store dependency.

---

## Summary Recommendation

For the **Conduit** project:

1.  **Enable WSS (TLS)** immediately for all connections. Plain `ws://` should not be used outside of local dev.
2.  **Use mTLS for Backend Services**: Secure the `Directory`, `UserService`, and `ACL` inter-communication.
3.  **Use Token Auth for Web Clients**: Keep the Admin UI simple by using standard WSS + JWT, unless you require military-grade security where every admin needs a hardware token/cert.
