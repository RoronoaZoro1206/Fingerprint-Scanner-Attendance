/**
 * Academic Term Updater
 * Handles real-time updates of the active academic term in the navbar
 */

// Global function to refresh academic term
window.refreshAcademicTerm = function() {
    console.log('Refreshing academic term...');
    
    fetch('/SharedData/GetActiveAcademicTerm')
        .then(response => response.json())
        .then(data => {
            console.log('Academic term refreshed:', data);
            
            const termText = document.getElementById('academicTermText');
            const termBadge = document.getElementById('academicTermBadge');
            
            if (termText && termBadge) {
                if (data.success && data.data && data.data.termName) {
                    termText.textContent = data.data.termName;
                    termBadge.classList.remove('inactive');
                } else {
                    termText.textContent = 'No Active Term';
                    termBadge.classList.add('inactive');
                }
                
                // Reinitialize Lucide icons
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        })
        .catch(error => {
            console.error('Error refreshing academic term:', error);
        });
};

// Listen for custom event to refresh academic term
document.addEventListener('academicTermUpdated', function() {
    console.log('Academic term updated event received');
    window.refreshAcademicTerm();
});

// Expose function globally for admin pages
window.triggerAcademicTermUpdate = function() {
    console.log('Triggering academic term update event');
    const event = new CustomEvent('academicTermUpdated');
    document.dispatchEvent(event);
};
