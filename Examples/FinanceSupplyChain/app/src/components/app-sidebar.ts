export class AppSidebar extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="sidebar-logo">
                <h2>Finance & SCM</h2>
                <span class="subtitle">v1.0</span>
            </div>
            
            <nav class="sidebar-nav">
                <div class="sidebar-section">
                    <h3>Overview</h3>
                    <ul>
                        <li><a href="#" data-page="dashboard" class="active"><span class="icon">📊</span>Dashboard</a></li>
                    </ul>
                </div>
                
                <div class="sidebar-section">
                    <h3>Finance</h3>
                    <ul>
                        <li><a href="#" data-page="general-ledger"><span class="icon">📒</span>General Ledger</a></li>
                        <li><a href="#" data-page="accounts-payable"><span class="icon">📤</span>Accounts Payable</a></li>
                        <li><a href="#" data-page="accounts-receivable"><span class="icon">📥</span>Accounts Receivable</a></li>
                        <li><a href="#" data-page="treasury"><span class="icon">🏦</span>Treasury</a></li>
                        <li><a href="#" data-page="forecast"><span class="icon">📈</span>Cash Forecast</a></li>
                    </ul>
                </div>
                
                <div class="sidebar-section">
                    <h3>Supply Chain</h3>
                    <ul>
                        <li><a href="#" data-page="inventory"><span class="icon">📦</span>Inventory</a></li>
                        <li><a href="#" data-page="procurement"><span class="icon">🛒</span>Procurement</a></li>
                    </ul>
                </div>
            </nav>
        `;

        this.querySelectorAll('a[data-page]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = (link as HTMLElement).dataset.page;
                
                // Update active state
                this.querySelectorAll('a').forEach(a => a.classList.remove('active'));
                link.classList.add('active');
                
                // Dispatch navigation event
                this.dispatchEvent(new CustomEvent('navigate', {
                    bubbles: true,
                    detail: { page }
                }));
            });
        });
    }
}
customElements.define('app-sidebar', AppSidebar);
