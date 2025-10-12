/**
 * Student Management - CRUD Operations with DataTables
 */

let studentsTable;
let usersData = [];
let programsData = [];
let sectionsData = [];

$(document).ready(function () {
    initializeDataTable();
    loadUsers();
    loadPrograms();
    loadSections();

    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    $('#btnAddStudent').on('click', showAddModal);
    $('#btnSaveStudent').on('click', saveStudent);
    $('#btnUpdateStudent').on('click', updateStudent);
    $('#btnConfirmDelete').on('click', confirmDeleteStudent);

    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        studentsTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        studentsTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        studentsTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        studentsTable.button('.buttons-pdf').trigger();
    });

    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    $('#addStudentModal, #editStudentModal, #deleteStudentModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    $('#addStudentModal').on('hidden.bs.modal', function () {
        $('#addStudentForm')[0].reset();
    });
});

function initializeDataTable() {
    studentsTable = $('#studentsTable').DataTable({
        ajax: {
            url: '/Student/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
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
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.studId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteStudent(${row.studId})" title="Delete">
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
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            },
            {
                extend: 'csv',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            },
            {
                extend: 'pdf',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            },
            {
                extend: 'print',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            }
        ],
        drawCallback: function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search students...",
            lengthMenu: "Show _MENU_ students",
            info: "Showing _START_ to _END_ of _TOTAL_ students",
            infoEmpty: "No students available",
            infoFiltered: "(filtered from _MAX_ total students)",
            zeroRecords: "No matching students found",
            emptyTable: "No students available in the system"
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

function loadUsers() {
    $.ajax({
        url: '/Student/GetUsers',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                usersData = response.data;
                populateUserDropdown();
            }
        }
    });
}

function loadPrograms() {
    $.ajax({
        url: '/Student/GetPrograms',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                programsData = response.data;
                populateProgramDropdowns();
            }
        }
    });
}

function loadSections() {
    $.ajax({
        url: '/Student/GetSections',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                sectionsData = response.data;
                populateSectionDropdowns();
            }
        }
    });
}

function populateUserDropdown() {
    const $select = $('#addUserId');
    $select.find('option:not(:first)').remove();
    usersData.forEach(user => {
        $select.append(`<option value="${user.userId}">${user.userName} (${user.userEmail})</option>`);
    });
}

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

function showAddModal() {
    $('#addStudentForm')[0].reset();
    $('#addStudentModal').modal('show');
}

function showEditModal(studId) {
    $.ajax({
        url: `/Student/GetById/${studId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const student = response.data;
                $('#editStudId').val(student.studId);
                $('#editUserId').val(student.userId);
                $('#editUserEmail').val(student.userEmail);
                $('#editStudNumber').val(student.studNumber);
                $('#editProgramId').val(student.programId || '');
                $('#editStudYearLevel').val(student.studYearLevel || '');
                $('#editSectionId').val(student.sectionId || '');
                $('#editStudentModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load student data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load student data.');
        }
    });
}

function saveStudent() {
    const form = $('#addStudentForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const studentData = {
        UserId: parseInt($('#addUserId').val()),
        StudNumber: $('#addStudNumber').val().trim(),
        ProgramId: $('#addProgramId').val() ? parseInt($('#addProgramId').val()) : null,
        StudYearLevel: $('#addStudYearLevel').val() ? parseInt($('#addStudYearLevel').val()) : null,
        SectionId: $('#addSectionId').val() ? parseInt($('#addSectionId').val()) : null
    };

    const $btn = $('#btnSaveStudent');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Student/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(studentData),
        success: function (response) {
            if (response.success) {
                Toast.created('Student');
                $('#addStudentModal').modal('hide');
                studentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create student.');
            }
        },
        error: function () {
            Toast.createError('Student');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Student');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

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
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Student/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(studentData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Student');
                $('#editStudentModal').modal('hide');
                studentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update student.');
            }
        },
        error: function () {
            Toast.updateError('Student');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Student');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

function deleteStudent(studId) {
    $('#deleteStudId').val(studId);
    $('#deleteStudentModal').modal('show');
}

function confirmDeleteStudent() {
    const studId = $('#deleteStudId').val();
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Student/Delete/${studId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Student');
                $('#deleteStudentModal').modal('hide');
                studentsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete student.');
            }
        },
        error: function () {
            Toast.deleteError('Student');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}
