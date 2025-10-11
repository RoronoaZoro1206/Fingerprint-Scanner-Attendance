/**
 * Class Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let classesTable;
let coursesData = [];
let roomsData = [];
let facultiesData = [];
let termsData = [];

$(document).ready(function () {
    // Load all dropdown data first, then initialize DataTable
    Promise.all([
        loadCourses(),
        loadRooms(),
        loadFaculties(),
        loadTerms()
    ]).then(() => {
        initializeDataTable();
    });

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddClass').on('click', showAddModal);
    $('#btnSaveClass').on('click', saveClass);
    $('#btnUpdateClass').on('click', updateClass);
    $('#btnConfirmDelete').on('click', confirmDeleteClass);

    // Export button handlers
    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        classesTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        classesTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        classesTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        classesTable.button('.buttons-pdf').trigger();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events - reinitialize icons when modal is shown
    $('#addClassModal, #editClassModal, #deleteClassModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addClassModal').on('hidden.bs.modal', function () {
        $('#addClassForm')[0].reset();
    });
});

/**
 * Load Courses for dropdown
 */
async function loadCourses() {
    try {
        const response = await $.ajax({
            url: '/Class/GetCourses',
            type: 'GET'
        });

        if (response.success) {
            coursesData = response.data;
            populateCourseDropdowns();
        }
    } catch (error) {
        console.error('Error loading courses:', error);
    }
}

/**
 * Load Rooms for dropdown
 */
async function loadRooms() {
    try {
        const response = await $.ajax({
            url: '/Class/GetRooms',
            type: 'GET'
        });

        if (response.success) {
            roomsData = response.data;
            populateRoomDropdowns();
        }
    } catch (error) {
        console.error('Error loading rooms:', error);
    }
}

/**
 * Load Faculties for dropdown
 */
async function loadFaculties() {
    try {
        const response = await $.ajax({
            url: '/Class/GetFaculties',
            type: 'GET'
        });

        if (response.success) {
            facultiesData = response.data;
            populateFacultyDropdowns();
        }
    } catch (error) {
        console.error('Error loading faculties:', error);
    }
}

/**
 * Load Academic Terms for dropdown
 */
async function loadTerms() {
    try {
        const response = await $.ajax({
            url: '/Class/GetTerms',
            type: 'GET'
        });

        if (response.success) {
            termsData = response.data;
            populateTermDropdowns();
        }
    } catch (error) {
        console.error('Error loading academic terms:', error);
    }
}

/**
 * Populate course dropdowns
 */
function populateCourseDropdowns() {
    const addSelect = $('#addCourseId');
    const editSelect = $('#editCourseId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    coursesData.forEach(course => {
        const option = `<option value="${course.courseId}">${course.courseName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate room dropdowns
 */
function populateRoomDropdowns() {
    const addSelect = $('#addRoomId');
    const editSelect = $('#editRoomId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    roomsData.forEach(room => {
        const option = `<option value="${room.roomId}">${room.roomName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate faculty dropdowns
 */
function populateFacultyDropdowns() {
    const addSelect = $('#addFacId');
    const editSelect = $('#editFacId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    facultiesData.forEach(faculty => {
        const option = `<option value="${faculty.facId}">${faculty.facName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate academic term dropdowns
 */
function populateTermDropdowns() {
    const addSelect = $('#addTermId');
    const editSelect = $('#editTermId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    termsData.forEach(term => {
        const option = `<option value="${term.termId}">${term.termName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    classesTable = $('#classesTable').DataTable({
        ajax: {
            url: '/Class/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load classes data.');
            }
        },
        columns: [
            { 
                data: 'courseName',
                width: '25%'
            },
            { 
                data: 'roomName',
                width: '20%'
            },
            { 
                data: 'facultyName',
                width: '25%'
            },
            { 
                data: 'termName',
                width: '20%'
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.classId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteClass(${row.classId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[3, 'desc'], [0, 'asc']],
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
                    doc.content[1].table.widths = ['25%', '20%', '30%', '25%'];
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
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search classes...",
            lengthMenu: "Show _MENU_ classes",
            info: "Showing _START_ to _END_ of _TOTAL_ classes",
            infoEmpty: "No classes available",
            infoFiltered: "(filtered from _MAX_ total classes)",
            zeroRecords: "No matching classes found",
            emptyTable: "No classes available in the system"
        },
        initComplete: function () {
            const searchInput = $('.dataTables_filter input');
            
            if (searchInput.parent().find('.search-icon').length === 0) {
                searchInput.wrap('<div class="search-input-wrapper"></div>');
                searchInput.before('<i data-lucide="search" class="search-icon"></i>');
                
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
 * Show Add Class Modal
 */
function showAddModal() {
    $('#addClassForm')[0].reset();
    $('#addClassModal').modal('show');
}

/**
 * Show Edit Class Modal
 */
function showEditModal(classId) {
    $.ajax({
        url: `/Class/GetById/${classId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const cls = response.data;
                $('#editClassId').val(cls.classId);
                $('#editCourseId').val(cls.courseId);
                $('#editRoomId').val(cls.roomId);
                $('#editFacId').val(cls.facId);
                $('#editTermId').val(cls.termId);
                $('#editClassModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load class data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load class data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Class
 */
function saveClass() {
    const form = $('#addClassForm')[0];
    
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const courseId = $('#addCourseId').val();
    const roomId = $('#addRoomId').val();
    const facId = $('#addFacId').val();
    const termId = $('#addTermId').val();

    if (!courseId || !roomId || !facId || !termId) {
        Toast.warning('All fields are required.');
        return;
    }

    const classData = {
        CourseId: parseInt(courseId),
        RoomId: parseInt(roomId),
        FacId: parseInt(facId),
        TermId: parseInt(termId)
    };

    const $btn = $('#btnSaveClass');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Class/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(classData),
        success: function (response) {
            if (response.success) {
                Toast.created('Class');
                $('#addClassModal').modal('hide');
                classesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create class.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Class');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Class');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Class
 */
function updateClass() {
    const form = $('#editClassForm')[0];
    
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const classId = $('#editClassId').val();
    const courseId = $('#editCourseId').val();
    const roomId = $('#editRoomId').val();
    const facId = $('#editFacId').val();
    const termId = $('#editTermId').val();

    if (!courseId || !roomId || !facId || !termId) {
        Toast.warning('All fields are required.');
        return;
    }

    const classData = {
        ClassId: parseInt(classId),
        CourseId: parseInt(courseId),
        RoomId: parseInt(roomId),
        FacId: parseInt(facId),
        TermId: parseInt(termId)
    };

    const $btn = $('#btnUpdateClass');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Class/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(classData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Class');
                $('#editClassModal').modal('hide');
                classesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update class.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Class');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Class');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteClass(classId) {
    $('#deleteClassId').val(classId);
    $('#deleteClassModal').modal('show');
}

/**
 * Confirm and Delete Class
 */
function confirmDeleteClass() {
    const classId = $('#deleteClassId').val();
    
    if (!classId) {
        Toast.error('Invalid class ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Class/Delete/${classId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Class');
                $('#deleteClassModal').modal('hide');
                classesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete class.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete class. It may be in use by other records.');
            } else {
                Toast.deleteError('Class');
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
