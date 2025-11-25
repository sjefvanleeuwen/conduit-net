export class DocSidebar extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="sidebar-logo">
                <a href="index.html" style="text-decoration: none;"><h2>ConduitNet</h2></a>
            </div>
            <nav class="sidebar-nav">
                <ul>
                    <li><a href="#" data-page="home"><i class="icon fa-solid fa-home"></i> Home</a></li>
                    <li><a href="#" data-page="cluster-setup"><i class="icon fa-solid fa-shield-halved"></i> Cluster Setup</a></li>
                    <li><a href="#" data-page="client"><i class="icon fa-solid fa-code"></i> TypeScript Client</a></li>
                    <li>
                        <a href="#" data-page="cli" class="has-submenu"><i class="icon fa-solid fa-terminal"></i> Conduit CLI</a>
                        <ul class="submenu" style="display: none; list-style: none; padding-left: 1.5em; font-size: 0.9em;">
                            <li><a href="#" data-page="cli" data-anchor="registry"><i class="icon fa-solid fa-box"></i> Registry</a></li>
                            <li><a href="#" data-page="cli" data-anchor="node"><i class="icon fa-solid fa-server"></i> Node</a></li>
                        </ul>
                    </li>
                    <li><a href="#" data-page="cqp"><i class="icon fa-solid fa-database"></i> CQP Manual</a></li>
                    <li><a href="#" data-page="architecture"><i class="icon fa-solid fa-sitemap"></i> Architecture</a></li>
                    <li><a href="#" data-page="telemetry"><i class="icon fa-solid fa-chart-line"></i> Telemetry</a></li>
                </ul>
            </nav>
        `;

        this.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', (e) => {
                const target = e.currentTarget as HTMLElement;
                const page = target.dataset.page;
                const anchor = target.dataset.anchor;

                // Handle Submenu Toggle
                if (target.classList.contains('has-submenu')) {
                    const submenu = target.nextElementSibling as HTMLElement;
                    if (submenu) {
                        const isVisible = submenu.style.display === 'block';
                        submenu.style.display = isVisible ? 'none' : 'block';
                    }
                }

                if (page) {
                    e.preventDefault();
                    this.dispatchEvent(new CustomEvent('navigate', { 
                        detail: { page, anchor },
                        bubbles: true,
                        composed: true
                    }));
                    
                    // Update active state
                    this.querySelectorAll('a').forEach(a => a.classList.remove('active'));
                    target.classList.add('active');
                    
                    // Keep parent active if child clicked
                    const parentLi = target.closest('ul.submenu')?.parentElement;
                    if (parentLi) {
                        const parentLink = parentLi.querySelector('a.has-submenu');
                        parentLink?.classList.add('active');
                    }
                }
            });
        });
    }
}
customElements.define('doc-sidebar', DocSidebar);
