/**
 * SweetAlert2 Wrapper for Status Messages and Confirmations
 * Provides animated, beautiful modals for important user interactions
 * 
 * Usage:
 *   SwalAlert.success("Operation completed!");
 *   SwalAlert.error("Something went wrong!");
 *   SwalAlert.confirm("Delete this item?", () => { ... });
 */

const SwalAlert = {
    /**
  * Show success message with animated checkmark
  * @param {string} title - Main message
     * @param {string} text - Optional detailed message
     */
    success: (title, text = '') => {
    return Swal.fire({
 icon: 'success',
      title: title,
   text: text,
         showConfirmButton: true,
            confirmButtonText: 'OK',
     confirmButtonColor: '#10b981',
            timer: 3000,
      timerProgressBar: true,
            showClass: {
          popup: 'swal2-show',
                backdrop: 'swal2-backdrop-show',
   icon: 'swal2-icon-show'
}
        });
    },

    /**
     * Show error message with animated X icon
     * @param {string} title - Main message
     * @param {string} text - Optional detailed message
     */
    error: (title, text = '') => {
return Swal.fire({
            icon: 'error',
            title: title,
        text: text,
   showConfirmButton: true,
        confirmButtonText: 'OK',
          confirmButtonColor: '#ef4444',
  showClass: {
                popup: 'swal2-show',
        backdrop: 'swal2-backdrop-show',
     icon: 'swal2-icon-show'
            }
    });
    },

    /**
     * Show warning message
     * @param {string} title - Main message
     * @param {string} text - Optional detailed message
     */
    warning: (title, text = '') => {
    return Swal.fire({
         icon: 'warning',
       title: title,
 text: text,
       showConfirmButton: true,
          confirmButtonText: 'OK',
            confirmButtonColor: '#f59e0b',
            showClass: {
    popup: 'swal2-show',
        backdrop: 'swal2-backdrop-show',
                icon: 'swal2-icon-show'
       }
        });
    },

    /**
     * Show info message
     * @param {string} title - Main message
     * @param {string} text - Optional detailed message
     */
    info: (title, text = '') => {
    return Swal.fire({
         icon: 'info',
            title: title,
       text: text,
 showConfirmButton: true,
     confirmButtonText: 'OK',
         confirmButtonColor: '#3b82f6',
          showClass: {
       popup: 'swal2-show',
          backdrop: 'swal2-backdrop-show',
    icon: 'swal2-icon-show'
    }
        });
    },

    /**
     * Show confirmation dialog
     * @param {string} title - Question to ask user
     * @param {function} onConfirm - Callback when user confirms
     * @param {string} confirmText - Text for confirm button
     * @param {string} cancelText - Text for cancel button
     */
    confirm: (title, onConfirm, confirmText = 'Yes, proceed', cancelText = 'Cancel') => {
        return Swal.fire({
            title: title,
            text: "This action cannot be undone.",
   icon: 'warning',
      showCancelButton: true,
        confirmButtonColor: '#ef4444',
  cancelButtonColor: '#6b7280',
      confirmButtonText: confirmText,
            cancelButtonText: cancelText,
       reverseButtons: true,
    showClass: {
       popup: 'swal2-show',
         backdrop: 'swal2-backdrop-show',
     icon: 'swal2-icon-show'
            }
        }).then((result) => {
     if (result.isConfirmed && onConfirm) {
  onConfirm();
        }
        });
    },

  /**
     * Show delete confirmation dialog
 * @param {string} itemName - Name of item being deleted
     * @param {function} onConfirm - Callback when user confirms
     */
    confirmDelete: (itemName, onConfirm) => {
     return Swal.fire({
            title: `Delete ${itemName}?`,
       text: "This action cannot be undone.",
            icon: 'warning',
         showCancelButton: true,
   confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel',
 reverseButtons: true,
 showClass: {
    popup: 'swal2-show',
    backdrop: 'swal2-backdrop-show',
      icon: 'swal2-icon-show'
    }
        }).then((result) => {
      if (result.isConfirmed && onConfirm) {
      onConfirm();
      }
        });
    },

    /**
  * CRUD operation helpers with SweetAlert2
     */
    created: (entity = "Record") => {
   return SwalAlert.success('Success!', `${entity} created successfully!`);
    },

    updated: (entity = "Record") => {
        return SwalAlert.success('Success!', `${entity} updated successfully!`);
    },

 deleted: (entity = "Record") => {
        return SwalAlert.success('Deleted!', `${entity} has been deleted.`);
    },

    saved: (entity = "Record") => {
        return SwalAlert.success('Success!', `${entity} saved successfully!`);
  },

    createError: (entity = "Record") => {
        return SwalAlert.error('Oops...', `Failed to create ${entity.toLowerCase()}.`);
    },

    updateError: (entity = "Record") => {
  return SwalAlert.error('Oops...', `Failed to update ${entity.toLowerCase()}.`);
    },

    deleteError: (entity = "Record") => {
        return SwalAlert.error('Oops...', `Failed to delete ${entity.toLowerCase()}.`);
    },

    loadError: (entity = "Record") => {
    return SwalAlert.error('Oops...', `Failed to load ${entity.toLowerCase()}.`);
    }
};

// Make globally available
window.SwalAlert = SwalAlert;
