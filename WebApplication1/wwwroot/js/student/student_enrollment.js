/**
 * Student Enrollment Management - Refactored to use common utilities
 * Manage class enrollments and unenrollments
 */

let studentInfo = null;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load student info and enrollments
    loadStudentInfo();

    // Event handlers
    $('#btnConfirmUnenroll').on('click', confirmUnenroll);

    // Reinitialize icons when modal is shown
    $('#unenrollModal').on('shown.bs.modal', function () {
        initializeLucideIcons();
    });
});

/**
 * Load student information
 */
function loadStudentInfo() {
    $.ajax({
        url: '/StudentEnrollment/GetStudentInfo',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                studentInfo = response.data;
                // Update available classes description with program name
                $('#availableDescription').text(`Classes available for enrollment (filtered by ${studentInfo.programName} program)`);
                loadCurrentEnrollments();
                loadAvailableClasses();
            } else {
                showNoProfileMessage(response.message);
            }
        },
        error: function () {
            console.error('Error loading student info');
            showNoProfileMessage('Failed to load student information. Please try again.');
        }
    });
}

/**
 * Show message when student has no profile
 */
function showNoProfileMessage(message) {
    const html = `
        <div class="empty-state">
            <i data-lucide="alert-circle" class="empty-state-icon"></i>
            <h3 class="empty-state-title">Profile Required</h3>
            <p class="empty-state-description">${message}</p>
        </div>
    `;

    $('#currentEnrollments').html(html);
    $('#availableClasses').html(html);
    $('#currentCount').text('0 Classes');
    $('#availableCount').text('0 Classes');
    initializeLucideIcons();
}

/**
 * Load current enrollments
 */
function loadCurrentEnrollments() {
    $('#currentEnrollments').html('<div class="loading-state"><div class="spinner-border text-primary"></div></div>');

    $.ajax({
        url: '/StudentEnrollment/GetCurrentEnrollments',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                displayCurrentEnrollments(response.data);
            } else {
                $('#currentEnrollments').html('<div class="empty-state"><p>Failed to load enrollments</p></div>');
            }
        },
        error: function () {
            console.error('Error loading enrollments');
            $('#currentEnrollments').html('<div class="empty-state"><p>Failed to load enrollments</p></div>');
        }
    });
}

/**
 * Display current enrollments
 */
function displayCurrentEnrollments(enrollments) {
    const container = $('#currentEnrollments');
    $('#currentCount').text(`${enrollments.length} ${enrollments.length === 1 ? 'Class' : 'Classes'}`);

    if (enrollments.length === 0) {
        container.html(`
            <div class="empty-state">
                <i data-lucide="book-open" class="empty-state-icon"></i>
                <h3 class="empty-state-title">No Enrollments Yet</h3>
                <p class="empty-state-description">You haven't enrolled in any classes yet. Browse available classes below to get started.</p>
            </div>
        `);
        initializeLucideIcons();
        return;
    }

    let html = '';
    enrollments.forEach(enrollment => {
        html += `
            <div class="enrollment-card">
                <div class="enrollment-header">
                    <div class="course-info">
                        <div class="course-code">${enrollment.courseCode}</div>
                        <h3 class="course-name">${enrollment.courseName}</h3>
                    </div>
                    <button class="btn-unenroll" onclick="showUnenrollModal(${enrollment.enrollId})">
                        <i data-lucide="x-circle" class="icon-sm"></i>
                        Unenroll
                    </button>
                </div>
                <div class="enrollment-details">
                    <div class="detail-item">
                        <span class="detail-label">Faculty</span>
                        <span class="detail-value">${enrollment.facultyName}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Schedule</span>
                        <span class="detail-value">${enrollment.schedule || 'TBA'}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Room</span>
                        <span class="detail-value">${enrollment.roomCode}</span>
                    </div>
                </div>
            </div>
        `;
    });

    container.html(html);
    initializeLucideIcons();
}

/**
 * Load available classes
 */
function loadAvailableClasses() {
    $('#availableClasses').html('<div class="loading-state"><div class="spinner-border text-primary"></div></div>');

    $.ajax({
        url: '/StudentEnrollment/GetAvailableClasses',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                displayAvailableClasses(response.data);
            } else {
                $('#availableClasses').html('<div class="empty-state"><p>Failed to load available classes</p></div>');
            }
        },
        error: function () {
            console.error('Error loading available classes');
            $('#availableClasses').html('<div class="empty-state"><p>Failed to load available classes</p></div>');
        }
    });
}

/**
 * Display available classes
 */
function displayAvailableClasses(classes) {
    const container = $('#availableClasses');
    $('#availableCount').text(`${classes.length} ${classes.length === 1 ? 'Class' : 'Classes'}`);

    if (classes.length === 0) {
        container.html(`
            <div class="empty-state">
                <i data-lucide="list" class="empty-state-icon"></i>
                <h3 class="empty-state-title">No Available Classes</h3>
                <p class="empty-state-description">There are no classes available for enrollment at this time, or you're already enrolled in all available classes.</p>
            </div>
        `);
        initializeLucideIcons();
        return;
    }

    let html = '';
    classes.forEach(classItem => {
        html += `
            <div class="enrollment-card">
                <div class="enrollment-header">
                    <div class="course-info">
                        <div class="course-code">${classItem.courseCode}</div>
                        <h3 class="course-name">${classItem.courseName}</h3>
                    </div>
                    <button class="btn-enroll" onclick="enrollInClass(${classItem.classId})">
                        <i data-lucide="plus-circle" class="icon-sm"></i>
                        Enroll
                    </button>
                </div>
                <div class="enrollment-details">
                    <div class="detail-item">
                        <span class="detail-label">Faculty</span>
                        <span class="detail-value">${classItem.facultyName}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Schedule</span>
                        <span class="detail-value">${classItem.schedule || 'TBA'}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Room</span>
                        <span class="detail-value">${classItem.roomCode}</span>
                    </div>
                </div>
            </div>
        `;
    });

    container.html(html);
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Enroll in a class
 */
function enrollInClass(classId) {
    const $btn = window.event.target.closest('button');
    const originalHtml = $btn.innerHTML;
    $btn.disabled = true;
    $btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enrolling...';

    $.ajax({
        url: '/StudentEnrollment/Enroll',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ ClassId: classId }),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                loadCurrentEnrollments();
                loadAvailableClasses();
            } else {
                Toast.error(response.message);
                $btn.disabled = false;
                $btn.innerHTML = originalHtml;
                initializeLucideIcons();
            }
        },
        error: function () {
            console.error('Error enrolling');
            Toast.error('Failed to enroll in class');
            $btn.disabled = false;
            $btn.innerHTML = originalHtml;
            initializeLucideIcons();
        }
    });
}

/**
 * Show unenroll confirmation modal
 */
function showUnenrollModal(enrollId) {
    $('#unenrollId').val(enrollId);
    $('#unenrollModal').modal('show');
}

/**
 * Confirm unenroll
 */
function confirmUnenroll() {
    const enrollId = $('#unenrollId').val();
    const $btn = $('#btnConfirmUnenroll');

    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Unenrolling...');

    $.ajax({
        url: '/StudentEnrollment/Unenroll',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ EnrollId: parseInt(enrollId) }),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                $('#unenrollModal').modal('hide');
                loadCurrentEnrollments();
                loadAvailableClasses();
            } else {
                Toast.error(response.message);
            }
        },
        error: function () {
            console.error('Error unenrolling');
            Toast.error('Failed to unenroll from class');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="x-circle" class="icon-sm"></i> Unenroll');
            initializeLucideIcons();
        }
    });
}
