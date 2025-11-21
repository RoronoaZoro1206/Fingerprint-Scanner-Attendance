/// <reference path="../global.d.ts" />

/**
 * Admin Audit - Refactored to use common utilities
 * Uses common AJAX, modal, and DataTable utilities
 * Read-only page with view and delete functionality
 */

let auditTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable and load statistics
    initializeDataTable();
    loadStatistics();

    // Event handler for confirm delete button
    $('#btnConfirmDelete').on('click', confirmDeleteAudit);

    // Setup modal icon reinitialization
    setupModalIconReinit(['deleteAuditModal', 'viewModal']);
});

/**
 * Initialize DataTable
 * ✅ FIX: Updated to use PascalCase property names to match JSON from controller
 */
function initializeDataTable() {
    auditTable = $('#auditTable').DataTable({
        ajax: {
            url: '/AdminAudit/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load audit logs.');
            }
        },
        columns: [
            { data: 'AudId', width: '8%' },              // ✅ FIX: PascalCase (was audId)
            { data: 'UserName', width: '13%' },          // ✅ FIX: PascalCase (was userName)
            { data: 'UserRole', width: '10%' },          // ✅ FIX: PascalCase (was userRole)
            // NEW: Log Type column with badges
            {
                data: 'LogType',                         // ✅ FIX: PascalCase (was logType)
                width: '12%',
                render: function (data) {
                    const badges = {
                        'Password Login': '<span class="log-type-badge password"><i data-lucide="key" class="icon-xs"></i> Password</span>',
                        'Biometric Login': '<span class="log-type-badge biometric"><i data-lucide="fingerprint" class="icon-xs"></i> Biometric</span>',
                        'Failed Login': '<span class="log-type-badge failed"><i data-lucide="alert-circle" class="icon-xs"></i> Failed</span>',
                        'User Logout': '<span class="log-type-badge logout"><i data-lucide="log-out" class="icon-xs"></i> Logout</span>',
                        'Admin Action': '<span class="log-type-badge admin"><i data-lucide="shield" class="icon-xs"></i> Admin</span>',
                        'Authentication': '<span class="log-type-badge auth"><i data-lucide="lock" class="icon-xs"></i> Auth</span>'
                    };
                    return badges[data] || `<span class="log-type-badge other"><i data-lucide="info" class="icon-xs"></i> ${data}</span>`;
                }
            },
            {
                data: 'AudAction',                       // ✅ FIX: PascalCase (was audAction)
                width: '13%',
                render: function (data) {
                    return formatActionBadge(data);
                }
            },
            {
                data: 'AudDetails',                      // ✅ FIX: PascalCase (was audDetails)
                width: '23%',
                render: function (data) {
                    if (!data || data === '') return '<span class="text-muted">No details</span>';
                    return data.length > 50 ? data.substring(0, 50) + '...' : data;
                }
            },
            { 
                data: 'AudActionTimeFormatted',          // ✅ FIX: PascalCase (was audActionTimeFormatted)
                width: '13%' 
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // ✅ FIX: Use row.AudId (PascalCase)
                    const escapedId = String(row.AudId).replace(/'/g, "\\'");

                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-view" onclick="viewAudit('${escapedId}')" title="View Details">
                                <i data-lucide="eye" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteAudit('${escapedId}')" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[6, 'desc']], // Order by date/time descending (column index adjusted)
        pageLength: 25,
        ...commonDataTableConfig,
        language: getDataTableLanguage('logs', 'log'),
        // Reinitialize icons after table draw
        drawCallback: function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Format action badge with color coding
 */
function formatActionBadge(action) {
    const lowerAction = action.toLowerCase();
    let badgeClass = 'other';

    if (lowerAction.includes('create') || lowerAction.includes('add')) {
        badgeClass = 'create';
    } else if (lowerAction.includes('update') || lowerAction.includes('edit')) {
        badgeClass = 'update';
    } else if (lowerAction.includes('delete') || lowerAction.includes('remove')) {
        badgeClass = 'delete';
    } else if (lowerAction.includes('view') || lowerAction.includes('read')) {
        badgeClass = 'view';
    } else if (lowerAction.includes('login')) {
        badgeClass = 'login';
    } else if (lowerAction.includes('logout')) {
        badgeClass = 'logout';
    }

    return `<span class="action-badge ${badgeClass}">${escapeHtml(action)}</span>`;
}

/**
 * Load audit statistics
 */
async function loadStatistics() {
    try {
        const response = await fetch('/AdminAudit/GetStats');
        const result = await response.json();

        if (result.success) {
            const stats = result.data;
            document.getElementById('totalLogs').textContent = stats.totalLogs;
            document.getElementById('todayLogs').textContent = stats.todayLogs;
            document.getElementById('weekLogs').textContent = stats.weekLogs;
            document.getElementById('monthLogs').textContent = stats.monthLogs;
        } else {
            console.error('Stats request failed:', result.message);
        }
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

/**
 * View audit log details
 * @param {string} id - Audit ID (can be "ADM-123" or "AUTH-456")
 */
window.viewAudit = async function(id) {
    try {
        // ✅ Parse the ID to extract the numeric part and type
        const isAuthLog = id.startsWith('AUTH-');
        const numericId = id.replace(/^(ADM-|AUTH-)/, '');

        const response = await fetch(`/AdminAudit/GetById/${numericId}?isAuth=${isAuthLog}`);
        const result = await response.json();

        if (result.success) {
            const audit = result.data;
            document.getElementById('viewAudId').textContent = id; // Show full ID with prefix
            document.getElementById('viewUserName').textContent = audit.userName;  // ✅ camelCase from controller
            document.getElementById('viewUserRole').textContent = audit.userRole;  // ✅ camelCase from controller
            document.getElementById('viewAction').textContent = audit.audAction;    // ✅ camelCase from controller
            document.getElementById('viewDetails').textContent = audit.audDetails || 'No details provided';
            document.getElementById('viewDateTime').textContent = audit.audActionTime;

            showModal('viewModal');
            
            // Reinitialize icons in modal
            setTimeout(() => {
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }, 100);
        } else {
            Toast.error(result.message || 'Failed to load audit log');
        }
    } catch (error) {
        console.error('Error viewing audit:', error);
        Toast.error('Failed to load audit log');
    }
}

/**
 * Show delete confirmation modal
 * @param {string} id - Audit ID (can be "ADM-123" or "AUTH-456")
 */
window.deleteAudit = function(id) {
    document.getElementById('deleteAudId').value = id;
    showModal('deleteAuditModal');
    
    // Reinitialize icons in modal
    setTimeout(() => {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }, 100);
}

/**
 * Confirm and delete audit log
 */
async function confirmDeleteAudit() {
    const id = document.getElementById('deleteAudId').value;

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    try {
        // ✅ Parse the ID to extract the numeric part and type
        const isAuthLog = id.startsWith('AUTH-');
        const numericId = id.replace(/^(ADM-|AUTH-)/, '');

        // ✅ FIX: Use DELETE method with route parameter
        const response = await fetch(`/AdminAudit/Delete/${numericId}?isAuth=${isAuthLog}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (result.success) {
            Toast.deleted('Audit log');
            hideModal('deleteAuditModal');
            auditTable.ajax.reload(null, false);
            loadStatistics();
            resetButton($btn);
        } else {
            Toast.error(result.message || 'Failed to delete audit log');
            resetButton($btn);
        }
    } catch (error) {
        console.error('Error deleting audit:', error);
        Toast.error('Failed to delete audit log');
        resetButton($btn);
    }
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
