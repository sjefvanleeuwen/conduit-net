import './app-sidebar'
import './app-header'
import './pages/dashboard-page'
import './pages/gl-page'
import './pages/ap-page'
import './pages/ar-page'
import './pages/treasury-page'
import './pages/forecast-page'
import './pages/inventory-page'
import './pages/procurement-page'

export class AppLayout extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="wrapper">
                <app-sidebar></app-sidebar>
                <div class="main-content">
                    <app-header></app-header>
                    <div class="content-wrapper" id="content-area">
                        <dashboard-page></dashboard-page>
                    </div>
                </div>
            </div>
        `;

        this.addEventListener('navigate', (e: Event) => {
            const customEvent = e as CustomEvent;
            this.loadPage(customEvent.detail.page);
        });
    }

    loadPage(page: string) {
        const content = this.querySelector('#content-area');
        if (!content) return;

        switch (page) {
            case 'dashboard':
                content.innerHTML = '<dashboard-page></dashboard-page>';
                break;
            case 'general-ledger':
                content.innerHTML = '<gl-page></gl-page>';
                break;
            case 'accounts-payable':
                content.innerHTML = '<ap-page></ap-page>';
                break;
            case 'accounts-receivable':
                content.innerHTML = '<ar-page></ar-page>';
                break;
            case 'treasury':
                content.innerHTML = '<treasury-page></treasury-page>';
                break;
            case 'forecast':
                content.innerHTML = '<forecast-page></forecast-page>';
                break;
            case 'inventory':
                content.innerHTML = '<inventory-page></inventory-page>';
                break;
            case 'procurement':
                content.innerHTML = '<procurement-page></procurement-page>';
                break;
            default:
                content.innerHTML = `<div class="panel"><h2>Page "${page}" not implemented</h2></div>`;
        }
    }
}
customElements.define('app-layout', AppLayout);
