/// <reference path="../global.d.ts" />

/**
 * Course Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 * 
 * ✅ FIXED: Case sensitivity - changed to snake_case to match backend
 */

let coursesTable;
let programsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load programs first, then initialize DataTable
    loadPrograms().then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddCourse').on('click', showAddModal);
    $('#btnSaveCourse').on('click', saveCourse);
    $('#btnUpdateCourse').on('click', updateCourse);
    $('#btnConfirmDelete').on('click', confirmDeleteCourse);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Courses', getCoursesData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('courses', getCoursesData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('courses', getCoursesData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Courses', getCoursesData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addCourseModal', 'editCourseModal', 'deleteCourseModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addCourseModal', 'addCourseForm');
});

/**
 * Load Programs for dropdown
 */
async function loadPrograms() {
    try {
        const response = await $.ajax({
            url: '/AdminCourse/GetPrograms',
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

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    programsData.forEach(program => {
        // ✅ FIX: Changed to snake_case
        const option = `<option value="${program.program_id}">${program.program_name}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    coursesTable = $('#coursesTable').DataTable({
        ajax: {
            url: '/AdminCourse/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load courses data.');
            }
        },
        columns: [
            { 
                // ✅ FIX: Changed from 'courseCode' to 'course_code' (snake_case)
                data: 'course_code',
                width: '12%'
            },
            { 
                // ✅ FIX: Changed from 'courseName' to 'course_name' (snake_case)
                data: 'course_name',
                width: '30%'
            },
            { 
                // ✅ FIX: Changed from 'courseUnits' to 'course_units' (snake_case)
                data: 'course_units',
                width: '8%',
                className: 'text-center'
            },
            { 
                // ✅ FIX: Changed from 'programName' to 'program_name' (snake_case)
                data: 'program_name',
                width: '30%'
            },
            {
                data: null,
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    // ✅ FIX: Changed from data.courseId to data.course_id (snake_case)
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.course_id})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteCourse(${data.course_id})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('courses', 'course')
    });
}

/**
 * Show Add Course Modal
 */
function showAddModal() {
    $('#addCourseForm')[0].reset();
    showModal('addCourseModal');
}

/**
 * Show Edit Course Modal
 */
function showEditModal(courseId) {
    ajaxGet(`/AdminCourse/GetById/${courseId}`,
        function (response) {
            const course = response.data;
            // ✅ FIX: Changed to snake_case
            $('#editCourseId').val(course.course_id);
            $('#editCourseCode').val(course.course_code);
            $('#editCourseName').val(course.course_name);
            $('#editCourseUnits').val(course.course_units);
            $('#editProgramId').val(course.program_id || '');
            $('#editCourseDesc').val(course.course_desc || '');
            showModal('editCourseModal');
        },
        function () {
            Toast.error('Failed to load course data.');
        }
    );
}

/**
 * Save New Course
 */
function saveCourse() {
    const form = $('#addCourseForm')[0];
    
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const courseCode = $('#addCourseCode').val().trim();
    const courseUnits = parseInt($('#addCourseUnits').val());

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
        CourseName: $('#addCourseName').val().trim(),
        CourseUnits: courseUnits,
        ProgramId: $('#addProgramId').val() ? parseInt($('#addProgramId').val()) : null,
        CourseDesc: $('#addCourseDesc').val().trim() || null
    };

    const $btn = $('#btnSaveCourse');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminCourse/Create', courseData,
        function () {
            Toast.created('Course');
            hideModal('addCourseModal');
            coursesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Update Existing Course
 */
function updateCourse() {
    const form = $('#editCourseForm')[0];
    
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const courseCode = $('#editCourseCode').val().trim();
    const courseUnits = parseInt($('#editCourseUnits').val());

    if (courseCode.length < 2) {
        Toast.warning('Course code must be at least 2 characters.');
        return;
    }

    if (courseUnits < 1 || courseUnits > 10) {
        Toast.warning('Course units must be between 1 and 10.');
        return;
    }

    const courseData = {
        CourseId: parseInt($('#editCourseId').val()),
        CourseCode: courseCode.toUpperCase(),
        CourseName: $('#editCourseName').val().trim(),
        CourseUnits: courseUnits,
        ProgramId: $('#editProgramId').val() ? parseInt($('#editProgramId').val()) : null,
        CourseDesc: $('#editCourseDesc').val().trim() || null
    };

    const $btn = $('#btnUpdateCourse');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminCourse/Update', courseData,
        function () {
            Toast.updated('Course');
            hideModal('editCourseModal');
            coursesTable.ajax.reload(null, false);
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
function deleteCourse(courseId) {
    $('#deleteCourseId').val(courseId);
    showModal('deleteCourseModal');
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

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminCourse/Delete/${courseId}`,
        function () {
            Toast.deleted('Course');
            hideModal('deleteCourseModal');
            coursesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get courses data for export
 */
function getCoursesData() {
    const data = [];
    coursesTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        // ✅ FIX: Changed to snake_case
        data.push({
            'Code': rowData.course_code,
            'Name': rowData.course_name,
            'Units': rowData.course_units,
            'Program': rowData.program_name
        });
    });
    return data;
}
