// Password visibility toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    const toggleButton = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');
    
    if (!toggleButton || !passwordInput) return;

    // Track current state
    let isVisible = false;

  // Function to update icon
    function updateIcon() {
  const iconName = isVisible ? 'eye-off' : 'eye';
        toggleButton.innerHTML = `<i data-lucide="${iconName}"></i>`;
        
    // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
     lucide.createIcons();
        }
    }

// Initialize icon
    updateIcon();

 // Toggle password visibility
    toggleButton.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
    // Toggle state
        isVisible = !isVisible;
        
        // Update input type
        passwordInput.setAttribute('type', isVisible ? 'text' : 'password');
        
     // Update icon
   updateIcon();
    });
});
