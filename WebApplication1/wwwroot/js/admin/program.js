/**
 * Program Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let programsTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable
    initializeDataTable();

    // Event Handlers
    $('#btnAddProgram').on('click', showAddModal);
    $('#btnSaveProgram').on('click', saveProgram);
    $('#btnUpdateProgram').on('click', updateProgram);
    $('#btnConfirmDelete').on('click', confirmDeleteProgram);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Programs', getProgramsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('programs', getProgramsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('programs', getProgramsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Programs', getProgramsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addProgramModal', 'editProgramModal', 'deleteProgramModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addProgramModal', 'addProgramForm');
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    programsTable = $('#programsTable').DataTable({
        ajax: {
            url: '/AdminProgram/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load programs data.');
            }
        },
        columns: [
            {
                data: 'programCode',
                width: '15%'
            },
            {
                data: 'programName',
                width: '25%'
            },
            {
                data: 'programDesc',
                width: '45%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No description</span>';
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.programId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteProgram(${data.programId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('programs', 'program')
    });
}

/**
 * Show Add Program Modal
 */
function showAddModal() {
    $('#addProgramForm')[0].reset();
    showModal('addProgramModal');
}

/**
 * Show Edit Program Modal
 */
function showEditModal(programId) {
    ajaxGet(`/AdminProgram/GetById/${programId}`,
        function (response) {
            const program = response.data;
            $('#editProgramId').val(program.programId);
            $('#editProgramCode').val(program.programCode);
            $('#editProgramName').val(program.programName);
            $('#editProgramDesc').val(program.programDesc || '');
            showModal('editProgramModal');
        },
        function () {
            Toast.error('Failed to load program data.');
        }
    );
}

/**
 * Save New Program
 */
function saveProgram() {
    const form = $('#addProgramForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const programCode = $('#addProgramCode').val().trim();
    if (programCode.length < 2) {
        Toast.warning('Program code must be at least 2 characters.');
        return;
    }

    const programData = {
        ProgramCode: programCode.toUpperCase(),
        ProgramName: $('#addProgramName').val().trim(),
        ProgramDesc: $('#addProgramDesc').val().trim() || null
    };

    const $btn = $('#btnSaveProgram');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminProgram/Create', programData,
        function () {
            Toast.created('Program');
            hideModal('addProgramModal');
            programsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Update Existing Program
 */
function updateProgram() {
    const form = $('#editProgramForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const programCode = $('#editProgramCode').val().trim();
    if (programCode.length < 2) {
        Toast.warning('Program code must be at least 2 characters.');
        return;
    }

    const programData = {
        ProgramId: parseInt($('#editProgramId').val()),
        ProgramCode: programCode.toUpperCase(),
        ProgramName: $('#editProgramName').val().trim(),
        ProgramDesc: $('#editProgramDesc').val().trim() || null
    };

    const $btn = $('#btnUpdateProgram');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminProgram/Update', programData,
        function () {
            Toast.updated('Program');
            hideModal('editProgramModal');
            programsTable.ajax.reload(null, false);
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
function deleteProgram(programId) {
    $('#deleteProgramId').val(programId);
    showModal('deleteProgramModal');
}

/**
 * Confirm and Delete Program
 */
function confirmDeleteProgram() {
    const programId = $('#deleteProgramId').val();

    if (!programId) {
        Toast.error('Invalid program ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminProgram/Delete/${programId}`,
        function () {
            Toast.deleted('Program');
            hideModal('deleteProgramModal');
            programsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get programs data for export
 */
function getProgramsData() {
    const data = [];
    programsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Code': rowData.programCode,
            'Name': rowData.programName,
            'Description': rowData.programDesc || 'No description'
        });
    });
    return data;
}
