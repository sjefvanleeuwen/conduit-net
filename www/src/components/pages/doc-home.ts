export class DocHome extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background1.png');">
                <h2>Introduction</h2>
                <p>Welcome to the ConduitNet documentation. This guide covers everything you need to know to build, deploy, and monitor distributed applications using the ConduitNet fabric.</p>
            </div>
            <div class="doc-content">

                <hr />

                <h2>What is ConduitNet?</h2>
                <p>ConduitNet is a high-performance distributed communication fabric for .NET 9. It provides a zero-config, decentralized mesh for microservices, enabling transparent RPC, real-time topology learning, and efficient message routing without the need for sidecars or complex gateway configurations.</p>
                
                <p>Key features include:</p>
                <ul>
                    <li><strong>Zero-Config Discovery:</strong> Nodes discover each other automatically.</li>
                    <li><strong>Smart Routing:</strong> Traffic is transparently redirected to the correct leader or peer.</li>
                    <li><strong>High Performance:</strong> Built on <code>System.IO.Pipelines</code> and <code>MessagePack</code> for low-latency, zero-copy I/O.</li>
                    <li><strong>Developer Friendly:</strong> Uses <code>DispatchProxy</code> to make remote calls feel like local method invocations.</li>
                </ul>

                <hr />

                <h2>Documentation Overview</h2>
                <p>This documentation is organized into the following sections:</p>

                <div class="features-grid">
                    <div class="feature-card">
                        <h3><i class="icon fa-solid fa-sitemap"></i> <a href="#architecture">Architecture</a></h3>
                        <p>A deep dive into the internal workings of ConduitNet, including the distributed consensus algorithm, leader election, and the message transport layer.</p>
                    </div>
                    <div class="feature-card">
                        <h3><i class="icon fa-solid fa-database"></i> <a href="#cqp">Query Manual (CQP)</a></h3>
                        <p>Learn how to use the Conduit Query Protocol (CQP) to filter and retrieve data across your distributed mesh efficiently.</p>
                    </div>
                    <div class="feature-card">
                        <h3><i class="icon fa-solid fa-code"></i> <a href="#client">TypeScript Client</a></h3>
                        <p>Complete reference for the TypeScript client, enabling web applications to interact seamlessly with the ConduitNet mesh.</p>
                    </div>
                    <div class="feature-card">
                        <h3><i class="icon fa-solid fa-chart-line"></i> <a href="#telemetry">Telemetry</a></h3>
                        <p>Guidance on monitoring your ConduitNet cluster, understanding metrics, and debugging distributed traces.</p>
                    </div>
                </div>
            </div>
        `;
    }
}
customElements.define('doc-home', DocHome);
