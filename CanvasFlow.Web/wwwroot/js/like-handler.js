// Use event delegation to handle buttons that are added dynamically to the DOM
document.addEventListener('click', async (event) => {
    // Check if the clicked element (or its parent) is a like button
    const button = event.target.closest('.btn-like');
    if (!button) return;

    const baseUrl = 'http://192.168.88.68:5000';

    const contentId = button.dataset.contentId;
    const isLiked = button.classList.contains('liked');
    const token = localStorage.getItem('token');

    // --- Optimistic UI update ---
    const textSpan  = button.querySelector('.like-text');
    const countSpan = button.querySelector('.like-count');
    const currentCount = countSpan ? parseInt(countSpan.textContent, 10) || 0 : 0;

    if (!isLiked) {
        button.classList.add('liked');
        if (textSpan)  textSpan.textContent  = 'Liked';
        if (countSpan) countSpan.textContent  = currentCount + 1;
    } else {
        button.classList.remove('liked');
        if (textSpan)  textSpan.textContent  = 'Like';
        if (countSpan) countSpan.textContent  = Math.max(0, currentCount - 1);
    }

    try {
        const response = await fetch(`${baseUrl}/api/Content/like/${contentId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.error || 'Failed to update like status');
        }
    } catch (error) {
        console.error('Like error:', error);
        // Rollback UI on failure
        button.classList.toggle('liked');
        if (textSpan)  textSpan.textContent  = isLiked ? 'Liked' : 'Like';
        if (countSpan) countSpan.textContent  = currentCount; // restore original count
        alert(`Error: ${error.message}`);
    }
});
