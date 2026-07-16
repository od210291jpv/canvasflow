document.addEventListener('DOMContentLoaded', function () {

    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/Auth';
        return;
    }

    const feedContent = document.getElementById('feed-content');
    const feedTitle = document.getElementById('feed-title');
    const baseUrl = 'http://192.168.88.68:5000';
   
    // Зчитуємо значення з data-атрибута з унікального елемента user-metadata
    const userMetadata = document.getElementById('user-metadata');
    const currentUserId = userMetadata ? parseInt(userMetadata.dataset.userId, 10) : 0;


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

    // Tag Filter Elements
    const tagChipsContainer = document.getElementById('tag-chips');

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
            else if (section == "Management")
            {
                // do nothing here
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

    function getSafeImageUrl(item) {
        const rawImageUrl = item.imageUrl || item.ImageUrl || '';
        if (!rawImageUrl) return ''; // Якщо картинки немає взагалі

        if (rawImageUrl.startsWith('http')) {
            return rawImageUrl; // Вже повне посилання
        } else {
            // Відносне посилання (проксі) - безпечно додаємо baseUrl
            const cleanBaseUrl = typeof baseUrl !== 'undefined' ? baseUrl.replace(/\/$/, '') : '';
            const cleanRawUrl = rawImageUrl.replace(/^\//, '');
            return `${cleanBaseUrl}/${cleanRawUrl}`;
        }
    }

    // Завантаження таблиці публікацій
    async function loadMyPublications() {
        const list = document.getElementById('my-publications-list');
        list.innerHTML = '<tr><td colspan="5" style="text-align:center;">Завантаження...</td></tr>';

        try {
            const response = await fetch(`${baseUrl}/api/Content/me`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`, // Передаємо токен
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                list.innerHTML = '<tr><td colspan="5" class="error">Помилка 401: Ви не авторизовані. Перевірте токен.</td></tr>';
                return;
            }

            const data = await response.json();

            if (response.ok && Array.isArray(data)) {
                list.innerHTML = '';
                if (data.length === 0) {
                    list.innerHTML = '<tr><td colspan="5" style="text-align:center;">У вас ще немає публікацій.</td></tr>';
                    return;
                }

                data.forEach(item => {
                    const finalImageUrl = getSafeImageUrl(item);
                    list.innerHTML += `
                            <tr>
                                <td><img src="${finalImageUrl}" alt="thumb"></td>
                                <td>${item.Title || item.title}</td>
                                <td>${new Date(item.UploadDate || item.uploadDate).toLocaleDateString()}</td>
                                <td>${item.LikeCount || item.likeCount || 0}</td>
                                <td>
                                     <button class="action-btn btn-edit" onclick="openEditModal(${item.id || item.Id})">✎ Редагувати</button>
                                    <button class="action-btn btn-delete" onclick="deletePublication(${item.id || item.Id})">🗑 Видалити</button>
                                </td>
                            </tr>
                        `;
                });
            } else {
                list.innerHTML = `<tr><td colspan="5" class="error">${data.error || 'Помилка завантаження даних.'}</td></tr>`;
            }
        } catch (error) {
            list.innerHTML = '<tr><td colspan="5" class="error">Помилка підключення.</td></tr>';
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
                    'Authorization': `Bearer ${token}`
                    // ВАЖЛИВО: Не додавайте 'Content-Type': 'multipart/form-data'!
                    // Браузер зробить це автоматично разом з потрібним boundary.
                },
                body: formData
            });
            const result = await response.json();

            if (response.ok) {
                // Reset form and show success message
                document.getElementById('add-publication-form').reset();
                statusDiv.textContent = '✅ Публікацію успішно завантажено!';
                statusDiv.style.color = 'var(--accent-color, #4ade80)';

                // Refresh the feed so the new post appears immediately
                loadFeed(1);
                // Refresh the publications list if it is visible
                loadMyPublications();
            } else {
                statusDiv.textContent = `❌ Помилка: ${result.error || 'Невідома помилка.'}`;
                statusDiv.style.color = 'var(--error-color, #f87171)';
            }
        } catch (error) {
            console.error('Error uploading content:', error);
            statusDiv.textContent = '❌ Помилка мережі. Спробуйте ще раз.';
            statusDiv.style.color = 'var(--error-color, #f87171)';
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

    // Feed Loading Logic for Infinite Scroll
    let feedPage = 1;
    let feedTotalPages = 1;
    let feedLoading = false;
    let currentTags = [];
    let sentinel = null;

    async function loadFeed(page = 1, tags = [], append = false) {
        if (feedLoading) return;
        feedLoading = true;
        feedPage = page;
        currentTags = tags;

        if (!append) {
            // Show skeleton loader on fresh load
            feedContent.innerHTML = `
                <div class="skeleton-feed">
                    ${[1,2,3].map(() => `
                    <div class="skeleton-item">
                        <div class="skeleton-header">
                            <div class="skeleton-avatar"></div>
                            <div class="skeleton-meta">
                                <div class="skeleton-line w-60"></div>
                                <div class="skeleton-line w-40"></div>
                            </div>
                        </div>
                        <div class="skeleton-image"></div>
                        <div class="skeleton-line w-80"></div>
                        <div class="skeleton-line w-full" style="margin-top:8px;"></div>
                    </div>`).join('')}
                </div>`;
            feedTitle.textContent = 'Community Feed';
        } else {
            // Show inline loader at the bottom of the feed when appending
            let loader = document.getElementById('feed-infinite-loader');
            if (!loader) {
                loader = document.createElement('div');
                loader.id = 'feed-infinite-loader';
                loader.className = 'skeleton-line w-40';
                loader.style.margin = '20px auto';
                loader.style.height = '8px';
                feedContent.appendChild(loader);
            }
        }

        let tagQuery = '';
        if (tags.length > 0) {
            tagQuery = `&tags=${tags.join(',')}`;
        }

        try {
            const response = await fetch(`${baseUrl}/api/Content/feed?page=${page}&limit=10${tagQuery}`);
            const data = await response.json();

            // Remove the inline loader if present
            const loader = document.getElementById('feed-infinite-loader');
            if (loader) loader.remove();

            if (response.ok) {
                const items = data.items ?? data;
                feedTotalPages = data.totalPages || 1;

                displayFeed(items, append);
                
                // Rebuild tag chips only on first load
                if (!append) {
                    renderTagsFromFeed(items, tags.length > 0 ? tags[0] : '');
                }

                setupScrollObserver();
            } else {
                if (!append) {
                    feedContent.innerHTML = `<div class="feed-error-state">⚠️ Error loading feed: ${data.error || 'Unknown error.'}</div>`;
                }
            }
        } catch (error) {
            console.error("Fetch error:", error);
            const loader = document.getElementById('feed-infinite-loader');
            if (loader) loader.remove();
            if (!append) {
                feedContent.innerHTML = '<div class="feed-error-state">⚠️ Could not connect to the feed service. Please try again later.</div>';
            }
        } finally {
            feedLoading = false;
        }
    }

    function displayFeed(content, append = false) {
        if (!content || content.length === 0) {
            if (!append) {
                feedContent.innerHTML = `
                    <div class="feed-empty-state">
                        <div class="feed-empty-icon">🌌</div>
                        <p>No posts here yet. Be the first to share something!</p>
                    </div>`;
            }
            return;
        }

        let html = '';
        content.forEach(item => {
            const desc = item.Description || item.description || '';
            const title = item.Title || item.title || 'Untitled';
            const uploadDate = item.UploadDate || item.uploadDate;
            const author = (item.User && item.User.Username) || (item.user && item.user.username) || 'Unknown';
            const authorId = item.UserId || item.userId;
            const authorInitial = author.charAt(0).toUpperCase();
            const likeCount = item.LikeCount || item.likeCount || 0;
            const contentId = item.Id || item.id;

            const finalImageUrl = getSafeImageUrl(item);
            const imageHtml = finalImageUrl
                ? `<div class="feed-media-wrapper">
                       <img src="${finalImageUrl}" alt="${title}" class="feed-media-img" loading="lazy">
                   </div>`
                : '';

            // Build in-card tag chips
            const rawTags = item.Tags || item.tags || [];
            const tagChipsHtml = rawTags.length > 0
                ? `<div class="feed-item-tags">${rawTags.map(t => {
                       const name = typeof t === 'string' ? t : (t.Name || t.name || '');
                       return name ? `<span class="feed-tag-chip">#${name}</span>` : '';
                   }).join('')}</div>`
                : '';

            const messageBtnHtml = (authorId !== currentUserId)
                ? `<button class="btn-message" onclick="startChat(${authorId}, '${author}')">
                       <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                       Message
                   </button>`
                : '';

            const dateStr = uploadDate ? new Date(uploadDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '';

            html += `
            <div class="feed-item">
                <div class="feed-item-header">
                    <div class="feed-author-block">
                        <div class="feed-avatar">${authorInitial}</div>
                        <div class="feed-author-meta">
                            <h4>${author}</h4>
                            <p class="feed-author-date">${dateStr}</p>
                        </div>
                    </div>
                    <button class="feed-item-more" title="More options">···</button>
                </div>
                ${imageHtml}
                <div class="feed-item-body">
                    <p class="feed-item-title">${title}</p>
                    ${desc ? `<p class="feed-item-desc">${desc}</p>` : ''}
                </div>
                ${tagChipsHtml}
                <div class="feed-actions">
                    <button class="btn-like" data-content-id="${contentId}">
                        <span class="like-icon">♥</span>
                        Like &nbsp;<span class="like-count">${likeCount}</span>
                    </button>
                    ${messageBtnHtml}
                    <div class="feed-actions-spacer"></div>
                </div>
            </div>`;
        });

        if (append) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = html;
            while (tempDiv.firstChild) {
                feedContent.appendChild(tempDiv.firstChild);
            }
        } else {
            feedContent.innerHTML = html;
        }
    }

    function setupScrollObserver() {
        // Remove existing sentinel if present
        if (sentinel) {
            sentinel.remove();
        }

        // Only create observer if there are more pages left
        if (feedPage >= feedTotalPages) return;

        sentinel = document.createElement('div');
        sentinel.id = 'feed-scroll-sentinel';
        sentinel.style.height = '10px';
        feedContent.appendChild(sentinel);

        const observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting && !feedLoading) {
                observer.disconnect();
                loadFeed(feedPage + 1, currentTags, true);
            }
        }, {
            rootMargin: '200px'
        });

        observer.observe(sentinel);
    }

    // Tag Filter Logic — chips derived from the current feed response
    function renderTagsFromFeed(feedItems, activeTag) {
        if (!tagChipsContainer) return;

        // Collect unique tag names that appear in the current page
        const seen = new Set();
        feedItems.forEach(item => {
            const rawTags = item.Tags || item.tags || [];
            rawTags.forEach(t => {
                const name = typeof t === 'string' ? t : (t.Name || t.name || '');
                if (name) seen.add(name);
            });
        });

        tagChipsContainer.innerHTML = '';

        // "All" chip — active when no tag filter is applied
        const allBtn = document.createElement('button');
        allBtn.className = 'tag-chip' + (activeTag === '' ? ' active' : '');
        allBtn.textContent = 'All';
        allBtn.setAttribute('data-tag', '');
        tagChipsContainer.appendChild(allBtn);

        // One chip per unique tag found in this feed page
        [...seen].sort().forEach(tagName => {
            const btn = document.createElement('button');
            btn.className = 'tag-chip' + (tagName === activeTag ? ' active' : '');
            btn.textContent = tagName;
            btn.setAttribute('data-tag', tagName);
            tagChipsContainer.appendChild(btn);
        });
    }

    // Initialize Feed (tags are built from the first feed response)
    loadFeed(1);

    // Handle chip clicks
    if (tagChipsContainer) {
        tagChipsContainer.addEventListener('click', (e) => {
            if (e.target.classList.contains('tag-chip')) {
                const chip = e.target;
                const tagValue = chip.getAttribute('data-tag');

                // Update UI
                document.querySelectorAll('.tag-chip').forEach(c => c.classList.remove('active'));
                chip.classList.add('active');

                // Fetch new data
                if (tagValue === "" || tagValue === null) {
                    loadFeed(1);
                } else {
                    loadFeed(1, [tagValue]);
                }
            }
        });
    }

    document.getElementById('btn-logout').addEventListener('click', () => {
        localStorage.removeItem('token');
        window.location.href = '/Auth';
    });

    // Edit Profile View/Edit toggles and saving
    const btnEditProfile = document.getElementById('btn-edit-profile');
    const btnCancelProfile = document.getElementById('btn-cancel-profile');
    const btnSaveProfile = document.getElementById('btn-save-profile');
    const profileViewMode = document.getElementById('profile-view-mode');
    const profileEditMode = document.getElementById('profile-edit-mode');
    const editUsernameInput = document.getElementById('edit-username');
    const displayUsername = document.getElementById('display-username');
    const avatarDisplay = document.getElementById('avatar-display');

    if (btnEditProfile) {
        btnEditProfile.addEventListener('click', () => {
            if (profileViewMode) profileViewMode.style.display = 'none';
            if (profileEditMode) profileEditMode.style.display = 'block';
        });
    }

    if (btnCancelProfile) {
        btnCancelProfile.addEventListener('click', () => {
            if (profileEditMode) profileEditMode.style.display = 'none';
            if (profileViewMode) profileViewMode.style.display = 'block';
            if (editUsernameInput && displayUsername) {
                editUsernameInput.value = displayUsername.textContent.trim();
            }
        });
    }

    if (btnSaveProfile) {
        btnSaveProfile.addEventListener('click', async () => {
            const newUsername = editUsernameInput.value.trim();
            if (!newUsername) return;

            btnSaveProfile.disabled = true;
            btnSaveProfile.textContent = 'Saving...';

            try {
                const response = await fetch(`${baseUrl}/api/auth/profile`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    },
                    body: JSON.stringify({ Username: newUsername })
                });

                if (response.ok) {
                    if (displayUsername) displayUsername.textContent = newUsername;
                    if (avatarDisplay) {
                        avatarDisplay.textContent = newUsername.substring(0, 1).toUpperCase();
                    }
                    if (profileEditMode) profileEditMode.style.display = 'none';
                    if (profileViewMode) profileViewMode.style.display = 'block';
                } else {
                    const errorData = await response.json();
                    alert('Failed to update profile: ' + (errorData.error || 'Unknown error'));
                }
            } catch (error) {
                console.error('Error updating profile:', error);
                alert('Network error while updating profile.');
            } finally {
                btnSaveProfile.disabled = false;
                btnSaveProfile.textContent = 'Save';
            }
        });
    }
});