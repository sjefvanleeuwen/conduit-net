export class DocSidebar extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="sidebar-logo">
                <a href="index.html" style="text-decoration: none;"><h2>ConduitNet</h2></a>
            </div>
            <nav class="sidebar-nav">
                <ul>
                    <li><a href="#" data-page="home"><i class="icon fa-solid fa-home"></i> Home</a></li>
                    <li><a href="#" data-page="client"><i class="icon fa-solid fa-code"></i> TypeScript Client</a></li>
                    <li><a href="#" data-page="cqp"><i class="icon fa-solid fa-database"></i> CQP Manual</a></li>
                    <li><a href="#" data-page="architecture"><i class="icon fa-solid fa-sitemap"></i> Architecture</a></li>
                    <li><a href="#" data-page="telemetry"><i class="icon fa-solid fa-chart-line"></i> Telemetry</a></li>
                </ul>
            </nav>
        `;

        this.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', (e) => {
                const page = (e.currentTarget as HTMLElement).dataset.page;
                if (page) {
                    e.preventDefault();
                    this.dispatchEvent(new CustomEvent('navigate', { 
                        detail: { page },
                        bubbles: true,
                        composed: true
                    }));
                    
                    // Update active state
                    this.querySelectorAll('a').forEach(a => a.classList.remove('active'));
                    (e.currentTarget as HTMLElement).classList.add('active');
                }
            });
        });
    }
}
customElements.define('doc-sidebar', DocSidebar);
