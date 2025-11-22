/**
 * Faculty Attendance - Refactored to use common utilities
 * Monitor and manage class attendance with DataTables
 */

let attendanceTable;
let currentClassId = null;
let selectedDate = null;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

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

    // Setup modal icon reinitialization
    setupModalIconReinit(['editAttendanceModal', 'deleteAttendanceModal']);

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
                console.log('✅ DataTables AJAX response:', json); // Enhanced debug log
                
                if (!json) {
                    console.error('❌ Response is null or undefined');
                    Toast.error('No response from server');
                    return [];
                }
                
                if (json.success === false) {
                    console.error('❌ Server returned error:', json.message);
                    Toast.error(json.message || 'Failed to load attendance');
                    showEmptyState();
                    return [];
                }
                
                if (!json.data) {
                    console.error('❌ Response missing data property');
                    Toast.error('Invalid response format');
                    showEmptyState();
                    return [];
                }
                
                // Update card header
                $('#classTitle').text(`${json.data.courseCode || 'Class'} - Attendance`);
                $('#classSubtitle').text(`${json.data.date || date}`);
                
                // ✅ FIX: Handle both 'students' (camelCase) and 'Students' (PascalCase)
                const studentsArray = json.data.students || json.data.Students || [];
                
                console.log(`✅ Found ${studentsArray.length} students in response`);
                
                if (studentsArray.length === 0) {
                    console.warn('⚠️ No students in class or no enrollment data');
                    Toast.info('No students enrolled in this class');
                }
                
                return studentsArray;
            },
            error: function (xhr, error, thrown) {
                console.error('❌ DataTables AJAX error:', {
                    status: xhr.status,
                    statusText: xhr.statusText,
                    error: error,
                    thrown: thrown,
                    responseText: xhr.responseText
                });
                
                // Parse error message from response
                let errorMessage = 'Failed to load attendance data';
                try {
                    const errorResponse = JSON.parse(xhr.responseText);
                    errorMessage = errorResponse.message || errorMessage;
                } catch (e) {
                    // If response is not JSON, use status text
                    if (xhr.statusText) {
                        errorMessage = xhr.statusText;
                    }
                }
                
                Toast.error(errorMessage);
                showEmptyState();
            }
        },
        columns: [
            {
                data: 'studentNumber',
                width: '15%',
                defaultContent: '-',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'name',
                width: '25%',
                defaultContent: '-',
                render: function (data, type, row) {
                    if (!data) return '-';
                    
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
                defaultContent: '-',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'timeOut',
                width: '15%',
                defaultContent: '-',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'status',
                width: '15%',
                defaultContent: 'absent',
                render: function (data) {
                    if (!data) data = 'absent';
                    
                    const statusLower = data.toLowerCase();
                    const icon = getStatusIcon(statusLower);
                    const displayStatus = data.charAt(0).toUpperCase() + data.slice(1).toLowerCase();
                    return `<span class="status-badge ${statusLower}">${icon}${displayStatus}</span>`;
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                defaultContent: '',
                render: function (data, type, row) {
                    if (!row.studId) {
                        console.warn('⚠️ Row missing studId:', row);
                        return '';
                    }
                    
                    // Escape quotes in name for onclick attribute
                    const escapedName = (row.name || '').replace(/'/g, "\\'").replace(/"/g, '&quot;');
                    const status = row.status || 'Absent';
                    
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.studId}, '${escapedName}', '${status}')" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="showDeleteModal(${row.studId}, '${escapedName}')" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        drawCallback: function() {
            // Reinitialize Lucide icons after table is drawn
            initializeLucideIcons();
        },
        ...commonDataTableConfig,
        language: getDataTableLanguage('students', 'student')
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
    showModal('editAttendanceModal');
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

    const attendanceData = {
        classId: currentClassId,
        studId: parseInt(studId),
        status: status,
        remarks: remarks,
        date: selectedDate
    };

    const $btn = $('#btnUpdateAttendance');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/FacultyAttendance/UpdateAttendance', attendanceData,
        function () {
            Toast.success('Attendance updated successfully');
            hideModal('editAttendanceModal');
            attendanceTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Show delete modal
 */
function showDeleteModal(studId, studentName) {
    $('#deleteStudId').val(studId);
    $('#deleteAttendanceModalLabel').text(`Delete attendance for ${studentName}?`);
    showModal('deleteAttendanceModal');
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

    const attendanceData = {
        classId: currentClassId,
        studId: parseInt(studId),
        date: selectedDate
    };

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete('/FacultyAttendance/DeleteAttendance', 
        function () {
            Toast.success('Attendance deleted successfully');
            hideModal('deleteAttendanceModal');
            attendanceTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        },
        attendanceData
    );
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
    setButtonLoading($btn, 'Marking...');

    ajaxPost('/FacultyAttendance/MarkAllPresent', { classId: currentClassId },
        function (response) {
            Toast.success(response.message || 'All students marked as present');
            attendanceTable.ajax.reload(null, false);
            resetButton($btn, 'Mark All Present');
        },
        function () {
            resetButton($btn, 'Mark All Present');
        }
    );
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
    initializeLucideIcons();
}
