/**
 * Section Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let sectionsTable;
let programsData = [];

$(document).ready(function () {
    // Load programs first, then initialize DataTable
    loadPrograms().then(() => {
        initializeDataTable();
    });

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddSection').on('click', showAddModal);
    $('#btnSaveSection').on('click', saveSection);
    $('#btnUpdateSection').on('click', updateSection);
    $('#btnConfirmDelete').on('click', confirmDeleteSection);

    // Export button handlers
    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        printSections();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        exportToExcel();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        exportToCSV();
    });

    $('#btnExportPDF').on('click', function(e) {
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
    $('#addSectionModal, #editSectionModal, #deleteSectionModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addSectionModal').on('hidden.bs.modal', function () {
        $('#addSectionForm')[0].reset();
    });
});

/**
 * Load Programs for dropdown
 */
async function loadPrograms() {
    try {
        const response = await $.ajax({
            url: '/Section/GetPrograms',
            type: 'GET'
        });

        if (response.success) {
            programsData = response.data;
            populateProgramDropdowns();
        }
    } catch (error) {
        console.error('Error loading programs:', error);
    }
}

/**
 * Populate program dropdowns
 */
function populateProgramDropdowns() {
    const addSelect = $('#addProgramId');
    const editSelect = $('#editProgramId');

    // Clear existing options except the first one
    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    // Add program options
    programsData.forEach(program => {
        const option = `<option value="${program.programId}">${program.programName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Get year level display text
 */
function getYearLevelText(yearLevel) {
    const yearLevels = {
        1: '1st Year',
        2: '2nd Year',
        3: '3rd Year',
        4: '4th Year'
    };
    return yearLevels[yearLevel] || 'N/A';
}

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    sectionsTable = $('#sectionsTable').DataTable({
        ajax: {
            url: '/Section/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load sections data.');
            }
        },
        columns: [
            { 
                data: 'sectionName',
                width: '25%'
            },
            { 
                data: 'programName',
                width: '40%'
            },
            { 
                data: 'yearLevel',
                width: '20%',
                render: function (data, type, row) {
                    return getYearLevelText(data);
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
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.sectionId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteSection(${row.sectionId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[2, 'asc'], [0, 'asc']], // Order by year level, then section name
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
                    doc.content[1].table.widths = ['25%', '40%', '35%'];
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
            searchPlaceholder: "Search sections...",
            lengthMenu: "Show _MENU_ sections",
            info: "Showing _START_ to _END_ of _TOTAL_ sections",
            infoEmpty: "No sections available",
            infoFiltered: "(filtered from _MAX_ total sections)",
            zeroRecords: "No matching sections found",
            emptyTable: "No sections available in the system"
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
 * Show Add Section Modal
 */
function showAddModal() {
    $('#addSectionForm')[0].reset();
    $('#addSectionModal').modal('show');
}

/**
 * Show Edit Section Modal
 */
function showEditModal(sectionId) {
    $.ajax({
        url: `/Section/GetById/${sectionId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const section = response.data;
                $('#editSectionId').val(section.sectionId);
                $('#editSectionName').val(section.sectionName);
                $('#editProgramId').val(section.programId);
                $('#editYearLevel').val(section.yearLevel);
                $('#editSectionModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load section data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load section data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Section
 */
function saveSection() {
    const form = $('#addSectionForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const sectionName = $('#addSectionName').val().trim();
    const programId = $('#addProgramId').val();
    const yearLevel = $('#addYearLevel').val();

    // Additional validation
    if (!sectionName || !programId || !yearLevel) {
        Toast.warning('All fields are required.');
        return;
    }

    const sectionData = {
        SectionName: sectionName,
        ProgramId: parseInt(programId),
        YearLevel: parseInt(yearLevel)
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveSection');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Section/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(sectionData),
        success: function (response) {
            if (response.success) {
                Toast.created('Section');
                $('#addSectionModal').modal('hide');
                sectionsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create section.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Section');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Section');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Section
 */
function updateSection() {
    const form = $('#editSectionForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const sectionId = $('#editSectionId').val();
    const sectionName = $('#editSectionName').val().trim();
    const programId = $('#editProgramId').val();
    const yearLevel = $('#editYearLevel').val();

    // Additional validation
    if (!sectionName || !programId || !yearLevel) {
        Toast.warning('All fields are required.');
        return;
    }

    const sectionData = {
        SectionId: parseInt(sectionId),
        SectionName: sectionName,
        ProgramId: parseInt(programId),
        YearLevel: parseInt(yearLevel)
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateSection');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Section/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(sectionData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Section');
                $('#editSectionModal').modal('hide');
                sectionsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update section.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Section');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Section');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteSection(sectionId) {
    // Store the section ID
    $('#deleteSectionId').val(sectionId);
    
    // Show the confirmation modal
    $('#deleteSectionModal').modal('show');
}

/**
 * Confirm and Delete Section
 */
function confirmDeleteSection() {
    const sectionId = $('#deleteSectionId').val();
    
    if (!sectionId) {
        Toast.error('Invalid section ID.');
        return;
    }

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Section/Delete/${sectionId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Section');
                $('#deleteSectionModal').modal('hide');
                sectionsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete section.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete section. It may be in use by other records.');
            } else {
                Toast.deleteError('Section');
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
function getSectionsData() {
    const data = [];
    sectionsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Section': rowData.sectionName,
            'Program': rowData.programName,
            'Year Level': getYearLevelText(rowData.yearLevel)
        });
    });
    return data;
}

function printSections() {
    const printWindow = window.open('', '_blank');
    const data = getSectionsData();
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Sections</title>
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
            <h1>Sections</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Section</th>
                        <th>Program</th>
                        <th>Year Level</th>
                    </tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += `<tr>
            <td>${row.Section}</td>
            <td>${row.Program}</td>
            <td>${row['Year Level']}</td>
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
    const data = getSectionsData();
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
    link.download = `sections_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getSectionsData();
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
    link.download = `sections_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printSections();
}
