/**
 * Department Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let departmentsTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable
    initializeDataTable();

    // Event Handlers
    $('#btnAddDepartment').on('click', showAddModal);
    $('#btnSaveDepartment').on('click', saveDepartment);
    $('#btnUpdateDepartment').on('click', updateDepartment);
    $('#btnConfirmDelete').on('click', confirmDeleteDepartment);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Departments', getDepartmentsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('departments', getDepartmentsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('departments', getDepartmentsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Departments', getDepartmentsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addDepartmentModal', 'editDepartmentModal', 'deleteDepartmentModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addDepartmentModal', 'addDepartmentForm');
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    departmentsTable = $('#departmentsTable').DataTable({
        ajax: {
            url: '/AdminDepartment/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load departments data.');
            }
        },
        columns: [
            {
                // ? FIX: Changed from 'deptCode' to 'dept_code' (snake_case)
                data: 'dept_code',
                width: '15%'
            },
            {
                // ? FIX: Changed from 'deptName' to 'dept_name' (snake_case)
                data: 'dept_name',
                width: '25%'
            },
            {
                // ? FIX: Changed from 'deptDesc' to 'dept_desc' (snake_case)
                data: 'dept_desc',
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
                    // ? FIX: Changed from data.deptId to data.dept_id (snake_case)
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.dept_id})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteDepartment(${data.dept_id})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('departments', 'department')
    });
}

/**
 * Show Add Department Modal
 */
function showAddModal() {
    $('#addDepartmentForm')[0].reset();
    showModal('addDepartmentModal');
}

/**
 * Show Edit Department Modal
 */
function showEditModal(deptId) {
    ajaxGet(`/AdminDepartment/GetById/${deptId}`,
        function (response) {
            const dept = response.data;
            // ? FIX: Changed from dept.deptId to dept.dept_id (snake_case)
            $('#editDeptId').val(dept.dept_id);
            $('#editDeptCode').val(dept.dept_code);
            $('#editDeptName').val(dept.dept_name);
            $('#editDeptDesc').val(dept.dept_desc || '');
            showModal('editDepartmentModal');
        },
        function () {
            Toast.error('Failed to load department data.');
        }
    );
}

/**
 * Save New Department
 */
function saveDepartment() {
    const form = $('#addDepartmentForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const deptCode = $('#addDeptCode').val().trim();
    if (deptCode.length < 2) {
        Toast.warning('Department code must be at least 2 characters.');
        return;
    }

    const departmentData = {
        DeptCode: deptCode.toUpperCase(),
        DeptName: $('#addDeptName').val().trim(),
        DeptDesc: $('#addDeptDesc').val().trim() || null
    };

    const $btn = $('#btnSaveDepartment');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminDepartment/Create', departmentData,
        function () {
            Toast.created('Department');
            hideModal('addDepartmentModal');
            departmentsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Update Existing Department
 */
function updateDepartment() {
    const form = $('#editDepartmentForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const deptCode = $('#editDeptCode').val().trim();
    if (deptCode.length < 2) {
        Toast.warning('Department code must be at least 2 characters.');
        return;
    }

    const departmentData = {
        DeptId: parseInt($('#editDeptId').val()),
        DeptCode: deptCode.toUpperCase(),
        DeptName: $('#editDeptName').val().trim(),
        DeptDesc: $('#editDeptDesc').val().trim() || null
    };

    const $btn = $('#btnUpdateDepartment');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminDepartment/Update', departmentData,
        function () {
            Toast.updated('Department');
            hideModal('editDepartmentModal');
            departmentsTable.ajax.reload(null, false);
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
function deleteDepartment(deptId) {
    $('#deleteDeptId').val(deptId);
    showModal('deleteDepartmentModal');
}

/**
 * Confirm and Delete Department
 */
function confirmDeleteDepartment() {
    const deptId = $('#deleteDeptId').val();
    
    if (!deptId) {
        Toast.error('Invalid department ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminDepartment/Delete/${deptId}`,
        function () {
            Toast.deleted('Department');
            hideModal('deleteDepartmentModal');
            departmentsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Get departments data for export
 */
function getDepartmentsData() {
    const data = [];
    departmentsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        // ? FIX: Changed to snake_case properties
        data.push({
            'Code': rowData.dept_code,
            'Name': rowData.dept_name,
            'Description': rowData.dept_desc || 'No description'
        });
    });
    return data;
}
