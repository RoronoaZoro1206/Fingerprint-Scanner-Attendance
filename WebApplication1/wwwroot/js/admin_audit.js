let auditTable;

$(document).ready(function() {
    initializeDataTable();
    loadStatistics();
    
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
});

function initializeDataTable() {
    auditTable = $('#auditTable').DataTable({
        ajax: {
            url: '/AdminAudit/GetAll',
            dataSrc: 'data',
            error: function(xhr, error, code) {
                console.error('DataTables AJAX error:', error, code);
                console.error('Response:', xhr.responseText);
            }
        },
        columns: [
            { data: 'audId' },
            { data: 'adminName' },
            { data: 'adminCode' },
            { 
                data: 'audAction',
                render: function(data) {
                    return formatActionBadge(data);
                }
            },
            { 
                data: 'audDetails',
                render: function(data) {
                    if (!data || data === '') return '<span class="text-muted">No details</span>';
                    return data.length > 50 ? data.substring(0, 50) + '...' : data;
                }
            },
            { data: 'audActionTime' },
            {
                data: null,
                orderable: false,
                render: function(data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-view" onclick="viewAudit(${row.audId})">
                                <i data-lucide="eye"></i>
                                View
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteAudit(${row.audId})">
                                <i data-lucide="trash-2"></i>
                                Delete
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[5, 'desc']],
        pageLength: 25,
        responsive: true,
        language: {
            search: "Search logs:",
            lengthMenu: "Show _MENU_ logs per page",
            info: "Showing _START_ to _END_ of _TOTAL_ logs",
            infoEmpty: "No logs available",
            infoFiltered: "(filtered from _MAX_ total logs)",
            zeroRecords: "No matching logs found"
        },
        drawCallback: function() {
            // Reinitialize Lucide icons after table draw
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

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

async function loadStatistics() {
    try {
        const response = await fetch('/AdminAudit/GetStats');
        const result = await response.json();
        
        console.log('Stats response:', result);
        
        if (result.success) {
            const stats = result.data;
            console.log('Stats data:', stats);
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
            
            const modal = new bootstrap.Modal(document.getElementById('viewModal'));
            modal.show();
            
            // Reinitialize Lucide icons in modal
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        } else {
            showToast(result.message || 'Failed to load audit log', 'error');
        }
    } catch (error) {
        console.error('Error viewing audit:', error);
        showToast('Failed to load audit log', 'error');
    }
}

async function deleteAudit(id) {
    if (!confirm('Are you sure you want to delete this audit log? This action cannot be undone.')) {
        return;
    }
    
    try {
        const response = await fetch(`/AdminAudit/Delete?id=${id}`, {
            method: 'DELETE'
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Audit log deleted successfully', 'success');
            auditTable.ajax.reload();
            loadStatistics();
        } else {
            showToast(result.message || 'Failed to delete audit log', 'error');
        }
    } catch (error) {
        console.error('Error deleting audit:', error);
        showToast('Failed to delete audit log', 'error');
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showToast(message, type = 'info') {
    if (typeof window.showToast === 'function') {
        window.showToast(message, type);
    } else {
        alert(message);
    }
}
