export class ConduitHero extends HTMLElement {
    static get observedAttributes() {
        return ['title', 'subtitle', 'background-image'];
    }

    connectedCallback() {
        this.render();
    }

    attributeChangedCallback() {
        this.render();
    }

    render() {
        const title = this.getAttribute('title') || '';
        const subtitle = this.getAttribute('subtitle') || '';
        const backgroundImage = this.getAttribute('background-image');

        // If background-image is provided, override the default from CSS
        const style = backgroundImage ? `style="background-image: url('${backgroundImage}')"` : '';

        this.innerHTML = `
            <section id="blog-banner" ${style}>
                <div class="content">
                    <header>
                        <h2>${title}</h2>
                        <p>${subtitle}</p>
                    </header>
                </div>
            </section>
        `;
    }
}

customElements.define('conduit-hero', ConduitHero);
