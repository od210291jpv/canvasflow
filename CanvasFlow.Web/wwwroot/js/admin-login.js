/**
 * Admin Login Page Scripts
 * Handles AJAX login with the auth endpoint and redirects to Dashboard on success
 */

document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('admin-login-form');
    const statusDiv = document.getElementById('login-status');
    const loginBtn = document.getElementById('login-btn');
    const btnText = loginBtn?.querySelector('.btn-text');
    const btnLoader = loginBtn?.querySelector('.btn-loader');
    const baseUrl = 'http://192.168.88.68:5000';

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const usernameInput = document.getElementById('admin-username').value.trim();
            const passwordInput = document.getElementById('admin-password').value;

            // Validate inputs
            if (!usernameInput || !passwordInput) {
                showStatus('Будь ласка, заповніть всі поля.', 'error');
                return;
            }

            // Show loading state
            setLoading(true);
            showStatus('Перевірка даних...', 'loading');

            try {
                const response = await fetch(`${baseUrl}/api/auth/login`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        username: usernameInput,
                        password: passwordInput
                    })
                });

                const data = await response.json();

                if (response.ok && data.token) {
                    // Store token in localStorage for API calls
                    localStorage.setItem('adminToken', data.token);
                    
                    // Also store in cookie for Razor Pages session compatibility
                    document.cookie = `AdminAuthToken=${data.token}; path=/; max-age=86400; SameSite=Strict`;

                    showStatus('Успішний вхід! Перенаправлення...', 'success');
                    
                    // Redirect to Admin Dashboard after a short delay
                    setTimeout(function () {
                        window.location.href = '/Admin/Dashboard';
                    }, 1200);
                } else {
                    showStatus(data.error || 'Помилка авторизації. Перевірте логін та пароль.', 'error');
                }
            } catch (error) {
                console.error('Admin login error:', error);
                showStatus('Помилка підключення до сервера. Спробуйте пізніше.', 'error');
            } finally {
                setLoading(false);
            }
        });

        // Clear status message when user starts typing
        const inputs = loginForm.querySelectorAll('input');
        inputs.forEach(function (input) {
            input.addEventListener('input', function () {
                if (statusDiv.classList.contains('error') || statusDiv.classList.contains('success')) {
                    hideStatus();
                }
            });
        });

        // Focus on username field on load
        const usernameField = document.getElementById('admin-username');
        if (usernameField) {
            usernameField.focus();
        }
    }

    /**
     * Show status message
     */
    function showStatus(message, type) {
        statusDiv.textContent = message;
        statusDiv.className = 'status-message ' + type;
        statusDiv.style.display = 'block';
    }

    /**
     * Hide status message
     */
    function hideStatus() {
        statusDiv.style.display = 'none';
        statusDiv.className = 'status-message';
    }

    /**
     * Set loading state on the button
     */
    function setLoading(isLoading) {
        if (!loginBtn || !btnText || !btnLoader) return;

        if (isLoading) {
            loginBtn.disabled = true;
            btnText.style.display = 'none';
            btnLoader.style.display = 'inline-flex';
        } else {
            loginBtn.disabled = false;
            btnText.style.display = 'inline';
            btnLoader.style.display = 'none';
        }
    }
});
