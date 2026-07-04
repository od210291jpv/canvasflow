document.addEventListener('DOMContentLoaded', () => {
    const tagChipsContainer = document.getElementById('tag-chips');
    const feedContent = document.getElementById('feed-content');
    let currentTags = [];

    // Function to fetch and render feed
    async function fetchFeed(tags = []) {
        feedContent.innerHTML = '<p class="placeholder-text">Loading content...</p>';
        
        const page = 1;
        const limit = 20;
        let tagQuery = '';
        if (tags.length > 0) {
            tagQuery = `&tags=${tags.join(',')}`;
        }

        try {
            // Note: In a real app, the token would be handled by an interceptor or global config
            const response = await fetch(`/api/content/feed?page=${page}&limit=${limit}${tagQuery}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!response.ok) throw new Error('Failed to fetch feed');

            const data = await response.json();
            renderFeed(data);
        } catch (error) {
            console.error('Error fetching feed:', error);
            feedContent.innerHTML = `<p class="error-text">Error loading feed: ${error.message}</p>`;
        }
    }

    // Function to render the feed items
    function renderFeed(items) {
        if (!items || items.length === 0) {
            feedContent.innerHTML = '<p class="placeholder-text">No content found for selected filters.</p>';
            return;
        }

        feedContent.innerHTML = '';
        items.forEach(item => {
            const div = document.createElement('div');
            div.className = 'feed-item'; 
            div.innerHTML = `
                <h4 class="feed-item-title">${item.Title}</h4>
                <p class="feed-item-description">${item.Description}</p>
                <div class="feed-item-tags">
                    ${item.Tags ? item.Tags.map(t => `<span class="small-tag">${t}</span>`).join('') : ''}
                </div>
            `;
            feedContent.appendChild(div);
        });
    }

    // Initialize tags: For this implementation, we'll assume a set of common tags 
    // or they could be fetched from an endpoint.
    const initialTags = ['General', 'Tech', 'News', 'Art', 'Gaming'];
    
    function initTags() {
        tagChipsContainer.innerHTML = '';
        initialTags.forEach((tag, index) => {
            const btn = document.createElement('button');
            btn.className = `tag-chip ${index === 0 ? 'active' : ''}`;
            btn.textContent = tag;
            btn.setAttribute('data-tag', tag);
            tagChipsContainer.appendChild(btn);
        });
    }

    initTags();
    fetchFeed([]); // Initial load

    // Handle chip clicks
    tagChipsContainer.addEventListener('click', (e) => {
        if (e.target.classList.contains('tag-chip')) {
            const chip = e.target;
            const tagValue = chip.getAttribute('data-tag');

            // Update UI
            document.querySelectorAll('.tag-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            // Fetch new data
            if (tagValue === "General" || tagValue === "") { // Assuming 'General' is the default/all
                fetchFeed([]);
            } else {
                fetchFeed([tagValue]);
            }
        }
    });
});