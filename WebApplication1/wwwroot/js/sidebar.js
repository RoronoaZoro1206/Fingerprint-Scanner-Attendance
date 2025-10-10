// Sidebar active link handler
document.addEventListener('DOMContentLoaded', function() {
    // Get current path
    const currentPath = window.location.pathname;
    
    // Get all sidebar links
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    
    // Set active class based on current path
    sidebarLinks.forEach(link => {
        const linkPath = new URL(link.href).pathname;
        
        // Check if current path matches or starts with link path
        if (currentPath === linkPath || (linkPath !== '/' && currentPath.startsWith(linkPath))) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
    
    // Handle click events
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function() {
            // Remove active from all links
            sidebarLinks.forEach(l => l.classList.remove('active'));
            // Add active to clicked link
            this.classList.add('active');
        });
    });
});
