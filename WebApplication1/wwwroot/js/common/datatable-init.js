/**
 * Common DataTable Initialization
 * Shared DataTable configuration and utilities
 */

/**
 * Initialize Lucide icons
 */
function initializeLucideIcons() {
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Add search icon to DataTables search input
 */
function addSearchIcon() {
    const searchInput = $('.dataTables_filter input');
    
    // Check if icon hasn't been added yet
    if (searchInput.parent().find('.search-icon').length === 0) {
        // Wrap input in a container
        searchInput.wrap('<div class="search-input-wrapper"></div>');
        
        // Add search icon before the input
        searchInput.before('<i data-lucide="search" class="search-icon"></i>');
        
        // Reinitialize Lucide icons
        setTimeout(function() {
            initializeLucideIcons();
        }, 100);
    }
}

/**
 * Common DataTable configuration
 */
const commonDataTableConfig = {
    pageLength: 10,
    lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
    dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
         '<"row"<"col-sm-12"tr>>' +
         '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
    drawCallback: function () {
        initializeLucideIcons();
    },
    initComplete: function () {
        addSearchIcon();
    }
};

/**
 * Get common language configuration for DataTables
 * @param {string} entityName - Name of the entity (e.g., "students", "courses")
 * @param {string} entitySingular - Singular form (e.g., "student", "course")
 */
function getDataTableLanguage(entityName, entitySingular) {
    return {
        search: "",
        searchPlaceholder: `Search ${entityName}...`,
        lengthMenu: `Show _MENU_ ${entityName}`,
        info: `Showing _START_ to _END_ of _TOTAL_ ${entityName}`,
        infoEmpty: `No ${entityName} available`,
        infoFiltered: `(filtered from _MAX_ total ${entityName})`,
        zeroRecords: `No matching ${entityName} found`,
        emptyTable: `No ${entityName} available in the system`
    };
}

/**
 * Format date for display
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

/**
 * Format date for input (YYYY-MM-DD)
 */
function formatDateForInput(dateString) {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
}

/**
 * ✅ NEW: Get today's date in YYYY-MM-DD format
 * Used for setting max date on date inputs
 */
function getTodayDate() {
    return new Date().toISOString().split('T')[0];
}

/**
 * ✅ NEW: Set max date to today for date inputs (prevent future dates)
 * @param {string} selector - CSS selector for date input element(s)
 */
function setMaxDateToToday(selector) {
    const today = getTodayDate();
    const elements = document.querySelectorAll(selector);
    
    elements.forEach(element => {
        if (element.type === 'date') {
            element.setAttribute('max', today);
            console.log(`✅ Max date set to ${today} for`, element.id || element.name);
        }
    });
}
