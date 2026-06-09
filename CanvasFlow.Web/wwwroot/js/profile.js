document.addEventListener('DOMContentLoaded', function () {

    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/Auth';
        return;
    }

    const feedContent = document.getElementById('feed-content');
    const paginationControls = document.getElementById('pagination-controls');
    const feedTitle = document.getElementById('feed-title');
    const baseUrl = 'http://192.168.88.68:5000';
   
    // Зчитуємо значення з data-атрибута (воно завжди буде рядком, тому перетворюємо в число)
    const currentUserId = parseInt(feedContent.dataset.userId, 10);


    // Navigation Logic
    const navLinks = document.querySelectorAll('.nav-link');
    const feedContainer = document.getElementById('feed-container');
    const placeholderContainer = document.getElementById('generic-placeholder');
    const placeholderTitle = document.getElementById('placeholder-title');

    const publicationsContainer = document.getElementById('publications-container');
    const myPublicationsList = document.getElementById('my-publications-list');
    const btnSubNavs = document.querySelectorAll('.btn-sub-nav');
    const subSections = document.querySelectorAll('.sub-section');
    let currentChatUserId = null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${baseUrl}/chathub`, { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

    connection.start().catch(err => console.error("SignalR Connection Error: ", err));

    connection.on("ReceiveMessage", function (senderId, message) {
        // If the user is currently looking at the chat with the sender, append it
        if (currentChatUserId === senderId) {
            const historyContainer = document.getElementById('chat-history');
            historyContainer.innerHTML += `<div class="msg-bubble msg-received">${message}</div>`;
            historyContainer.scrollTop = historyContainer.scrollHeight;
        } else {
            // Otherwise, reload the inbox to show the unread dot
            loadInbox();
        }
    });

    window.startChat = function (userId, userName) {
        const messagesLink = document.querySelector('[data-section="Messages"]');
        if (messagesLink) {
            messagesLink.click();
            setTimeout(() => {
                openChat(userId, userName);
            }, 100);
        }
    };

    const messagesContainer = document.getElementById('messages-container');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            navLinks.forEach(l => l.classList.remove('active-link'));
            link.classList.add('active-link');
            const section = link.getAttribute('data-section');

            // 1. ПРИХОВУЄМО ВСІ СЕКЦІЇ БЕЗ ВИНЯТКУ
            feedContainer.style.display = 'none';
            placeholderContainer.style.display = 'none';
            publicationsContainer.style.display = 'none';
            messagesContainer.style.display = 'none'; // <-- Додано цей рядок

            // 2. ПОКАЗУЄМО ТІЛЬКИ АКТИВНУ
            if (section === 'Feed') {
                feedContainer.style.display = 'block';
            }
            else if (section === 'Publications') {
                publicationsContainer.style.display = 'block';
                loadMyPublications();
            }
            else if (section === 'Messages') {
                messagesContainer.style.display = 'block'; // Використовуємо змінну
                loadInbox();
            }
            else {
                placeholderContainer.style.display = 'flex';
                placeholderTitle.textContent = section;
            }
        });
    });

    btnSubNavs.forEach(btn => {
        btn.addEventListener('click', () => {
            btnSubNavs.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const target = btn.getAttribute('data-target');

            subSections.forEach(sec => sec.style.display = 'none');
            document.getElementById(target).style.display = 'block';

            if (target === 'manage-pubs') loadMyPublications();
        });
    });

    async function loadInbox() {
        const token = localStorage.getItem('token');
        const inboxList = document.getElementById('inbox-list');

        try {
            const response = await fetch(`${baseUrl}/api/Messaging/inbox`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const data = await response.json();
                inboxList.innerHTML = '';

                if (data.length === 0) {
                    inboxList.innerHTML = '<p class="placeholder-text">No messages yet.</p>';
                    return;
                }

                data.forEach(chat => {
                    const isUnread = chat.hasUnread ? '<span class="unread-dot"></span>' : '';
                    inboxList.innerHTML += `
                        <div class="inbox-item" onclick="openChat(${chat.otherUserId}, '${chat.otherUserName}')">
                            <strong>${chat.otherUserName}</strong> ${isUnread}
                            <div style="font-size: 0.8rem; opacity: 0.6; margin-top: 5px;">Click to view</div>
                        </div>
                    `;
                });
            }
        } catch (err) {
            console.error('Error loading inbox:', err);
        }
    }

    window.openChat = async function (otherUserId, otherUserName) {
        currentChatUserId = otherUserId;
        document.getElementById('active-chat-name').textContent = `Chat with ${otherUserName}`;
        document.getElementById('chat-input-area').style.display = 'flex';

        // Highlight active inbox item visually (optional DOM manipulation)

        const token = localStorage.getItem('token');
        const historyContainer = document.getElementById('chat-history');
        historyContainer.innerHTML = '<div class="text-center">Loading...</div>';

        try {
            const response = await fetch(`${baseUrl}/api/Messaging/history/${otherUserId}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const messages = await response.json();
                historyContainer.innerHTML = '';

                messages.forEach(msg => {
                    const isSentByMe = msg.senderId !== otherUserId; // Adjust logic based on your DTO
                    const bubbleClass = isSentByMe ? 'msg-sent' : 'msg-received';
                    historyContainer.innerHTML += `<div class="msg-bubble ${bubbleClass}">${msg.content}</div>`;
                });

                // Scroll to bottom
                historyContainer.scrollTop = historyContainer.scrollHeight;

                // Refresh inbox to update unread status
                loadInbox();
            }
        } catch (err) {
            console.error('Error loading history:', err);
        }
    };

    // Send Message Logic
    document.getElementById('btn-send-message').addEventListener('click', async () => {
        if (!currentChatUserId) return;

        const inputField = document.getElementById('chat-message-input');
        const content = inputField.value.trim();
        if (!content) return;

        const token = localStorage.getItem('token');

        try {
            const response = await fetch(`${baseUrl}/api/Messaging/chat?otherUserId=${currentChatUserId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ Content: content })
            });

            if (response.ok) {
                // Append immediately to UI
                const historyContainer = document.getElementById('chat-history');
                historyContainer.innerHTML += `<div class="msg-bubble msg-sent">${content}</div>`;
                historyContainer.scrollTop = historyContainer.scrollHeight;
                inputField.value = '';
                loadInbox();
            }
        } catch (err) {
            console.error('Error sending message:', err);
        }
    });

    // Завантаження таблиці публікацій
    async function loadMyPublications() {
        myPublicationsList.innerHTML = '<tr><td colspan="5" class="text-center">Завантаження...</td></tr>';
        // Для отримання токена, якщо ви використовуєте JWT (додайте в headers). Якщо Cookie - fetch передає їх автоматично.
        try {
            const response = await fetch(`${baseUrl}/api/Content/me`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`, // <--- ОСЬ ТУТ МИ ВІДПРАВЛЯЄМО ТОКЕН
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                localStorage.removeItem('token');
                window.location.href = '/Auth';
                return;
            }

            const data = await response.json();

            if (response.ok && Array.isArray(data)) {
                myPublicationsList.innerHTML = '';
                if (data.length === 0) {
                    myPublicationsList.innerHTML = '<tr><td colspan="5" class="text-center">У вас ще немає публікацій.</td></tr>';
                    return;
                }
                data.forEach(item => {
                    myPublicationsList.innerHTML += `
                            <tr>
                                <td><img src="${baseUrl}${item.imageUrl || item.ImageUrl}" alt="thumb"></td>
                                <td>${item.title || item.Title}</td>
                                <td>${new Date(item.uploadDate || item.UploadDate).toLocaleDateString()}</td>
                                <td>${item.likeCount || item.LikeCount}</td>
                                <td>
                                    <button class="action-btn btn-edit" onclick="openEditModal(${item.id || item.Id})">✎ Редагувати</button>
                                    <button class="action-btn btn-delete" onclick="deletePublication(${item.id || item.Id})">🗑 Видалити</button>
                                </td>
                            </tr>
                        `;
                });
            } else {
                myPublicationsList.innerHTML = `<tr><td colspan="5" class="text-center" style="color:var(--error-color)">Помилка: ${data.message || data.error || 'Бекенд ще не повертає масив даних.'}</td></tr>`;
            }
        } catch (error) {
            myPublicationsList.innerHTML = '<tr><td colspan="5" class="text-center" style="color:var(--error-color)">Помилка підключення.</td></tr>';
        }
    }

    // Додавання нової публікації (Create)
    document.getElementById('add-publication-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        const statusDiv = document.getElementById('upload-status');
        statusDiv.textContent = 'Завантаження...';
        statusDiv.className = 'status-message';
        statusDiv.style.color = '#fff';

        const token = localStorage.getItem('token');
        if (!token) {
            statusDiv.textContent = 'Помилка: Ви не авторизовані (відсутній токен).';
            statusDiv.style.color = 'var(--error-color)';
            return;
        }

        const formData = new FormData();
        formData.append('Title', document.getElementById('pub-title').value);
        formData.append('Description', document.getElementById('pub-desc').value);
        formData.append('File', document.getElementById('pub-file').files[0]);

        // Форматування тегів
        const tags = document.getElementById('pub-tags').value;
        if (tags) {
            tags.split(',').forEach(tag => formData.append('Tags', tag.trim()));
        }

        try {
            const response = await fetch(`${baseUrl}/api/Content/upload`, {
                method: 'POST',
                headers: {
                    // 2. ДОДАЄМО ТОКЕН СЮДИ
                    'Authorization': `Bearer ${token}`
                    // ВАЖЛИВО: Не додавайте 'Content-Type': 'multipart/form-data'! 
                    // Браузер зробить це автоматично разом з потрібним boundary.
                },
                body: formData
            });
            const result = await response.json();

            if (response.ok) {
                statusDiv.textContent = 'Успішно опубліковано!';
                statusDiv.style.color = 'var(--accent-color)';
                document.getElementById('add-publication-form').reset();
                // Повертаємось на вкладку управління
                document.querySelector('[data-target="manage-pubs"]').click();
            } else {
                statusDiv.textContent = `Помилка: ${result.error}`;
                statusDiv.style.color = 'var(--error-color)';
            }
        } catch (error) {
            statusDiv.textContent = 'Сталася помилка при завантаженні.';
            statusDiv.style.color = 'var(--error-color)';
        }
    });

    // Видалення (Delete)
    window.deletePublication = async function (id) {
        if (!confirm('Ви впевнені, що хочете видалити цю публікацію?')) return;

        const token = localStorage.getItem('token'); // Отримуємо токен

        try {
            const response = await fetch(`${baseUrl}/api/Content/delete/${id}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}` // Додаємо токен
                }
            });
            if (response.ok) {
                loadMyPublications(); // Оновлюємо таблицю
            } else {
                alert('Помилка видалення. Перевірте консоль.');
            }
        } catch (error) {
            console.error(error);
        }
    }

    // Редагування (Update) - Модальне вікно
    const editModal = document.getElementById('edit-modal');
    document.querySelector('.close-edit-modal').addEventListener('click', () => editModal.style.display = 'none');

    window.openEditModal = async function (id) {
        const token = localStorage.getItem('token');

        try {
            // ВИПРАВЛЕНО: Змінено метод з 'PUT' на 'GET'
            // Ендпоінт має бути таким же, як і для отримання однієї публікації (наприклад, api/Content/get/{id})
            const response = await fetch(`${baseUrl}/api/Content/get/${id}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            const data = await response.json();

            if (response.ok) {
                // Заповнення полів форми
                document.getElementById('edit-pub-id').value = data.id || data.Id;
                document.getElementById('edit-pub-title').value = data.title || data.Title;
                document.getElementById('edit-pub-desc').value = data.description || data.Description;

                const tags = data.tags || data.Tags || [];
                document.getElementById('edit-pub-tags').value = tags.map(t => t.name || t.Name || t).join(', ');

                // Відкриваємо модальне вікно
                document.getElementById('edit-modal').style.display = 'flex';
            } else {
                console.error('Server error:', data);
                alert('Не вдалося завантажити дані публікації.');
            }
        } catch (err) {
            console.error(err);
            alert('Помилка завантаження даних. Перевірте з\'єднання.');
        }
    }

    document.getElementById('edit-publication-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('edit-pub-id').value;
        const statusDiv = document.getElementById('edit-status');
        const token = localStorage.getItem('token');
        statusDiv.textContent = 'Збереження...';

        const payload = {
            Title: document.getElementById('edit-pub-title').value,
            Description: document.getElementById('edit-pub-desc').value,
            //ImageUrl: '', // Зображення зазвичай не оновлюється тут, або потрібна інша логіка для файлів
            Tags: document.getElementById('edit-pub-tags').value.split(',').map(t => t.trim()).filter(t => t)
        };

        try {
            const response = await fetch(`${baseUrl}/api/Content/edit/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },

                body: JSON.stringify(payload)
            });

            if (response.ok) {
                editModal.style.display = 'none';
                loadMyPublications();
            } else {
                const res = await response.json();
                statusDiv.textContent = `Помилка: ${res.error}`;
            }
        } catch (err) { statusDiv.textContent = 'Помилка мережі'; }
    });

    // Fullscreen Modal Logic
    const modal = document.getElementById('media-modal');
    const modalImg = document.getElementById('modal-image');
    const closeBtn = document.querySelector('.close-modal');

    // Event delegation for dynamically loaded images
    feedContent.addEventListener('click', (e) => {
        if (e.target.classList.contains('feed-media-img')) {
            modal.style.display = "flex";
            modalImg.src = e.target.src;
            document.body.style.overflow = 'hidden'; // Prevent background scrolling
        }
    });

    const closeModal = () => {
        modal.style.display = "none";
        document.body.style.overflow = 'auto'; // Restore background scrolling
    };

    closeBtn.addEventListener('click', closeModal);
    modal.addEventListener('click', (e) => {
        if (e.target === modal) closeModal(); // Close if clicked outside the image
    });

    // Feed Loading Logic
    async function loadFeed(page = 1) {
        feedContent.innerHTML = '<div class="loading-spinner">Loading feed...</div>';
        paginationControls.innerHTML = '';
        feedTitle.textContent = 'Community Feed';

        try {
            const response = await fetch(`${baseUrl}/api/Content/feed?page=${page}&limit=20`);
            const data = await response.json();

            if (response.ok) {
                displayFeed(data);
                renderPagination(data.totalPages || 1, page); // Fallback to 1 if missing
            } else {
                feedContent.innerHTML = `<div class="alert alert-error" style="color: var(--error-color);">Error loading feed: ${data.error || 'Unknown error.'}</div>`;
            }
        } catch (error) {
            console.error("Fetch error:", error);
            feedContent.innerHTML = '<div class="alert alert-error" style="color: var(--error-color);">Could not connect to the feed service. Please try again later.</div>';
        }
    }

    function displayFeed(content) {
        let html = '';
        content.forEach(item => {
            // Determine description property (handling different cases)
            const desc = item.Description || item.description || '';
            const title = item.Title || item.title || 'Untitled';
            const uploadDate = item.UploadDate || item.uploadDate;
            const author = (item.User && item.User.Username) || (item.user && item.user.username) || 'Unknown';
            const authorId = item.UserId || item.userId;
            const authorInitial = author.charAt(0).toUpperCase();

            const messageBtn = (authorId !== currentUserId)
                ? `<button class="btn-message" onclick="startChat(${authorId}, '${author}')">💬 Message</button>`
                : '';

            html += `
                    <div class="feed-item">
                        <div class="feed-header">
                            <div class="feed-avatar-placeholder">${authorInitial}</div>
                            <div class="feed-info">
                                <h4 class="feed-title">${title}</h4>
                                <p class="feed-author">By ${author} on ${new Date(uploadDate).toLocaleDateString()}</p>
                            </div>
                        </div>
                        <div class="feed-media">
                            <img src="${baseUrl}/${item.imageUrl || item.ImageUrl}" alt="${title}" class="feed-media-img" loading="lazy">
                        </div>
                        <div class="feed-body">
                            <p>${desc}</p>
                        </div>
                        <div class="feed-actions">
                            <button class="btn-like" data-content-id="${item.Id || item.id}">❤️ Like (${item.LikeCount || item.likeCount || 0})</button>
                            ${messageBtn}
                        </div>
                    </div>
                `;
        });
        feedContent.innerHTML = html;
    }

    function renderPagination(totalPages, currentPage) {
        if (totalPages <= 1) return; // Hide if only 1 page

        let paginationHtml = '';
        const maxPagesToShow = 5;
        const startPage = Math.max(1, currentPage - Math.floor(maxPagesToShow / 2));
        const endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);

        paginationHtml += `<button class="pagination-btn" data-page="prev" ${currentPage === 1 ? 'disabled' : ''}>&laquo; Previous</button>`;
        paginationHtml += `<button class="pagination-btn ${currentPage === 1 ? 'active' : ''}" data-page="1" ${currentPage === 1 ? 'disabled' : ''}>1</button>`;

        for (let i = startPage; i <= endPage; i++) {
            if (i !== 1 && i !== totalPages) {
                paginationHtml += `<button class="pagination-btn ${i === currentPage ? 'active' : ''}" data-page="${i}">${i}</button>`;
            }
        }

        if (totalPages > 1) {
            paginationHtml += `<button class="pagination-btn ${currentPage === totalPages ? 'active' : ''}" data-page="${totalPages}" ${currentPage === totalPages ? 'disabled' : ''}>${totalPages}</button>`;
        }

        paginationHtml += `<button class="pagination-btn" data-page="next" ${currentPage === totalPages ? 'disabled' : ''}>Next &raquo;</button>`;

        paginationControls.innerHTML = paginationHtml;
    }

    document.getElementById('btn-logout').addEventListener('click', () => {
        localStorage.removeItem('token');
        window.location.href = '/Auth';
    });

    paginationControls.addEventListener('click', (e) => {
        if (e.target.classList.contains('pagination-btn') && !e.target.disabled) {
            const page = e.target.dataset.page;
            const currentActive = document.querySelector('.pagination-btn.active');
            const currentPage = currentActive ? parseInt(currentActive.dataset.page) : 1;

            if (page === 'prev') {
                loadFeed(currentPage - 1);
            } else if (page === 'next') {
                loadFeed(currentPage + 1);
            } else {
                loadFeed(parseInt(page));
            }
        }
    });

    // Initialize
    loadFeed(1);
});