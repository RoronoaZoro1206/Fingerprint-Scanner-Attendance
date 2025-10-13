/**
 * Student ID Pool Management - CRUD Operations with DataTables
 */

let studentIdPoolTable;

$(document).ready(function () {
    initializeDataTable();

    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    $('#btnGenerateIds').on('click', showGenerateModal);
    $('#btnConfirmGenerate').on('click', generateStudentIds);
    $('#btnUpdateRemarks').on('click', updateRemarks);
    $('#btnConfirmDelete').on('click', confirmDelete);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printStudentIdPool();
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

    // Modal events
    $('#generateIdsModal, #editRemarksModal, #deleteIdModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    $('#generateIdsModal').on('hidden.bs.modal', function () {
        $('#generateIdsForm')[0].reset();
        $('#previewText').text('Enter values to see preview');
    });

    // Update preview when form values change
    $('#genYear, #genStartNumber, #genCount').on('input', updatePreview);

    // Set default year to current year
    $('#genYear').val(new Date().getFullYear());
});

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
                render: function (data, type, row) {
                    const editBtn = `<button class="btn-action btn-edit" onclick="showEditRemarksModal(${row.poolId})" title="Edit Remarks">
                                        <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                                    </button>`;
                    
                    const deleteBtn = row.isUsed 
                        ? `<button class="btn-action btn-delete" disabled title="Cannot delete used ID">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #9ca3af;"></i>
                            </button>`
                        : `<button class="btn-action btn-delete" onclick="deleteStudentId(${row.poolId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>`;
                    
                    return `<div class="action-buttons">${editBtn}${deleteBtn}</div>`;
                }
            }
        ],
        order: [[0, 'desc']],
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
            '<"row"<"col-sm-12"tr>>' +
            '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        buttons: [
            {
                extend: 'excel',
                exportOptions: { columns: [0, 1, 2, 3, 4] }
            },
            {
                extend: 'csv',
                exportOptions: { columns: [0, 1, 2, 3, 4] }
            },
            {
                extend: 'pdf',
                exportOptions: { columns: [0, 1, 2, 3, 4] }
            },
            {
                extend: 'print',
                exportOptions: { columns: [0, 1, 2, 3, 4] }
            }
        ],
        drawCallback: function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search student IDs...",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "No entries available",
            infoFiltered: "(filtered from _MAX_ total entries)",
            zeroRecords: "No matching entries found",
            emptyTable: "No student IDs in the pool"
        },
        initComplete: function () {
            const searchInput = $('.dataTables_filter input');
            if (searchInput.parent().find('.search-icon').length === 0) {
                searchInput.wrap('<div class="search-input-wrapper"></div>');
                searchInput.before('<i data-lucide="search" class="search-icon"></i>');
                setTimeout(function () {
                    if (typeof lucide !== 'undefined') {
                        lucide.createIcons();
                    }
                }, 100);
            }
        }
    });
}

function showGenerateModal() {
    $('#generateIdsForm')[0].reset();
    $('#genYear').val(new Date().getFullYear());
    $('#genStartNumber').val(1);
    $('#genCount').val(50);
    updatePreview();
    $('#generateIdsModal').modal('show');
}

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

function generateStudentIds() {
    const form = $('#generateIdsForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const year = parseInt($('#genYear').val());
    const startNumber = parseInt($('#genStartNumber').val());
    const count = parseInt($('#genCount').val());
    const remarks = $('#genRemarks').val().trim();

    if (count < 1 || count > 1000) {
        Toast.warning('Count must be between 1 and 1000');
        return;
    }

    const requestData = {
        Year: year,
        StartNumber: startNumber,
        Count: count,
        GeneratedBy: null,
        Remarks: remarks || null
    };

    const $btn = $('#btnConfirmGenerate');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Generating...');

    $.ajax({
        url: '/AdminStudentIdPool/Generate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(requestData),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                $('#generateIdsModal').modal('hide');
                studentIdPoolTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to generate student IDs.');
            }
        },
        error: function () {
            Toast.error('An error occurred while generating student IDs.');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="hash" class="icon-sm"></i> Generate IDs');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

function showEditRemarksModal(poolId) {
    $.ajax({
        url: `/AdminStudentIdPool/GetById/${poolId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const pool = response.data;
                $('#editPoolId').val(pool.poolId);
                $('#editStudentIdCode').val(pool.studentIdCode);
                $('#editRemarks').val(pool.remarks || '');
                $('#editRemarksModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load student ID data.');
            }
        },
        error: function () {
            Toast.error('Failed to load student ID data.');
        }
    });
}

function updateRemarks() {
    const form = $('#editRemarksForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const poolId = parseInt($('#editPoolId').val());
    const remarks = $('#editRemarks').val().trim();

    const requestData = {
        PoolId: poolId,
        Remarks: remarks || null
    };

    const $btn = $('#btnUpdateRemarks');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/AdminStudentIdPool/UpdateRemarks',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(requestData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Remarks');
                $('#editRemarksModal').modal('hide');
                studentIdPoolTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update remarks.');
            }
        },
        error: function () {
            Toast.updateError('Remarks');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Remarks');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

function deleteStudentId(poolId) {
    $('#deletePoolId').val(poolId);
    $('#deleteIdModal').modal('show');
}

function confirmDelete() {
    const poolId = $('#deletePoolId').val();

    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/AdminStudentIdPool/Delete/${poolId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Student ID');
                $('#deleteIdModal').modal('hide');
                studentIdPoolTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete student ID.');
            }
        },
        error: function () {
            Toast.deleteError('Student ID');
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

function printStudentIdPool() {
    const printWindow = window.open('', '_blank');
    const data = getStudentIdPoolData();
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Student ID Pool</title>
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
            <h1>Student ID Pool</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Student ID</th>
                        <th>Status</th>
                        <th>Generated At</th>
                        <th>Used At</th>
                        <th>Remarks</th>
                    </tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += `<tr>
            <td>${row['Student ID']}</td>
            <td>${row.Status}</td>
            <td>${row['Generated At']}</td>
            <td>${row['Used At']}</td>
            <td>${row.Remarks}</td>
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
    const data = getStudentIdPoolData();
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
    link.download = `student_id_pool_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getStudentIdPoolData();
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
    link.download = `student_id_pool_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printStudentIdPool();
}
