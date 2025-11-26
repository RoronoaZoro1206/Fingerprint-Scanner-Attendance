/**
 * Date Picker Validation - Prevent Future Dates
 * Ensures all attendance-related date inputs cannot select dates after today
 * Applied globally across Faculty, Student, and Admin modules
 * 
 * This is separate from birthdate-validation.js which handles birthdate fields specifically
 * ✅ CRITICAL FIX: Dynamic date updating to handle midnight transitions
 * ✅ NEW FIX: Update max date on focus/click for immediate accuracy
 */

/**
 * Get today's date in YYYY-MM-DD format using Philippine timezone (UTC+8)
 * Always returns current Philippine date, even after midnight
 * ✅ FIX: Use Asia/Manila timezone to ensure consistency with server
 */
function getTodayDateString() {
    // Convert to Philippine timezone
    const philippineDate = new Date(new Date().toLocaleString('en-US', { timeZone: 'Asia/Manila' }));
    const year = philippineDate.getFullYear();
    const month = String(philippineDate.getMonth() + 1).padStart(2, '0');
    const day = String(philippineDate.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

/**
 * Initialize attendance date validation
 * Sets the max attribute to today's date to prevent future selections
 */
function initializeAttendanceDateValidation() {
    // Get today's date in YYYY-MM-DD format
    const today = getTodayDateString();
    
    // Attendance-related date field selectors
    const attendanceDateSelectors = [
        '#dateSelect',                          // Faculty Attendance date selector
        '#attendanceDate',                      // Generic attendance date
        'input[id*="dateSelect"]',              // Any date selector
        'input[id*="attendanceDate"]',          // Any attendance date
        '.date-selector input[type="date"]',    // Date selector class
        '.attendance-date input[type="date"]'   // Attendance date class
    ];
    
    // Apply max date restriction to all matching inputs
    attendanceDateSelectors.forEach(selector => {
        const elements = document.querySelectorAll(selector);
        elements.forEach(element => {
            // Skip birthdate fields (handled by birthdate-validation.js)
            if (element.id && element.id.toLowerCase().includes('birthdate')) {
                return;
            }
            if (element.name && element.name.toLowerCase().includes('birthdate')) {
                return;
            }
            
            // Set max attribute to today
            element.setAttribute('max', today);
            
            // ✅ NEW: Update max date when user clicks/focuses the input
            // This ensures immediate update after midnight without waiting for timer
            element.addEventListener('focus', function() {
                const currentToday = getTodayDateString();
                const currentMax = this.getAttribute('max');
                if (currentMax !== currentToday) {
                    this.setAttribute('max', currentToday);
                    console.log(`🔄 Updated max date on focus: ${currentMax} → ${currentToday}`);
                }
            });
            
            // ✅ NEW: Also update on click (for calendar icon clicks)
            element.addEventListener('click', function() {
                const currentToday = getTodayDateString();
                const currentMax = this.getAttribute('max');
                if (currentMax !== currentToday) {
                    this.setAttribute('max', currentToday);
                    console.log(`🔄 Updated max date on click: ${currentMax} → ${currentToday}`);
                }
            });
            
            // Add visual feedback - show warning if trying to select future date
            element.addEventListener('input', function() {
                const selectedDate = new Date(this.value);
                const maxDate = new Date(getTodayDateString());
                
                if (selectedDate > maxDate) {
                    this.value = getTodayDateString(); // Reset to today
                    
                    // Show warning toast if Toast is available
                    if (typeof Toast !== 'undefined') {
                        Toast.warning('Cannot select future dates. Date has been set to today.');
                    } else if (typeof Toastify !== 'undefined') {
                        Toastify({
                            text: "Cannot select future dates",
                            duration: 3000,
                            gravity: "top",
                            position: "right",
                            backgroundColor: "#f59e0b",
                        }).showToast();
                    } else {
                        console.warn('Cannot select future dates for attendance');
                    }
                }
            });
            
            console.log(`✅ Date validation applied to: ${selector} (max: ${today})`);
        });
    });
}

/**
 * Reinitialize attendance date validation for dynamically loaded content
 * Call this after modals are shown or content is dynamically added
 */
function reinitializeAttendanceDateValidation() {
    initializeAttendanceDateValidation();
}

/**
 * ✅ IMPROVED: Update max date every minute to handle midnight transitions
 * This ensures the date picker automatically allows the new day after midnight
 * Combined with focus/click handlers for immediate updates
 */
function startDateValidationUpdater() {
    // Update immediately
    initializeAttendanceDateValidation();
    
    // Update every minute (60000ms) to catch midnight transitions
    setInterval(function() {
        const today = getTodayDateString();
        console.log(`🕐 Updating date picker max to: ${today}`);
        
        // Update all date inputs
        const allDateInputs = document.querySelectorAll('input[type="date"]');
        allDateInputs.forEach(input => {
            // Skip birthdate fields
            if (input.id && input.id.toLowerCase().includes('birthdate')) {
                return;
            }
            if (input.name && input.name.toLowerCase().includes('birthdate')) {
                return;
            }
            
            // Update max attribute to current date
            const currentMax = input.getAttribute('max');
            if (currentMax !== today) {
                input.setAttribute('max', today);
                console.log(`✅ Updated ${input.id || input.name || 'date input'} max to ${today}`);
            }
        });
    }, 60000); // Run every 60 seconds (1 minute)
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    startDateValidationUpdater();
    console.log('✅ Attendance date validation initialized globally with auto-updater + focus/click handlers');
});

// Reinitialize when Bootstrap modals are shown
document.addEventListener('shown.bs.modal', function() {
    reinitializeAttendanceDateValidation();
    console.log('✅ Attendance date validation reinitialized for modal');
});

// Export for use in other scripts
if (typeof window !== 'undefined') {
    window.initializeAttendanceDateValidation = initializeAttendanceDateValidation;
    window.reinitializeAttendanceDateValidation = reinitializeAttendanceDateValidation;
    window.getTodayDateString = getTodayDateString;
}
