// Password visibility toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    const toggleButton = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');
    
    if (toggleButton && passwordInput) {
        toggleButton.addEventListener('click', function() {
            // Toggle password visibility
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            
            // Toggle icon between eye and eye-off
            const icon = toggleButton.querySelector('i');
            if (icon) {
                const currentIcon = icon.getAttribute('data-lucide');
                icon.setAttribute('data-lucide', currentIcon === 'eye' ? 'eye-off' : 'eye');
                
                // Reinitialize lucide icons to update the icon
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        });
    }
});
