import { ConduitService } from '../services/ConduitService';

export class ForecastPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Cash Flow Forecast</h1>
                <p class="subtitle">Projected Cash Position</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [forecast, alerts] = await Promise.all([
                ConduitService.getCashFlowForecast(30),
                ConduitService.getForecastAlerts()
            ]);
            this.render(forecast as any, alerts as any[]);
        } catch (error) {
            console.error('Failed to load Forecast data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Cash Flow Forecast</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(forecast: any, alerts: any[]) {
        this.innerHTML = `
            <div class="page-header">
                <h1>Cash Flow Forecast</h1>
                <p class="subtitle">30-Day Projected Cash Position</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Opening Balance</h3>
                    <p class="value">$${this.formatNumber(forecast?.openingBalance ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Projected Closing</h3>
                    <p class="value positive">$${this.formatNumber(forecast?.closingBalance ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Total Inflows</h3>
                    <p class="value positive">$${this.formatNumber(forecast?.totalInflows ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Total Outflows</h3>
                    <p class="value negative">$${this.formatNumber(forecast?.totalOutflows ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Minimum Balance</h3>
                    <p class="value ${(forecast?.minimumBalance ?? 0) < 100000 ? 'warning' : ''}">$${this.formatNumber(forecast?.minimumBalance ?? 0)}</p>
                    <p class="change">${forecast?.minimumBalanceDate ? new Date(forecast.minimumBalanceDate).toLocaleDateString() : ''}</p>
                </div>
            </div>

            ${alerts?.length > 0 ? this.renderAlerts(alerts) : ''}

            <div class="panel">
                <div class="panel-header">
                    <h2>Daily Cash Flow</h2>
                </div>
                ${this.renderForecastTable(forecast)}
            </div>
        `;
    }

    renderAlerts(alerts: any[]): string {
        const severityMap = ['', 'Info', 'Warning', 'Critical'];
        const badgeMap = ['', 'info', 'warning', 'danger'];
        
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>⚠️ Alerts</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Severity</th>
                            <th>Date</th>
                            <th>Message</th>
                            <th>Recommendation</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${alerts.map(a => `
                            <tr>
                                <td><span class="badge ${badgeMap[a.severity]}">${severityMap[a.severity]}</span></td>
                                <td>${new Date(a.alertDate).toLocaleDateString()}</td>
                                <td>${a.message}</td>
                                <td>${a.recommendation || '-'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderForecastTable(forecast: any): string {
        if (!forecast?.periods?.length) return '<p>No forecast data available</p>';
        
        // Show weekly summary instead of daily to keep it manageable
        const weeklyData: any[] = [];
        let weekInflows = 0, weekOutflows = 0;
        let weekStart = new Date(forecast.periods[0].date);
        
        forecast.periods.forEach((period: any, index: number) => {
            weekInflows += period.inflows;
            weekOutflows += period.outflows;
            
            if ((index + 1) % 7 === 0 || index === forecast.periods.length - 1) {
                weeklyData.push({
                    weekStart: weekStart,
                    weekEnd: new Date(period.date),
                    inflows: weekInflows,
                    outflows: weekOutflows,
                    netFlow: weekInflows - weekOutflows,
                    closingBalance: period.closingBalance
                });
                weekInflows = 0;
                weekOutflows = 0;
                if (index < forecast.periods.length - 1) {
                    weekStart = new Date(forecast.periods[index + 1].date);
                }
            }
        });

        return `
            <table class="table">
                <thead>
                    <tr>
                        <th>Week</th>
                        <th class="amount">Inflows</th>
                        <th class="amount">Outflows</th>
                        <th class="amount">Net Flow</th>
                        <th class="amount">Closing Balance</th>
                    </tr>
                </thead>
                <tbody>
                    ${weeklyData.map(week => `
                        <tr>
                            <td>${week.weekStart.toLocaleDateString()} - ${week.weekEnd.toLocaleDateString()}</td>
                            <td class="amount positive">$${this.formatNumber(week.inflows)}</td>
                            <td class="amount negative">$${this.formatNumber(week.outflows)}</td>
                            <td class="amount ${week.netFlow >= 0 ? 'positive' : 'negative'}">$${this.formatNumber(week.netFlow)}</td>
                            <td class="amount">$${this.formatNumber(week.closingBalance)}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;
    }

    formatNumber(value: number): string {
        return value?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
    }
}
customElements.define('forecast-page', ForecastPage);
