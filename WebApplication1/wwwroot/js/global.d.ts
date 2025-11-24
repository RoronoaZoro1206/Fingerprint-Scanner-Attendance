/**
 * Global Type Definitions
 * Declares types for libraries and utilities used globally in the application
 */

// jQuery (already installed via npm)
/// <reference types="jquery" />

// Bootstrap types
interface JQuery {
    modal(action: 'show' | 'hide' | 'toggle'): JQuery;
    modal(options?: any): JQuery;
    DataTable(options?: any): DataTables.Api;
}

// Lucide Icons
declare const lucide: {
    createIcons(): void;
};

// Toast Notification System
declare const Toast: {
    success(message: string): void;
    error(message: string): void;
    warning(message: string): void;
    info(message: string): void;
    created(entityName: string): void;
    updated(entityName: string): void;
    deleted(entityName: string): void;
    updateError(entityName: string): void;
    deleteError(entityName: string): void;
};

// SweetAlert2 Wrapper
declare const SwalAlert: {
    success(title: string, text?: string): Promise<any>;
    error(title: string, text?: string): Promise<any>;
    warning(title: string, text?: string): Promise<any>;
    info(title: string, text?: string): Promise<any>;
    confirm(
        title: string,
        onConfirm: () => void,
        confirmButtonText?: string,
        cancelButtonText?: string,
        text?: string
    ): Promise<any>;
    delete(title: string, onConfirm: () => void, text?: string): Promise<any>;
};

// Common Utility Functions (from modal-utils.js)
declare function initializeLucideIcons(): void;
declare function showModal(modalId: string): void;
declare function hideModal(modalId: string): void;
declare function setupModalIconReinit(modalIds: string[]): void;
declare function setupFormClearOnHide(modalId: string, formId: string): void;
declare function setButtonLoading($button: JQuery, loadingText: string): void;
declare function resetButton($button: JQuery): void;

// Common AJAX Functions (from ajax-utils.js)
interface AjaxResponse {
    success: boolean;
    message?: string;
    data?: any;
}

declare function ajaxGet(
    url: string,
    successCallback: (response: AjaxResponse) => void,
    errorCallback?: (error: any) => void
): void;

declare function ajaxPost(
    url: string,
    data: any,
    successCallback: (response: AjaxResponse) => void,
    errorCallback?: (error: any) => void
): void;

declare function ajaxPut(
    url: string,
    data: any,
    successCallback: (response: AjaxResponse) => void,
    errorCallback?: (error: any) => void
): void;

declare function ajaxDelete(
    url: string,
    successCallback: (response: AjaxResponse) => void,
    errorCallback?: (error: any) => void,
    data?: any
): void;

declare function handleAjaxError(xhr: JQuery.jqXHR, defaultMessage?: string): void;

// Export Utility Functions (from export-utils.js)
declare function printData(title: string, data: any[]): void;
declare function exportToExcelFile(filename: string, data: any[]): void;
declare function exportToCSVFile(filename: string, data: any[]): void;
declare function exportToPDFFile(title: string, data: any[]): void;

// DataTable Configuration (from datatable-init.js)
declare const commonDataTableConfig: DataTables.Settings;
declare function getDataTableLanguage(entityName: string, entitySingular: string): any;
declare function formatDate(dateString: string): string;
declare function formatDateForInput(dateString: string): string;

// Form Validation Functions (from form-validation.js)
declare function validateForm(form: HTMLFormElement): boolean;
declare function validateRequiredFields(fields: string[], message?: string): boolean;
declare function validateMinLength(value: string, minLength: number, fieldName: string): boolean;
declare function validateRange(value: number, min: number, max: number, fieldName: string): boolean;
declare function validateDateRange(startDate: string, endDate: string, message?: string): boolean;
declare function trimObjectValues(obj: any): any;

// UI Utility Functions (from ui-utils.js)
declare function showLoading(containerId: string, message?: string): void;
declare function showEmptyState(containerId: string, title: string, message: string, iconName?: string): void;
declare function showErrorState(containerId: string, message: string): void;
declare function toggleElement(elementId: string, show: boolean): void;
declare function scrollToTop(): void;
declare function scrollToElement(elementId: string): void;
declare function debounce(func: Function, wait: number): Function;
declare function copyToClipboard(text: string): Promise<boolean>;
declare function formatNumber(num: number): string;
declare function truncateText(text: string, maxLength: number): string;

// Window extensions for admin functions
interface Window {
    viewAudit?: (id: string) => Promise<void>;
    deleteAudit?: (id: string) => void;
    showEditModal?: (id: number) => void;
    deleteClass?: (id: number) => void;
    deleteSchedule?: (id: number) => void;
    showEditModalForClass?: (classId: number) => void;
    deleteRoom?: (id: number) => void;
    deleteProgram?: (id: number) => void;
    deleteCourse?: (id: number) => void;
    showUnenrollModal?: (enrollId: number) => void;
    enrollInClass?: (classId: number) => void;
}
