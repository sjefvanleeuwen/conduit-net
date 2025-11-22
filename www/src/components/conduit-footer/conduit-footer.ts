import template from './conduit-footer.html?raw';

export class ConduitFooter extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        const currentYear = new Date().getFullYear();
        this.innerHTML = template.replace('{{YEAR}}', currentYear.toString());
    }
}

customElements.define('conduit-footer', ConduitFooter);
