import { ConduitService } from '../services/ConduitService';
import { IAclService } from '../contracts';

export class AdminRoles extends HTMLElement {
    private aclService: IAclService | null = null;

    connectedCallback() {
        this.render();
        this.initializeClient();
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>Role Administration</h1>
                <button class="btn-primary" id="add-role-btn">Create Role</button>
            </div>
            
            <div class="dashboard-panel">
                <div class="role-manager">
                    <div class="role-list">
                        <h3>Roles</h3>
                        <ul id="roles-ul">
                            <li>Loading...</li>
                        </ul>
                    </div>
                    <div class="role-details">
                        <h3>Permissions</h3>
                        <div id="permissions-content">Select a role to view permissions</div>
                    </div>
                </div>
            </div>
        `;
        
        this.querySelector('#add-role-btn')?.addEventListener('click', () => this.createRole());
    }

    async initializeClient() {
        try {
            this.aclService = await ConduitService.getAclService();
            // Mock loading roles since IAclService doesn't have GetAllRoles yet (based on contract)
            // We might need to update the contract or just show a manual input for now.
            this.renderRoles(['Admin', 'User', 'Guest']); 
        } catch (err) {
            console.error(err);
            this.querySelector('#roles-ul')!.innerHTML = '<li>Failed to connect to ACL Service</li>';
        }
    }

    renderRoles(roles: string[]) {
        const ul = this.querySelector('#roles-ul');
        if (!ul) return;
        
        ul.innerHTML = roles.map(role => `
            <li><a href="#" data-role="${role}">${role}</a></li>
        `).join('');

        ul.querySelectorAll('a').forEach(a => {
            a.addEventListener('click', (e) => {
                e.preventDefault();
                this.loadPermissions((e.target as HTMLElement).dataset.role!);
            });
        });
    }

    async loadPermissions(role: string) {
        const container = this.querySelector('#permissions-content');
        if (!container || !this.aclService) return;

        container.innerHTML = 'Loading...';
        try {
            const perms = await this.aclService.GetRolePermissionsAsync(role);
            if (perms.length === 0) {
                container.innerHTML = '<p>No permissions assigned.</p>';
            } else {
                container.innerHTML = `
                    <ul>
                        ${perms.map(p => `<li>${p}</li>`).join('')}
                    </ul>
                `;
            }
        } catch (err) {
            container.innerHTML = `<p class="error">Failed to load permissions: ${err}</p>`;
        }
    }

    async createRole() {
        const role = prompt('Enter new role name:');
        if (role && this.aclService) {
            try {
                await this.aclService.CreateRoleAsync(role);
                alert('Role created!');
                // Refresh list...
            } catch (err) {
                alert('Failed to create role: ' + err);
            }
        }
    }
}

customElements.define('admin-roles', AdminRoles);
