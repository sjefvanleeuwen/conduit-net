import { ConduitService } from '../services/ConduitService';

export class AppHeader extends HTMLElement {
    private unsubscribe: (() => void) | null = null;

    connectedCallback() {
        this.innerHTML = `
            <div class="header-left">
                <span class="breadcrumb">Finance & Supply Chain System</span>
            </div>
            <div class="header-right">
                <div class="connection-status disconnected" id="connection-status">
                    <span class="status-dot"></span>
                    <span class="status-text">Disconnected</span>
                </div>
            </div>
        `;

        this.unsubscribe = ConduitService.onStatusChange((status) => {
            this.updateConnectionStatus(status);
        });

        // Attempt to connect
        ConduitService.connect().catch(console.error);
    }

    disconnectedCallback() {
        if (this.unsubscribe) {
            this.unsubscribe();
        }
    }

    updateConnectionStatus(status: string) {
        const element = this.querySelector('#connection-status');
        const textElement = this.querySelector('.status-text');
        if (!element || !textElement) return;

        element.className = `connection-status ${status}`;
        
        switch (status) {
            case 'connected':
                textElement.textContent = 'Connected';
                break;
            case 'connecting':
                textElement.textContent = 'Connecting...';
                break;
            default:
                textElement.textContent = 'Disconnected';
        }
    }
}
customElements.define('app-header', AppHeader);
