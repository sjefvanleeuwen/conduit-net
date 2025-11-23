export class DocHeader extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="header-left">
                <button id="sidebar-toggle">☰</button>
                <span class="breadcrumb" id="breadcrumb">Documentation / Home</span>
            </div>
            <div class="header-right">
                <span class="user-info">ConduitNet Docs</span>
            </div>
        `;
    }

    updateBreadcrumb(text: string) {
        const el = this.querySelector('#breadcrumb');
        if (el) el.textContent = text;
    }
}
customElements.define('doc-header', DocHeader);
