/**
 * Faculty Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let facultiesTable;
let usersData = [];
let departmentsData = [];

$(document).ready(function () {
    // Initialize DataTable
    initializeDataTable();

    // Load users and departments
    loadUsers();
    loadDepartments();

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddFaculty').on('click', showAddModal);
    $('#btnSaveFaculty').on('click', saveFaculty);
    $('#btnUpdateFaculty').on('click', updateFaculty);
    $('#btnConfirmDelete').on('click', confirmDeleteFaculty);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printFaculties();
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
    $('#addFacultyModal, #editFacultyModal, #deleteFacultyModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addFacultyModal').on('hidden.bs.modal', function () {
        $('#addFacultyForm')[0].reset();
    });
});

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    facultiesTable = $('#facultiesTable').DataTable({
        ajax: {
            url: '/AdminFaculty/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
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
                render: function (data, type, row) {
                    return data || '<span style="color: #9ca3af;">No Department</span>';
                }
            },
            {
                data: 'facRank',
                width: '20%',
                render: function (data, type, row) {
                    return data || '<span style="color: #9ca3af;">Not Specified</span>';
                }
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.facId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteFaculty(${row.facId})" title="Delete">
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
                    columns: [0, 1, 2, 3]
                }
            },
            {
                extend: 'csv',
                text: '<i data-lucide="file-text" class="icon-sm"></i> CSV',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3]
                }
            },
            {
                extend: 'pdf',
                text: '<i data-lucide="file" class="icon-sm"></i> PDF',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3]
                },
                customize: function (doc) {
                    doc.content[1].table.widths = ['25%', '25%', '25%', '25%'];
                    doc.styles.tableHeader.fillColor = '#f9fafb';
                    doc.styles.tableHeader.color = '#374151';
                }
            },
            {
                extend: 'print',
                text: '<i data-lucide="printer" class="icon-sm"></i> Print',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3]
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
            searchPlaceholder: "Search faculties...",
            lengthMenu: "Show _MENU_ faculties",
            info: "Showing _START_ to _END_ of _TOTAL_ faculties",
            infoEmpty: "No faculties available",
            infoFiltered: "(filtered from _MAX_ total faculties)",
            zeroRecords: "No matching faculties found",
            emptyTable: "No faculties available in the system"
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
 * Load users with Faculty role
 */
function loadUsers() {
    $.ajax({
        url: '/AdminFaculty/GetUsers',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                usersData = response.data;
                populateUserDropdown();
            }
        },
        error: function (xhr) {
            console.error('Error loading users:', xhr);
        }
    });
}

/**
 * Load departments
 */
function loadDepartments() {
    $.ajax({
        url: '/AdminFaculty/GetDepartments',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                departmentsData = response.data;
                populateDepartmentDropdowns();
            }
        },
        error: function (xhr) {
            console.error('Error loading departments:', xhr);
        }
    });
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
 * Show Add Faculty Modal
 */
function showAddModal() {
    $('#addFacultyForm')[0].reset();
    $('#addFacultyModal').modal('show');
}

/**
 * Show Edit Faculty Modal
 */
function showEditModal(facId) {
    $.ajax({
        url: `/AdminFaculty/GetById/${facId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const faculty = response.data;
                $('#editFacId').val(faculty.facId);
                $('#editUserId').val(faculty.userId);
                $('#editUserEmail').val(faculty.userEmail);
                $('#editDeptId').val(faculty.deptId || '');
                $('#editFacRank').val(faculty.facRank || '');
                $('#editFacultyModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load faculty data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load faculty data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Faculty
 */
function saveFaculty() {
    const form = $('#addFacultyForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const userId = $('#addUserId').val();
    const deptId = $('#addDeptId').val();
    const facRank = $('#addFacRank').val().trim();

    // Additional validation
    if (!userId) {
        Toast.warning('Please select a user.');
        return;
    }

    const facultyData = {
        UserId: parseInt(userId),
        DeptId: deptId ? parseInt(deptId) : null,
        FacRank: facRank || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveFaculty');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/AdminFaculty/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(facultyData),
        success: function (response) {
            if (response.success) {
                Toast.created('Faculty');
                $('#addFacultyModal').modal('hide');
                facultiesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create faculty.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Faculty');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Faculty');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Faculty
 */
function updateFaculty() {
    const form = $('#editFacultyForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const facId = $('#editFacId').val();
    const userId = $('#editUserId').val();
    const deptId = $('#editDeptId').val();
    const facRank = $('#editFacRank').val().trim();

    const facultyData = {
        FacId: parseInt(facId),
        UserId: parseInt(userId),
        DeptId: deptId ? parseInt(deptId) : null,
        FacRank: facRank || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateFaculty');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/AdminFaculty/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(facultyData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Faculty');
                $('#editFacultyModal').modal('hide');
                facultiesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update faculty.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Faculty');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Faculty');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteFaculty(facId) {
    // Store the faculty ID
    $('#deleteFacId').val(facId);
    
    // Show the confirmation modal
    $('#deleteFacultyModal').modal('show');
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

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/AdminFaculty/Delete/${facId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Faculty');
                $('#deleteFacultyModal').modal('hide');
                facultiesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete faculty.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete faculty. It may be in use by other records.');
            } else {
                Toast.deleteError('Faculty');
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

function printFaculties() {
    const printWindow = window.open('', '_blank');
    const data = getFacultiesData();
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Faculty</title>
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
            <h1>Faculty</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Email</th>
                        <th>Department</th>
                        <th>Rank</th>
                    </tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += `<tr>
            <td>${row.Name}</td>
            <td>${row.Email}</td>
            <td>${row.Department}</td>
            <td>${row.Rank}</td>
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
    const data = getFacultiesData();
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
    link.download = `faculty_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getFacultiesData();
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
    link.download = `faculty_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printFaculties();
}
