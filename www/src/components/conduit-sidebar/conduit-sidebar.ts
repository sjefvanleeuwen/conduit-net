import template from './conduit-sidebar.html?raw';

export class ConduitSidebar extends HTMLElement {
    constructor() {
        super();
    }

    connectedCallback() {
        // Capture any content passed inside the tag
        const originalContent = this.innerHTML;
        
        // Render the sidebar skeleton
        this.innerHTML = template;
        
        // Inject the original content into the placeholder
        // If there was content, we assume it's a section or list that fits under the HR
        if (originalContent.trim()) {
            const container = this.querySelector('#sidebar-content');
            if (container) {
                container.innerHTML = originalContent;
            }
        } else {
            // If no content, remove the HR and the container to clean up
            const hr = this.querySelector('hr');
            const container = this.querySelector('#sidebar-content');
            if (hr) hr.remove();
            if (container) container.remove();
        }

        // Highlight active link
        this.highlightActiveLink();
    }

    highlightActiveLink() {
        const currentPath = window.location.pathname;
        const links = this.querySelectorAll('#sidebar a');
        
        links.forEach(link => {
            const href = link.getAttribute('href');
            if (href && currentPath.endsWith(href)) {
                link.classList.add('active');
            }
        });
    }
}

customElements.define('conduit-sidebar', ConduitSidebar);
