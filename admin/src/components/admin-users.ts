import { ConduitContext, DbSet, FilterOperator } from 'conduit-ts-client';

// Define the Entity matching the C# DTO
interface User {
    Id: number;
    Username: string;
    Name: string;
    Email: string;
    Roles: string[];
    LastLogin: string;
}

export class AdminUsers extends HTMLElement {
    private context: ConduitContext;
    private users: DbSet<User>;

    constructor() {
        super();
        // Initialize the Fluent Client
        // In a real app, the transport would be the WebSocket client
        this.context = new ConduitContext(async (query) => {
            console.log('Sending Fluent Query:', JSON.stringify(query, null, 2));
            
            // Mock Data for Demo
            let data = [
                { Id: 1, Username: 'admin', Name: 'Alice Admin', Email: 'alice@conduit.net', Roles: ['Admin'], LastLogin: '2023-11-01' },
                { Id: 2, Username: 'bob', Name: 'Bob User', Email: 'bob@conduit.net', Roles: ['User'], LastLogin: '2023-11-05' },
                { Id: 3, Username: 'charlie', Name: 'Charlie Mod', Email: 'charlie@conduit.net', Roles: ['Moderator'], LastLogin: '2023-10-20' }
            ];

            // Apply Mock Filtering (Client-side simulation of Server logic)
            if (query.filters && query.filters.length > 0) {
                for (const filter of query.filters) {
                    if (filter.operator === FilterOperator.Contains) {
                        const val = filter.value.toString().toLowerCase();
                        data = data.filter(u => (u as any)[filter.fieldName].toString().toLowerCase().includes(val));
                    }
                    // Add other operators if needed for demo
                }
            }

            return data;
        });
        
        this.users = this.context.set<User>('Users');
    }

    connectedCallback() {
        this.render();
        this.loadUsers();
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>User Administration</h1>
                <div class="controls">
                    <input type="text" id="search-input" placeholder="Search users..." />
                    <button class="btn-primary" id="search-btn">Search</button>
                </div>
            </div>
            
            <div class="dashboard-panel">
                <table class="table" id="users-list-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Username</th>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Roles</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td colspan="6">Loading...</td></tr>
                    </tbody>
                </table>
            </div>
        `;
        
        this.querySelector('#search-btn')?.addEventListener('click', () => {
            const term = (this.querySelector('#search-input') as HTMLInputElement).value;
            this.loadUsers(term);
        });
    }

    async loadUsers(searchTerm: string = '') {
        const tbody = this.querySelector('tbody');
        if (!tbody) return;
        tbody.innerHTML = '<tr><td colspan="6">Loading...</td></tr>';

        try {
            // Build the query using the Fluent API
            let query = this.users.take(20).orderByDescending(u => u.LastLogin);

            if (searchTerm) {
                query = query.where(u => u.Name.contains(searchTerm));
            }

            const users = await query.toListAsync();
            this.renderUsers(users);
        } catch (err) {
            console.error(err);
            tbody.innerHTML = '<tr><td colspan="6" class="error">Failed to load users</td></tr>';
        }
    }

    renderUsers(users: User[]) {
        const tbody = this.querySelector('tbody');
        if (!tbody) return;

        if (users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6">No users found</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(user => `
            <tr>
                <td>${user.Id}</td>
                <td>${user.Username}</td>
                <td>${user.Name}</td>
                <td>${user.Email || '-'}</td>
                <td>${user.Roles.join(', ')}</td>
                <td>
                    <button class="btn-sm" onclick="alert('Edit ${user.Id}')">Edit</button>
                </td>
            </tr>
        `).join('');
    }
}

customElements.define('admin-users', AdminUsers);
