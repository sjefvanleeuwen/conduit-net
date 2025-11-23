import { ConduitService } from '../services/ConduitService';
import { IUserService, UserDto, mapUserDto } from '../contracts';

export class AdminUsers extends HTMLElement {
    private userService: IUserService | null = null;

    connectedCallback() {
        this.render();
        this.initializeClient();
    }

    render() {
        this.innerHTML = `
            <div class="dashboard-header">
                <h1>User Administration</h1>
                <button class="btn-primary" id="add-user-btn">Add User</button>
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
        
        this.querySelector('#add-user-btn')?.addEventListener('click', () => this.showAddUserModal());
    }

    async initializeClient() {
        try {
            this.userService = await ConduitService.getUserService();
            this.loadUsers();
        } catch (err) {
            console.error(err);
            this.querySelector('tbody')!.innerHTML = '<tr><td colspan="6" class="error">Failed to connect to User Service</td></tr>';
        }
    }

    async loadUsers() {
        if (!this.userService) return;
        try {
            const rawUsers = await this.userService.GetAllUsersAsync() as any[];
            const users = rawUsers.map(mapUserDto);
            this.renderUsers(users);
        } catch (err) {
            console.error(err);
            this.querySelector('tbody')!.innerHTML = '<tr><td colspan="6" class="error">Failed to load users</td></tr>';
        }
    }

    renderUsers(users: UserDto[]) {
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
                    <button class="btn-sm btn-danger" onclick="alert('Delete ${user.Id}')">Delete</button>
                </td>
            </tr>
        `).join('');
    }

    showAddUserModal() {
        alert('Add User Modal Placeholder');
    }
}

customElements.define('admin-users', AdminUsers);
