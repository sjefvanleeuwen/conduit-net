export class DocClusterSetup extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background1.png');">
                <h2>Cluster Setup & Security</h2>
                <p>Learn how to deploy a secure ConduitNet cluster with Mutual TLS (mTLS) and manage certificates.</p>
            </div>
            <div class="doc-content">
                
                <h2>Overview</h2>
                <p>ConduitNet is designed to run in a Zero Trust environment. By default, the backend services communicate over <strong>Secure WebSockets (wss://)</strong> using Mutual TLS (mTLS). This ensures that only authorized nodes signed by your Certificate Authority (CA) can join the mesh.</p>

                <hr class="major" />

                <h2>1. Certificate Generation</h2>
                <p>ConduitNet includes a built-in tool to generate the necessary PKI infrastructure without relying on external tools like OpenSSL. This tool creates a self-signed CA and issues certificates for your nodes and clients.</p>

                <h3>Running the CertGen Tool</h3>
                <p>Run the following command from the root of the repository:</p>
                <pre><code class="language-bash">dotnet run --project ConduitNet/ConduitNet.Tools.CertGen/ConduitNet.Tools.CertGen.csproj</code></pre>

                <p>This will create a <code>certs/</code> directory containing:</p>
                <ul>
                    <li><code>ca.crt</code>: The public certificate of your private Certificate Authority.</li>
                    <li><code>node.pfx</code>: The certificate used by backend nodes (Server Authentication).</li>
                    <li><code>client.pfx</code>: The certificate used by nodes to talk to each other (Client Authentication).</li>
                </ul>

                <div class="alert alert-info">
                    <strong>Note:</strong> The generated certificates include Subject Alternative Names (SAN) for <code>localhost</code> and <code>127.0.0.1</code> to facilitate local development.
                </div>

                <hr class="major" />

                <h2>2. Starting the Cluster</h2>
                <p>We provide a PowerShell script to orchestrate the startup of the core services (Directory, Telemetry) and example services (User, ACL).</p>

                <h3>Using the Startup Script</h3>
                <pre><code class="language-powershell">.\\run-backend-stack.ps1</code></pre>

                <p>This script performs the following actions:</p>
                <ol>
                    <li>Builds the entire solution.</li>
                    <li>Checks for certificates; if missing, it runs the <code>CertGen</code> tool automatically.</li>
                    <li>Launches the services in separate windows using <strong>wss://</strong> ports.</li>
                </ol>

                <h3>Service Ports</h3>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Service</th>
                            <th>URL</th>
                            <th>Description</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Directory Service</td>
                            <td><code>wss://localhost:5000</code></td>
                            <td>The registry and leader of the cluster.</td>
                        </tr>
                        <tr>
                            <td>Telemetry Service</td>
                            <td><code>wss://localhost:5001</code></td>
                            <td>Collects metrics and traces.</td>
                        </tr>
                        <tr>
                            <td>User Service</td>
                            <td><code>wss://localhost:5002</code></td>
                            <td>Example business service.</td>
                        </tr>
                        <tr>
                            <td>ACL Service</td>
                            <td><code>wss://localhost:5003</code></td>
                            <td>Example access control service.</td>
                        </tr>
                    </tbody>
                </table>

                <hr class="major" />

                <h2>3. Verifying Connectivity</h2>
                <p>Since the services require a client certificate to connect, you cannot simply open a browser to test the WebSocket connection. We provide a tester tool for this purpose.</p>

                <pre><code class="language-bash">dotnet run --project ConduitNet/ConduitNet.Tools.MtlsTester/ConduitNet.Tools.MtlsTester.csproj</code></pre>

                <p>If successful, you will see:</p>
                <pre><code class="language-text">Connecting to wss://localhost:5000/conduit...
Server certificate issuer matches our CA.
Successfully connected via mTLS!</code></pre>

                <hr class="major" />

                <h2>4. Cross-Platform Support</h2>
                <p>The security implementation is fully compatible with Linux and Windows.</p>
                <ul>
                    <li><strong>No Windows Certificate Store dependency:</strong> Certificates are loaded directly from the <code>certs/</code> folder.</li>
                    <li><strong>Kestrel Integration:</strong> The backend automatically configures Kestrel to require certificates when the <code>certs/</code> folder is detected.</li>
                </ul>

            </div>
        `;
        
        // Re-run Prism for syntax highlighting
        if ((window as any).Prism) {
            (window as any).Prism.highlightAll();
        }
    }
}
customElements.define('doc-cluster-setup', DocClusterSetup);
