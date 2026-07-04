const baseUrl = 'http://192.168.88.68:5000';
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/notificationhub`, {
        accessTokenFactory: () => {
            // Using 'token' as per other JS files in the project
            return localStorage.getItem("token") || "";
        }
    })
    .withAutomaticReconnect()
    .build();

const notifBadge = document.getElementById("notif-badge");
const notifDropdown = document.getElementById("notifications-dropdown");
const notifList = document.getElementById("notifications-list");
const userId = document.getElementById("user-metadata").dataset.userId;

// Function to update the UI with a new notification
function addNotificationToUI(notification) {
    const li = document.createElement("li");
    li.className = "notification-item";
    
    const time = new Date(notification.timestamp).toLocaleTimeString();
    
    li.innerHTML = `
        <span class="time">${time}</span>
        <div>${notification.content}</div>
    `;
    
    // Add to the top of the list
    notifList.prepend(li);

    // Update badge count
    let currentCount = parseInt(notifBadge.innerText) || 0;
    notifBadge.innerText = currentCount + 1;
    notifBadge.classList.remove("hidden");
}

// SignalR Event Handler
connection.on("ReceiveNotification", (notification) => {
    console.log("New notification received:", notification);
    addNotificationToUI(notification);
});

// Connect to Hub
async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        
        // Fetch initial notifications (Inbox Sync)
        fetchInitialNotifications();
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000); // Retry after 5s
    }
}

// Fetch initial notifications from API with Authorization header
async function fetchInitialNotifications() {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${baseUrl}/api/messaging/notifications?userId=${userId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            notifList.innerHTML = ""; // Clear list
            data.forEach(notif => addNotificationToUI(notif));
            
            // Update badge count based on total unread
            const unreadCount = data.filter(n => !n.isRead).length;
            if (unreadCount > 0) {
                notifBadge.innerText = unreadCount;
                notifBadge.classList.remove("hidden");
            } else {
                notifBadge.classList.add("hidden");
            }
        } else {
            console.error("Failed to fetch notifications, status:", response.status);
        }
    } catch (err) {
        console.error("Error fetching initial notifications:", err);
    }
}

// Toggle Dropdown
document.getElementById("notif-bell").addEventListener("click", () => {
    notifDropdown.classList.toggle("hidden");
});

// Close dropdown when clicking outside
document.addEventListener("click", (e) => {
    if (!document.getElementById("notif-bell").contains(e.target) && 
        !notifDropdown.contains(e.target)) {
        notifDropdown.classList.add("hidden");
    }
});

// Start the connection
startConnection();
