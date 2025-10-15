/**
 * Academic Term Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let termsTable;

$(document).ready(function () {
    // Initialize DataTable
    initializeDataTable();

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddTerm').on('click', showAddModal);
    $('#btnSaveTerm').on('click', saveTerm);
    $('#btnUpdateTerm').on('click', updateTerm);
    $('#btnConfirmDelete').on('click', confirmDeleteTerm);
    $('#btnConfirmSetActive').on('click', confirmSetActiveTerm);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printTerms();
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
    $('#addTermModal, #editTermModal, #deleteTermModal, #setActiveModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addTermModal').on('hidden.bs.modal', function () {
        $('#addTermForm')[0].reset();
    });
});

/**
 * Format date for display
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

/**
 * Format date for input (YYYY-MM-DD)
 */
function formatDateForInput(dateString) {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
}

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    termsTable = $('#termsTable').DataTable({
        ajax: {
            url: '/AdminAcademicTerm/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load academic terms data.');
            }
        },
        columns: [
            {
                data: 'termName',
                width: '30%'
            },
            {
                data: 'startDate',
                width: '20%',
                render: function (data, type, row) {
                    return formatDate(data);
                }
            },
            {
                data: 'endDate',
                width: '20%',
                render: function (data, type, row) {
                    return formatDate(data);
                }
            },
            {
                data: 'isActive',
                width: '15%',
                render: function (data, type, row) {
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
                render: function (data, type, row) {
                    const setActiveBtn = !row.isActive ?
                        `<button class="btn-action btn-activate" onclick="setActiveTerm(${row.termId})" title="Set as Active">
                            <i data-lucide="check-circle" class="icon-sm" style="color: #10b981;"></i>
                        </button>` : '';

                    return `
                        <div class="action-buttons">
                            ${setActiveBtn}
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.termId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteTerm(${row.termId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[1, 'desc']], // Order by start date descending
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
                    doc.content[1].table.widths = ['30%', '20%', '20%', '30%'];
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
            searchPlaceholder: "Search academic terms...",
            lengthMenu: "Show _MENU_ terms",
            info: "Showing _START_ to _END_ of _TOTAL_ terms",
            infoEmpty: "No academic terms available",
            infoFiltered: "(filtered from _MAX_ total terms)",
            zeroRecords: "No matching academic terms found",
            emptyTable: "No academic terms available in the system"
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
                setTimeout(function () {
                    if (typeof lucide !== 'undefined') {
                        lucide.createIcons();
                    }
                }, 100);
            }
        }
    });
}

/**
 * Show Add Term Modal
 */
function showAddModal() {
    $('#addTermForm')[0].reset();
    $('#addTermModal').modal('show');
}

/**
 * Show Edit Term Modal
 */
function showEditModal(termId) {
    $.ajax({
        url: `/AdminAcademicTerm/GetById/${termId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const term = response.data;
                $('#editTermId').val(term.termId);
                $('#editTermName').val(term.termName);
                $('#editStartDate').val(formatDateForInput(term.startDate));
                $('#editEndDate').val(formatDateForInput(term.endDate));
                $('#editIsActive').prop('checked', term.isActive);
                $('#editTermModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load academic term data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load academic term data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Term
 */
function saveTerm() {
    const form = $('#addTermForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const termName = $('#addTermName').val().trim();
    const startDate = $('#addStartDate').val();
    const endDate = $('#addEndDate').val();
    const isActive = $('#addIsActive').is(':checked');

    // Additional validation
    if (!termName || !startDate || !endDate) {
        Toast.warning('All fields are required.');
        return;
    }

    if (new Date(endDate) <= new Date(startDate)) {
        Toast.warning('End date must be after start date.');
        return;
    }

    const termData = {
        TermName: termName,
        StartDate: startDate,
        EndDate: endDate,
        IsActive: isActive
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveTerm');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/AdminAcademicTerm/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(termData),
        success: function (response) {
            if (response.success) {
                Toast.created('Academic Term');
                $('#addTermModal').modal('hide');
                termsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create academic term.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Academic Term');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Term');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Term
 */
function updateTerm() {
    const form = $('#editTermForm')[0];

    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const termId = $('#editTermId').val();
    const termName = $('#editTermName').val().trim();
    const startDate = $('#editStartDate').val();
    const endDate = $('#editEndDate').val();
    const isActive = $('#editIsActive').is(':checked');

    // Additional validation
    if (!termName || !startDate || !endDate) {
        Toast.warning('All fields are required.');
        return;
    }

    if (new Date(endDate) <= new Date(startDate)) {
        Toast.warning('End date must be after start date.');
        return;
    }

    const termData = {
        TermId: parseInt(termId),
        TermName: termName,
        StartDate: startDate,
        EndDate: endDate,
        IsActive: isActive
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateTerm');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/AdminAcademicTerm/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(termData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Academic Term');
                $('#editTermModal').modal('hide');
                termsTable.ajax.reload(null, false);
                
                // Trigger navbar update if active status changed
                if (isActive) {
                    if (typeof window.triggerAcademicTermUpdate === 'function') {
                        window.triggerAcademicTermUpdate();
                    }
                }
            } else {
                Toast.error(response.message || 'Failed to update academic term.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Academic Term');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Term');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Set Active Confirmation Modal
 */
function setActiveTerm(termId) {
    // Store the term ID
    $('#setActiveTermId').val(termId);

    // Show the confirmation modal
    $('#setActiveModal').modal('show');
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

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmSetActive');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Setting...');

    $.ajax({
        url: `/AdminAcademicTerm/SetActive/${termId}`,
        type: 'POST',
        success: function (response) {
            if (response.success) {
                Toast.success('Active term set successfully!');
                $('#setActiveModal').modal('hide');
                termsTable.ajax.reload(null, false);
                
                // Trigger navbar update
                if (typeof window.triggerAcademicTermUpdate === 'function') {
                    window.triggerAcademicTermUpdate();
                }
            } else {
                Toast.error(response.message || 'Failed to set active term.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            Toast.error('Failed to set active term.');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="check-circle" class="icon-sm"></i> Set Active');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteTerm(termId) {
    // Store the term ID
    $('#deleteTermId').val(termId);

    // Show the confirmation modal
    $('#deleteTermModal').modal('show');
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

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/AdminAcademicTerm/Delete/${termId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Academic Term');
                $('#deleteTermModal').modal('hide');
                termsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete academic term.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete academic term. It may be in use by other records.');
            } else {
                Toast.deleteError('Academic Term');
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
function getTermsData() {
    const data = [];
    termsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Term Name': rowData.termName,
            'Start Date': formatDate(rowData.startDate),
            'End Date': formatDate(rowData.endDate),
            'Status': rowData.isActive ? 'Active' : 'Inactive'
        });
    });
    return data;
}

function printTerms() {
    const printWindow = window.open('', '_blank');
    const data = getTermsData();

    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Academic Terms</title>
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
            <h1>Academic Terms</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Term Name</th>
                        <th>Start Date</th>
                        <th>End Date</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>`;

    data.forEach(row => {
        html += `<tr>
            <td>${row['Term Name']}</td>
            <td>${row['Start Date']}</td>
            <td>${row['End Date']}</td>
            <td>${row.Status}</td>
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
    const data = getTermsData();
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
    link.download = `academic_terms_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getTermsData();
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
    link.download = `academic_terms_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printTerms();
}
