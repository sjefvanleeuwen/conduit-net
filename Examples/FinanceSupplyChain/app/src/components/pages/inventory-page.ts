import { ConduitService } from '../services/ConduitService';

export class InventoryPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Inventory</h1>
                <p class="subtitle">Stock Management</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [items, stockLevels, lowStock, valuation] = await Promise.all([
                ConduitService.getInventoryItems(),
                ConduitService.getStockLevels(),
                ConduitService.getLowStockItems(),
                ConduitService.getStockValuation()
            ]);
            this.render(items as any[], stockLevels as any[], lowStock as any[], valuation as any);
        } catch (error) {
            console.error('Failed to load Inventory data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Inventory</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(items: any[], stockLevels: any[], lowStock: any[], valuation: any) {
        this.innerHTML = `
            <div class="page-header">
                <h1>Inventory</h1>
                <p class="subtitle">Stock Management</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Total Items</h3>
                    <p class="value">${items.length}</p>
                </div>
                <div class="widget">
                    <h3>Stock Value</h3>
                    <p class="value">$${this.formatNumber(valuation?.totalValue ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Low Stock Items</h3>
                    <p class="value ${lowStock.length > 0 ? 'warning' : ''}">${lowStock.length}</p>
                </div>
                <div class="widget">
                    <h3>Total Units</h3>
                    <p class="value">${this.formatNumber(valuation?.totalUnits ?? 0)}</p>
                </div>
            </div>

            <div class="tabs">
                <div class="tab active" data-tab="stock">Stock Levels</div>
                <div class="tab" data-tab="items">Item Master</div>
                <div class="tab" data-tab="valuation">Valuation</div>
            </div>

            <div id="tab-content">
                ${this.renderStockLevels(stockLevels)}
            </div>
        `;

        this.setupTabs(stockLevels, items, valuation);
    }

    setupTabs(stockLevels: any[], items: any[], valuation: any) {
        this.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const tabName = (tab as HTMLElement).dataset.tab;
                const content = this.querySelector('#tab-content');
                if (content) {
                    switch (tabName) {
                        case 'stock':
                            content.innerHTML = this.renderStockLevels(stockLevels);
                            break;
                        case 'items':
                            content.innerHTML = this.renderItems(items);
                            break;
                        case 'valuation':
                            content.innerHTML = this.renderValuation(valuation);
                            break;
                    }
                }
            });
        });
    }

    renderStockLevels(stockLevels: any[]): string {
        const statusMap = ['', 'In Stock', 'Low Stock', 'Out of Stock', 'Overstock'];
        const badgeMap = ['', 'success', 'warning', 'danger', 'info'];
        
        return `
            <div class="panel">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Item Code</th>
                            <th>Name</th>
                            <th class="amount">On Hand</th>
                            <th class="amount">Reserved</th>
                            <th class="amount">Available</th>
                            <th class="amount">On Order</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${stockLevels.map(s => `
                            <tr>
                                <td>${s.itemCode}</td>
                                <td>${s.itemName}</td>
                                <td class="amount">${s.onHand}</td>
                                <td class="amount">${s.reserved}</td>
                                <td class="amount">${s.available}</td>
                                <td class="amount">${s.onOrder}</td>
                                <td><span class="badge ${badgeMap[s.status]}">${statusMap[s.status]}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderItems(items: any[]): string {
        return `
            <div class="panel">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Category</th>
                            <th>Unit</th>
                            <th class="amount">Cost</th>
                            <th class="amount">Price</th>
                            <th class="amount">Reorder Point</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${items.map(item => `
                            <tr>
                                <td>${item.itemCode}</td>
                                <td>${item.name}</td>
                                <td>${item.category}</td>
                                <td>${item.unit}</td>
                                <td class="amount">$${this.formatNumber(item.unitCost)}</td>
                                <td class="amount">$${this.formatNumber(item.sellingPrice)}</td>
                                <td class="amount">${item.reorderPoint}</td>
                                <td><span class="badge ${item.isActive ? 'success' : 'danger'}">${item.isActive ? 'Active' : 'Inactive'}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderValuation(valuation: any): string {
        if (!valuation?.items?.length) return '<div class="panel"><p>No valuation data</p></div>';
        
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Stock Valuation as of ${new Date(valuation.asOfDate).toLocaleDateString()}</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Item Code</th>
                            <th>Name</th>
                            <th class="amount">Quantity</th>
                            <th class="amount">Unit Cost</th>
                            <th class="amount">Total Value</th>
                            <th class="amount">% of Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${valuation.items.map((item: any) => `
                            <tr>
                                <td>${item.itemCode}</td>
                                <td>${item.itemName}</td>
                                <td class="amount">${item.quantity}</td>
                                <td class="amount">$${this.formatNumber(item.unitCost)}</td>
                                <td class="amount">$${this.formatNumber(item.totalValue)}</td>
                                <td class="amount">${item.valuePercent?.toFixed(1)}%</td>
                            </tr>
                        `).join('')}
                        <tr>
                            <td colspan="4"><strong>Total</strong></td>
                            <td class="amount"><strong>$${this.formatNumber(valuation.totalValue)}</strong></td>
                            <td class="amount"><strong>100%</strong></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        `;
    }

    formatNumber(value: number): string {
        return value?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
    }
}
customElements.define('inventory-page', InventoryPage);
