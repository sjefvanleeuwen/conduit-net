import { ConduitService } from '../services/ConduitService';
import { IConduitDirectory, IUserService, NodeInfo, UserDto, mapNodeInfo, mapUserDto } from '../contracts';

export class AdminDashboard extends HTMLElement {
    private directory: IConduitDirectory | null = null;
    private userService: IUserService | null = null;

    connectedCallback() {
        this.render();
        this.initializeClient();
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>Dashboard</h1>
                <div id="connection-status" class="badge">Connecting...</div>
            </div>
            
            <div class="dashboard-widgets">
                <div class="widget">
                    <h3>Active Nodes</h3>
                    <p class="value" id="node-count">-</p>
                </div>
                <div class="widget">
                    <h3>Total Users</h3>
                    <p class="value" id="user-count">-</p>
                </div>
                <div class="widget">
                    <h3>System Status</h3>
                    <p class="value" id="system-status">Checking...</p>
                </div>
            </div>

            <div class="dashboard-panel">
                <h3>Discovered Nodes</h3>
                <table class="table" id="nodes-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Address</th>
                            <th>Services</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td colspan="3">Loading...</td></tr>
                    </tbody>
                </table>
            </div>

            <div class="dashboard-panel" style="margin-top: 20px;">
                <h3>Users</h3>
                <table class="table" id="users-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Username</th>
                            <th>Name</th>
                            <th>Roles</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td colspan="4">Loading...</td></tr>
                    </tbody>
                </table>
            </div>
        `;
    }

    async initializeClient() {
        try {
            this.updateStatus('Connecting...', 'info');
            
            // Use the Service Manager
            this.directory = await ConduitService.getDirectory();
            this.userService = await ConduitService.getUserService();
            
            this.updateStatus('Connected', 'success');
            
            // Fetch Data
            await this.refreshData();

        } catch (err) {
            console.error('Failed to connect:', err);
            this.updateStatus('Connection Failed', 'error');
        }
    }

    updateStatus(text: string, type: 'success' | 'error' | 'info') {
        const el = this.querySelector('#connection-status');
        if (el) {
            el.textContent = text;
            el.className = `badge ${type}`;
        }
    }

    async refreshData() {
        if (!this.directory || !this.userService) return;

        try {
            // 1. Get Nodes (Discover all services by asking for specific ones or just checking known ones)
            // Since IConduitDirectory doesn't have a "GetAllNodes", we might need to discover specific services.
            // Let's try discovering IUserService and IAclService nodes.
            const rawUserNodes = await this.directory.DiscoverAsync('IUserService') as any[];
            const rawAclNodes = await this.directory.DiscoverAsync('IAclService') as any[];
            const rawTelemetryNodes = await this.directory.DiscoverAsync('ITelemetryCollector') as any[];
            
            const userNodes = rawUserNodes.map(mapNodeInfo);
            const aclNodes = rawAclNodes.map(mapNodeInfo);
            const telemetryNodes = rawTelemetryNodes.map(mapNodeInfo);

            // Merge unique nodes
            const allNodes = new Map<string, NodeInfo>();
            [...userNodes, ...aclNodes, ...telemetryNodes].forEach(n => allNodes.set(n.Id, n));
            
            this.renderNodes(Array.from(allNodes.values()));
            this.querySelector('#node-count')!.textContent = allNodes.size.toString();
            this.querySelector('#system-status')!.textContent = 'Online';

            // 2. Get Users
            const rawUsers = await this.userService.GetAllUsersAsync() as any[];
            const users = rawUsers.map(mapUserDto);
            this.renderUsers(users);
            this.querySelector('#user-count')!.textContent = users.length.toString();

        } catch (err) {
            console.error('Error fetching data:', err);
            this.querySelector('#system-status')!.textContent = 'Error';
        }
    }

    renderNodes(nodes: NodeInfo[]) {
        const tbody = this.querySelector('#nodes-table tbody');
        if (!tbody) return;

        if (nodes.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3">No nodes found</td></tr>';
            return;
        }

        tbody.innerHTML = nodes.map(node => `
            <tr>
                <td>${node.Id.substring(0, 8)}...</td>
                <td>${node.Address}</td>
                <td>${node.Services.join(', ')}</td>
            </tr>
        `).join('');
    }

    renderUsers(users: UserDto[]) {
        const tbody = this.querySelector('#users-table tbody');
        if (!tbody) return;

        if (users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4">No users found</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(user => `
            <tr>
                <td>${user.Id}</td>
                <td>${user.Username}</td>
                <td>${user.Name}</td>
                <td>${user.Roles.join(', ')}</td>
            </tr>
        `).join('');
    }
}

customElements.define('admin-dashboard', AdminDashboard);
