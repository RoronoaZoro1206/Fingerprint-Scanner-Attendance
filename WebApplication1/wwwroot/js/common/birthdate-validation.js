/**
 * Birthdate Validation - Prevent Future Dates
 * Ensures all birthdate inputs cannot select dates after today
 * This is applied globally across Admin, Faculty, and Student modules
 */

/**
 * Initialize birthdate validation on all date inputs with birthdate-related IDs
 * Sets the max attribute to today's date to prevent future selections
 */
function initializeBirthdateValidation() {
    // Get today's date in YYYY-MM-DD format
    const today = new Date().toISOString().split('T')[0];
    
    // Common birthdate field selectors
    const birthdateSelectors = [
        '#birthdate',
        '#addUserBirthdate',
        '#editUserBirthdate',
        'input[name="UserBirthdate"]',
     'input[name="Birthdate"]',
'input[type="date"][id*="birthdate" i]',  // Case-insensitive match
    'input[type="date"][name*="birthdate" i]' // Case-insensitive match
    ];
    
    // Apply max date restriction to all matching inputs
    birthdateSelectors.forEach(selector => {
        const elements = document.querySelectorAll(selector);
        elements.forEach(element => {
            element.setAttribute('max', today);
         
   // Add visual feedback - show warning if trying to select future date
         element.addEventListener('input', function() {
  const selectedDate = new Date(this.value);
          const maxDate = new Date(today);
       
 if (selectedDate > maxDate) {
          this.value = today; // Reset to today
  
          // Show warning toast if Toast is available
            if (typeof Toast !== 'undefined') {
          Toast.warning('Birthdate cannot be in the future. Date has been set to today.');
           } else if (typeof Toastify !== 'undefined') {
  Toastify({
          text: "Birthdate cannot be in the future",
             duration: 3000,
              gravity: "top",
       position: "right",
           backgroundColor: "#f59e0b",
             }).showToast();
        } else {
        alert('Birthdate cannot be in the future.');
      }
             }
        });
      
            console.log(`? Birthdate validation applied to: ${selector}`);
        });
    });
}

/**
 * Reinitialize birthdate validation for dynamically loaded content
 * Call this after modals are shown or content is dynamically added
 */
function reinitializeBirthdateValidation() {
    initializeBirthdateValidation();
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeBirthdateValidation();
    console.log('?? Birthdate validation initialized globally');
});

// Reinitialize when Bootstrap modals are shown (for Admin User modals)
document.addEventListener('shown.bs.modal', function() {
reinitializeBirthdateValidation();
    console.log('?? Birthdate validation reinitialized for modal');
});

// Export for use in other scripts
if (typeof window !== 'undefined') {
    window.initializeBirthdateValidation = initializeBirthdateValidation;
    window.reinitializeBirthdateValidation = reinitializeBirthdateValidation;
}
