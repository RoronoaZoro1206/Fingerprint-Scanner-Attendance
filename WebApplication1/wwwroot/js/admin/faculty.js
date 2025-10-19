/**
 * Faculty Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let facultiesTable;
let usersData = [];
let departmentsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load users and departments, then initialize DataTable
    Promise.all([loadUsers(), loadDepartments()]).then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddFaculty').on('click', showAddModal);
    $('#btnSaveFaculty').on('click', saveFaculty);
    $('#btnUpdateFaculty').on('click', updateFaculty);
    $('#btnConfirmDelete').on('click', confirmDeleteFaculty);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Faculty', getFacultiesData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('faculty', getFacultiesData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('faculty', getFacultiesData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Faculty', getFacultiesData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addFacultyModal', 'editFacultyModal', 'deleteFacultyModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addFacultyModal', 'addFacultyForm');
});

/**
 * Load users with Faculty role
 */
async function loadUsers() {
    try {
        const response = await $.ajax({
            url: '/AdminFaculty/GetUsers',
            type: 'GET'
        });

        if (response.success) {
            usersData = response.data;
            populateUserDropdown();
        }
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

/**
 * Load departments
 */
async function loadDepartments() {
    try {
        const response = await $.ajax({
            url: '/AdminFaculty/GetDepartments',
            type: 'GET'
        });

        if (response.success) {
            departmentsData = response.data;
            populateDepartmentDropdowns();
        }
    } catch (error) {
        console.error('Error loading departments:', error);
    }
}

/**
 * Populate user dropdown
 */
function populateUserDropdown() {
    const $select = $('#addUserId');
    $select.find('option:not(:first)').remove();
    
    usersData.forEach(user => {
        $select.append(`<option value="${user.userId}">${user.userName} (${user.userEmail})</option>`);
    });
}

/**
 * Populate department dropdowns
 */
function populateDepartmentDropdowns() {
    const $addSelect = $('#addDeptId');
    const $editSelect = $('#editDeptId');
    
    $addSelect.find('option:not(:first)').remove();
    $editSelect.find('option:not(:first)').remove();
    
    departmentsData.forEach(dept => {
        const option = `<option value="${dept.deptId}">${dept.deptName}</option>`;
        $addSelect.append(option);
        $editSelect.append(option);
    });
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    facultiesTable = $('#facultiesTable').DataTable({
        ajax: {
            url: '/AdminFaculty/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load faculties data.');
            }
        },
        columns: [
            {
                data: 'facName',
                width: '25%'
            },
            {
                data: 'userEmail',
                width: '25%'
            },
            {
                data: 'deptName',
                width: '20%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No Department</span>';
                }
            },
            {
                data: 'facRank',
                width: '20%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">Not Specified</span>';
                }
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.facId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteFaculty(${data.facId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('faculties', 'faculty')
    });
}

/**
 * Show Add Faculty Modal
 */
function showAddModal() {
    $('#addFacultyForm')[0].reset();
    showModal('addFacultyModal');
}

/**
 * Show Edit Faculty Modal
 */
function showEditModal(facId) {
    ajaxGet(`/AdminFaculty/GetById/${facId}`,
        function (response) {
            const faculty = response.data;
            $('#editFacId').val(faculty.facId);
            $('#editUserId').val(faculty.userId);
            $('#editUserEmail').val(faculty.userEmail);
            $('#editDeptId').val(faculty.deptId || '');
            $('#editFacRank').val(faculty.facRank || '');
            showModal('editFacultyModal');
        },
        function () {
            Toast.error('Failed to load faculty data.');
        }
    );
}

/**
 * Save New Faculty
 */
function saveFaculty() {
    const form = $('#addFacultyForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const userId = $('#addUserId').val();
    if (!userId) {
        Toast.warning('Please select a user.');
        return;
    }

    const facultyData = {
        UserId: parseInt(userId),
        DeptId: $('#addDeptId').val() ? parseInt($('#addDeptId').val()) : null,
        FacRank: $('#addFacRank').val().trim() || null
    };

    const $btn = $('#btnSaveFaculty');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminFaculty/Create', facultyData,
        function () {
            Toast.created('Faculty');
            hideModal('addFacultyModal');
            facultiesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Update Existing Faculty
 */
function updateFaculty() {
    const form = $('#editFacultyForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const facultyData = {
        FacId: parseInt($('#editFacId').val()),
        UserId: parseInt($('#editUserId').val()),
        DeptId: $('#editDeptId').val() ? parseInt($('#editDeptId').val()) : null,
        FacRank: $('#editFacRank').val().trim() || null
    };

    const $btn = $('#btnUpdateFaculty');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminFaculty/Update', facultyData,
        function () {
            Toast.updated('Faculty');
            hideModal('editFacultyModal');
            facultiesTable.ajax.reload(null, false);
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
function deleteFaculty(facId) {
    $('#deleteFacId').val(facId);
    showModal('deleteFacultyModal');
}

/**
 * Confirm and Delete Faculty
 */
function confirmDeleteFaculty() {
    const facId = $('#deleteFacId').val();
    
    if (!facId) {
        Toast.error('Invalid faculty ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminFaculty/Delete/${facId}`,
        function () {
            Toast.deleted('Faculty');
            hideModal('deleteFacultyModal');
            facultiesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get faculties data for export
 */
function getFacultiesData() {
    const data = [];
    facultiesTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Name': rowData.facName,
            'Email': rowData.userEmail,
            'Department': rowData.deptName || 'No Department',
            'Rank': rowData.facRank || 'Not Specified'
        });
    });
    return data;
}
