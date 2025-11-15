/// <reference path="../global.d.ts" />

/**
 * Program Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let programsTable;
let departmentsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load departments first, then initialize DataTable
    loadDepartments().then(() => {
        initializeDataTable();
    });

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
 * Load Departments for dropdown
 */
async function loadDepartments() {
    try {
        const response = await $.ajax({
            url: '/AdminDepartment/GetAll',
            type: 'GET'
        });

        if (response.data) {
            departmentsData = response.data;
            populateDepartmentDropdowns();
        }
    } catch (error) {
        console.error('Error loading departments:', error);
        Toast.error('Failed to load departments data');
    }
}

/**
 * Populate department dropdowns
 */
function populateDepartmentDropdowns() {
    const addSelect = $('#addDeptId');
    const editSelect = $('#editDeptId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    departmentsData.forEach(dept => {
        // ? FIX: Use snake_case properties matching database
        const option = `<option value="${dept.dept_id}">${dept.dept_name}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Get department name by ID
 */
function getDepartmentName(deptId) {
    if (!deptId) return 'No Department';
    const dept = departmentsData.find(d => d.dept_id === deptId);
    return dept ? dept.dept_name : 'Unknown Department';
}

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
                // ? FIX: Changed from 'programCode' to 'program_code' (snake_case)
                data: 'program_code',
                width: '15%'
            },
            {
                // ? FIX: Changed from 'programName' to 'program_name' (snake_case)
                data: 'program_name',
                width: '25%'
            },
            {
                // ? FIX: Changed from 'programDesc' to 'program_desc' (snake_case)
                data: 'program_desc',
                width: '30%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No description</span>';
                }
            },
            {
                // ? FIX: Changed from 'deptName' to 'dept_name' (snake_case)
                data: 'dept_name',
                width: '15%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No Department</span>';
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    // ? FIX: Changed from data.programId to data.program_id (snake_case)
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.program_id})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteProgram(${data.program_id})" title="Delete">
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
            // ? FIX: Changed to snake_case properties
            $('#editProgramId').val(program.program_id);
            $('#editProgramCode').val(program.program_code);
            $('#editProgramName').val(program.program_name);
            $('#editProgramDesc').val(program.program_desc || '');
            $('#editDeptId').val(program.dept_id || '');
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
        ProgramDesc: $('#addProgramDesc').val().trim() || null,
        DeptId: $('#addDeptId').val() ? parseInt($('#addDeptId').val()) : null
    };

    const $btn = $('#btnSaveProgram');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminProgram/Create', programData,
        function () {
            Toast.created('Program');
            hideModal('addProgramModal');
            programsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
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
        ProgramDesc: $('#editProgramDesc').val().trim() || null,
        DeptId: $('#editDeptId').val() ? parseInt($('#editDeptId').val()) : null
    };

    const $btn = $('#btnUpdateProgram');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminProgram/Update', programData,
        function () {
            Toast.updated('Program');
            hideModal('editProgramModal');
            programsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
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
            Toast deleted('Program');
            hideModal('deleteProgramModal');
            programsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Get programs data for export
 */
function getProgramsData() {
    const data = [];
    programsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        // ? FIX: Changed to snake_case properties
        data.push({
            'Code': rowData.program_code,
            'Name': rowData.program_name,
            'Description': rowData.program_desc || 'No description',
            'Department': rowData.dept_name || 'No Department'
        });
    });
    return data;
}
