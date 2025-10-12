let schedulesTable;
let classesData = [];

$(document).ready(function () {
    loadClasses().then(() => {
        initializeDataTable();
    });

    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    $('#btnAddSchedule').on('click', showAddModal);
    $('#btnSaveSchedule').on('click', saveSchedule);
    $('#btnUpdateSchedule').on('click', updateSchedule);
    $('#btnConfirmDelete').on('click', confirmDelete);

    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        schedulesTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        schedulesTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        schedulesTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        schedulesTable.button('.buttons-pdf').trigger();
    });

    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    $('#addScheduleModal, #editScheduleModal, #deleteScheduleModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    $('#addScheduleModal').on('hidden.bs.modal', function () {
        $('#addScheduleForm')[0].reset();
    });
});

async function loadClasses() {
    try {
        const response = await $.ajax({
            url: '/ClassSchedule/GetClasses',
            type: 'GET'
        });

        if (response.success) {
            classesData = response.data;
            populateClassDropdowns();
        }
    } catch (error) {
        console.error('Error loading classes:', error);
    }
}

function populateClassDropdowns() {
    const addSelect = $('#addClassId');
    const editSelect = $('#editClassId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    classesData.forEach(cls => {
        const option = `<option value="${cls.classId}">${cls.courseName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

function initializeDataTable() {
    schedulesTable = $('#schedulesTable').DataTable({
        ajax: {
            url: '/ClassSchedule/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load schedules data.');
            }
        },
        columns: [
            { data: 'courseName', width: '30%' },
            { data: 'schedDayOfWeek', width: '20%' },
            { data: 'schedStartTime', width: '15%' },
            { data: 'schedEndTime', width: '15%' },
            {
                data: null,
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.schedId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteSchedule(${row.schedId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[1, 'asc'], [2, 'asc']],
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        buttons: [
            { extend: 'excel', exportOptions: { columns: [0, 1, 2, 3] } },
            { extend: 'csv', exportOptions: { columns: [0, 1, 2, 3] } },
            { extend: 'pdf', exportOptions: { columns: [0, 1, 2, 3] } },
            { extend: 'print', exportOptions: { columns: [0, 1, 2, 3] } }
        ],
        drawCallback: function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search schedules...",
            lengthMenu: "Show _MENU_ schedules",
            info: "Showing _START_ to _END_ of _TOTAL_ schedules",
            infoEmpty: "No schedules available",
            infoFiltered: "(filtered from _MAX_ total schedules)",
            zeroRecords: "No matching schedules found",
            emptyTable: "No schedules available"
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

function showAddModal() {
    $('#addScheduleForm')[0].reset();
    $('#addScheduleModal').modal('show');
}

function showEditModal(schedId) {
    $.ajax({
        url: `/ClassSchedule/GetById/${schedId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const sched = response.data;
                $('#editSchedId').val(sched.schedId);
                $('#editClassId').val(sched.classId);
                $('#editDayOfWeek').val(sched.schedDayOfWeek);
                $('#editStartTime').val(sched.schedStartTime);
                $('#editEndTime').val(sched.schedEndTime);
                $('#editScheduleModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load schedule data.');
            }
        },
        error: function () {
            Toast.error('Failed to load schedule data.');
        }
    });
}

function saveSchedule() {
    const form = $('#addScheduleForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const scheduleData = {
        ClassId: parseInt($('#addClassId').val()),
        SchedDayOfWeek: $('#addDayOfWeek').val(),
        SchedStartTime: $('#addStartTime').val(),
        SchedEndTime: $('#addEndTime').val()
    };

    const $btn = $('#btnSaveSchedule');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/ClassSchedule/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(scheduleData),
        success: function (response) {
            if (response.success) {
                Toast.created('Schedule');
                $('#addScheduleModal').modal('hide');
                schedulesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create schedule.');
            }
        },
        error: function () {
            Toast.createError('Schedule');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

function updateSchedule() {
    const form = $('#editScheduleForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const scheduleData = {
        SchedId: parseInt($('#editSchedId').val()),
        ClassId: parseInt($('#editClassId').val()),
        SchedDayOfWeek: $('#editDayOfWeek').val(),
        SchedStartTime: $('#editStartTime').val(),
        SchedEndTime: $('#editEndTime').val()
    };

    const $btn = $('#btnUpdateSchedule');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/ClassSchedule/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(scheduleData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Schedule');
                $('#editScheduleModal').modal('hide');
                schedulesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update schedule.');
            }
        },
        error: function () {
            Toast.updateError('Schedule');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

function deleteSchedule(schedId) {
    $('#deleteSchedId').val(schedId);
    $('#deleteScheduleModal').modal('show');
}

function confirmDelete() {
    const schedId = $('#deleteSchedId').val();
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/ClassSchedule/Delete/${schedId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Schedule');
                $('#deleteScheduleModal').modal('hide');
                schedulesTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete schedule.');
            }
        },
        error: function () {
            Toast.deleteError('Schedule');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}
