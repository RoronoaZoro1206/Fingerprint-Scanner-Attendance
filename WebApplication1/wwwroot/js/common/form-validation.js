/**
 * Common Form Validation Utilities
 * Shared form validation functions
 */

/**
 * Validate form and show browser validation messages
 * @param {HTMLFormElement} form - Form element to validate
 * @returns {boolean} - True if valid, false otherwise
 */
function validateForm(form) {
    if (!form.checkValidity()) {
        form.reportValidity();
        return false;
    }
    return true;
}

/**
 * Validate required fields
 * @param {Object} fields - Object with field names and values
 * @param {string} message - Error message to show
 * @returns {boolean} - True if all fields have values
 */
function validateRequiredFields(fields, message = 'All fields are required.') {
    for (const [key, value] of Object.entries(fields)) {
        if (!value || value.trim() === '') {
            Toast.warning(message);
            return false;
        }
    }
    return true;
}

/**
 * Validate minimum length
 * @param {string} value - Value to validate
 * @param {number} minLength - Minimum length required
 * @param {string} fieldName - Name of the field for error message
 * @returns {boolean} - True if valid
 */
function validateMinLength(value, minLength, fieldName) {
    if (value.length < minLength) {
        Toast.warning(`${fieldName} must be at least ${minLength} characters.`);
        return false;
    }
    return true;
}

/**
 * Validate number range
 * @param {number} value - Value to validate
 * @param {number} min - Minimum value
 * @param {number} max - Maximum value
 * @param {string} fieldName - Name of the field for error message
 * @returns {boolean} - True if valid
 */
function validateRange(value, min, max, fieldName) {
    if (value < min || value > max) {
        Toast.warning(`${fieldName} must be between ${min} and ${max}.`);
        return false;
    }
    return true;
}

/**
 * Validate date range
 * @param {string} startDate - Start date string
 * @param {string} endDate - End date string
 * @param {string} message - Error message
 * @returns {boolean} - True if valid
 */
function validateDateRange(startDate, endDate, message = 'End date must be after start date.') {
    if (new Date(endDate) <= new Date(startDate)) {
        Toast.warning(message);
        return false;
    }
    return true;
}

/**
 * Trim all string values in an object
 * @param {Object} obj - Object with string values
 * @returns {Object} - Object with trimmed values
 */
function trimObjectValues(obj) {
    const trimmed = {};
    for (const [key, value] of Object.entries(obj)) {
        trimmed[key] = typeof value === 'string' ? value.trim() : value;
    }
    return trimmed;
}
