// adminProfile.js - Script for Admin Profile page
// Fetches user profile data from /api/auth/me endpoint and displays it on the page

document.addEventListener('DOMContentLoaded', function () {
    loadUserProfile();
});

async function loadUserProfile() {
    const profileContainer = document.getElementById('admin-profile-content');
    const loadingElement = document.getElementById('profile-loading');
    const errorElement = document.getElementById('profile-error');
    const baseUrl = 'http://192.168.88.68:5000';

    if (!profileContainer) return;

    // Show loading state
    if (loadingElement) {
        loadingElement.style.display = 'block';
    }
    if (errorElement) {
        errorElement.style.display = 'none';
    }
    profileContainer.innerHTML = '';

    try {
        // Get token from localStorage (same as other admin pages)
        const token = localStorage.getItem('adminToken');

        if (!token) {
            throw new Error('No authentication token found. Please log in again.');
        }

        // Fetch user profile data from /api/auth/me endpoint
        const response = await fetch(`${baseUrl}/api/auth/me`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                // Token expired or invalid, redirect to login
                localStorage.removeItem('adminToken');
                window.location.href = '/Admin/Login';
                return;
            }
            throw new Error(`Failed to load profile: ${response.statusText}`);
        }

        const userData = await response.json();

        // Hide loading element
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }

        // Render the profile card
        renderProfileCard(userData, profileContainer);

    } catch (error) {
        console.error('Error loading admin profile:', error);
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
        if (errorElement) {
            errorElement.textContent = error.message;
            errorElement.style.display = 'block';
        }
    }
}

function renderProfileCard(userData, container) {
    // Create profile card wrapper
    const card = document.createElement('div');
    card.className = 'admin-profile-card';

    // Get status display text and class
    const statusInfo = getStatusDisplay(userData.accountStatus);

    // Build profile HTML
    card.innerHTML = `
        <div class="profile-header">
            <div class="profile-avatar">
                <span class="avatar-icon">${getAvatarIcon(userData.role)}</span>
            </div>
            <div class="profile-title-section">
                <h2 class="profile-username">${escapeHtml(userData.username)}</h2>
                <span class="profile-role-badge ${userData.role === 'Admin' ? 'role-admin' : 'role-user'}">${escapeHtml(userData.role)}</span>
            </div>
        </div>

        <div class="profile-details">
            <div class="detail-row">
                <span class="detail-label">ID</span>
                <span class="detail-value">${userData.id}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">Email</span>
                <span class="detail-value email-value">${escapeHtml(userData.email)}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">Account Status</span>
                <span class="detail-value status-badge ${statusInfo.class}">${statusInfo.text}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">Publications</span>
                <span class="detail-value count-value">${userData.publicationCount}</span>
            </div>
        </div>
    `;

    container.appendChild(card);
}

function getStatusDisplay(status) {
    switch (status) {
        case 'Active':
            return { text: 'Active', class: 'status-active' };
        case 'Blocked':
            return { text: 'Blocked', class: 'status-blocked' };
        case 'Pending':
            return { text: 'Pending', class: 'status-pending' };
        default:
            return { text: status || 'Unknown', class: 'status-unknown' };
    }
}

function getAvatarIcon(role) {
    if (role === 'Admin') {
        return '👑';
    }
    return '👤';
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
