/**
 * Admin Dashboard Logic
 * Handles fetching and interacting with the Admin API
 */

document.addEventListener("DOMContentLoaded", () => {
    const apiBaseUrl = "http://192.168.88.68:5000/api/Admin";
    const token = localStorage.getItem("adminToken") || localStorage.getItem("token");

    // ── Helpers ────────────────────────────────────────────────
    function authHeaders() {
        return {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`
        };
    }

    function showToast(message, type = "success") {
        const existing = document.getElementById("admin-toast");
        if (existing) existing.remove();

        const toast = document.createElement("div");
        toast.id = "admin-toast";
        toast.className = `admin-toast admin-toast--${type}`;
        toast.textContent = message;
        document.body.appendChild(toast);

        requestAnimationFrame(() => toast.classList.add("admin-toast--visible"));
        setTimeout(() => {
            toast.classList.remove("admin-toast--visible");
            setTimeout(() => toast.remove(), 400);
        }, 3200);
    }

    function statusBadge(status) {
        const map = {
            "Active":  { cls: "bg-success", icon: "✓" },
            "Pending": { cls: "bg-warning", icon: "⏳" },
            "Blocked": { cls: "bg-danger",  icon: "✗" },
        };
        const { cls, icon } = map[status] || { cls: "bg-info", icon: "?" };
        return `<span class="badge ${cls}">${icon} ${status}</span>`;
    }

    // ── Tab switching ───────────────────────────────────────────
    window.switchTab = function (tab) {
        document.querySelectorAll(".admin-tab-section").forEach(s => s.classList.remove("active"));
        document.querySelectorAll(".admin-nav-btn").forEach(b => b.classList.remove("active"));

        document.getElementById(`section-${tab}`).classList.add("active");
        document.getElementById(`btn-${tab}`).classList.add("active");

        if (tab === "users")   loadUsers();
        if (tab === "content") loadContentModeration();
        if (tab === "audit")   loadAuditLogs();
    };

    // ═══════════════════════════════════════════════════════════
    //  USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    let allUsers = [];
    let currentFilter = "All";
    let searchQuery = "";

    async function loadUsers() {
        const tbody = document.querySelector("#usersTable tbody");
        tbody.innerHTML = `<tr><td colspan="5" class="admin-table-loading">Loading users…</td></tr>`;

        try {
            const res = await fetch(`${apiBaseUrl}/users`, { headers: authHeaders() });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            allUsers = await res.json();
            updateUserStats();
            renderUsers();
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="5" class="admin-table-empty">⚠ Failed to load users: ${err.message}</td></tr>`;
        }
    }

    function updateUserStats() {
        const total   = allUsers.length;
        const pending = allUsers.filter(u => u.accountStatus === "Pending").length;
        const active  = allUsers.filter(u => u.accountStatus === "Active").length;
        const blocked = allUsers.filter(u => u.accountStatus === "Blocked").length;

        const bar = document.getElementById("user-stats-bar");
        if (!bar) return;

        bar.innerHTML = `
            <button class="user-stat${currentFilter === "All"     ? " active" : ""}" data-filter="All">
                <span class="user-stat__value">${total}</span>
                <span class="user-stat__label">Total</span>
            </button>
            <button class="user-stat user-stat--pending${currentFilter === "Pending" ? " active" : ""}" data-filter="Pending">
                <span class="user-stat__value">${pending}</span>
                <span class="user-stat__label">Pending</span>
            </button>
            <button class="user-stat user-stat--active${currentFilter === "Active"  ? " active" : ""}" data-filter="Active">
                <span class="user-stat__value">${active}</span>
                <span class="user-stat__label">Active</span>
            </button>
            <button class="user-stat user-stat--blocked${currentFilter === "Blocked" ? " active" : ""}" data-filter="Blocked">
                <span class="user-stat__value">${blocked}</span>
                <span class="user-stat__label">Blocked</span>
            </button>`;

        bar.querySelectorAll(".user-stat").forEach(card => {
            card.addEventListener("click", () => {
                currentFilter = card.getAttribute("data-filter");
                updateUserStats();
                renderUsers();
            });
        });
    }

    function renderUsers() {
        const tbody = document.querySelector("#usersTable tbody");
        const q = searchQuery.toLowerCase();

        const rows = allUsers.filter(u => {
            const matchesFilter = currentFilter === "All" || u.accountStatus === currentFilter;
            const matchesSearch = !q ||
                u.username.toLowerCase().includes(q) ||
                u.email.toLowerCase().includes(q) ||
                String(u.id).includes(q);
            return matchesFilter && matchesSearch;
        });

        if (rows.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" class="admin-table-empty">No users found.</td></tr>`;
            return;
        }

        tbody.innerHTML = rows.map(u => {
            const isBlocked = u.accountStatus === "Blocked";
            const isPending = u.accountStatus === "Pending";
            const isActive  = u.accountStatus === "Active";

            const approveBtn = (isPending || isBlocked)
                ? `<button class="btn btn-sm btn-success" onclick="approveUser(${u.id})">${isPending ? "✓ Approve" : "↺ Unblock"}</button>`
                : "";
            const blockBtn = (isActive || isPending)
                ? `<button class="btn btn-sm btn-danger" onclick="blockUser(${u.id})">✗ Block</button>`
                : "";

            return `
            <tr>
                <td><span class="user-id-cell">#${u.id}</span></td>
                <td>
                    <div class="user-cell">
                        <div class="user-cell__avatar">${u.username.charAt(0).toUpperCase()}</div>
                        <div class="user-cell__meta">
                            <span class="user-cell__name">${u.username}</span>
                            <span class="user-cell__email">${u.email}</span>
                        </div>
                    </div>
                </td>
                <td>${statusBadge(u.accountStatus)}</td>
                <td><span class="badge bg-info">${u.role}</span></td>
                <td class="action-cell">
                    ${approveBtn}
                    ${blockBtn}
                </td>
            </tr>`;
        }).join("");
    }

    const searchInput = document.getElementById("user-search");
    if (searchInput) {
        searchInput.addEventListener("input", e => {
            searchQuery = e.target.value.trim();
            renderUsers();
        });
    }

    window.approveUser = async function (userId) {
        try {
            const res = await fetch(`${apiBaseUrl}/user/status?targetUserId=${userId}`, {
                method: "POST",
                headers: authHeaders(),
                body: JSON.stringify(1)  // 1 = Active
            });
            if (!res.ok) throw new Error(await res.text());
            showToast("User approved / unblocked successfully.", "success");
            await loadUsers();
        } catch (err) {
            showToast(`Failed: ${err.message}`, "error");
        }
    };

    window.blockUser = async function (userId) {
        if (!confirm("Block this user? They will no longer be able to log in.")) return;
        try {
            const res = await fetch(`${apiBaseUrl}/user/block?targetUserId=${userId}`, {
                method: "POST",
                headers: authHeaders()
            });
            if (!res.ok) throw new Error(await res.text());
            showToast("User blocked.", "error");
            await loadUsers();
        } catch (err) {
            showToast(`Failed: ${err.message}`, "error");
        }
    };

    // ═══════════════════════════════════════════════════════════
    //  CONTENT MODERATION
    // ═══════════════════════════════════════════════════════════
    async function loadContentModeration() {
        const tbody = document.querySelector("#contentTable tbody");
        tbody.innerHTML = `<tr><td colspan="4" class="admin-table-loading">Loading content…</td></tr>`;
        try {
            const res = await fetch("http://192.168.88.68:5000/api/Content/pending");
            if (!res.ok) throw new Error("Failed to load content");
            const contents = await res.json();

            if (!contents.length) {
                tbody.innerHTML = `<tr><td colspan="4" class="admin-table-empty">No pending content.</td></tr>`;
                return;
            }

            tbody.innerHTML = contents.map(c => `
                <tr>
                    <td><span class="user-id-cell">#${c.id}</span></td>
                    <td>${c.title}</td>
                    <td>${statusBadge(c.status || "Pending")}</td>
                    <td class="action-cell">
                        <button class="btn btn-sm btn-success" onclick="publishContent(${c.id}, true)">✓ Publish</button>
                        <button class="btn btn-sm btn-danger"  onclick="publishContent(${c.id}, false)">✗ Reject</button>
                    </td>
                </tr>`).join("");
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="4" class="admin-table-empty">No pending content found.</td></tr>`;
        }
    }

    window.publishContent = async function (contentId, isPublished) {
        try {
            const res = await fetch(`${apiBaseUrl}/content/publish?contentId=${contentId}`, {
                method: "POST",
                headers: authHeaders(),
                body: JSON.stringify(isPublished)
            });
            if (!res.ok) throw new Error("Failed to update content status");
            showToast(isPublished ? "Content published!" : "Content rejected.", isPublished ? "success" : "error");
            loadContentModeration();
        } catch (err) {
            showToast(err.message, "error");
        }
    };

    // ═══════════════════════════════════════════════════════════
    //  AUDIT LOGS
    // ═══════════════════════════════════════════════════════════
    async function loadAuditLogs() {
        const tbody = document.querySelector("#auditTable tbody");
        tbody.innerHTML = `<tr><td colspan="3" class="admin-table-loading">Loading logs…</td></tr>`;
        try {
            const res = await fetch(`${apiBaseUrl}/audit/logs?page=1&limit=50`, { headers: authHeaders() });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const logs = await res.json();

            if (!logs.length) {
                tbody.innerHTML = `<tr><td colspan="3" class="admin-table-empty">No audit logs yet.</td></tr>`;
                return;
            }

            tbody.innerHTML = logs.map(log => `
                <tr>
                    <td style="white-space:nowrap">${new Date(log.timestamp).toLocaleString()}</td>
                    <td>${log.adminUserId}</td>
                    <td>${log.details || log.action || "—"}</td>
                </tr>`).join("");
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="3" class="admin-table-empty">Error loading logs.</td></tr>`;
        }
    }

    // Bootstrap
    loadUsers();
});
