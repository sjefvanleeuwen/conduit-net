export class AdminSidebar extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="sidebar-logo">
                <h2>Conduit Admin</h2>
            </div>
            <nav class="sidebar-nav">
                <ul>
                    <li><a href="#" data-page="dashboard" class="active"><i class="icon fa-tachometer-alt"></i> Dashboard</a></li>
                    <li><a href="#" data-page="users"><i class="icon fa-users"></i> Users</a></li>
                    <li><a href="#" data-page="roles"><i class="icon fa-lock"></i> Roles</a></li>
                    <li><a href="#" data-page="telemetry"><i class="icon fa-chart-line"></i> Telemetry</a></li>
                    <li><a href="#" data-page="settings"><i class="icon fa-cogs"></i> Settings</a></li>
                </ul>
            </nav>
        `;

        this.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = (e.currentTarget as HTMLElement).dataset.page;
                this.dispatchEvent(new CustomEvent('navigate', { 
                    detail: { page },
                    bubbles: true,
                    composed: true
                }));
                
                // Update active state
                this.querySelectorAll('a').forEach(a => a.classList.remove('active'));
                (e.currentTarget as HTMLElement).classList.add('active');
            });
        });
    }
}
customElements.define('admin-sidebar', AdminSidebar);
