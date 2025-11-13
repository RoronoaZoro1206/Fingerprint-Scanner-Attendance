/**
 * Academic Term Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let termsTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable
    initializeDataTable();

    // Event Handlers
    $('#btnAddTerm').on('click', showAddModal);
    $('#btnSaveTerm').on('click', saveTerm);
    $('#btnUpdateTerm').on('click', updateTerm);
    $('#btnConfirmDelete').on('click', confirmDeleteTerm);
    $('#btnConfirmSetActive').on('click', confirmSetActiveTerm);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Academic Terms', getTermsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('academic_terms', getTermsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('academic_terms', getTermsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Academic Terms', getTermsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addTermModal', 'editTermModal', 'deleteTermModal', 'setActiveModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addTermModal', 'addTermForm');
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    termsTable = $('#termsTable').DataTable({
        ajax: {
            url: '/AdminAcademicTerm/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load academic terms data.');
            }
        },
        columns: [
            {
                // ? FIX: Changed from 'termName' to 'term_name' (snake_case)
                data: 'term_name',
                width: '30%'
            },
            {
                // ? FIX: Changed from 'startDate' to 'start_date' (snake_case)
                data: 'start_date',
                width: '20%',
                render: function (data) {
                    return formatDate(data);
                }
            },
            {
                // ? FIX: Changed from 'endDate' to 'end_date' (snake_case)
                data: 'end_date',
                width: '20%',
                render: function (data) {
                    return formatDate(data);
                }
            },
            {
                // ? FIX: Changed from 'isActive' to 'is_active' (snake_case)
                data: 'is_active',
                width: '15%',
                render: function (data) {
                    if (data) {
                        return '<span class="status-badge active"><span class="status-dot"></span>Active</span>';
                    } else {
                        return '<span class="status-badge inactive"><span class="status-dot"></span>Inactive</span>';
                    }
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    // ? FIX: Changed from data.isActive to data.is_active (snake_case)
                    // ? FIX: Changed from data.termId to data.term_id (snake_case)
                    const setActiveBtn = !data.is_active ?
                        `<button class="btn-action btn-activate" onclick="setActiveTerm(${data.term_id})" title="Set as Active">
                            <i data-lucide="check-circle" class="icon-sm" style="color: #10b981;"></i>
                        </button>` : '';

                    return `
                        <div class="action-buttons">
                            ${setActiveBtn}
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.term_id})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteTerm(${data.term_id})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[1, 'desc']], // Order by start date descending
        ...commonDataTableConfig,
        language: getDataTableLanguage('terms', 'term')
    });
}

/**
 * Show Add Term Modal
 */
function showAddModal() {
    $('#addTermForm')[0].reset();
    showModal('addTermModal');
}

/**
 * Show Edit Term Modal
 */
function showEditModal(termId) {
    ajaxGet(`/AdminAcademicTerm/GetById/${termId}`,
        function (response) {
            const term = response.data;
            // ? FIX: Changed to snake_case properties
            $('#editTermId').val(term.term_id);
            $('#editTermName').val(term.term_name);
            $('#editStartDate').val(formatDateForInput(term.start_date));
            $('#editEndDate').val(formatDateForInput(term.end_date));
            $('#editIsActive').prop('checked', term.is_active);
            showModal('editTermModal');
        },
        function () {
            Toast.error('Failed to load academic term data.');
        }
    );
}

/**
 * Save New Term
 */
function saveTerm() {
    const form = $('#addTermForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const startDate = $('#addStartDate').val();
    const endDate = $('#addEndDate').val();

    if (new Date(endDate) <= new Date(startDate)) {
        Toast.warning('End date must be after start date.');
        return;
    }

    const termData = {
        TermName: $('#addTermName').val().trim(),
        StartDate: startDate,
        EndDate: endDate,
        IsActive: $('#addIsActive').is(':checked')
    };

    const $btn = $('#btnSaveTerm');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminAcademicTerm/Create', termData,
        function () {
            Toast.created('Academic Term');
            hideModal('addTermModal');
            termsTable.ajax.reload(null, false);
            
            // Trigger navbar update if the new term is active
            if (termData.IsActive && typeof window.refreshAcademicTerm === 'function') {
                window.refreshAcademicTerm();
            }
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Update Existing Term
 */
function updateTerm() {
    const form = $('#editTermForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const startDate = $('#editStartDate').val();
    const endDate = $('#editEndDate').val();

    if (new Date(endDate) <= new Date(startDate)) {
        Toast.warning('End date must be after start date.');
        return;
    }

    const isActive = $('#editIsActive').is(':checked');
    const termData = {
        TermId: parseInt($('#editTermId').val()),
        TermName: $('#editTermName').val().trim(),
        StartDate: startDate,
        EndDate: endDate,
        IsActive: isActive
    };

    const $btn = $('#btnUpdateTerm');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminAcademicTerm/Update', termData,
        function () {
            Toast.updated('Academic Term');
            hideModal('editTermModal');
            termsTable.ajax.reload(null, false);
            
            // Trigger navbar update if active status changed
            if (isActive && typeof window.refreshAcademicTerm === 'function') {
                window.refreshAcademicTerm();
            }
        },
        function () {
            // Error already handled by ajaxPut
        }
    );

    resetButton($btn);
}

/**
 * Show Set Active Confirmation Modal
 */
function setActiveTerm(termId) {
    $('#setActiveTermId').val(termId);
    showModal('setActiveModal');
}

/**
 * Confirm and Set Active Term
 */
function confirmSetActiveTerm() {
    const termId = $('#setActiveTermId').val();

    if (!termId) {
        Toast.error('Invalid term ID.');
        return;
    }

    const $btn = $('#btnConfirmSetActive');
    setButtonLoading($btn, 'Setting...');

    ajaxPost(`/AdminAcademicTerm/SetActive/${termId}`, {},
        function () {
            Toast.success('Active term set successfully!');
            hideModal('setActiveModal');
            termsTable.ajax.reload(null, false);
            
            // Trigger navbar update
            if (typeof window.refreshAcademicTerm === 'function') {
                window.refreshAcademicTerm();
            }
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Show Delete Confirmation Modal
 */
function deleteTerm(termId) {
    $('#deleteTermId').val(termId);
    showModal('deleteTermModal');
}

/**
 * Confirm and Delete Term
 */
function confirmDeleteTerm() {
    const termId = $('#deleteTermId').val();

    if (!termId) {
        Toast.error('Invalid term ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminAcademicTerm/Delete/${termId}`,
        function () {
            Toast.deleted('Academic Term');
            hideModal('deleteTermModal');
            termsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get terms data for export
 */
function getTermsData() {
    const data = [];
    termsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        // ? FIX: Changed to snake_case properties
        data.push({
            'Term Name': rowData.term_name,
            'Start Date': formatDate(rowData.start_date),
            'End Date': formatDate(rowData.end_date),
            'Status': rowData.is_active ? 'Active' : 'Inactive'
        });
    });
    return data;
}
