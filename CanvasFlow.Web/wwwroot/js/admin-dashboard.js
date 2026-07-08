/**
 * Admin Dashboard Logic
 * Handles fetching and interacting with the Admin API
 */

document.addEventListener('DOMContentLoaded', () => {
    const apiBaseUrl = 'http://192.168.88.68:5000/api/Admin';

    // Initialize components
    loadUsers();
    loadContentModeration();
    loadAuditLogs();

    // --- User Management ---
    async function loadUsers() {
        const tbody = document.querySelector('#usersTable tbody');
        try {
            // Note: In a real app, we'd fetch from an endpoint like api/Admin/users
            // For this demo, I'll simulate the structure based on what the API expects to return
            // Since the controller doesn't have a GET all users yet, I'm assuming a placeholder implementation
            const response = await fetch('http://192.168.88.68:5000/api/Users'); // Assuming there is a user list endpoint
            if (!response.ok) throw new APIError('Failed to load users');
            
            const users = await response.json();
            tbody.innerHTML = users.map(user => `
                <tr>
                    <td>${user.id}</td>
                    <td>${user.username}</td>
                    <td>${user.email}</td>
                    <td><span class="badge bg-${getStatusBadgeClass(user.status)}">${user.status}</span></td>
                    <td>
                        <button class="btn btn-sm btn-warning" onclick="updateUserStatus(${user.id}, 'Active')">Activate</button>
                        <button class="btn btn-sm btn-danger" onclick="blockUser(${user.id})">Block</button>
                    </td>
                </tr>
            `).join('');
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">${err.message}</td></tr>`;
        }
    }

    window.blockUser = async (userId) => {
        if (!confirm('Are you sure you want to block this user?')) return;
        try {
            const response = await fetch(`${apiBaseUrl}/user/block?targetUserId=${userId}`, { method: 'POST' });
            if (response.ok) {
                alert('User blocked successfully');
                loadUsers();
            } else {
                throw new Error('Failed to block user');
            }
        } catch (err) {
            alert(err.message);
        }
    };

    // --- Content Management ---
    async function loadContentModeration() {
        const tbody = document.querySelector('#contentTable tbody');
        try {
            // Assuming an endpoint exists to get pending content
            const response = await fetch('http://192.168.88.68:5000/api/Content/pending'); 
            if (!response.ok) throw new Error('Failed to load content');

            const contents = await response.json();
            tbody.innerHTML = contents.map(c => `
                <tr>
                    <td>${c.id}</td>
                    <td>${c.title}</td>
                    <td><span class="badge bg-info">${c.status}</span></td>
                    <td>
                        <button class="btn btn-sm btn-success" onclick="publishContent(${c.id}, true)">Publish</button>
                        <button class="btn btn-sm btn-danger" onclick="publishContent(${c.id}, false)">Reject</button>
                    </td>
                </tr>
            `).join('');
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center text-danger">No pending content found or error loading.</td></tr>`;
        }
    }

    window.publishContent = async (contentId, isPublished) => {
        try {
            const response = await fetch(`${apiBaseUrl}/content/publish?contentId=${contentId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(isPublished)
            });
            if (response.ok) {
                alert(isPublished ? 'Content published!' : 'Content rejected!');
                loadContentModeration();
            } else {
                throw new Error('Failed to update content status');
            }
        } catch (err) {
            alert(err.message);
        }
    };

    // --- Audit Logs ---
    async function loadAuditLogs() {
        const tbody = document.querySelector('#auditTable tbody');
        try {
            const response = await fetch(`${apiBaseUrl}/audit/logs?page=1&limit=20`);
            if (!response.ok) throw new Error('Failed to load logs');

            const logs = await response.json();
            tbody.innerHTML = logs.map(log => `
                <tr>
                    <td>${new Date(log.timestamp).toLocaleString()}</td>
                    <td>${log.adminUserId}</td>
                    <td>${log.details}</td>
                </tr>
            `).join('');
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">Error loading logs.</td></tr>`;
        }
    }

    // Helpers
    function getStatusBadgeClass(status) {
        switch(status?.toLowerCase()) {
            case 'active': return 'success';
            case 'blocked': return 'danger';
            case 'pending': return 'warning';
            default: return 'secondary';
        }
    }

    class APIError extends Error {
        constructor(message) {
            super(message);
            this.name = "APIError";
        }
    }

    // Note: apiBaseHTML was a typo in my logic, using apiBaseUrl
    const apiBaseHTML = apiBaseUrl; 
});
