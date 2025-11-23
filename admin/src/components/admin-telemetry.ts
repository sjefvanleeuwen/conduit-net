import { ConduitService } from '../services/ConduitService';
import { ITelemetryCollector, TraceSpanDto, mapTraceSpanDto } from '../contracts';

export class AdminTelemetry extends HTMLElement {
    private telemetryService: ITelemetryCollector | null = null;
    private refreshInterval: number | null = null;

    connectedCallback() {
        this.render();
        this.initializeClient();
    }

    disconnectedCallback() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>Telemetry Traces</h1>
                <button class="btn-primary" id="refresh-btn">Refresh</button>
            </div>
            
            <div class="dashboard-panel">
                <table class="table" id="traces-table">
                    <thead>
                        <tr>
                            <th>Trace ID</th>
                            <th>Service</th>
                            <th>Name</th>
                            <th>Start Time</th>
                            <th>Duration (ms)</th>
                            <th>Tags</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td colspan="6">Loading...</td></tr>
                    </tbody>
                </table>
            </div>
        `;
        
        this.querySelector('#refresh-btn')?.addEventListener('click', () => this.loadTraces());
    }

    async initializeClient() {
        try {
            this.telemetryService = await ConduitService.getTelemetryService();
            this.loadTraces();
            
            // Auto refresh every 5 seconds
            this.refreshInterval = window.setInterval(() => this.loadTraces(), 5000);
        } catch (err) {
            console.error(err);
            this.querySelector('tbody')!.innerHTML = '<tr><td colspan="6" class="error">Failed to connect to Telemetry Service</td></tr>';
        }
    }

    async loadTraces() {
        if (!this.telemetryService) return;
        try {
            const rawSpans = await this.telemetryService.GetRecentSpansAsync() as any[];
            const spans = rawSpans.map(mapTraceSpanDto);
            this.renderSpans(spans);
        } catch (err) {
            console.error(err);
            // Don't overwrite if it's just a transient error during auto-refresh
            if (!this.querySelector('tr')?.innerHTML.includes('Trace ID')) {
                 this.querySelector('tbody')!.innerHTML = '<tr><td colspan="6" class="error">Failed to load traces</td></tr>';
            }
        }
    }

    renderSpans(spans: TraceSpanDto[]) {
        const tbody = this.querySelector('tbody');
        if (!tbody) return;

        if (spans.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6">No traces found</td></tr>';
            return;
        }

        tbody.innerHTML = spans.map(span => `
            <tr>
                <td><span class="badge info">${span.TraceId.substring(0, 8)}...</span></td>
                <td>${span.ServiceName}</td>
                <td>${span.Name}</td>
                <td>${new Date(span.StartTime).toLocaleTimeString()}</td>
                <td>${span.Duration.toFixed(2)}</td>
                <td>${Object.keys(span.Tags).length} tags</td>
            </tr>
        `).join('');
    }
}

customElements.define('admin-telemetry', AdminTelemetry);
