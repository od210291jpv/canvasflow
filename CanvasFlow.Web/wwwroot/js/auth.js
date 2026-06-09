// Auth Page Scripts
document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('login-form');
    const baseUrl = 'http://localhost:5000';

    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const statusDiv = document.getElementById('login-status');
            const usernameInput = document.getElementById('login-username').value;
            const passwordInput = document.getElementById('login-password').value;

            statusDiv.style.display = 'block';
            statusDiv.className = 'alert';
            statusDiv.textContent = 'Перевірка даних...';

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
                    localStorage.setItem('token', data.token);
                    document.cookie = `AuthToken=${data.token}; path=/; max-age=86400; SameSite=Strict`;
                    
                    statusDiv.textContent = 'Успішний вхід! Перенаправлення...';
                    statusDiv.classList.add('alert-success');
                    
                    window.location.href = '/Profile';
                } else {
                    statusDiv.textContent = data.error || 'Помилка авторизації. Перевірте логін та пароль.';
                    statusDiv.classList.add('alert-error');
                }
            } catch (error) {
                console.error("Fetch error:", error);
                statusDiv.textContent = 'Помилка підключення до сервера.';
                statusDiv.classList.add('alert-error');
            }
        });
    }
});
