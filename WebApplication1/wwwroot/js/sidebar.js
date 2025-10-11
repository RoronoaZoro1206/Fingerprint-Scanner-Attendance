// Sidebar active link handler
document.addEventListener('DOMContentLoaded', function () {
    // Get current path
    const currentPath = window.location.pathname.toLowerCase();

    // Function to set active links based on current URL
    function setActiveLinks() {
        // Get all sidebar links (both desktop and mobile)
        const sidebarLinks = document.querySelectorAll('.sidebar-link');

        // First, remove all active classes
        sidebarLinks.forEach(link => {
            link.classList.remove('active');
        });

        // Find the best matching link
        let bestMatch = null;
        let bestMatchLength = 0;

        sidebarLinks.forEach(link => {
            const linkPath = new URL(link.href).pathname.toLowerCase();

            // Exact match has highest priority
            if (currentPath === linkPath) {
                if (linkPath.length > bestMatchLength) {
                    bestMatch = linkPath;
                    bestMatchLength = linkPath.length;
                }
            }
        });

        // Apply active class only to the best match
        if (bestMatch) {
            sidebarLinks.forEach(link => {
                const linkPath = new URL(link.href).pathname.toLowerCase();
                if (linkPath === bestMatch) {
                    link.classList.add('active');
                }
            });
        }
    }

    // Set active links on page load
    setActiveLinks();

    // Handle click events - set active on the clicked link and its counterpart
    document.querySelectorAll('.sidebar-link').forEach(link => {
        link.addEventListener('click', function (e) {
            const clickedHref = this.href;

            // Remove active from all links
            document.querySelectorAll('.sidebar-link').forEach(l => {
                l.classList.remove('active');
            });

            // Add active to all links with the same href (desktop and mobile versions)
            document.querySelectorAll('.sidebar-link').forEach(l => {
                if (l.href === clickedHref) {
                    l.classList.add('active');
                }
            });
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
