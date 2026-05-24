import React, { useState } from 'react';

const ArtCard = ({ art }) => {
    const [isLiked, setIsLiked] = useState(false);
    const [likeCount, setLikeCount] = useState(art.likeCount || 0);

    const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:49297';

    const handleLike = async () => {
        const token = localStorage.getItem('authToken');
        
        // Optimistic UI
        const wasLiked = isLiked;
        setIsLiked(!wasLiked);
        setLikeCount(prev => wasLiked ? prev - 1 : prev + 1);

        try {
            const response = await fetch(`${apiUrl}/api/Content/like/${art.id}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (!response.ok) {
                // Revert on failure
                setIsLiked(wasLiked);
                setLikeCount(prev => wasLiked ? prev + 1 : prev - 1);
            }
        } catch (err) {
            // Revert on network error
            setIsLiked(wasLiked);
            setLikeCount(prev => wasLiked ? prev + 1 : prev - 1);
        }
    };

    // 1. Отримуємо шлях і визначаємо тип медіа
    let displayImageUrl = art.imageUrl && art.imageUrl.trim() !== "" ? art.imageUrl : null;
    let isVideo = false;

    if (displayImageUrl) {
        // Виправляємо зворотні слеші (специфіка Windows/.NET)
        displayImageUrl = displayImageUrl.replace(/\\/g, '/');
        
        // Якщо шлях відносний (не починається з http), додаємо URL бекенду
        if (!displayImageUrl.startsWith('http')) {
            const separator = displayImageUrl.startsWith('/') ? '' : '/';
            displayImageUrl = `${apiUrl}${separator}${displayImageUrl}`;
        }

        // Перевіряємо, чи це відео (розширення можна доповнити)
        const videoExtensions = ['.mp4', '.webm', '.ogg'];
        isVideo = videoExtensions.some(ext => displayImageUrl.toLowerCase().endsWith(ext));
    }

    const fallbackImage = "https://via.placeholder.com/600x400/eeeeee/999999?text=No+Image+Available";

    return (
        <div className="art-card glass-card" style={{ display: 'flex', flexDirection: 'column', marginBottom: '30px' }}>
            <div className="art-image-container">
                {displayImageUrl ? (
                    isVideo ? (
                        <video
                            src={displayImageUrl}
                            controls
                            style={{ width: '100%', height: 'auto', display: 'block' }}
                            onError={(e) => {
                                // Якщо відео не знайдене, ховаємо його
                                e.target.style.display = 'none';
                            }}
                        />
                    ) : (
                        <img
                            src={displayImageUrl}
                            alt={`Artwork: ${art.title}`}
                            className="art-image"
                            loading="lazy"
                            style={{ width: '100%', height: 'auto', display: 'block' }}
                            onError={(e) => {
                                e.target.onerror = null; 
                                e.target.src = fallbackImage;
                            }}
                        />
                    )
                ) : (
                    <div style={{ 
                        width: '100%', 
                        aspectRatio: '16/10', 
                        display: 'flex', 
                        alignItems: 'center', 
                        justifyContent: 'center',
                        background: 'rgba(0,0,0,0.05)',
                        color: 'var(--color-text-muted)'
                    }}>
                        <p>No Media Available</p>
                    </div>
                )}
            </div>

            <div className="art-details" style={{ padding: '20px' }}>
                <h2 className="art-title" style={{ margin: '0 0 10px 0' }}>{art.title}</h2>
                <p className="art-artist" style={{ margin: '0 0 15px 0' }}>By <span style={{ color: 'var(--color-primary)', fontWeight: 'bold' }}>{art.user?.username || 'Unknown'}</span></p>

                <div className="art-tags" style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                    {art.tags?.map(tag => (
                        <span key={tag.id || tag.name} className="tag-pill" style={{ background: 'var(--color-secondary)', color: 'white', padding: '4px 10px', borderRadius: '15px', fontSize: '0.85rem' }}>
                            #{tag.name}
                        </span>
                    ))}
                </div>

                <div className="art-actions" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '20px' }}>
                    <button
                        className="action-btn"
                        onClick={handleLike}
                        style={{ 
                            background: isLiked ? 'var(--color-accent)' : 'transparent', 
                            color: isLiked ? 'white' : 'var(--color-primary)', 
                            border: '1px solid ' + (isLiked ? 'var(--color-accent)' : 'var(--color-primary)'), 
                            padding: '8px 15px', 
                            cursor: 'pointer',
                            borderRadius: '5px',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '5px'
                        }}
                    >
                        <span>{isLiked ? '❤️' : '🤍'}</span>
                        {isLiked ? 'Liked!' : 'Like'}
                    </button>
                    
                    <span style={{ fontSize: '0.9rem', color: 'var(--color-secondary)' }}>
                        {likeCount} likes
                    </span>
                </div>
            </div>
        </div>
    );
};

export default ArtCard;