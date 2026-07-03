document.addEventListener('DOMContentLoaded', () => {
    const likeButtons = document.querySelectorAll('.like-button');

    likeButtons.forEach(button => {
        button.addEventListener('click', async function() {
            const contentId = this.dataset.contentId;
            let isLiked = this.classList.contains('liked');

            // Optimistic UI Update
            if (!isLiked) {
                this.classList.add('liked');
                this.querySelector('.like-text').textContent = 'Liked';
            } else {
                this.classList.remove('liked');
                this.querySelector('.like-text').textContent = 'Like';
            }

            try {
                const response = await fetch(`/api/Content/like/${contentId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error('Failed to update like status');
                }
            } catch (error) {
                console.error('Like error:', error);
                // Rollback UI on failure
                this.classList.toggle('liked');
                this.querySelector('.like-text').textContent = isLiked ? 'Liked' : 'Like';
                alert('Failed to update like status. Please try again.');
            }
        });
    });
});
