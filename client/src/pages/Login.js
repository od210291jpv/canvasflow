import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setIsLoading(true);

        const apiUrl = process.env.REACT_APP_API_URL || 'https://localhost:49297';

        try {
            const response = await fetch(`${apiUrl}/api/Auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, password }),
            });

            const data = await response.json();

            if (response.ok) {
                localStorage.setItem('authToken', data.token);
                navigate('/feed');
            } else {
                setError(data.error || 'Invalid username or password.');
            }
        } catch (err) {
            setError('Unable to connect to the server. Please ensure the backend is running.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="login-container glass-card" style={{ maxWidth: '400px', margin: '50px auto', textAlign: 'center' }}>
            <h2 style={{ color: 'var(--color-primary)' }}>ArtFlow Login</h2>
            <p>Sign in to view the latest art feed.</p>

            {error && <p style={{ color: 'var(--color-accent)' }} className="error-message">{error}</p>}        

            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px', marginTop: '20px' }}>
                <input
                    type="text"
                    placeholder="Username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required
                    aria-label="Username"
                />
                <input
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    aria-label="Password"
                />
                <button type="submit" className="btn" style={{ width: '100%' }} disabled={isLoading}>
                    {isLoading ? 'Logging in...' : 'Log In'}
                </button>
            </form>
        </div >
    );
};

export default Login;
