import React, { useState } from 'react';
import './UploadModal.css';

const UploadModal = ({ isOpen, onClose, onSubmitSuccess }) => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [tags, setTags] = useState('');
    const [file, setFile] = useState(null); // Змінено: тепер зберігаємо об'єкт файлу
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');

    const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:49297';

    if (!isOpen) return null;

    // Обробник вибору файлу
    const handleFileChange = (e) => {
        if (e.target.files && e.target.files[0]) {
            setFile(e.target.files[0]);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        
        if (!file) {
            setError('Будь ласка, виберіть медіафайл (зображення або відео).');
            return;
        }

        setIsSubmitting(true);

        try {
            const token = localStorage.getItem('authToken');
            const tagList = tags.split(',').map(t => t.trim()).filter(t => t !== '');
            
            // Створюємо FormData для відправки файлу разом з іншими даними
            const formData = new FormData();
            formData.append('title', title.trim());
            formData.append('description', description.trim());
            
            // Важливо: ім'я ключа 'file' має збігатися з назвою параметра у вашому C# контролері!
            // Якщо у C# написано (IFormFile image), змініть 'file' на 'image' нижче.
            formData.append('file', file); 
            
            // Додаємо теги. Залежно від бекенду, він може очікувати масив або один рядок.
            // Відправляємо кожен тег окремо, щоб ASP.NET міг зібрати їх у List<string>
            tagList.forEach(tag => formData.append('tags', tag));

            const response = await fetch(`${apiUrl}/api/Content/upload`, {
                method: 'POST',
                headers: {
                    // ВАЖЛИВО: Ми НЕ встановлюємо 'Content-Type' тут. 
                    // Браузер автоматично встановить 'multipart/form-data' при використанні FormData
                    'Authorization': `Bearer ${token}`
                },
                body: formData, // Відправляємо FormData замість JSON
            });

            if (response.ok) {
                const newArt = await response.json();
                onSubmitSuccess(newArt);
                // Очищаємо форму після успіху
                setTitle('');
                setDescription('');
                setTags('');
                setFile(null);
                onClose();
            } else {
                const data = await response.json();
                setError(data.error || 'Не вдалося опублікувати арт.');
            }
        } catch (err) {
            setError('Помилка з\'єднання із сервером.');
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

                    {/* Оновлене поле для вибору файлу */}
                    <div className="form-group">
                        <label htmlFor="fileInput">Media File (Image/Video) *</label>
                        <input
                            id="fileInput"
                            type="file"
                            accept="image/*,video/*,.gif" // Обмежуємо вибір лише медіафайлами
                            onChange={handleFileChange}
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