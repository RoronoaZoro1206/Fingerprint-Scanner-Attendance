// Sidebar active link handler and collapsible groups
document.addEventListener('DOMContentLoaded', function () {
    // Get current path
    const currentPath = window.location.pathname.toLowerCase();

    // Initialize collapsible sidebar groups
    initializeSidebarGroups();

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

    // Handle mobile offcanvas link clicks
    // For links with data-bs-dismiss="offcanvas", we need to manually navigate after closing
    const offcanvasLinks = document.querySelectorAll('.offcanvas [data-bs-dismiss="offcanvas"]');
    offcanvasLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            const href = this.getAttribute('href');

            // Only handle if it's a valid navigation link (not # or javascript:)
            if (href && href !== '#' && !href.startsWith('javascript:')) {
                e.preventDefault(); // Prevent default navigation

                // Get the offcanvas instance
                const offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);

                if (offcanvas) {
                    // Close the offcanvas
                    offcanvas.hide();

                    // Navigate after offcanvas is hidden
                    offcanvasElement.addEventListener('hidden.bs.offcanvas', function () {
                        window.location.href = href;
                    }, { once: true }); // Use once: true to ensure it only fires once
                } else {
                    // If no offcanvas instance, just navigate
                    window.location.href = href;
                }
            }
        });
    });
});


/**
 * Initialize collapsible sidebar groups
 */
function initializeSidebarGroups() {
    // Get all group toggles
    const groupToggles = document.querySelectorAll('.sidebar-group-toggle');

    groupToggles.forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const groupName = this.getAttribute('data-group');
            const content = document.querySelector(`[data-group-content="${groupName}"]`);

            if (content) {
                // Toggle expanded class
                const isExpanded = content.classList.contains('expanded');

                if (isExpanded) {
                    // Collapse
                    content.classList.remove('expanded');
                    this.classList.remove('expanded');
                } else {
                    // Expand
                    content.classList.add('expanded');
                    this.classList.add('expanded');
                }

                // Reinitialize Lucide icons
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        });
    });

    // Auto-expand group if current page is in that group
    autoExpandActiveGroup();
}

/**
 * Auto-expand the group containing the active link
 */
function autoExpandActiveGroup() {
    const currentPath = window.location.pathname.toLowerCase();
    const allLinks = document.querySelectorAll('.sidebar-link.sub-link');

    allLinks.forEach(link => {
        const linkPath = new URL(link.href).pathname.toLowerCase();

        if (currentPath === linkPath || currentPath.startsWith(linkPath)) {
            // Find parent group content
            const groupContent = link.closest('.sidebar-group-content');
            if (groupContent) {
                const groupName = groupContent.getAttribute('data-group-content');
                const toggle = document.querySelector(`[data-group="${groupName}"]`);

                if (toggle && groupContent) {
                    groupContent.classList.add('expanded');
                    toggle.classList.add('expanded');
                }
            }
        }
    });

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}
