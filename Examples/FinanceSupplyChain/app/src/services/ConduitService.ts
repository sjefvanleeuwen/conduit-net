import { ConduitClient } from 'conduit-ts-client';

class ConduitServiceClass {
    private client: ConduitClient | null = null;
    private directoryUrl = 'ws://localhost:5000/conduit';
    private connectionStatus: 'disconnected' | 'connecting' | 'connected' = 'disconnected';
    private statusListeners: ((status: string) => void)[] = [];

    async connect(): Promise<void> {
        if (this.client) return;
        
        this.setStatus('connecting');
        try {
            this.client = new ConduitClient(this.directoryUrl);
            await this.client.connect();
            this.setStatus('connected');
            console.log('[ConduitService] Connected to directory');
        } catch (error) {
            this.setStatus('disconnected');
            console.error('[ConduitService] Connection failed:', error);
            throw error;
        }
    }

    private setStatus(status: 'disconnected' | 'connecting' | 'connected') {
        this.connectionStatus = status;
        this.statusListeners.forEach(listener => listener(status));
    }

    onStatusChange(listener: (status: string) => void): () => void {
        this.statusListeners.push(listener);
        listener(this.connectionStatus);
        return () => {
            this.statusListeners = this.statusListeners.filter(l => l !== listener);
        };
    }

    getStatus(): string {
        return this.connectionStatus;
    }

    async invoke<T>(service: string, method: string, ...args: unknown[]): Promise<T> {
        if (!this.client) {
            await this.connect();
        }
        return this.client!.invoke<T>(service, method, ...args);
    }

    // Finance1 - General Ledger
    async getAccounts() {
        return this.invoke<unknown[]>('IGeneralLedgerService', 'GetAccountsAsync');
    }

    async getTrialBalance(asOfDate: Date) {
        return this.invoke<unknown>('IGeneralLedgerService', 'GetTrialBalanceAsync', asOfDate.toISOString());
    }

    async getJournalEntries(from: Date, to: Date) {
        return this.invoke<unknown[]>('IGeneralLedgerService', 'GetJournalEntriesAsync', from.toISOString(), to.toISOString());
    }

    // Finance1 - Accounts Payable
    async getVendors() {
        return this.invoke<unknown[]>('IAccountsPayableService', 'GetVendorsAsync');
    }

    async getApInvoices() {
        return this.invoke<unknown[]>('IAccountsPayableService', 'GetInvoicesAsync');
    }

    async getPendingApInvoices() {
        return this.invoke<unknown[]>('IAccountsPayableService', 'GetPendingInvoicesAsync');
    }

    async getApAgingSummary() {
        return this.invoke<unknown>('IAccountsPayableService', 'GetAgingSummaryAsync');
    }

    // Finance1 - Accounts Receivable
    async getCustomers() {
        return this.invoke<unknown[]>('IAccountsReceivableService', 'GetCustomersAsync');
    }

    async getArInvoices() {
        return this.invoke<unknown[]>('IAccountsReceivableService', 'GetInvoicesAsync');
    }

    async getOutstandingArInvoices() {
        return this.invoke<unknown[]>('IAccountsReceivableService', 'GetOutstandingInvoicesAsync');
    }

    async getArAgingReport() {
        return this.invoke<unknown>('IAccountsReceivableService', 'GetAgingReportAsync');
    }

    // Finance2 - Treasury
    async getBankAccounts() {
        return this.invoke<unknown[]>('ITreasuryService', 'GetBankAccountsAsync');
    }

    async getCashPosition() {
        return this.invoke<unknown>('ITreasuryService', 'GetCashPositionAsync');
    }

    async getPendingTransfers() {
        return this.invoke<unknown[]>('ITreasuryService', 'GetPendingTransfersAsync');
    }

    // Finance2 - Forecasting
    async getCashFlowForecast(days: number) {
        return this.invoke<unknown>('IForecastingService', 'GetCashFlowForecastAsync', days);
    }

    async getForecastAlerts() {
        return this.invoke<unknown[]>('IForecastingService', 'GetForecastAlertsAsync');
    }

    // Supply Chain - Inventory
    async getInventoryItems() {
        return this.invoke<unknown[]>('IInventoryService', 'GetItemsAsync');
    }

    async getStockLevels() {
        return this.invoke<unknown[]>('IInventoryService', 'GetStockLevelsAsync');
    }

    async getLowStockItems() {
        return this.invoke<unknown[]>('IInventoryService', 'GetLowStockItemsAsync');
    }

    async getStockValuation() {
        return this.invoke<unknown>('IInventoryService', 'GetStockValuationAsync');
    }

    // Supply Chain - Procurement
    async getSuppliers() {
        return this.invoke<unknown[]>('IProcurementService', 'GetSuppliersAsync');
    }

    async getPurchaseOrders() {
        return this.invoke<unknown[]>('IProcurementService', 'GetPurchaseOrdersAsync');
    }

    async getPendingRequisitions() {
        return this.invoke<unknown[]>('IProcurementService', 'GetPendingRequisitionsAsync');
    }
}

export const ConduitService = new ConduitServiceClass();
