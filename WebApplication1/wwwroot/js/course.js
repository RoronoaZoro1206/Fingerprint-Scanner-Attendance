/**
 * Course Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let coursesTable;
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
    $('#btnAddCourse').on('click', showAddModal);
    $('#btnSaveCourse').on('click', saveCourse);
    $('#btnUpdateCourse').on('click', updateCourse);
    $('#btnConfirmDelete').on('click', confirmDeleteCourse);

    // Export button handlers
    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        coursesTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        coursesTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        coursesTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        coursesTable.button('.buttons-pdf').trigger();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events - reinitialize icons when modal is shown
    $('#addCourseModal, #editCourseModal, #deleteCourseModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addCourseModal').on('hidden.bs.modal', function () {
        $('#addCourseForm')[0].reset();
    });
});

/**
 * Load Programs for dropdown
 */
async function loadPrograms() {
    try {
        const response = await $.ajax({
            url: '/Course/GetPrograms',
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
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    coursesTable = $('#coursesTable').DataTable({
        ajax: {
            url: '/Course/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load courses data.');
            }
        },
        columns: [
            { 
                data: 'courseCode',
                width: '12%'
            },
            { 
                data: 'courseName',
                width: '30%'
            },
            { 
                data: 'courseUnits',
                width: '8%',
                className: 'text-center'
            },
            { 
                data: 'programName',
                width: '30%'
            },
            {
                data: null,
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.courseId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteCourse(${row.courseId})" title="Delete">
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
                    doc.content[1].table.widths = ['12%', '30%', '8%', '50%'];
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
            searchPlaceholder: "Search courses...",
            lengthMenu: "Show _MENU_ courses",
            info: "Showing _START_ to _END_ of _TOTAL_ courses",
            infoEmpty: "No courses available",
            infoFiltered: "(filtered from _MAX_ total courses)",
            zeroRecords: "No matching courses found",
            emptyTable: "No courses available in the system"
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
 * Show Add Course Modal
 */
function showAddModal() {
    $('#addCourseForm')[0].reset();
    $('#addCourseModal').modal('show');
}

/**
 * Show Edit Course Modal
 */
function showEditModal(courseId) {
    $.ajax({
        url: `/Course/GetById/${courseId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const course = response.data;
                $('#editCourseId').val(course.courseId);
                $('#editCourseCode').val(course.courseCode);
                $('#editCourseName').val(course.courseName);
                $('#editCourseUnits').val(course.courseUnits);
                $('#editProgramId').val(course.programId || '');
                $('#editCourseDesc').val(course.courseDesc || '');
                $('#editCourseModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load course data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load course data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Course
 */
function saveCourse() {
    const form = $('#addCourseForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const courseCode = $('#addCourseCode').val().trim();
    const courseName = $('#addCourseName').val().trim();
    const courseUnits = parseInt($('#addCourseUnits').val());
    const programId = $('#addProgramId').val();
    const courseDesc = $('#addCourseDesc').val().trim();

    // Additional validation
    if (!courseCode || !courseName) {
        Toast.warning('Course code and name are required.');
        return;
    }

    if (courseCode.length < 2) {
        Toast.warning('Course code must be at least 2 characters.');
        return;
    }

    if (courseUnits < 1 || courseUnits > 10) {
        Toast.warning('Course units must be between 1 and 10.');
        return;
    }

    const courseData = {
        CourseCode: courseCode.toUpperCase(),
        CourseName: courseName,
        CourseUnits: courseUnits,
        ProgramId: programId ? parseInt(programId) : null,
        CourseDesc: courseDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveCourse');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Course/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(courseData),
        success: function (response) {
            if (response.success) {
                Toast.created('Course');
                $('#addCourseModal').modal('hide');
                coursesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create course.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Course');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Course');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Course
 */
function updateCourse() {
    const form = $('#editCourseForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const courseId = $('#editCourseId').val();
    const courseCode = $('#editCourseCode').val().trim();
    const courseName = $('#editCourseName').val().trim();
    const courseUnits = parseInt($('#editCourseUnits').val());
    const programId = $('#editProgramId').val();
    const courseDesc = $('#editCourseDesc').val().trim();

    // Additional validation
    if (!courseCode || !courseName) {
        Toast.warning('Course code and name are required.');
        return;
    }

    if (courseCode.length < 2) {
        Toast.warning('Course code must be at least 2 characters.');
        return;
    }

    if (courseUnits < 1 || courseUnits > 10) {
        Toast.warning('Course units must be between 1 and 10.');
        return;
    }

    const courseData = {
        CourseId: parseInt(courseId),
        CourseCode: courseCode.toUpperCase(),
        CourseName: courseName,
        CourseUnits: courseUnits,
        ProgramId: programId ? parseInt(programId) : null,
        CourseDesc: courseDesc || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateCourse');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Course/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(courseData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Course');
                $('#editCourseModal').modal('hide');
                coursesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update course.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Course');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Course');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteCourse(courseId) {
    // Store the course ID
    $('#deleteCourseId').val(courseId);
    
    // Show the confirmation modal
    $('#deleteCourseModal').modal('show');
}

/**
 * Confirm and Delete Course
 */
function confirmDeleteCourse() {
    const courseId = $('#deleteCourseId').val();
    
    if (!courseId) {
        Toast.error('Invalid course ID.');
        return;
    }

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Course/Delete/${courseId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Course');
                $('#deleteCourseModal').modal('hide');
                coursesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete course.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete course. It may be in use by other records.');
            } else {
                Toast.deleteError('Course');
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
