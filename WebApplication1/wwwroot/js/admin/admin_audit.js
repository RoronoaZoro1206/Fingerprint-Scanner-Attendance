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
            { data: 'audId', width: '6%' },
            { data: 'adminName', width: '13%' },
            { data: 'adminCode', width: '10%' },
            // NEW: Log Type column with badges
            {
                data: 'logType',
                width: '12%',
                render: function (data) {
                    if (data === 'Biometric Login') {
                        return `<span class="log-type-badge biometric"><i data-lucide="fingerprint" class="icon-xs"></i> Biometric</span>`;
                    } else {
                        return `<span class="log-type-badge admin"><i data-lucide="shield" class="icon-xs"></i> Admin</span>`;
                    }
                }
            },
            {
                data: 'audAction',
                width: '13%',
                render: function (data) {
                    return formatActionBadge(data);
                }
            },
            {
                data: 'audDetails',
                width: '25%',
                render: function (data) {
                    if (!data || data === '') return '<span class="text-muted">No details</span>';
                    return data.length > 50 ? data.substring(0, 50) + '...' : data;
                }
            },
            { data: 'audActionTime', width: '13%' },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-view" onclick="viewAudit(${row.audId})" title="View Details">
                                <i data-lucide="eye" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteAudit(${row.audId})" title="Delete">
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
 */
async function viewAudit(id) {
    try {
        const response = await fetch(`/AdminAudit/GetById?id=${id}`);
        const result = await response.json();

        if (result.success) {
            const audit = result.data;
            document.getElementById('viewAudId').textContent = audit.audId;
            document.getElementById('viewAdminName').textContent = audit.adminName;
            document.getElementById('viewAdminCode').textContent = audit.adminCode;
            document.getElementById('viewAction').textContent = audit.audAction;
            document.getElementById('viewDetails').textContent = audit.audDetails || 'No details provided';
            document.getElementById('viewDateTime').textContent = audit.audActionTime;

            showModal('viewModal');
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
 */
function deleteAudit(id) {
    document.getElementById('deleteAudId').value = id;
    showModal('deleteAuditModal');
}

/**
 * Confirm and delete audit log
 */
async function confirmDeleteAudit() {
    const id = document.getElementById('deleteAudId').value;

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    try {
        const response = await fetch(`/AdminAudit/Delete?id=${id}`, {
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
