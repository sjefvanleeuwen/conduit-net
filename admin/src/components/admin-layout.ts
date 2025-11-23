import './admin-sidebar'
import './admin-header'
import './admin-dashboard'
import './admin-users'
import './admin-roles'
import './admin-telemetry'

export class AdminLayout extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="wrapper">
                <admin-sidebar></admin-sidebar>
                <div class="main-content">
                    <admin-header></admin-header>
                    <div class="content-wrapper" id="content-area">
                        <admin-dashboard></admin-dashboard>
                    </div>
                </div>
            </div>
        `;

        this.addEventListener('navigate', (e: any) => {
            this.loadPage(e.detail.page);
        });
    }

    loadPage(page: string) {
        const content = this.querySelector('#content-area');
        if (!content) return;

        switch (page) {
            case 'dashboard':
                content.innerHTML = '<admin-dashboard></admin-dashboard>';
                break;
            case 'users':
                content.innerHTML = '<admin-users></admin-users>';
                break;
            case 'roles':
                content.innerHTML = '<admin-roles></admin-roles>';
                break;
            case 'telemetry':
                content.innerHTML = '<admin-telemetry></admin-telemetry>';
                break;
            default:
                content.innerHTML = `<h2>Page ${page} not implemented</h2>`;
        }
    }
}
customElements.define('admin-layout', AdminLayout);
