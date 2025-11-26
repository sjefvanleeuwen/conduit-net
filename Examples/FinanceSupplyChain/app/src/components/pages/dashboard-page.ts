import { ConduitService } from '../services/ConduitService';

export class DashboardPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Dashboard</h1>
                <p class="subtitle">Financial and Supply Chain Overview</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading data...</p></div>
        `;

        await this.loadData();
    }

    async loadData() {
        try {
            const [cashPosition, arAging, apAging, lowStock] = await Promise.all([
                ConduitService.getCashPosition().catch(() => null),
                ConduitService.getArAgingReport().catch(() => null),
                ConduitService.getApAgingSummary().catch(() => null),
                ConduitService.getLowStockItems().catch(() => null)
            ]);

            this.render(cashPosition, arAging, apAging, lowStock);
        } catch (error) {
            console.error('Failed to load dashboard data:', error);
            this.innerHTML = `
                <div class="page-header">
                    <h1>Dashboard</h1>
                </div>
                <div class="panel">
                    <p>Failed to load data. Make sure all services are running.</p>
                    <button class="btn btn-primary" onclick="location.reload()">Retry</button>
                </div>
            `;
        }
    }

    render(cashPosition: any, arAging: any, apAging: any, lowStock: any) {
        const totalCash = cashPosition?.totalCash ?? 0;
        const totalAR = arAging?.grandTotal ?? 0;
        const totalAP = apAging?.totalOutstanding ?? 0;
        const lowStockCount = lowStock?.length ?? 0;

        this.innerHTML = `
            <div class="page-header">
                <h1>Dashboard</h1>
                <p class="subtitle">Financial and Supply Chain Overview</p>
            </div>
            
            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Cash Position</h3>
                    <p class="value positive">$${this.formatNumber(totalCash)}</p>
                    <p class="change">Total available cash</p>
                </div>
                <div class="widget">
                    <h3>Accounts Receivable</h3>
                    <p class="value">$${this.formatNumber(totalAR)}</p>
                    <p class="change">Outstanding invoices</p>
                </div>
                <div class="widget">
                    <h3>Accounts Payable</h3>
                    <p class="value negative">$${this.formatNumber(totalAP)}</p>
                    <p class="change">Pending payments</p>
                </div>
                <div class="widget">
                    <h3>Low Stock Items</h3>
                    <p class="value ${lowStockCount > 0 ? 'warning' : ''}">${lowStockCount}</p>
                    <p class="change">Items below reorder point</p>
                </div>
            </div>

            <div class="panel-grid">
                <div class="panel">
                    <div class="panel-header">
                        <h2>AR Aging Summary</h2>
                    </div>
                    ${this.renderArAging(arAging)}
                </div>
                
                <div class="panel">
                    <div class="panel-header">
                        <h2>AP Aging Summary</h2>
                    </div>
                    ${this.renderApAging(apAging)}
                </div>
            </div>

            <div class="panel">
                <div class="panel-header">
                    <h2>Low Stock Alerts</h2>
                </div>
                ${this.renderLowStock(lowStock)}
            </div>
        `;
    }

    renderArAging(aging: any): string {
        if (!aging) return '<p>No data available</p>';
        
        return `
            <table class="table">
                <thead>
                    <tr>
                        <th>Period</th>
                        <th class="amount">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    <tr><td>Current</td><td class="amount">$${this.formatNumber(aging.totalCurrent)}</td></tr>
                    <tr><td>1-30 Days</td><td class="amount">$${this.formatNumber(aging.total1to30)}</td></tr>
                    <tr><td>31-60 Days</td><td class="amount">$${this.formatNumber(aging.total31to60)}</td></tr>
                    <tr><td>61-90 Days</td><td class="amount">$${this.formatNumber(aging.total61to90)}</td></tr>
                    <tr><td>Over 90 Days</td><td class="amount negative">$${this.formatNumber(aging.totalOver90)}</td></tr>
                    <tr><td><strong>Total</strong></td><td class="amount"><strong>$${this.formatNumber(aging.grandTotal)}</strong></td></tr>
                </tbody>
            </table>
        `;
    }

    renderApAging(aging: any): string {
        if (!aging) return '<p>No data available</p>';
        
        return `
            <table class="table">
                <thead>
                    <tr>
                        <th>Period</th>
                        <th class="amount">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    <tr><td>Current</td><td class="amount">$${this.formatNumber(aging.current)}</td></tr>
                    <tr><td>1-30 Days</td><td class="amount">$${this.formatNumber(aging.days1to30)}</td></tr>
                    <tr><td>31-60 Days</td><td class="amount">$${this.formatNumber(aging.days31to60)}</td></tr>
                    <tr><td>61-90 Days</td><td class="amount">$${this.formatNumber(aging.days61to90)}</td></tr>
                    <tr><td>Over 90 Days</td><td class="amount negative">$${this.formatNumber(aging.over90Days)}</td></tr>
                    <tr><td><strong>Total</strong></td><td class="amount"><strong>$${this.formatNumber(aging.totalOutstanding)}</strong></td></tr>
                </tbody>
            </table>
        `;
    }

    renderLowStock(items: any[]): string {
        if (!items || items.length === 0) {
            return '<p>No low stock items</p>';
        }

        return `
            <table class="table">
                <thead>
                    <tr>
                        <th>Item Code</th>
                        <th>Name</th>
                        <th class="amount">On Hand</th>
                        <th class="amount">Reorder Point</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
                    ${items.map(item => `
                        <tr>
                            <td>${item.itemCode}</td>
                            <td>${item.itemName}</td>
                            <td class="amount">${item.onHand}</td>
                            <td class="amount">${item.reorderPoint}</td>
                            <td><span class="badge ${item.status === 3 ? 'danger' : 'warning'}">${item.status === 3 ? 'Out of Stock' : 'Low Stock'}</span></td>
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
customElements.define('dashboard-page', DashboardPage);
