// wwwroot/js/logout-handler.js
document.addEventListener('DOMContentLoaded', function () {
    const logoutForm = document.querySelector('form[asp-page-handler="Logout"]');
    
    if (logoutForm) {
        logoutForm.addEventListener('submit', function (e) {
            localStorage.removeItem('token');
            
            document.cookie = "AuthToken=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            console.log("Client-side tokens cleared. Proceeding to server logout...");
        });
    }
});
