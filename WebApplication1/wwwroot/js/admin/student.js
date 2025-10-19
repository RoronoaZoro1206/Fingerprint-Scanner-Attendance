/**
 * Student Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let studentsTable;
let usersData = [];
let programsData = [];
let sectionsData = [];
let searchTimeout = null;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load data, then initialize DataTable
    Promise.all([loadUsers(), loadPrograms(), loadSections()]).then(() => {
        initializeDataTable();
    });

    // Setup student ID search for add and edit modals
    setupStudentIdSearch('#addStudNumber', '#addStudNumberDropdown');
    setupStudentIdSearch('#editStudNumber', '#editStudNumberDropdown');

    // Event Handlers
    $('#btnAddStudent').on('click', showAddModal);
    $('#btnSaveStudent').on('click', saveStudent);
    $('#btnUpdateStudent').on('click', updateStudent);
    $('#btnConfirmDelete').on('click', confirmDeleteStudent);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Students', getStudentsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('students', getStudentsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('students', getStudentsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Students', getStudentsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addStudentModal', 'editStudentModal', 'deleteStudentModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addStudentModal', 'addStudentForm');
});

/**
 * Load users with Student role
 */
async function loadUsers() {
    try {
        const response = await $.ajax({
            url: '/AdminStudent/GetUsers',
            type: 'GET'
        });

        if (response.success) {
            usersData = response.data;
            populateUserDropdown();
        }
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

/**
 * Load programs
 */
async function loadPrograms() {
    try {
        const response = await $.ajax({
            url: '/AdminStudent/GetPrograms',
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
 * Load sections
 */
async function loadSections() {
    try {
        const response = await $.ajax({
            url: '/AdminStudent/GetSections',
            type: 'GET'
        });

        if (response.success) {
            sectionsData = response.data;
            populateSectionDropdowns();
        }
    } catch (error) {
        console.error('Error loading sections:', error);
    }
}

/**
 * Setup student ID search with autocomplete
 */
function setupStudentIdSearch(inputSelector, dropdownSelector) {
    const $input = $(inputSelector);
    const $dropdown = $(dropdownSelector);

    // Search on input
    $input.on('input', function () {
        const searchTerm = $(this).val().trim();

        // Clear previous timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        // Debounce search
        searchTimeout = setTimeout(function () {
            searchStudentIds(searchTerm, $dropdown);
        }, 300);
    });

    // Show dropdown on focus
    $input.on('focus', function () {
        const searchTerm = $(this).val().trim();
        searchStudentIds(searchTerm, $dropdown);
    });

    // Hide dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.student-id-search-wrapper').length) {
            $dropdown.removeClass('show');
        }
    });
}

/**
 * Search available student IDs
 */
function searchStudentIds(searchTerm, $dropdown) {
    $dropdown.html('<div class="student-id-dropdown-loading">Searching...</div>');
    $dropdown.addClass('show');

    ajaxGet('/AdminStudent/SearchAvailableStudentIds?search=' + encodeURIComponent(searchTerm),
        function (response) {
            displayStudentIdResults(response.data, $dropdown);
        },
        function () {
            $dropdown.html('<div class="student-id-dropdown-empty">Error loading student IDs</div>');
        }
    );
}

/**
 * Display student ID search results
 */
function displayStudentIdResults(results, $dropdown) {
    $dropdown.empty();

    if (results.length === 0) {
        $dropdown.html('<div class="student-id-dropdown-empty">No available student IDs found</div>');
    } else {
        results.forEach(function (item) {
            const $item = $('<div class="student-id-dropdown-item"></div>')
                .text(item.studentIdCode)
                .on('click', function () {
                    const $input = $dropdown.siblings('input');
                    $input.val(item.studentIdCode);
                    $dropdown.removeClass('show');
                });
            $dropdown.append($item);
        });
    }

    $dropdown.addClass('show');
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
 * Populate program dropdowns
 */
function populateProgramDropdowns() {
    const $addSelect = $('#addProgramId');
    const $editSelect = $('#editProgramId');
    $addSelect.find('option:not(:first)').remove();
    $editSelect.find('option:not(:first)').remove();
    programsData.forEach(program => {
        const option = `<option value="${program.programId}">${program.programName}</option>`;
        $addSelect.append(option);
        $editSelect.append(option);
    });
}

/**
 * Populate section dropdowns
 */
function populateSectionDropdowns() {
    const $addSelect = $('#addSectionId');
    const $editSelect = $('#editSectionId');
    $addSelect.find('option:not(:first)').remove();
    $editSelect.find('option:not(:first)').remove();
    sectionsData.forEach(section => {
        const option = `<option value="${section.sectionId}">${section.sectionName}</option>`;
        $addSelect.append(option);
        $editSelect.append(option);
    });
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    studentsTable = $('#studentsTable').DataTable({
        ajax: {
            url: '/AdminStudent/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load students data.');
            }
        },
        columns: [
            { data: 'studNumber', width: '12%' },
            { data: 'studName', width: '18%' },
            { data: 'userEmail', width: '20%' },
            {
                data: 'programName',
                width: '18%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No Program</span>';
                }
            },
            {
                data: 'studYearLevel',
                width: '10%',
                render: function (data) {
                    if (!data) return '<span style="color: #9ca3af;">N/A</span>';
                    return data + (data == 1 ? 'st' : data == 2 ? 'nd' : data == 3 ? 'rd' : 'th') + ' Year';
                }
            },
            {
                data: 'sectionName',
                width: '12%',
                render: function (data) {
                    return data || '<span style="color: #9ca3af;">No Section</span>';
                }
            },
            {
                data: null,
                width: '10%',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.studId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteStudent(${data.studId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('students', 'student')
    });
}

/**
 * Show Add Student Modal
 */
function showAddModal() {
    $('#addStudentForm')[0].reset();
    $('#addStudNumberDropdown').removeClass('show').empty();
    showModal('addStudentModal');
}

/**
 * Show Edit Student Modal
 */
function showEditModal(studId) {
    ajaxGet(`/AdminStudent/GetById/${studId}`,
        function (response) {
            const student = response.data;
            $('#editStudId').val(student.studId);
            $('#editUserId').val(student.userId);
            $('#editUserEmail').val(student.userEmail);
            $('#editStudNumber').val(student.studNumber);
            $('#editProgramId').val(student.programId || '');
            $('#editStudYearLevel').val(student.studYearLevel || '');
            $('#editSectionId').val(student.sectionId || '');
            $('#editStudNumberDropdown').removeClass('show').empty();
            showModal('editStudentModal');
        },
        function () {
            Toast.error('Failed to load student data.');
        }
    );
}

/**
 * Save New Student
 */
function saveStudent() {
    const form = $('#addStudentForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const studNumber = $('#addStudNumber').val();
    if (!studNumber) {
        Toast.warning('Please select a student ID from the available pool.');
        return;
    }

    const studentData = {
        UserId: parseInt($('#addUserId').val()),
        StudNumber: studNumber,
        ProgramId: $('#addProgramId').val() ? parseInt($('#addProgramId').val()) : null,
        StudYearLevel: $('#addStudYearLevel').val() ? parseInt($('#addStudYearLevel').val()) : null,
        SectionId: $('#addSectionId').val() ? parseInt($('#addSectionId').val()) : null
    };

    const $btn = $('#btnSaveStudent');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminStudent/Create', studentData,
        function () {
            Toast.created('Student');
            hideModal('addStudentModal');
            studentsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
}

/**
 * Update Existing Student
 */
function updateStudent() {
    const form = $('#editStudentForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const studentData = {
        StudId: parseInt($('#editStudId').val()),
        UserId: parseInt($('#editUserId').val()),
        StudNumber: $('#editStudNumber').val().trim(),
        ProgramId: $('#editProgramId').val() ? parseInt($('#editProgramId').val()) : null,
        StudYearLevel: $('#editStudYearLevel').val() ? parseInt($('#editStudYearLevel').val()) : null,
        SectionId: $('#editSectionId').val() ? parseInt($('#editSectionId').val()) : null
    };

    const $btn = $('#btnUpdateStudent');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminStudent/Update', studentData,
        function () {
            Toast.updated('Student');
            hideModal('editStudentModal');
            studentsTable.ajax.reload(null, false);
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
function deleteStudent(studId) {
    $('#deleteStudId').val(studId);
    showModal('deleteStudentModal');
}

/**
 * Confirm and Delete Student
 */
function confirmDeleteStudent() {
    const studId = $('#deleteStudId').val();
    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminStudent/Delete/${studId}`,
        function () {
            Toast.deleted('Student');
            hideModal('deleteStudentModal');
            studentsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get students data for export
 */
function getStudentsData() {
    const data = [];
    studentsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        const yearLevel = rowData.studYearLevel ? 
            rowData.studYearLevel + (rowData.studYearLevel == 1 ? 'st' : rowData.studYearLevel == 2 ? 'nd' : rowData.studYearLevel == 3 ? 'rd' : 'th') + ' Year' : 
            'N/A';
        data.push({
            'Student Number': rowData.studNumber,
            'Name': rowData.studName,
            'Email': rowData.userEmail,
            'Program': rowData.programName || 'No Program',
            'Year Level': yearLevel,
            'Section': rowData.sectionName || 'No Section'
        });
    });
    return data;
}
