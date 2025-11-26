import { ConduitService } from '../services/ConduitService';

export class TreasuryPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>Treasury</h1>
                <p class="subtitle">Cash Management & Bank Accounts</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;
        await this.loadData();
    }

    async loadData() {
        try {
            const [bankAccounts, cashPosition, transfers] = await Promise.all([
                ConduitService.getBankAccounts(),
                ConduitService.getCashPosition(),
                ConduitService.getPendingTransfers()
            ]);
            this.render(bankAccounts as any[], cashPosition as any, transfers as any[]);
        } catch (error) {
            console.error('Failed to load Treasury data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>Treasury</h1></div>
                <div class="panel"><p>Failed to load data.</p></div>
            `;
        }
    }

    render(bankAccounts: any[], cashPosition: any, transfers: any[]) {
        const accountTypes = ['', 'Checking', 'Savings', 'Money Market', 'Investment'];
        
        this.innerHTML = `
            <div class="page-header">
                <h1>Treasury</h1>
                <p class="subtitle">Cash Management & Bank Accounts</p>
            </div>

            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Total Cash</h3>
                    <p class="value positive">$${this.formatNumber(cashPosition?.totalCash ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Available</h3>
                    <p class="value">$${this.formatNumber(cashPosition?.totalAvailable ?? 0)}</p>
                </div>
                <div class="widget">
                    <h3>Pending Transfers</h3>
                    <p class="value">${transfers.length}</p>
                </div>
                <div class="widget">
                    <h3>Bank Accounts</h3>
                    <p class="value">${bankAccounts.length}</p>
                </div>
            </div>

            <div class="panel">
                <div class="panel-header">
                    <h2>Bank Accounts</h2>
                    <button class="btn btn-primary btn-sm">+ New Transfer</button>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Account</th>
                            <th>Bank</th>
                            <th>Type</th>
                            <th>Currency</th>
                            <th class="amount">Current Balance</th>
                            <th class="amount">Available</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${bankAccounts.map(acc => `
                            <tr>
                                <td>${acc.accountName}<br><small style="color: var(--fg-light)">${acc.accountNumber}</small></td>
                                <td>${acc.bankName}</td>
                                <td>${accountTypes[acc.accountType] || acc.accountType}</td>
                                <td>${acc.currency}</td>
                                <td class="amount positive">$${this.formatNumber(acc.currentBalance)}</td>
                                <td class="amount">$${this.formatNumber(acc.availableBalance)}</td>
                                <td><span class="badge ${acc.isActive ? 'success' : 'danger'}">${acc.isActive ? 'Active' : 'Inactive'}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>

            ${this.renderCurrencyPositions(cashPosition)}

            ${transfers.length > 0 ? this.renderPendingTransfers(transfers) : ''}
        `;
    }

    renderCurrencyPositions(cashPosition: any): string {
        if (!cashPosition?.byCurrency?.length) return '';
        
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Position by Currency</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Currency</th>
                            <th class="amount">Total Balance</th>
                            <th class="amount">Available</th>
                            <th>Accounts</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${cashPosition.byCurrency.map((pos: any) => `
                            <tr>
                                <td><strong>${pos.currency}</strong></td>
                                <td class="amount">$${this.formatNumber(pos.totalBalance)}</td>
                                <td class="amount">$${this.formatNumber(pos.availableBalance)}</td>
                                <td>${pos.accountCount}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderPendingTransfers(transfers: any[]): string {
        const statusMap = ['', 'Pending', 'Approved', 'Completed', 'Cancelled'];
        
        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Pending Transfers</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Transfer #</th>
                            <th>From</th>
                            <th>To</th>
                            <th class="amount">Amount</th>
                            <th>Requested</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${transfers.map(t => `
                            <tr>
                                <td>${t.transferNumber}</td>
                                <td>${t.fromAccountName}</td>
                                <td>${t.toAccountName}</td>
                                <td class="amount">${t.currency} ${this.formatNumber(t.amount)}</td>
                                <td>${new Date(t.requestedAt).toLocaleDateString()}</td>
                                <td><span class="badge warning">${statusMap[t.status]}</span></td>
                                <td>
                                    <button class="btn btn-sm btn-primary">Approve</button>
                                    <button class="btn btn-sm btn-secondary">Reject</button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    formatNumber(value: number): string {
        return value?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
    }
}
customElements.define('treasury-page', TreasuryPage);
