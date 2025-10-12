/**
 * Department Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let departmentsTable;

$(document).ready(function () {
    // Initialize DataTable
    initializeDataTable();

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddDepartment').on('click', showAddModal);
    $('#btnSaveDepartment').on('click', saveDepartment);
    $('#btnUpdateDepartment').on('click', updateDepartment);
    $('#btnConfirmDelete').on('click', confirmDeleteDepartment);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printDepartments();
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcel();
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSV();
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDF();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events - reinitialize icons when modal is shown
    $('#addDepartmentModal, #editDepartmentModal, #deleteDepartmentModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addDepartmentModal').on('hidden.bs.modal', function () {
        $('#addDepartmentForm')[0].reset();
    });
});

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    departmentsTable = $('#departmentsTable').DataTable({
        ajax: {
            url: '/Department/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load departments data.');
            }
        },
        columns: [
            {
                data: 'deptCode',
                width: '15%'
            },
            {
                data: 'deptName',
                width: '25%'
            },
            {
                data: 'deptDesc',
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
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.deptId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteDepartment(${row.deptId})" title="Delete">
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
            searchPlaceholder: "Search departments...",
            lengthMenu: "Show _MENU_ departments",
            info: "Showing _START_ to _END_ of _TOTAL_ departments",
            infoEmpty: "No departments available",
            infoFiltered: "(filtered from _MAX_ total departments)",
            zeroRecords: "No matching departments found",
            emptyTable: "No departments available in the system"
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
 * Show Add Department Modal
 */
function showAddModal() {
    $('#addDepartmentForm')[0].reset();
    $('#addDepartmentModal').modal('show');
}

/**
 * Show Edit Department Modal
 */
function showEditModal(deptId) {
    $.ajax({
        url: `/Department/GetById/${deptId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const dept = response.data;
                $('#editDeptId').val(dept.deptId);
                $('#editDeptCode').val(dept.deptCode);
                $('#editDeptName').val(dept.deptName);
                $('#editDeptDesc').val(dept.deptDesc || '');
                $('#editDepartmentModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load department data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load department data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Department
 */
function saveDepartment() {
    const form = $('#addDepartmentForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const deptCode = $('#addDeptCode').val().trim();
    const deptName = $('#addDeptName').val().trim();
    const deptDesc = $('#addDeptDesc').val().trim();

    // Additional validation
    if (!deptCode || !deptName) {
        Toast.warning('Department code and name are required.');
        return;
    }

    if (deptCode.length < 2) {
        Toast.warning('Department code must be at least 2 characters.');
        return;
    }

    const departmentData = {
        DeptCode: deptCode.toUpperCase(),
        DeptName: deptName,
        DeptDesc: deptDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveDepartment');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Department/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(departmentData),
        success: function (response) {
            if (response.success) {
                Toast.created('Department');
                $('#addDepartmentModal').modal('hide');
                departmentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create department.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Department');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Department');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Department
 */
function updateDepartment() {
    const form = $('#editDepartmentForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const deptId = $('#editDeptId').val();
    const deptCode = $('#editDeptCode').val().trim();
    const deptName = $('#editDeptName').val().trim();
    const deptDesc = $('#editDeptDesc').val().trim();

    // Additional validation
    if (!deptCode || !deptName) {
        Toast.warning('Department code and name are required.');
        return;
    }

    if (deptCode.length < 2) {
        Toast.warning('Department code must be at least 2 characters.');
        return;
    }

    const departmentData = {
        DeptId: parseInt(deptId),
        DeptCode: deptCode.toUpperCase(),
        DeptName: deptName,
        DeptDesc: deptDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateDepartment');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Department/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(departmentData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Department');
                $('#editDepartmentModal').modal('hide');
                departmentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update department.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Department');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Department');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteDepartment(deptId) {
    // Store the department ID
    $('#deleteDeptId').val(deptId);
    
    // Show the confirmation modal
    $('#deleteDepartmentModal').modal('show');
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

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Department/Delete/${deptId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Department');
                $('#deleteDepartmentModal').modal('hide');
                departmentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete department.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete department. It may be in use by other records.');
            } else {
                Toast.deleteError('Department');
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

// Export Functions
function getDepartmentsData() {
    const data = [];
    departmentsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Code': rowData.deptCode,
            'Name': rowData.deptName,
            'Description': rowData.deptDesc || 'No description'
        });
    });
    return data;
}

function printDepartments() {
    const printWindow = window.open('', '_blank');
    const data = getDepartmentsData();
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Departments</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; }
                h1 { color: #1a1a1a; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: left; border: 1px solid #ddd; }
                th { background-color: #f9fafb; font-weight: 600; }
                tr:nth-child(even) { background-color: #f9fafb; }
                @media print {
                    button { display: none; }
                }
            </style>
        </head>
        <body>
            <h1>Departments</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Code</th>
                        <th>Name</th>
                        <th>Description</th>
                    </tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += `<tr>
            <td>${row.Code}</td>
            <td>${row.Name}</td>
            <td>${row.Description}</td>
        </tr>`;
    });
    
    html += `
                </tbody>
            </table>
            <script>
                window.onload = function() { window.print(); }
            </script>
        </body>
        </html>`;
    
    printWindow.document.write(html);
    printWindow.document.close();
}

function exportToCSV() {
    const data = getDepartmentsData();
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }
    
    const headers = Object.keys(data[0]);
    let csv = headers.join(',') + '\n';
    
    data.forEach(row => {
        const values = headers.map(header => {
            const value = row[header] || '';
            return `"${value.toString().replace(/"/g, '""')}"`;
        });
        csv += values.join(',') + '\n';
    });
    
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `departments_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getDepartmentsData();
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }
    
    const headers = Object.keys(data[0]);
    let html = '<table><thead><tr>';
    headers.forEach(header => {
        html += `<th>${header}</th>`;
    });
    html += '</tr></thead><tbody>';
    
    data.forEach(row => {
        html += '<tr>';
        headers.forEach(header => {
            html += `<td>${row[header] || ''}</td>`;
        });
        html += '</tr>';
    });
    html += '</tbody></table>';
    
    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `departments_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printDepartments();
}
