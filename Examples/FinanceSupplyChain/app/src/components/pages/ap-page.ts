import { ConduitService } from '../services/ConduitService';

export class ApPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Accounts Payable</h1>
                <p class="subtitle">Vendor Invoices & Payments</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [vendors, invoices, aging] = await Promise.all([
                ConduitService.getVendors(),
                ConduitService.getApInvoices(),
                ConduitService.getApAgingSummary()
            ]);
            this.render(vendors as any[], invoices as any[], aging as any);
        } catch (error) {
            console.error('Failed to load AP data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Accounts Payable</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(vendors: any[], invoices: any[], aging: any) {
        const statusMap = ['', 'Pending', 'Approved', 'Partially Paid', 'Paid', 'Cancelled'];
        
        this.innerHTML = `
            <div class="page-header">
                <h1>Accounts Payable</h1>
                <p class="subtitle">Vendor Invoices & Payments</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Total Outstanding</h3>
                    <p class="value negative">$${this.formatNumber(aging?.totalOutstanding ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Vendors</h3>
                    <p class="value">${vendors.length}</p>
                </div>
                <div class="widget">
                    <h3>Over 90 Days</h3>
                    <p class="value ${(aging?.over90Days ?? 0) > 0 ? 'warning' : ''}">$${this.formatNumber(aging?.over90Days ?? 0)}</p>
                </div>
            </div>

            <div class="tabs">
                <div class="tab active" data-tab="invoices">Invoices</div>
                <div class="tab" data-tab="vendors">Vendors</div>
            </div>

            <div id="tab-content">
                <div class="panel">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Invoice #</th>
                                <th>Vendor</th>
                                <th>Date</th>
                                <th>Due Date</th>
                                <th class="amount">Total</th>
                                <th>Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${invoices.map(inv => `
                                <tr>
                                    <td>${inv.invoiceNumber}</td>
                                    <td>${inv.vendorName}</td>
                                    <td>${new Date(inv.invoiceDate).toLocaleDateString()}</td>
                                    <td>${new Date(inv.dueDate).toLocaleDateString()}</td>
                                    <td class="amount">$${this.formatNumber(inv.total)}</td>
                                    <td><span class="badge ${this.getStatusBadge(inv.status)}">${statusMap[inv.status]}</span></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        this.setupTabs(invoices, vendors, statusMap);
    }

    setupTabs(invoices: any[], vendors: any[], statusMap: string[]) {
        this.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const tabName = (tab as HTMLElement).dataset.tab;
                const content = this.querySelector('#tab-content');
                if (content) {
                    content.innerHTML = tabName === 'invoices' 
                        ? this.renderInvoices(invoices, statusMap)
                        : this.renderVendors(vendors);
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
                            <th>Vendor</th>
                            <th>Date</th>
                            <th>Due Date</th>
                            <th class="amount">Total</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${invoices.map(inv => `
                            <tr>
                                <td>${inv.invoiceNumber}</td>
                                <td>${inv.vendorName}</td>
                                <td>${new Date(inv.invoiceDate).toLocaleDateString()}</td>
                                <td>${new Date(inv.dueDate).toLocaleDateString()}</td>
                                <td class="amount">$${this.formatNumber(inv.total)}</td>
                                <td><span class="badge ${this.getStatusBadge(inv.status)}">${statusMap[inv.status]}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderVendors(vendors: any[]): string {
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
                        ${vendors.map(v => `
                            <tr>
                                <td>${v.vendorNumber}</td>
                                <td>${v.name}</td>
                                <td>${v.contactName}</td>
                                <td>${v.paymentTerms}</td>
                                <td class="amount">$${this.formatNumber(v.creditLimit)}</td>
                                <td><span class="badge ${v.isActive ? 'success' : 'danger'}">${v.isActive ? 'Active' : 'Inactive'}</span></td>
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
customElements.define('ap-page', ApPage);
