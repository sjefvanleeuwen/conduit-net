export class DocCli extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background2.png');">
                <h2>Conduit CLI (cn)</h2>
                <p>The unified command-line interface for managing the entire Conduit ecosystem, from development to deployment.</p>
            </div>
            <div class="doc-content">
                <hr />

                <h2>Overview</h2>
                <p>The Conduit CLI (<code>cn</code>) replaces multiple standalone tools with a single, modern binary. It follows a noun-verb structure similar to other cloud CLIs.</p>
                
                <pre><code>cn &lt;group&gt; &lt;command&gt; [arguments] [flags]</code></pre>

                <hr />

                <h2 id="registry">Registry Commands</h2>
                <p>Manage packages and interactions with the Conduit Service Registry.</p>

                <h3>Publishing a Package</h3>
                <p>To build and push the current project as a <code>.cnp</code> package:</p>
                <pre><code>cn registry publish</code></pre>
                <p>This command zips the build output and uploads it to the configured registry.</p>

                <h3>Installing a Package</h3>
                <p>To download and extract a package to your local cache:</p>
                <pre><code>cn registry install &lt;package-name&gt;</code></pre>

                <hr />

                <h2 id="node">Node Commands</h2>
                <p>Manage local or remote Conduit Nodes.</p>

                <h3>Starting a Node</h3>
                <pre><code>cn node start</code></pre>
                <p>Starts a Conduit Node in the current directory.</p>

                <hr />

                <h2 id="installation">Installation</h2>
                <p>The CLI is distributed as a .NET Tool.</p>
                <pre><code>dotnet tool install -g Conduit.Cli</code></pre>
            </div>
        `;
    }
}
customElements.define('doc-cli', DocCli);
