/**
 * Faculty Attendance - Monitor and manage class attendance with DataTables
 */

let attendanceTable;
let currentClassId = null;
let selectedDate = null;

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Set default date to today
    const today = new Date().toISOString().split('T')[0];
    $('#dateSelect').val(today);
    selectedDate = today;

    // Load faculty classes for dropdown
    loadClassesDropdown();

    // Event handlers
    $('#classSelect').on('change', handleClassChange);
    $('#dateSelect').on('change', handleDateChange);
    $('#btnMarkAllPresent').on('click', markAllPresent);
    $('#btnUpdateAttendance').on('click', updateAttendance);
    $('#btnConfirmDelete').on('click', confirmDeleteAttendance);

    // Modal events - reinitialize icons when modal is shown
    $('#editAttendanceModal, #deleteAttendanceModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Check if classId is in URL (from My Classes page)
    const urlParams = new URLSearchParams(window.location.search);
    const classId = urlParams.get('classId');
    if (classId) {
        currentClassId = parseInt(classId);
        // Wait for dropdown to load, then select the class
        setTimeout(() => {
            $('#classSelect').val(classId).trigger('change');
        }, 500);
    }
});

/**
 * Load faculty classes for dropdown
 */
function loadClassesDropdown() {
    $.ajax({
        url: '/FacultyClass/GetMyClasses',
        type: 'GET',
        success: function (response) {
            if (response.success && response.data) {
                populateClassDropdown(response.data);
            }
        },
        error: function (xhr) {
            console.error('Error loading classes:', xhr);
            Toast.error('Failed to load classes');
        }
    });
}

/**
 * Populate class dropdown
 */
function populateClassDropdown(classes) {
    const select = $('#classSelect');
    select.empty();
    select.append('<option value="">Select a class...</option>');

    classes.forEach(cls => {
        select.append(`
            <option value="${cls.classId}">
                ${cls.courseCode} - ${cls.courseName}
            </option>
        `);
    });

    // If currentClassId is set, select it
    if (currentClassId) {
        select.val(currentClassId);
    }
}

/**
 * Handle class selection change
 */
function handleClassChange() {
    const classId = $('#classSelect').val();

    if (!classId) {
        showEmptyState();
        $('#btnMarkAllPresent').prop('disabled', true);
        return;
    }

    currentClassId = parseInt(classId);
    loadAttendanceForDate();
}

/**
 * Handle date selection change
 */
function handleDateChange() {
    selectedDate = $('#dateSelect').val();

    if (currentClassId) {
        loadAttendanceForDate();
    }
}

/**
 * Load attendance for selected date
 */
function loadAttendanceForDate() {
    if (!currentClassId || !selectedDate) {
        return;
    }

    // Enable/disable Mark All Present button based on if date is today
    const today = new Date().toISOString().split('T')[0];
    const isToday = selectedDate === today;
    $('#btnMarkAllPresent').prop('disabled', !isToday);

    initializeDataTable(currentClassId, selectedDate);
}

/**
 * Initialize DataTable
 */
function initializeDataTable(classId, date) {
    // Destroy existing table if it exists
    if (attendanceTable) {
        attendanceTable.destroy();
    }

    // Show card, hide empty state
    $('#attendanceCard').show();
    $('#emptyState').hide();

    attendanceTable = $('#attendanceTable').DataTable({
        ajax: {
            url: `/FacultyAttendance/GetClassAttendance?classId=${classId}&date=${date}`,
            dataSrc: function (json) {
                if (json.success) {
                    // Update card header
                    $('#classTitle').text(`${json.data.courseCode} - Attendance`);
                    $('#classSubtitle').text(`${json.data.date}`);
                    return json.data.students;
                }
                Toast.error(json.message || 'Failed to load attendance');
                return [];
            },
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load attendance data.');
            }
        },
        columns: [
            {
                data: 'studentNumber',
                width: '15%'
            },
            {
                data: 'name',
                width: '25%',
                render: function (data, type, row) {
                    const profileImage = row.profileImage && row.profileImage !== 'default_profile.png' 
                        ? `/images/profile/${row.profileImage}` 
                        : '/images/profile/default_profile.png';
                    return `
                        <div style="display: flex; align-items: center; gap: 0.75rem;">
                            <img src="${profileImage}" 
                                 alt="${data}" 
                                 class="student-profile-img"
                                 onerror="this.src='/images/profile/default_profile.png'">
                            <span>${data}</span>
                        </div>
                    `;
                }
            },
            {
                data: 'timeIn',
                width: '15%',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'timeOut',
                width: '15%',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'status',
                width: '15%',
                render: function (data) {
                    const statusLower = data.toLowerCase();
                    const icon = getStatusIcon(statusLower);
                    return `<span class="status-badge ${statusLower}">${icon}${data}</span>`;
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
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.studId}, '${row.name}', '${row.status}')" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="showDeleteModal(${row.studId}, '${row.name}')" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;d
                }
            }
        ],
        order: [[0, 'asc']],
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
            '<"row"<"col-sm-12"tr>>' +
            '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        drawCallback: function () {
            // Reinitialize Lucide icons after table redraw
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search students...",
            lengthMenu: "Show _MENU_ students",
            info: "Showing _START_ to _END_ of _TOTAL_ students",
            infoEmpty: "No students found",
            infoFiltered: "(filtered from _MAX_ total students)",
            zeroRecords: "No matching students found",
            emptyTable: "No students enrolled in this class"
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
 * Get status icon
 */
function getStatusIcon(status) {
    if (status === 'present') {
        return '<i data-lucide="check-circle" class="status-icon" style="width: 14px; height: 14px;"></i>';
    } else if (status === 'absent') {
        return '<i data-lucide="x-circle" class="status-icon" style="width: 14px; height: 14px;"></i>';
    } else if (status === 'late') {
        return '<i data-lucide="clock" class="status-icon" style="width: 14px; height: 14px;"></i>';
    }
    return '';
}

/**
 * Show edit modal
 */
function showEditModal(studId, studentName, status) {
    $('#editStudId').val(studId);
    $('#editStudentName').val(studentName);
    $('#editStatus').val(status);
    $('#editRemarks').val('');
    $('#editAttendanceModal').modal('show');
}

/**
 * Update attendance
 */
function updateAttendance() {
    const studId = $('#editStudId').val();
    const status = $('#editStatus').val();
    const remarks = $('#editRemarks').val();

    if (!status) {
        Toast.warning('Please select a status');
        return;
    }

    const $btn = $('#btnUpdateAttendance');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/FacultyAttendance/UpdateAttendance',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify({
            classId: currentClassId,
            studId: parseInt(studId),
            status: status,
            remarks: remarks,
            date: selectedDate
        }),
        success: function (response) {
            if (response.success) {
                Toast.success('Attendance updated successfully');
                $('#editAttendanceModal').modal('hide');
                attendanceTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update attendance');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            Toast.error('Failed to update attendance');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Attendance');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show delete modal
 */
function showDeleteModal(studId, studentName) {
    $('#deleteStudId').val(studId);
    $('#deleteAttendanceModalLabel').text(`Delete attendance for ${studentName}?`);
    $('#deleteAttendanceModal').modal('show');
}

/**
 * Confirm delete attendance
 */
function confirmDeleteAttendance() {
    const studId = $('#deleteStudId').val();

    if (!studId) {
        Toast.error('Invalid student ID');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: '/FacultyAttendance/DeleteAttendance',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify({
            classId: currentClassId,
            studId: parseInt(studId),
            date: selectedDate
        }),
        success: function (response) {
            if (response.success) {
                Toast.success('Attendance deleted successfully');
                $('#deleteAttendanceModal').modal('hide');
                attendanceTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete attendance');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            Toast.error('Failed to delete attendance');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Mark all students as present
 */
function markAllPresent() {
    if (!currentClassId) {
        Toast.warning('Please select a class first');
        return;
    }

    const $btn = $('#btnMarkAllPresent');
    $btn.prop('disabled', true).text('Marking...');

    $.ajax({
        url: '/FacultyAttendance/MarkAllPresent',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ classId: currentClassId }),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message || 'All students marked as present');
                attendanceTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to mark attendance');
            }
        },
        error: function (xhr) {
            console.error('Error marking attendance:', xhr);
            Toast.error('Failed to mark attendance');
        },
        complete: function () {
            $btn.prop('disabled', false).text('Mark All Present');
        }
    });
}

/**
 * Show empty state
 */
function showEmptyState() {
    if (attendanceTable) {
        attendanceTable.destroy();
        attendanceTable = null;
    }

    $('#attendanceCard').hide();
    $('#emptyState').show();

    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}
