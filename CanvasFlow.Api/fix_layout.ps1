$clientSrc = "C:\test\requirements\canvasflow\client\src"

$layoutContent = @'
import React, { useState, useEffect } from 'react';
import './Layout.css';

const Layout = ({ children }) => {
    const [theme, setTheme] = useState('white');
    const [isDarkMode, setIsDarkMode] = useState(false);

    useEffect(() => {
        const storedTheme = localStorage.getItem('appTheme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'white');
        setTheme(storedTheme);
        setIsDarkMode(storedTheme === 'dark');
        document.body.className = `theme-${storedTheme}`;
    }, []);

    const toggleTheme = (newTheme) => {
        setTheme(newTheme);
        setIsDarkMode(newTheme === 'dark');
        localStorage.setItem('appTheme', newTheme);
        document.body.className = `theme-${newTheme}`;
    };

    const toggleDarkMode = () => {
        const newMode = !isDarkMode;
        const newTheme = newMode ? 'dark' : 'white';
        toggleTheme(newTheme);
    };

    return (
        <div className="app-layout">
            <header className="main-header glass-card">
                <div className="logo">ArtFlow</div>
                <nav style={{ display: 'flex', gap: '10px' }}>
                    <button className="theme-toggle btn" onClick={toggleDarkMode}>
                        {isDarkMode ? '☀️ Light' : '🌙 Dark'}
                    </button>
                    <button className="theme-toggle btn" onClick={() => toggleTheme('pink')}>
                        🌸 Pink
                    </button>
                    <button className="theme-toggle btn" onClick={() => toggleTheme('white')}>
                        ⚪ White
                    </button>
                </nav>
            </header>
            <main className="container">
                {children}
            </main>
        </div >
    );
};

export default Layout;
'@

$layoutContent | Set-Content "$clientSrc\components\Layout.js" -Encoding UTF8
