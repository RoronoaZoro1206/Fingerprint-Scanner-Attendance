// Password visibility toggle functionality for register form
document.addEventListener('DOMContentLoaded', function() {
    // Toggle for Password field
    const togglePasswordBtn = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');
    
    if (togglePasswordBtn && passwordInput) {
        togglePasswordBtn.addEventListener('click', function() {
            togglePasswordVisibility(passwordInput, togglePasswordBtn);
        });
    }

    // Toggle for Confirm Password field
    const toggleConfirmPasswordBtn = document.getElementById('toggleConfirmPassword');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    
    if (toggleConfirmPasswordBtn && confirmPasswordInput) {
        toggleConfirmPasswordBtn.addEventListener('click', function() {
            togglePasswordVisibility(confirmPasswordInput, toggleConfirmPasswordBtn);
        });
    }

    function togglePasswordVisibility(input, button) {
        // Toggle password visibility
        const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
        input.setAttribute('type', type);
        
        // Toggle icon between eye and eye-off
        const icon = button.querySelector('i');
        if (icon) {
            const currentIcon = icon.getAttribute('data-lucide');
            icon.setAttribute('data-lucide', currentIcon === 'eye' ? 'eye-off' : 'eye');
            
            // Reinitialize lucide icons to update the icon
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    }
});
