/**
 * Common Modal Utilities
 * Shared modal handling functions
 */

/**
 * Show modal and reinitialize icons
 * @param {string} modalId - ID of the modal element
 */
function showModal(modalId) {
    $(`#${modalId}`).modal('show');
    
    // Reinitialize icons when modal is shown
    $(`#${modalId}`).on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });
}

/**
 * Hide modal
 * @param {string} modalId - ID of the modal element
 */
function hideModal(modalId) {
    $(`#${modalId}`).modal('hide');
}

/**
 * Setup modal icon reinitialization for multiple modals
 * @param {Array} modalIds - Array of modal IDs
 */
function setupModalIconReinit(modalIds) {
    modalIds.forEach(modalId => {
        $(`#${modalId}`).on('shown.bs.modal', function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        });
    });
}

/**
 * Clear form when modal is hidden
 * @param {string} modalId - ID of the modal element
 * @param {string} formId - ID of the form element
 */
function setupFormClearOnHide(modalId, formId) {
    $(`#${modalId}`).on('hidden.bs.modal', function () {
        $(`#${formId}`)[0].reset();
    });
}

/**
 * Disable button with loading state
 * @param {jQuery} $button - jQuery button element
 * @param {string} loadingText - Text to show while loading
 */
function setButtonLoading($button, loadingText = 'Loading...') {
    const originalHtml = $button.html();
    $button.data('original-html', originalHtml);
    $button.prop('disabled', true).html(`<span class="spinner-border spinner-border-sm me-2"></span>${loadingText}`);
}

/**
 * Reset button to original state
 * @param {jQuery} $button - jQuery button element
 */
function resetButton($button) {
    const originalHtml = $button.data('original-html');
    $button.prop('disabled', false).html(originalHtml);
    
    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}
