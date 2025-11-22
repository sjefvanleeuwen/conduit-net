import template from './conduit-nav.html?raw';

export class ConduitNav extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        this.innerHTML = template;
    }
}

customElements.define('conduit-nav', ConduitNav);
