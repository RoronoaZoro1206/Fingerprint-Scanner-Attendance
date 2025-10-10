// Sidebar active link handler
document.addEventListener('DOMContentLoaded', function() {
    // Get current path
    const currentPath = window.location.pathname;
    
    // Function to set active links
    function setActiveLinks() {
        // Get all sidebar links (both desktop and mobile)
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
    }
    
    // Set active links on page load
    setActiveLinks();
    
    // Handle click events
    document.querySelectorAll('.sidebar-link').forEach(link => {
        link.addEventListener('click', function() {
            // Remove active from all links
            document.querySelectorAll('.sidebar-link').forEach(l => l.classList.remove('active'));
            // Add active to clicked link
            this.classList.add('active');
        });
    });
    
    // Reinitialize lucide icons when offcanvas is shown
    const offcanvasElement = document.getElementById('sidebarOffcanvas');
    if (offcanvasElement) {
        offcanvasElement.addEventListener('shown.bs.offcanvas', function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        });
    }
});
