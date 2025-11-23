export class AdminHeader extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="header-left">
                <button id="sidebar-toggle">☰</button>
                <span class="breadcrumb">Home / Dashboard</span>
            </div>
            <div class="header-right">
                <span class="user-info">Admin User</span>
            </div>
        `;
    }
}
customElements.define('admin-header', AdminHeader);
