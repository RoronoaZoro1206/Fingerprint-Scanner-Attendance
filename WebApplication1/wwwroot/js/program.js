/**
 * Program Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let programsTable;

$(document).ready(function () {
    // Initialize DataTable
    initializeDataTable();

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddProgram').on('click', showAddModal);
    $('#btnSaveProgram').on('click', saveProgram);
    $('#btnUpdateProgram').on('click', updateProgram);
    $('#btnConfirmDelete').on('click', confirmDeleteProgram);

    // Export button handlers
    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        programsTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        programsTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        programsTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        programsTable.button('.buttons-pdf').trigger();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events - reinitialize icons when modal is shown
    $('#addProgramModal, #editProgramModal, #deleteProgramModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addProgramModal').on('hidden.bs.modal', function () {
        $('#addProgramForm')[0].reset();
    });
});

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    programsTable = $('#programsTable').DataTable({
        ajax: {
            url: '/Program/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
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
                render: function (data, type, row) {
                    return data || '<span style="color: #9ca3af;">No description</span>';
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.programId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteProgram(${row.programId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        buttons: [
            {
                extend: 'excel',
                text: '<i data-lucide="file-spreadsheet" class="icon-sm"></i> Excel',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2]
                }
            },
            {
                extend: 'csv',
                text: '<i data-lucide="file-text" class="icon-sm"></i> CSV',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2]
                }
            },
            {
                extend: 'pdf',
                text: '<i data-lucide="file" class="icon-sm"></i> PDF',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2]
                },
                customize: function (doc) {
                    doc.content[1].table.widths = ['15%', '25%', '60%'];
                    doc.styles.tableHeader.fillColor = '#f9fafb';
                    doc.styles.tableHeader.color = '#374151';
                }
            },
            {
                extend: 'print',
                text: '<i data-lucide="printer" class="icon-sm"></i> Print',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2]
                }
            }
        ],
        drawCallback: function () {
            // Reinitialize Lucide icons after table redraw
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search programs...",
            lengthMenu: "Show _MENU_ programs",
            info: "Showing _START_ to _END_ of _TOTAL_ programs",
            infoEmpty: "No programs available",
            infoFiltered: "(filtered from _MAX_ total programs)",
            zeroRecords: "No matching programs found",
            emptyTable: "No programs available in the system"
        },
        initComplete: function () {
            // Add search icon to the search input
            const searchInput = $('.dataTables_filter input');
            
            // Check if icon hasn't been added yet
            if (searchInput.parent().find('.search-icon').length === 0) {
                // Wrap input in a container
                searchInput.wrap('<div class="search-input-wrapper"></div>');
                
                // Add search icon before the input
                searchInput.before('<i data-lucide="search" class="search-icon"></i>');
                
                // Reinitialize Lucide icons
                setTimeout(function() {
                    if (typeof lucide !== 'undefined') {
                        lucide.createIcons();
                    }
                }, 100);
            }
        }
    });
}

/**
 * Show Add Program Modal
 */
function showAddModal() {
    $('#addProgramForm')[0].reset();
    $('#addProgramModal').modal('show');
}

/**
 * Show Edit Program Modal
 */
function showEditModal(programId) {
    $.ajax({
        url: `/Program/GetById/${programId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const program = response.data;
                $('#editProgramId').val(program.programId);
                $('#editProgramCode').val(program.programCode);
                $('#editProgramName').val(program.programName);
                $('#editProgramDesc').val(program.programDesc || '');
                $('#editProgramModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load program data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load program data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Program
 */
function saveProgram() {
    const form = $('#addProgramForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const programCode = $('#addProgramCode').val().trim();
    const programName = $('#addProgramName').val().trim();
    const programDesc = $('#addProgramDesc').val().trim();

    // Additional validation
    if (!programCode || !programName) {
        Toast.warning('Program code and name are required.');
        return;
    }

    if (programCode.length < 2) {
        Toast.warning('Program code must be at least 2 characters.');
        return;
    }

    const programData = {
        ProgramCode: programCode.toUpperCase(),
        ProgramName: programName,
        ProgramDesc: programDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveProgram');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Program/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(programData),
        success: function (response) {
            if (response.success) {
                Toast.created('Program');
                $('#addProgramModal').modal('hide');
                programsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create program.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Program');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Program');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Program
 */
function updateProgram() {
    const form = $('#editProgramForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const programId = $('#editProgramId').val();
    const programCode = $('#editProgramCode').val().trim();
    const programName = $('#editProgramName').val().trim();
    const programDesc = $('#editProgramDesc').val().trim();

    // Additional validation
    if (!programCode || !programName) {
        Toast.warning('Program code and name are required.');
        return;
    }

    if (programCode.length < 2) {
        Toast.warning('Program code must be at least 2 characters.');
        return;
    }

    const programData = {
        ProgramId: parseInt(programId),
        ProgramCode: programCode.toUpperCase(),
        ProgramName: programName,
        ProgramDesc: programDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateProgram');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Program/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(programData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Program');
                $('#editProgramModal').modal('hide');
                programsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update program.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Program');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Program');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteProgram(programId) {
    // Store the program ID
    $('#deleteProgramId').val(programId);
    
    // Show the confirmation modal
    $('#deleteProgramModal').modal('show');
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

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Program/Delete/${programId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Program');
                $('#deleteProgramModal').modal('hide');
                programsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete program.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete program. It may be in use by other records.');
            } else {
                Toast.deleteError('Program');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}
