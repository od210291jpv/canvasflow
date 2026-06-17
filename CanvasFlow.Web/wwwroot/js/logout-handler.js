document.addEventListener('DOMContentLoaded', function () {
    const logoutForm = document.querySelector('form[asp-page-handler="Logout"]');
    
    if (logoutForm) {
        logoutForm.addEventListener('submit', async function (e) {
            e.preventDefault(); // Stop standard form submission

            const token = localStorage.getItem('token');
            if (!token) {
                window.location.href = '/auth';
                return;
            }

            try {
                const response = await fetch('/api/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });

                if (response.ok) {
                    // Clear local storage and session
                    localStorage.removeItem('token');
                    localStorage.removeItem('user_data');
                    sessionStorage.clear();
                    
                    // Clear cookies if any are used for auth
                    document.cookie = "AuthToken=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                    
                    console.log("Logout successful. Redirecting...");
                    window.location.href = '/auth'; // Explicit redirect to login page
                } else {
                    const errorData = await response.json();
                    console.error('Logout failed:', errorData.error);
                    alert('Logout failed: ' + (errorData.error || 'Unknown error'));
                }
            } catch (error) {
                console.error('Network error during logout:', error);
                // Fallback: clear data and redirect even if network fails to ensure user isn't stuck
                localStorage.removeItem('token');
                window.location.href = '/auth';
            }
        });
    }
});
