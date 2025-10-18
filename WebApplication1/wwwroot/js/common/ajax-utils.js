/**
 * Common AJAX Utilities
 * Shared AJAX request handling functions
 */

/**
 * Make a GET request
 * @param {string} url - API endpoint URL
 * @param {Function} successCallback - Success callback function
 * @param {Function} errorCallback - Error callback function (optional)
 */
function ajaxGet(url, successCallback, errorCallback) {
    $.ajax({
        url: url,
        type: 'GET',
        success: function(response) {
            if (response.success) {
                successCallback(response);
            } else {
                Toast.error(response.message || 'Request failed');
                if (errorCallback) errorCallback(response);
            }
        },
        error: function(xhr, status, error) {
            console.error('GET Error:', error);
            Toast.error('Request failed');
            if (errorCallback) errorCallback(xhr);
        }
    });
}

/**
 * Make a POST request
 * @param {string} url - API endpoint URL
 * @param {Object} data - Data to send
 * @param {Function} successCallback - Success callback function
 * @param {Function} errorCallback - Error callback function (optional)
 */
function ajaxPost(url, data, successCallback, errorCallback) {
    $.ajax({
        url: url,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function(response) {
            if (response.success) {
                successCallback(response);
            } else {
                Toast.error(response.message || 'Request failed');
                if (errorCallback) errorCallback(response);
            }
        },
        error: function(xhr, status, error) {
            console.error('POST Error:', error);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.error('Request failed');
            }
            if (errorCallback) errorCallback(xhr);
        }
    });
}

/**
 * Make a PUT request
 * @param {string} url - API endpoint URL
 * @param {Object} data - Data to send
 * @param {Function} successCallback - Success callback function
 * @param {Function} errorCallback - Error callback function (optional)
 */
function ajaxPut(url, data, successCallback, errorCallback) {
    $.ajax({
        url: url,
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function(response) {
            if (response.success) {
                successCallback(response);
            } else {
                Toast.error(response.message || 'Request failed');
                if (errorCallback) errorCallback(response);
            }
        },
        error: function(xhr, status, error) {
            console.error('PUT Error:', error);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.error('Request failed');
            }
            if (errorCallback) errorCallback(xhr);
        }
    });
}

/**
 * Make a DELETE request
 * @param {string} url - API endpoint URL
 * @param {Function} successCallback - Success callback function
 * @param {Function} errorCallback - Error callback function (optional)
 */
function ajaxDelete(url, successCallback, errorCallback) {
    $.ajax({
        url: url,
        type: 'DELETE',
        success: function(response) {
            if (response.success) {
                successCallback(response);
            } else {
                Toast.error(response.message || 'Request failed');
                if (errorCallback) errorCallback(response);
            }
        },
        error: function(xhr, status, error) {
            console.error('DELETE Error:', error);
            if (xhr.status === 409) {
                Toast.error('Cannot delete. It may be in use by other records.');
            } else {
                Toast.error('Request failed');
            }
            if (errorCallback) errorCallback(xhr);
        }
    });
}

/**
 * Handle AJAX error with appropriate message
 * @param {Object} xhr - XMLHttpRequest object
 * @param {string} defaultMessage - Default error message
 */
function handleAjaxError(xhr, defaultMessage = 'An error occurred') {
    console.error('AJAX Error:', xhr);
    
    if (xhr.status === 400) {
        Toast.error('Invalid data provided. Please check your input.');
    } else if (xhr.status === 401) {
        Toast.error('Unauthorized. Please log in again.');
    } else if (xhr.status === 403) {
        Toast.error('Access denied.');
    } else if (xhr.status === 404) {
        Toast.error('Resource not found.');
    } else if (xhr.status === 409) {
        Toast.error('Conflict. The resource may be in use.');
    } else if (xhr.status === 500) {
        Toast.error('Server error. Please try again later.');
    } else {
        Toast.error(defaultMessage);
    }
}
