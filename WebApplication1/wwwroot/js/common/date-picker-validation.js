/**
 * Date Picker Validation - Prevent Future Dates
 * Ensures all attendance-related date inputs cannot select dates after today
 * Applied globally across Faculty, Student, and Admin modules
 * 
 * This is separate from birthdate-validation.js which handles birthdate fields specifically
 */

/**
 * Initialize attendance date validation
 * Sets the max attribute to today's date to prevent future selections
 */
function initializeAttendanceDateValidation() {
    // Get today's date in YYYY-MM-DD format
    const today = new Date().toISOString().split('T')[0];
    
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
            
            // Add visual feedback - show warning if trying to select future date
            element.addEventListener('input', function() {
                const selectedDate = new Date(this.value);
                const maxDate = new Date(today);
                
                if (selectedDate > maxDate) {
                    this.value = today; // Reset to today
                    
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
            
            console.log(`✅ Date validation applied to: ${selector}`);
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

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeAttendanceDateValidation();
    console.log('✅ Attendance date validation initialized globally');
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
}
