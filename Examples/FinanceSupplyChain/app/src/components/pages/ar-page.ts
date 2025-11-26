import { ConduitService } from '../services/ConduitService';

export class ArPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Accounts Receivable</h1>
                <p class="subtitle">Customer Invoices & Collections</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [customers, invoices, aging] = await Promise.all([
                ConduitService.getCustomers(),
                ConduitService.getArInvoices(),
                ConduitService.getArAgingReport()
            ]);
            this.render(customers as any[], invoices as any[], aging as any);
        } catch (error) {
            console.error('Failed to load AR data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Accounts Receivable</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(customers: any[], invoices: any[], aging: any) {
        const statusMap = ['', 'Draft', 'Sent', 'Partially Paid', 'Paid', 'Overdue', 'Cancelled'];
        
        this.innerHTML = `
            <div class="page-header">
                <h1>Accounts Receivable</h1>
                <p class="subtitle">Customer Invoices & Collections</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Total Outstanding</h3>
                    <p class="value positive">$${this.formatNumber(aging?.grandTotal ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Customers</h3>
                    <p class="value">${customers.length}</p>
                </div>
                <div class="widget">
                    <h3>Over 90 Days</h3>
                    <p class="value ${(aging?.totalOver90 ?? 0) > 0 ? 'warning' : ''}">$${this.formatNumber(aging?.totalOver90 ?? 0)}</p>
                </div>
            </div>

            <div class="tabs">
                <div class="tab active" data-tab="invoices">Invoices</div>
                <div class="tab" data-tab="customers">Customers</div>
            </div>

            <div id="tab-content">
                <div class="panel">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Invoice #</th>
                                <th>Customer</th>
                                <th>Date</th>
                                <th>Due Date</th>
                                <th class="amount">Total</th>
                                <th class="amount">Balance</th>
                                <th>Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${invoices.map(inv => `
                                <tr>
                                    <td>${inv.invoiceNumber}</td>
                                    <td>${inv.customerName}</td>
                                    <td>${new Date(inv.invoiceDate).toLocaleDateString()}</td>
                                    <td>${new Date(inv.dueDate).toLocaleDateString()}</td>
                                    <td class="amount">$${this.formatNumber(inv.total)}</td>
                                    <td class="amount">$${this.formatNumber(inv.balance)}</td>
                                    <td><span class="badge ${this.getStatusBadge(inv.status)}">${statusMap[inv.status]}</span></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        this.setupTabs(invoices, customers, statusMap);
    }

    setupTabs(invoices: any[], customers: any[], statusMap: string[]) {
        this.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const tabName = (tab as HTMLElement).dataset.tab;
                const content = this.querySelector('#tab-content');
                if (content) {
                    content.innerHTML = tabName === 'invoices' 
                        ? this.renderInvoices(invoices, statusMap)
                        : this.renderCustomers(customers);
                }
            });
        });
    }

    renderInvoices(invoices: any[], statusMap: string[]): string {
        return `
            <div class="panel">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Invoice #</th>
                            <th>Customer</th>
                            <th>Date</th>
                            <th>Due Date</th>
                            <th class="amount">Total</th>
                            <th class="amount">Balance</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${invoices.map(inv => `
                            <tr>
                                <td>${inv.invoiceNumber}</td>
                                <td>${inv.customerName}</td>
                                <td>${new Date(inv.invoiceDate).toLocaleDateString()}</td>
                                <td>${new Date(inv.dueDate).toLocaleDateString()}</td>
                                <td class="amount">$${this.formatNumber(inv.total)}</td>
                                <td class="amount">$${this.formatNumber(inv.balance)}</td>
                                <td><span class="badge ${this.getStatusBadge(inv.status)}">${statusMap[inv.status]}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderCustomers(customers: any[]): string {
        return `
            <div class="panel">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Contact</th>
                            <th>Payment Terms</th>
                            <th class="amount">Credit Limit</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${customers.map(c => `
                            <tr>
                                <td>${c.customerNumber}</td>
                                <td>${c.name}</td>
                                <td>${c.contactName}</td>
                                <td>${c.paymentTerms}</td>
                                <td class="amount">$${this.formatNumber(c.creditLimit)}</td>
                                <td><span class="badge ${c.isActive ? 'success' : 'danger'}">${c.isActive ? 'Active' : 'Inactive'}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    getStatusBadge(status: number): string {
        switch (status) {
            case 4: return 'success';
            case 2: return 'info';
            case 3: return 'warning';
            case 5: return 'danger';
            default: return 'info';
        }
    }

    formatNumber(value: number): string {
        return value?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
    }
}
customElements.define('ar-page', ArPage);
