import { ConduitService } from '../services/ConduitService';

export class ProcurementPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Procurement</h1>
                <p class="subtitle">Purchase Orders & Suppliers</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [suppliers, orders, requisitions] = await Promise.all([
                ConduitService.getSuppliers(),
                ConduitService.getPurchaseOrders(),
                ConduitService.getPendingRequisitions()
            ]);
            this.render(suppliers as any[], orders as any[], requisitions as any[]);
        } catch (error) {
            console.error('Failed to load Procurement data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Procurement</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(suppliers: any[], orders: any[], requisitions: any[]) {
        const statusMap = ['', 'Draft', 'Pending Approval', 'Approved', 'Sent', 'Partially Received', 'Received', 'Cancelled', 'Closed'];
        const ratingMap = ['', 'Excellent', 'Good', 'Acceptable', 'Needs Improvement', 'Unacceptable', 'Not Rated'];
        
        this.innerHTML = `
            <div class="page-header">
                <h1>Procurement</h1>
                <p class="subtitle">Purchase Orders & Suppliers</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Purchase Orders</h3>
                    <p class="value">${orders.length}</p>
                </div>
                <div class="widget">
                    <h3>Active Suppliers</h3>
                    <p class="value">${suppliers.filter(s => s.isActive).length}</p>
                </div>
                <div class="widget">
                    <h3>Pending Requisitions</h3>
                    <p class="value ${requisitions.length > 0 ? 'warning' : ''}">${requisitions.length}</p>
                </div>
            </div>

            <div class="tabs">
                <div class="tab active" data-tab="orders">Purchase Orders</div>
                <div class="tab" data-tab="suppliers">Suppliers</div>
                <div class="tab" data-tab="requisitions">Requisitions</div>
            </div>

            <div id="tab-content">
                ${this.renderOrders(orders, statusMap)}
            </div>
        `;

        this.setupTabs(orders, suppliers, requisitions, statusMap, ratingMap);
    }

    setupTabs(orders: any[], suppliers: any[], requisitions: any[], statusMap: string[], ratingMap: string[]) {
        this.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const tabName = (tab as HTMLElement).dataset.tab;
                const content = this.querySelector('#tab-content');
                if (content) {
                    switch (tabName) {
                        case 'orders':
                            content.innerHTML = this.renderOrders(orders, statusMap);
                            break;
                        case 'suppliers':
                            content.innerHTML = this.renderSuppliers(suppliers, ratingMap);
                            break;
                        case 'requisitions':
                            content.innerHTML = this.renderRequisitions(requisitions);
                            break;
                    }
                }
            });
        });
    }

    renderOrders(orders: any[], statusMap: string[]): string {
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Purchase Orders</h2>
                    <button class="btn btn-primary btn-sm">+ New PO</button>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>PO #</th>
                            <th>Supplier</th>
                            <th>Order Date</th>
                            <th>Expected Delivery</th>
                            <th class="amount">Total</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${orders.map(po => `
                            <tr>
                                <td>${po.poNumber}</td>
                                <td>${po.supplierName}</td>
                                <td>${new Date(po.orderDate).toLocaleDateString()}</td>
                                <td>${po.expectedDeliveryDate ? new Date(po.expectedDeliveryDate).toLocaleDateString() : '-'}</td>
                                <td class="amount">$${this.formatNumber(po.total)}</td>
                                <td><span class="badge ${this.getStatusBadge(po.status)}">${statusMap[po.status]}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderSuppliers(suppliers: any[], ratingMap: string[]): string {
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Suppliers</h2>
                    <button class="btn btn-primary btn-sm">+ Add Supplier</button>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Contact</th>
                            <th>Payment Terms</th>
                            <th>Rating</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${suppliers.map(s => `
                            <tr>
                                <td>${s.code}</td>
                                <td>${s.name}</td>
                                <td>${s.contactName}<br><small style="color: var(--fg-light)">${s.email}</small></td>
                                <td>${s.paymentTerms}</td>
                                <td><span class="badge ${this.getRatingBadge(s.rating)}">${ratingMap[s.rating]}</span></td>
                                <td><span class="badge ${s.isActive ? 'success' : 'danger'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderRequisitions(requisitions: any[]): string {
        const statusMap = ['', 'Draft', 'Submitted', 'Approved', 'Converted to PO', 'Rejected', 'Cancelled'];
        
        if (requisitions.length === 0) {
            return '<div class="panel"><p>No pending requisitions</p></div>';
        }

        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Pending Requisitions</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Req #</th>
                            <th>Requested By</th>
                            <th>Request Date</th>
                            <th>Needed By</th>
                            <th>Justification</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${requisitions.map(r => `
                            <tr>
                                <td>${r.requisitionNumber}</td>
                                <td>${r.requestedBy}</td>
                                <td>${new Date(r.requestDate).toLocaleDateString()}</td>
                                <td>${new Date(r.neededByDate).toLocaleDateString()}</td>
                                <td>${r.justification || '-'}</td>
                                <td><span class="badge info">${statusMap[r.status]}</span></td>
                                <td>
                                    <button class="btn btn-sm btn-primary">Approve</button>
                                    <button class="btn btn-sm btn-secondary">Convert to PO</button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    getStatusBadge(status: number): string {
        switch (status) {
            case 6: return 'success'; // Received
            case 3: case 4: return 'info'; // Approved, Sent
            case 2: case 5: return 'warning'; // Pending, Partially Received
            case 7: return 'danger'; // Cancelled
            default: return 'info';
        }
    }

    getRatingBadge(rating: number): string {
        switch (rating) {
            case 1: return 'success';
            case 2: return 'info';
            case 3: return 'warning';
            case 4: case 5: return 'danger';
            default: return 'info';
        }
    }

    formatNumber(value: number): string {
        return value?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
    }
}
customElements.define('procurement-page', ProcurementPage);
