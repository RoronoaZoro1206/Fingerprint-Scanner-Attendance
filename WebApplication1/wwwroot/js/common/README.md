# Common JavaScript Files

This directory contains shared JavaScript utilities used across Admin, Faculty, and Student modules to eliminate code duplication and improve maintainability.

## Files

### Core Utilities
- **form-validation.js** - Form validation helper functions
- **modal-utils.js** - Modal handling utilities
- **export-utils.js** - Export functions (print, CSV, Excel, PDF)
- **datatable-init.js** - DataTable initialization and configuration
- **ajax-utils.js** - AJAX request wrappers
- **ui-utils.js** - UI helper functions (loading states, empty states, etc.)

## Usage

Include these common JavaScript files in your layout files before role-specific JS:

```html
<!-- Common Scripts -->
<script src="~/js/common/form-validation.js"></script>
<script src="~/js/common/modal-utils.js"></script>
<script src="~/js/common/export-utils.js"></script>
<script src="~/js/common/datatable-init.js"></script>
<script src="~/js/common/ajax-utils.js"></script>
<script src="~/js/common/ui-utils.js"></script>

<!-- Role-specific scripts -->
<script src="~/js/admin/specific-page.js"></script>
```

## Available Functions

### Form Validation (form-validation.js)
- `validateForm(form)` - Validate HTML5 form
- `validateRequiredFields(fields, message)` - Check required fields
- `validateMinLength(value, minLength, fieldName)` - Validate minimum length
- `validateRange(value, min, max, fieldName)` - Validate number range
- `validateDateRange(startDate, endDate, message)` - Validate date range
- `trimObjectValues(obj)` - Trim all string values in object

### Modal Utils (modal-utils.js)
- `showModal(modalId)` - Show modal with icon reinitialization
- `hideModal(modalId)` - Hide modal
- `setupModalIconReinit(modalIds)` - Setup icon reinit for multiple modals
- `setupFormClearOnHide(modalId, formId)` - Clear form when modal closes
- `setButtonLoading($button, loadingText)` - Set button to loading state
- `resetButton($button)` - Reset button to original state

### Export Utils (export-utils.js)
- `printData(title, data)` - Print data in formatted table
- `exportToCSVFile(filename, data)` - Export to CSV
- `exportToExcelFile(filename, data)` - Export to Excel
- `exportToPDFFile(title, data)` - Export to PDF (fallback to print)

### DataTable Init (datatable-init.js)
- `initializeLucideIcons()` - Initialize Lucide icons
- `addSearchIcon()` - Add search icon to DataTables
- `commonDataTableConfig` - Common DataTable configuration object
- `getDataTableLanguage(entityName, entitySingular)` - Get language config
- `formatDate(dateString)` - Format date for display
- `formatDateForInput(dateString)` - Format date for input

### AJAX Utils (ajax-utils.js)
- `ajaxGet(url, successCallback, errorCallback)` - GET request
- `ajaxPost(url, data, successCallback, errorCallback)` - POST request
- `ajaxPut(url, data, successCallback, errorCallback)` - PUT request
- `ajaxDelete(url, successCallback, errorCallback)` - DELETE request
- `handleAjaxError(xhr, defaultMessage)` - Handle AJAX errors

### UI Utils (ui-utils.js)
- `showLoading(containerId, message)` - Show loading state
- `showEmptyState(containerId, title, message, iconName)` - Show empty state
- `showErrorState(containerId, message)` - Show error state
- `toggleElement(elementId, show)` - Toggle element visibility
- `scrollToTop()` - Scroll to top of page
- `scrollToElement(elementId)` - Scroll to specific element
- `debounce(func, wait)` - Debounce function
- `copyToClipboard(text)` - Copy text to clipboard
- `formatNumber(num)` - Format number with commas
- `truncateText(text, maxLength)` - Truncate text

## Benefits

1. **Code Reusability** - Common functions defined once and reused
2. **Easier Maintenance** - Update logic in one place
3. **Consistency** - Ensures consistent behavior across modules
4. **Reduced Bundle Size** - Less duplicate JavaScript code
5. **Better Testing** - Centralized functions are easier to test

## Customization

Role-specific JavaScript should be placed in their respective directories:
- `wwwroot/js/admin/` - Admin-specific scripts
- `wwwroot/js/faculty/` - Faculty-specific scripts
- `wwwroot/js/student/` - Student-specific scripts

These role-specific files can use the common utilities and add custom functionality.
