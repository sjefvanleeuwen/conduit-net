export class DocClient extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background8.png');">
                <h2>Connect Your Frontend</h2>
                <p>The ConduitNet TypeScript client brings the power of transparent RPC to the browser. Interact with your .NET microservices as if they were local JavaScript objects.</p>
            </div>
            <div class="doc-content">

                <section>
                    <h3>Installation</h3>
                    <p>Install the client package via npm. It has zero dependencies other than the MessagePack serializer.</p>
                    <pre class="line-numbers"><code class="language-bash">npm install conduit-ts-client</code></pre>
                </section>

                <section>
                    <h3>Basic Usage</h3>
                    <p>Connect to any Conduit Node in your mesh. The client handles the WebSocket handshake and binary framing automatically.</p>
                    <pre class="line-numbers"><code class="language-typescript">import { ConduitClient } from 'conduit-ts-client';

// Connect to the mesh
const client = new ConduitClient('ws://localhost:5000/');
await client.connect();

// Invoke a method dynamically
const result = await client.invoke('IUserService', 'GetUserAsync', [123]);</code></pre>
                </section>

                <section>
                    <h3>Transparent Proxies</h3>
                    <p>For the best developer experience, define your interfaces in TypeScript and create a transparent proxy. This gives you full IntelliSense and type safety.</p>
                    <pre class="line-numbers"><code class="language-typescript">// 1. Define the interface (matches your .NET contract)
interface IUserService {
    GetUserAsync(id: number): Promise<UserDto>;
    SaveUserAsync(user: UserDto): Promise<void>;
}

interface UserDto {
    Id: number;
    Name: string;
    Email: string;
}

// 2. Create the proxy
const userService = client.createProxy&lt;IUserService&gt;('IUserService');

// 3. Call methods naturally
try {
    const user = await userService.GetUserAsync(123);
    console.log(\`Hello, \${user.Name}!\`);
} catch (err) {
    console.error('RPC Failed:', err);
}</code></pre>
                </section>

                <section>
                    <h3>Features</h3>
                    <ul>
                        <li><strong>Binary Protocol:</strong> Uses MessagePack for compact, high-performance serialization.</li>
                        <li><strong>Zero-Copy:</strong> Efficiently handles binary frames using <code>Uint8Array</code> and <code>DataView</code>.</li>
                        <li><strong>Type Safe:</strong> Leverages TypeScript generics and proxies for a robust development experience.</li>
                        <li><strong>Universal:</strong> Works in any modern browser or Node.js environment that supports WebSockets.</li>
                    </ul>
                </section>
            </div>
        `;
        // Re-run Prism highlighting if available
        if ((window as any).Prism) {
            (window as any).Prism.highlightAllUnder(this);
        }
    }
}
customElements.define('doc-client', DocClient);
