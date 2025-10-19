/**
 * Student ID Pool Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let studentIdPoolTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable
    initializeDataTable();

    // Event Handlers
    $('#btnGenerateIds').on('click', showGenerateModal);
    $('#btnConfirmGenerate').on('click', generateStudentIds);
    $('#btnUpdateRemarks').on('click', updateRemarks);
    $('#btnConfirmDelete').on('click', confirmDelete);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Student ID Pool', getStudentIdPoolData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('student_id_pool', getStudentIdPoolData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('student_id_pool', getStudentIdPoolData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Student ID Pool', getStudentIdPoolData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['generateIdsModal', 'editRemarksModal', 'deleteIdModal']);

    // Clear form when generate modal is hidden
    $('#generateIdsModal').on('hidden.bs.modal', function () {
        $('#generateIdsForm')[0].reset();
        $('#previewText').text('Enter values to see preview');
    });

    // Update preview when form values change
    $('#genYear, #genStartNumber, #genCount').on('input', updatePreview);

    // Set default year to current year
    $('#genYear').val(new Date().getFullYear());
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    studentIdPoolTable = $('#studentIdPoolTable').DataTable({
        ajax: {
            url: '/AdminStudentIdPool/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load student ID pool data.');
            }
        },
        columns: [
            {
                data: 'studentIdCode',
                width: '15%'
            },
            {
                data: 'isUsed',
                width: '12%',
                render: function (data) {
                    if (data) {
                        return `<span class="status-badge used">
                                    <i data-lucide="x-circle" class="status-badge-icon"></i>
                                    Used
                                </span>`;
                    } else {
                        return `<span class="status-badge available">
                                    <i data-lucide="check-circle" class="status-badge-icon"></i>
                                    Available
                                </span>`;
                    }
                }
            },
            {
                data: 'generatedAt',
                width: '18%',
                render: function (data) {
                    return data ? new Date(data).toLocaleString() : 'N/A';
                }
            },
            {
                data: 'usedAt',
                width: '18%',
                render: function (data) {
                    return data ? new Date(data).toLocaleString() : '<span style="color: #9ca3af;">Not used</span>';
                }
            },
            {
                data: 'remarks',
                width: '27%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No remarks</span>';
                }
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    const editBtn = `<button class="btn-action btn-edit" onclick="showEditRemarksModal(${data.poolId})" title="Edit Remarks">
                                        <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                                    </button>`;
                    
                    const deleteBtn = data.isUsed 
                        ? `<button class="btn-action btn-delete" disabled title="Cannot delete used ID">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #9ca3af;"></i>
                            </button>`
                        : `<button class="btn-action btn-delete" onclick="deleteStudentId(${data.poolId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>`;
                    
                    return `<div class="action-buttons">${editBtn}${deleteBtn}</div>`;
                }
            }
        ],
        order: [[0, 'desc']],
        ...commonDataTableConfig,
        language: {
            search: "",
            searchPlaceholder: "Search student IDs...",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "No entries available",
            infoFiltered: "(filtered from _MAX_ total entries)",
            zeroRecords: "No matching entries found",
            emptyTable: "No student IDs in the pool"
        }
    });
}

/**
 * Show Generate IDs Modal
 */
function showGenerateModal() {
    $('#generateIdsForm')[0].reset();
    $('#genYear').val(new Date().getFullYear());
    $('#genStartNumber').val(1);
    $('#genCount').val(50);
    updatePreview();
    showModal('generateIdsModal');
}

/**
 * Update preview text
 */
function updatePreview() {
    const year = $('#genYear').val();
    const startNumber = parseInt($('#genStartNumber').val()) || 0;
    const count = parseInt($('#genCount').val()) || 0;

    if (year && startNumber > 0 && count > 0) {
        const firstId = `${year}-${String(startNumber).padStart(4, '0')}`;
        const lastId = `${year}-${String(startNumber + count - 1).padStart(4, '0')}`;
        $('#previewText').text(`Will generate ${count} IDs from ${firstId} to ${lastId}`);
    } else {
        $('#previewText').text('Enter values to see preview');
    }
}

/**
 * Generate Student IDs
 */
function generateStudentIds() {
    const form = $('#generateIdsForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const count = parseInt($('#genCount').val());
    if (count < 1 || count > 1000) {
        Toast.warning('Count must be between 1 and 1000');
        return;
    }

    const requestData = {
        Year: parseInt($('#genYear').val()),
        StartNumber: parseInt($('#genStartNumber').val()),
        Count: count,
        GeneratedBy: null,
        Remarks: $('#genRemarks').val().trim() || null
    };

    const $btn = $('#btnConfirmGenerate');
    setButtonLoading($btn, 'Generating...');

    ajaxPost('/AdminStudentIdPool/Generate', requestData,
        function (response) {
            Toast.success(response.message);
            hideModal('generateIdsModal');
            studentIdPoolTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Show Edit Remarks Modal
 */
function showEditRemarksModal(poolId) {
    ajaxGet(`/AdminStudentIdPool/GetById/${poolId}`,
        function (response) {
            const pool = response.data;
            $('#editPoolId').val(pool.poolId);
            $('#editStudentIdCode').val(pool.studentIdCode);
            $('#editRemarks').val(pool.remarks || '');
            showModal('editRemarksModal');
        },
        function () {
            Toast.error('Failed to load student ID data.');
        }
    );
}

/**
 * Update Remarks
 */
function updateRemarks() {
    const form = $('#editRemarksForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const requestData = {
        PoolId: parseInt($('#editPoolId').val()),
        Remarks: $('#editRemarks').val().trim() || null
    };

    const $btn = $('#btnUpdateRemarks');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminStudentIdPool/UpdateRemarks', requestData,
        function () {
            Toast.updated('Remarks');
            hideModal('editRemarksModal');
            studentIdPoolTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPut
        }
    );

    resetButton($btn);
}

/**
 * Show Delete Confirmation Modal
 */
function deleteStudentId(poolId) {
    $('#deletePoolId').val(poolId);
    showModal('deleteIdModal');
}

/**
 * Confirm and Delete Student ID
 */
function confirmDelete() {
    const poolId = $('#deletePoolId').val();
    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminStudentIdPool/Delete/${poolId}`,
        function () {
            Toast.deleted('Student ID');
            hideModal('deleteIdModal');
            studentIdPoolTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get student ID pool data for export
 */
function getStudentIdPoolData() {
    const data = [];
    studentIdPoolTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Student ID': rowData.studentIdCode,
            'Status': rowData.isUsed ? 'Used' : 'Available',
            'Generated At': rowData.generatedAt ? new Date(rowData.generatedAt).toLocaleString() : 'N/A',
            'Used At': rowData.usedAt ? new Date(rowData.usedAt).toLocaleString() : 'Not used',
            'Remarks': rowData.remarks || 'No remarks'
        });
    });
    return data;
}
