$clientSrc = "C:\test\requirements\canvasflow\client\src"

$uploadModalContent = @'
import React, { useState } from 'react';
import './UploadModal.css';

const UploadModal = ({ isOpen, onClose, onSubmitSuccess }) => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [tags, setTags] = useState('');
    const [imageUrl, setImageUrl] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');

    const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:49297';

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        
        const trimmedUrl = imageUrl.trim();
        if (!trimmedUrl) {
            setError('Please provide a valid image URL.');
            return;
        }

        setIsSubmitting(true);

        try {
            const token = localStorage.getItem('authToken');
            const tagList = tags.split(',').map(t => t.trim()).filter(t => t !== '');
            
            const response = await fetch(`${apiUrl}/api/Content/upload`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    title: title.trim(),
                    description: description.trim(),
                    imageUrl: trimmedUrl,
                    tags: tagList
                }),
            });

            if (response.ok) {
                const newArt = await response.json();
                onSubmitSuccess(newArt);
                setTitle('');
                setDescription('');
                setTags('');
                setImageUrl('');
                onClose();
            } else {
                const data = await response.json();
                setError(data.error || 'Failed to publish artwork.');
            }
        } catch (err) {
            setError('Error connecting to the server.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="modal-backdrop" onClick={onClose}>
            <div className="modal-content glass-card" onClick={e => e.stopPropagation()}>
                <div className="modal-header">
                    <h2>Publish Your Masterpiece</h2>
                    <button className="close-btn" onClick={onClose}>&times;</button>
                </div>

                {error && <div className="error-message">{error}</div>}

                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label htmlFor="title">Title *</label>
                        <input
                            id="title"
                            type="text"
                            value={title}
                            onChange={e => setTitle(e.target.value)}
                            required
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="description">Description *</label>
                        <textarea
                            id="description"
                            value={description}
                            onChange={e => setDescription(e.target.value)}
                            rows="3"
                            required
                        ></textarea>
                    </div>

                    <div className="form-group">
                        <label htmlFor="imageUrl">Image URL *</label>
                        <input
                            id="imageUrl"
                            type="text"
                            value={imageUrl}
                            onChange={e => setImageUrl(e.target.value)}
                            placeholder="https://example.com/art.jpg"
                            required
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="tags">Tags (comma separated)</label>
                        <input
                            id="tags"
                            type="text"
                            value={tags}
                            onChange={e => setTags(e.target.value)}
                        />
                    </div>

                    <button type="submit" className="btn" style={{ width: '100%' }} disabled={isSubmitting}>
                        {isSubmitting ? 'Publishing...' : 'Publish to Feed'}
                    </button>
                </form>
            </div>
        </div>
    );
};

export default UploadModal;
'@

$uploadModalContent | Set-Content "$clientSrc\components\UploadModal.js" -Encoding UTF8
