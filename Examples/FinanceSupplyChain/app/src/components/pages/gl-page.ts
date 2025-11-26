import { ConduitService } from '../services/ConduitService';

export class GlPage extends HTMLElement {
    async connectedCallback() {
        this.innerHTML = `
            <div class="page-header">
                <h1>General Ledger</h1>
                <p class="subtitle">Chart of Accounts & Journal Entries</p>
            </div>
            <div class="loading"><div class="spinner"></div><p>Loading...</p></div>
        `;

        await this.loadData();
    }

    async loadData() {
        try {
            const [accounts, trialBalance] = await Promise.all([
                ConduitService.getAccounts(),
                ConduitService.getTrialBalance(new Date())
            ]);
            this.render(accounts as any[], trialBalance as any);
        } catch (error) {
            console.error('Failed to load GL data:', error);
            this.innerHTML = `
                <div class="page-header"><h1>General Ledger</h1></div>
                <div class="panel"><p>Failed to load data. Ensure GeneralLedger service is running.</p></div>
            `;
        }
    }

    render(accounts: any[], trialBalance: any) {
        this.innerHTML = `
            <div class="page-header">
                <h1>General Ledger</h1>
                <p class="subtitle">Chart of Accounts & Journal Entries</p>
            </div>

            <div class="tabs">
                <div class="tab active" data-tab="accounts">Chart of Accounts</div>
                <div class="tab" data-tab="trial-balance">Trial Balance</div>
            </div>

            <div id="tab-content">
                ${this.renderAccounts(accounts)}
            </div>
        `;

        this.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const tabName = (tab as HTMLElement).dataset.tab;
                const content = this.querySelector('#tab-content');
                if (content) {
                    content.innerHTML = tabName === 'accounts' 
                        ? this.renderAccounts(accounts)
                        : this.renderTrialBalance(trialBalance);
                }
            });
        });
    }

    renderAccounts(accounts: any[]): string {
        const accountTypes = ['Asset', 'Liability', 'Equity', 'Revenue', 'Expense'];
        
        return `
            <div class="panel">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Account #</th>
                            <th>Name</th>
                            <th>Type</th>
                            <th class="amount">Balance</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${accounts.map(acc => `
                            <tr>
                                <td>${acc.accountNumber}</td>
                                <td>${acc.name}</td>
                                <td>${accountTypes[acc.type - 1] || acc.type}</td>
                                <td class="amount">$${this.formatNumber(acc.balance)}</td>
                                <td><span class="badge ${acc.isActive ? 'success' : 'danger'}">${acc.isActive ? 'Active' : 'Inactive'}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    }

    renderTrialBalance(tb: any): string {
        if (!tb) return '<div class="panel"><p>No trial balance data</p></div>';

        return `
            <div class="panel">
                <div class="panel-header">
                    <h2>Trial Balance as of ${new Date(tb.asOfDate).toLocaleDateString()}</h2>
                </div>
                <table class="table">
                    <thead>
                        <tr>
                            <th>Account #</th>
                            <th>Account Name</th>
                            <th class="amount">Debit</th>
                            <th class="amount">Credit</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${tb.accounts?.map((acc: any) => `
                            <tr>
                                <td>${acc.accountNumber}</td>
                                <td>${acc.accountName}</td>
                                <td class="amount">${acc.debitBalance > 0 ? '$' + this.formatNumber(acc.debitBalance) : ''}</td>
                                <td class="amount">${acc.creditBalance > 0 ? '$' + this.formatNumber(acc.creditBalance) : ''}</td>
                            </tr>
                        `).join('') || ''}
                        <tr>
                            <td colspan="2"><strong>Totals</strong></td>
                            <td class="amount"><strong>$${this.formatNumber(tb.totalDebits)}</strong></td>
                            <td class="amount"><strong>$${this.formatNumber(tb.totalCredits)}</strong></td>
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
customElements.define('gl-page', GlPage);
