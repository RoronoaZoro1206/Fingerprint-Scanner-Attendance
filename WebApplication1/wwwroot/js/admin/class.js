/**
 * Class Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 * 
 * ✅ FIXED: Case sensitivity - using PascalCase to match C# API responses
 */

let classesTable;
let coursesData = [];
let roomsData = [];
let facultiesData = [];
let termsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load all dropdown data first, then initialize DataTable
    Promise.all([
        loadCourses(),
        loadRooms(),
        loadFaculties(),
        loadTerms()
    ]).then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddClass').on('click', showAddModal);
    $('#btnSaveClass').on('click', saveClass);
    $('#btnUpdateClass').on('click', updateClass);
    $('#btnConfirmDelete').on('click', confirmDeleteClass);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Classes', getClassesData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('classes', getClassesData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('classes', getClassesData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Classes', getClassesData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addClassModal', 'editClassModal', 'deleteClassModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addClassModal', 'addClassForm');
});

/**
 * Load Courses for dropdown
 */
async function loadCourses() {
    try {
        const response = await $.ajax({
            url: '/AdminClass/GetCourses',
            type: 'GET'
        });

        if (response.success) {
            coursesData = response.data;
            console.log('Loaded courses:', coursesData); // Debug log
            populateCourseDropdowns();
        }
    } catch (error) {
        console.error('Error loading courses:', error);
        Toast.error('Failed to load courses.');
    }
}

/**
 * Load Rooms for dropdown
 */
async function loadRooms() {
    try {
        const response = await $.ajax({
            url: '/AdminClass/GetRooms',
            type: 'GET'
        });

        if (response.success) {
            roomsData = response.data;
            console.log('Loaded rooms:', roomsData); // Debug log
            populateRoomDropdowns();
        }
    } catch (error) {
        console.error('Error loading rooms:', error);
        Toast.error('Failed to load rooms.');
    }
}

/**
 * Load Faculties for dropdown
 */
async function loadFaculties() {
    try {
        const response = await $.ajax({
            url: '/AdminClass/GetFaculties',
            type: 'GET'
        });

        if (response.success) {
            facultiesData = response.data;
            console.log('Loaded faculties:', facultiesData); // Debug log
            populateFacultyDropdowns();
        }
    } catch (error) {
        console.error('Error loading faculties:', error);
        Toast.error('Failed to load faculties.');
    }
}

/**
 * Load Academic Terms for dropdown
 */
async function loadTerms() {
    try {
        const response = await $.ajax({
            url: '/AdminClass/GetTerms',
            type: 'GET'
        });

        if (response.success) {
            termsData = response.data;
            console.log('Loaded terms:', termsData); // Debug log
            populateTermDropdowns();
        }
    } catch (error) {
        console.error('Error loading academic terms:', error);
        Toast.error('Failed to load academic terms.');
    }
}

/**
 * Populate course dropdowns
 * ✅ FIX: Using PascalCase properties (CourseId, CourseName)
 */
function populateCourseDropdowns() {
    const addSelect = $('#addCourseId');
    const editSelect = $('#editCourseId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    coursesData.forEach(course => {
        // ✅ FIX: Changed from course.courseId to course.CourseId (PascalCase)
        // ✅ FIX: Changed from course.courseName to course.CourseName (PascalCase)
        const option = `<option value="${course.CourseId}">${course.CourseName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate room dropdowns
 * ✅ FIX: Using PascalCase properties (RoomId, RoomCode)
 */
function populateRoomDropdowns() {
    const addSelect = $('#addRoomId');
    const editSelect = $('#editRoomId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    roomsData.forEach(room => {
        // ✅ FIX: Changed from room.roomId to room.RoomId (PascalCase)
        // ✅ FIX: Changed from room.roomName to room.RoomCode (correct property name)
        const option = `<option value="${room.RoomId}">${room.RoomCode}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate faculty dropdowns
 * ✅ FIX: Using camelCase properties (facId, facName) - matches FacultySimpleDto
 */
function populateFacultyDropdowns() {
    const addSelect = $('#addFacId');
    const editSelect = $('#editFacId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    facultiesData.forEach(faculty => {
        // ✅ FIX: Using camelCase (facId, facName) as returned by GetFaculties endpoint
        const option = `<option value="${faculty.facId}">${faculty.facName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Populate academic term dropdowns
 * ✅ FIX: Using PascalCase properties (TermId, TermName)
 */
function populateTermDropdowns() {
    const addSelect = $('#addTermId');
    const editSelect = $('#editTermId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    termsData.forEach(term => {
        // ✅ FIX: Changed from term.termId to term.TermId (PascalCase)
        // ✅ FIX: Changed from term.termName to term.TermName (PascalCase)
        const option = `<option value="${term.TermId}">${term.TermName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    classesTable = $('#classesTable').DataTable({
        ajax: {
            url: '/AdminClass/GetAll',
            dataSrc: 'data',
            error: function () {
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
        ...commonDataTableConfig,
        language: getDataTableLanguage('classes', 'class')
    });
}

/**
 * Show Add Class Modal
 */
function showAddModal() {
    $('#addClassForm')[0].reset();
    showModal('addClassModal');
}

/**
 * Show Edit Class Modal
 * ✅ FIX: Use PascalCase properties to match ClassDetailDto response
 */
function showEditModal(classId) {
    ajaxGet(`/AdminClass/GetById/${classId}`,
        function (response) {
            const cls = response.data;
            // ✅ FIX: Changed from cls.classId to cls.ClassId (PascalCase)
            $('#editClassId').val(cls.ClassId);
            // ✅ FIX: Changed from cls.courseId to cls.CourseId (PascalCase)
            $('#editCourseId').val(cls.CourseId);
            // ✅ FIX: Changed from cls.roomId to cls.RoomId (PascalCase)
            $('#editRoomId').val(cls.RoomId);
            // ✅ FIX: Changed from cls.facId to cls.FacId (PascalCase)
            $('#editFacId').val(cls.FacId);
            // ✅ FIX: Changed from cls.termId to cls.TermId (PascalCase)
            $('#editTermId').val(cls.TermId);
            showModal('editClassModal');
        },
        function () {
            Toast.error('Failed to load class data.');
        }
    );
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
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminClass/Create', classData,
        function () {
            Toast.created('Class');
            hideModal('addClassModal');
            classesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
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

    const courseId = $('#editCourseId').val();
    const roomId = $('#editRoomId').val();
    const facId = $('#editFacId').val();
    const termId = $('#editTermId').val();

    if (!courseId || !roomId || !facId || !termId) {
        Toast.warning('All fields are required.');
        return;
    }

    const classData = {
        ClassId: parseInt($('#editClassId').val()),
        CourseId: parseInt(courseId),
        RoomId: parseInt(roomId),
        FacId: parseInt(facId),
        TermId: parseInt(termId)
    };

    const $btn = $('#btnUpdateClass');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminClass/Update', classData,
        function () {
            Toast.updated('Class');
            hideModal('editClassModal');
            classesTable.ajax.reload(null, false);
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
function deleteClass(classId) {
    $('#deleteClassId').val(classId);
    showModal('deleteClassModal');
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
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminClass/Delete/${classId}`,
        function () {
            Toast.deleted('Class');
            hideModal('deleteClassModal');
            classesTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get classes data for export
 */
function getClassesData() {
    const data = [];
    classesTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Course': rowData.courseName,
            'Room': rowData.roomName,
            'Faculty': rowData.facultyName,
            'Term': rowData.termName
        });
    });
    return data;
}
