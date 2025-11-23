import { ConduitContext, FilterOperator } from 'conduit-ts-client';
import './query-builder';
import { QueryBuilder } from './query-builder';

export class AdminQuery extends HTMLElement {
    private context: ConduitContext;

    constructor() {
        super();
        this.context = new ConduitContext(async (query) => {
            console.log('Executing Ad-Hoc Query:', query);
            // Mock Data
            let data = [
                { Id: 1, Username: 'admin', Name: 'Alice Admin', Email: 'alice@conduit.net', Roles: ['Admin'], LastLogin: '2023-11-01' },
                { Id: 2, Username: 'bob', Name: 'Bob User', Email: 'bob@conduit.net', Roles: ['User'], LastLogin: '2023-11-05' },
                { Id: 3, Username: 'charlie', Name: 'Charlie Mod', Email: 'charlie@conduit.net', Roles: ['Moderator'], LastLogin: '2023-10-20' },
                { Id: 4, Username: 'dave', Name: 'Dave Dev', Email: 'dave@conduit.net', Roles: ['User'], LastLogin: '2023-11-10' }
            ];

            // Apply Mock Filtering
            if (query.filters && query.filters.length > 0) {
                for (const filter of query.filters) {
                    const val = filter.value.toString().toLowerCase();
                    if (filter.operator === FilterOperator.Contains) {
                        data = data.filter(u => (u as any)[filter.fieldName].toString().toLowerCase().includes(val));
                    } else if (filter.operator === FilterOperator.Eq) {
                        data = data.filter(u => (u as any)[filter.fieldName].toString().toLowerCase() === val);
                    }
                }
            }
            
            // Apply Mock Sorting
            if (query.sorts && query.sorts.length > 0) {
                const sort = query.sorts[0];
                data.sort((a, b) => {
                    const valA = (a as any)[sort.fieldName];
                    const valB = (b as any)[sort.fieldName];
                    if (valA < valB) return sort.isDescending ? 1 : -1;
                    if (valA > valB) return sort.isDescending ? -1 : 1;
                    return 0;
                });
            }

            return data;
        });
    }

    connectedCallback() {
        this.render();
        this.setupEvents();
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>Ad-Hoc Query Builder</h1>
            </div>
            
            <div class="dashboard-panel">
                <p>Build complex queries dynamically using the CQP Builder component.</p>
                
                <query-builder id="user-query-builder"></query-builder>
                
                <div class="controls" style="margin-top: 1rem;">
                    <button class="btn-primary" id="run-query-btn">Run Query</button>
                </div>
            </div>

            <div class="dashboard-panel" style="margin-top: 1rem;">
                <h3>Results</h3>
                <div id="query-results">
                    <p>No results yet.</p>
                </div>
            </div>
        `;

        const builder = this.querySelector('#user-query-builder') as QueryBuilder;
        if (builder) {
            builder.fields = ['Id', 'Username', 'Name', 'Email', 'Roles', 'LastLogin'];
        }
    }

    setupEvents() {
        this.querySelector('#run-query-btn')?.addEventListener('click', () => this.runQuery());
    }

    async runQuery() {
        const builder = this.querySelector('#user-query-builder') as QueryBuilder;
        const resultsDiv = this.querySelector('#query-results');
        
        if (!builder || !resultsDiv) return;

        resultsDiv.innerHTML = '<p>Running...</p>';

        try {
            // Get the raw query object from the builder
            const queryDto = builder.value;

            // We can't easily use the Fluent API here because the Fluent API is for compile-time construction.
            // However, we can use the Transport directly since we have the DTO!
            // Or we can hack the DbSet to inject the query.
            
            // Let's use the transport directly for this "Advanced" use case
            // But wait, DbSet doesn't expose transport publicly.
            // Actually, we can just use the context's transport if we exposed it, or create a new context.
            
            // For this demo, we'll just use the mock transport logic we defined in constructor
            // In reality, we'd have a 'client.send(query)' method.
            
            // Let's simulate the execution
            const results = await (this.context as any).transport(queryDto);
            
            this.renderResults(results);

        } catch (err) {
            resultsDiv.innerHTML = `<p class="error">Error: ${err}</p>`;
        }
    }

    renderResults(results: any[]) {
        const resultsDiv = this.querySelector('#query-results');
        if (!resultsDiv) return;

        if (results.length === 0) {
            resultsDiv.innerHTML = '<p>No results found.</p>';
            return;
        }

        // Dynamic Table
        const keys = Object.keys(results[0]);
        
        resultsDiv.innerHTML = `
            <table class="table">
                <thead>
                    <tr>${keys.map(k => `<th>${k}</th>`).join('')}</tr>
                </thead>
                <tbody>
                    ${results.map(row => `
                        <tr>${keys.map(k => `<td>${Array.isArray(row[k]) ? row[k].join(', ') : row[k]}</td>`).join('')}</tr>
                    `).join('')}
                </tbody>
            </table>
        `;
    }
}

customElements.define('admin-query', AdminQuery);
