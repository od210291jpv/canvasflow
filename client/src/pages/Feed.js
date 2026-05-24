import React, { useState, useEffect, useCallback } from 'react';
import ArtCard from '../components/ArtCard';
import SkeletonLoader from '../components/SkeletonLoader';
import UploadModal from '../components/UploadModal';

const Feed = () => {
    const [artFeed, setArtFeed] = useState([]);
    const [isLoading, setIsLoading] = useState(false);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [error, setError] = useState('');

    const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:49297';

    const fetchArt = useCallback(async (isInitial = false) => {
        if (isLoading || (!hasMore && !isInitial)) return;

        setIsLoading(true);
        setError('');
        const currentPage = isInitial ? 1 : page;
        const limit = 6;

        try {
            const token = localStorage.getItem('authToken');
            const response = await fetch(`${apiUrl}/api/Content/feed?page=${currentPage}&limit=${limit}`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                
                // Ensure data is an array
                const newItems = Array.isArray(data) ? data : [];

                if (isInitial) {
                    setArtFeed(newItems);
                    setPage(2);
                } else {
                    setArtFeed(prev => [...prev, ...newItems]);
                    setPage(prev => prev + 1);
                }

                if (newItems.length < limit) {
                    setHasMore(false);
                }
            } else {
                setError('Failed to load feed.');
            }
        } catch (err) {
            setError('Error connecting to the server.');
        } finally {
            setIsLoading(false);
        }
    }, [page, isLoading, hasMore, apiUrl]);

    useEffect(() => {
        fetchArt(true);
    }, []);

    const handleArtCreationSuccess = (newArt) => {
        if (newArt) {
            setArtFeed(prev => [newArt, ...prev]);
        }
    };

    return (
        <div className="feed-container">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h1 style={{ color: 'var(--color-primary)' }}>ArtFlow Community Art Feed</h1>
                <button 
                    className="btn" 
                    style={{ padding: '10px 20px', fontSize: '1.1rem' }}
                    onClick={() => setIsModalOpen(true)}
                >
                    + Create Art
                </button>
            </div>

            {error && <div className="error-message" style={{ margin: '20px 0' }}>{error}</div>}

            <div className="art-grid">
                {artFeed.length > 0 ? (
                    artFeed.filter(art => art && art.id).map(art => (
                        <ArtCard key={art.id} art={art} />
                    ))
                ) : !isLoading && (
                    <div style={{ textAlign: 'center', padding: '50px', color: 'var(--color-text-muted)' }}>
                        <p>No art found in the feed yet.</p>
                    </div>
                )}
            </div >

            <div className="loading-indicator" style={{ textAlign: 'center', margin: '40px 0' }}>
                {isLoading ? <SkeletonLoader /> : hasMore ? (
                    <button
                        className="btn"
                        onClick={() => fetchArt()}
                        disabled={isLoading}
                        style={{ width: '200px' }}
                    >
                        Load More Art
                    </button>
                ) : (
                    <p style={{ color: 'var(--color-secondary)' }}>You've reached the end of the feed.</p>      
                )}
            </div >

            <UploadModal
                isOpen={isModalOpen}
                onClose={() => setIsModalOpen(false)}
                onSubmitSuccess={handleArtCreationSuccess}
            />
        </div>
    );
};

export default Feed;
