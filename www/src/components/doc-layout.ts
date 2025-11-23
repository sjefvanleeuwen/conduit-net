import './doc-sidebar';
import './doc-header';
// Import pages (we will create these next)
import './pages/doc-home';
import './pages/doc-client';
import './pages/doc-cqp';
import './pages/doc-architecture';
import './pages/doc-telemetry';

export class DocLayout extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="wrapper">
                <doc-sidebar></doc-sidebar>
                <div class="main-content">
                    <doc-header></doc-header>
                    <div class="content-wrapper" id="content-area">
                        <doc-home></doc-home>
                    </div>
                </div>
            </div>
        `;

        this.addEventListener('navigate', (e: any) => {
            const page = e.detail.page;
            window.history.pushState({ page }, '', `#${page}`);
            this.loadPage(page);
        });

        window.addEventListener('popstate', (e) => {
            const page = e.state?.page || this.getPageFromUrl();
            this.loadPage(page);
        });

        // Initial load
        const initialPage = this.getPageFromUrl();
        this.loadPage(initialPage);
    }

    getPageFromUrl() {
        const hash = window.location.hash.slice(1);
        return hash || 'home';
    }

    loadPage(page: string) {
        const content = this.querySelector('#content-area');
        const header = this.querySelector('doc-header') as any;
        const sidebar = this.querySelector('doc-sidebar') as any;
        if (!content) return;

        let componentName = '';
        let breadcrumb = '';

        switch (page) {
            case 'home':
                componentName = 'doc-home';
                breadcrumb = 'Documentation / Home';
                break;
            case 'client':
                componentName = 'doc-client';
                breadcrumb = 'Documentation / TypeScript Client';
                break;
            case 'cqp':
                componentName = 'doc-cqp';
                breadcrumb = 'Documentation / CQP Manual';
                break;
            case 'architecture':
                componentName = 'doc-architecture';
                breadcrumb = 'Documentation / Architecture';
                break;
            case 'telemetry':
                componentName = 'doc-telemetry';
                breadcrumb = 'Documentation / Telemetry';
                break;
            default:
                componentName = 'doc-home';
                breadcrumb = 'Documentation / Home';
        }

        content.innerHTML = `<${componentName}></${componentName}>`;
        if (header && header.updateBreadcrumb) {
            header.updateBreadcrumb(breadcrumb);
        }
        
        // Update sidebar active state
        if (sidebar) {
            const links = sidebar.querySelectorAll('a');
            links.forEach((link: any) => {
                link.classList.remove('active');
                if (link.dataset.page === page) {
                    link.classList.add('active');
                }
            });
        }
    }
}
customElements.define('doc-layout', DocLayout);
