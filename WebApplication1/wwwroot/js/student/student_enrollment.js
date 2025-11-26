/// <reference path="../../global.d.ts" />

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
                loadBlockedClasses(); // ✅ NEW: Load blocked classes
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
 * ✅ FIXED: Added null/undefined checks with fallback to both camelCase and PascalCase
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
        // ✅ FIX: Handle both camelCase and PascalCase with null/undefined checks
        const enrollId = enrollment.enrollId || enrollment.EnrollId;
        const courseCode = enrollment.courseCode || enrollment.CourseCode || 'UNKNOWN';
        const courseName = enrollment.courseName || enrollment.CourseName || 'Unknown Course';
        const facultyName = enrollment.facultyName || enrollment.FacultyName || 'No Faculty Assigned';
        const roomCode = enrollment.roomCode || enrollment.RoomCode || 'TBA';
        const schedule = enrollment.schedule || enrollment.Schedule || 'TBA';

        html += `
            <div class="enrollment-card">
                <div class="enrollment-header">
                    <div class="course-info">
                        <div class="course-code">${courseCode}</div>
                        <h3 class="course-name">${courseName}</h3>
                    </div>
                    <button class="btn-unenroll" onclick="showUnenrollModal(${enrollId})">
                        <i data-lucide="x-circle" class="icon-sm"></i>
                        Unenroll
                    </button>
                </div>
                <div class="enrollment-details">
                    <div class="detail-item">
                        <span class="detail-label">Faculty</span>
                        <span class="detail-value">${facultyName}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Schedule</span>
                        <span class="detail-value">${schedule}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Room</span>
                        <span class="detail-value">${roomCode}</span>
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
 * ✅ FIXED: Added null/undefined checks with fallback to both camelCase and PascalCase
 */
function displayAvailableClasses(classes) {
    const container = $('#availableClasses');
    $('#availableCount').text(`${classes.length} ${classes.length === 1 ? 'Class' : 'Classes'}`);

    if (classes.length === 0) {
        container.html(`
            <div class="empty-state">
                <i data-lucide="list" class="empty-state-icon"></i>
                <h3 class="empty-state-title">No Available Classes</h3>
                <p class="empty-state-description">There are no classes available for enrollment at this time, or you're already enrolled in all available classes for your program.</p>
            </div>
        `);
        initializeLucideIcons();
        return;
    }

    let html = '';
    classes.forEach(classItem => {
        // ✅ FIX: Handle both camelCase and PascalCase with null/undefined checks
        const courseCode = classItem.courseCode || classItem.CourseCode || 'UNKNOWN';
        const courseName = classItem.courseName || classItem.CourseName || 'Unknown Course';
        const facultyName = classItem.facultyName || classItem.FacultyName || 'No Faculty Assigned';
        const roomCode = classItem.roomCode || classItem.RoomCode || 'TBA';
        const schedule = classItem.schedule || classItem.Schedule || 'TBA';
        const classId = classItem.classId || classItem.ClassId;

        html += `
            <div class="enrollment-card">
                <div class="enrollment-header">
                    <div class="course-info">
                        <div class="course-code">${courseCode}</div>
                        <h3 class="course-name">${courseName}</h3>
                    </div>
                    <button class="btn-enroll" onclick="enrollInClass(${classId})">
                        <i data-lucide="plus-circle" class="icon-sm"></i>
                        Enroll
                    </button>
                </div>
                <div class="enrollment-details">
                    <div class="detail-item">
                        <span class="detail-label">Faculty</span>
                        <span class="detail-value">${facultyName}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Schedule</span>
                        <span class="detail-value">${schedule}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Room</span>
                        <span class="detail-value">${roomCode}</span>
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

// ✅ CRITICAL FIX: Global flag to prevent concurrent enrollment requests (race condition)
let isEnrollmentInProgress = false;

/**
 * Enroll in a class
 * ✅ UPDATED: Uses SweetAlert2 for all blocked scenarios with consistent Faculty-style design
 * ✅ FIXED: Added global lock to prevent race conditions when clicking multiple enroll buttons
 */
function enrollInClass(classId) {
    // ✅ FIX: Validate classId
    if (!classId || classId === 'undefined' || classId === undefined) {
        Toast.error('Invalid class ID. Please refresh the page and try again.');
        console.error('Invalid classId:', classId);
        return;
    }

    // ✅ CRITICAL FIX: Prevent concurrent enrollment requests
    if (isEnrollmentInProgress) {
        Toast.warning('Please wait for the current enrollment to complete.');
        console.warn('Enrollment already in progress, blocking concurrent request');
        return;
    }

    // Set global lock
    isEnrollmentInProgress = true;

    const $btn = window.event.target.closest('button');
    const originalHtml = $btn.innerHTML;
    
    // ✅ CRITICAL FIX: Disable ALL enroll buttons to prevent race conditions
    const $allEnrollBtns = $('.btn-enroll');
    $allEnrollBtns.prop('disabled', true);
    
    $btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enrolling...';

    $.ajax({
        url: '/StudentEnrollment/Enroll',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ ClassId: parseInt(classId) }),
        success: function (response) {
            // ✅ CRITICAL FIX: Release global lock on success
            isEnrollmentInProgress = false;
            
            if (response.success) {
                Toast.success(response.message || 'Successfully enrolled in class');
                loadCurrentEnrollments();
                loadAvailableClasses();
            } else {
                // ✅ REFACTORED: Check if this is a TBA error (Scenario #6)
                if (response.isTbaError) {
                    // Parse TBA error message
                    const messageParts = response.message.split('\n\n');
                    const tbaHeader = messageParts[0] || '';
                    const tbaDetails = messageParts[1] || '';
                    const tbaReason = messageParts[2] || '';
                    const tbaSolution = messageParts[3] || '';
                    
                    // Show SweetAlert2 modal for TBA with Faculty-style design
                    Swal.fire({
                        icon: 'error',
                        title: 'TBA Schedule Not Allowed',
                        html: `
                            <div style="text-align: left; margin-top: 0.75rem;">
                                <!-- TBA Header -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    font-size: 0.9375rem;
                                    font-weight: 600;
                                    color: #991b1b;
                                    line-height: 1.5;
                                ">
                                    ⚠️ ${tbaHeader.replace('⚠️ TBA Schedule Not Allowed', 'TBA Schedule Not Allowed')}
                                </p>
                                
                                <!-- TBA Details -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fef2f2;
                                    border-left: 3px solid #dc2626;
                                    border-radius: 6px;
                                    font-size: 0.9375rem;
                                    line-height: 1.65;
                                    color: #7f1d1d;
                                ">
                                    ${tbaDetails}
                                </p>
                                
                                <!-- Reason -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fefce8;
                                    border-left: 3px solid #f59e0b;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #78350f;
                                ">
                                    ${tbaReason}
                                </p>
                                
                                <!-- Solution -->
                                <p style="
                                    margin: 0;
                                    padding: 0.875rem;
                                    background: #eff6ff;
                                    border-left: 3px solid #3b82f6;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #1e40af;
                                ">
                                    ${tbaSolution}
                                </p>
                            </div>
                        `,
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#d33',
                        width: '580px',
                        padding: '1.75rem 2rem 2rem',
                        customClass: {
                            popup: 'enrollment-error-modal',
                            title: 'enrollment-error-title',
                            htmlContainer: 'enrollment-error-content',
                            confirmButton: 'enrollment-error-button'
                        },
                        buttonsStyling: false,
                        allowOutsideClick: false
                    });
                }
                // ✅ REFACTORED: Check if this is a time conflict error (Scenarios #1, #4, #5)
                else if (response.isConflict) {
                    // Parse schedule conflict message
                    const messageParts = response.message.split('\n\n');
                    const conflictHeader = messageParts[0] || '';
                    const conflictDetails = messageParts[1] || '';
                    const conflictNewClass = messageParts[2] || '';
                    const conflictSolution = messageParts[3] || '';
                    
                    // Show SweetAlert2 modal for time conflicts with Faculty-style design
                    Swal.fire({
                        icon: 'error',
                        title: 'Schedule Conflict Detected',
                        html: `
                            <div style="text-align: left; margin-top: 0.75rem;">
                                <!-- Conflict Header -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    font-size: 0.9375rem;
                                    font-weight: 600;
                                    color: #991b1b;
                                    line-height: 1.5;
                                ">
                                    ⚠️ ${conflictHeader.replace('⚠️ Schedule Conflict Detected!', 'Schedule Conflict Detected!')}
                                </p>
                                
                                <!-- Existing Class Details -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fef2f2;
                                    border-left: 3px solid #dc2626;
                                    border-radius: 6px;
                                    font-size: 0.9375rem;
                                    line-height: 1.65;
                                    color: #7f1d1d;
                                ">
                                    📚 ${conflictDetails}
                                </p>
                                
                                <!-- New Class Details -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fefce8;
                                    border-left: 3px solid #f59e0b;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #78350f;
                                ">
                                    ${conflictNewClass}
                                </p>
                                
                                <!-- Solution -->
                                <p style="
                                    margin: 0;
                                    padding: 0.875rem;
                                    background: #eff6ff;
                                    border-left: 3px solid #3b82f6;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #1e40af;
                                ">
                                    ${conflictSolution}
                                </p>
                            </div>
                        `,
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#d33',
                        width: '580px',
                        padding: '1.75rem 2rem 2rem',
                        customClass: {
                            popup: 'enrollment-error-modal',
                            title: 'enrollment-error-title',
                            htmlContainer: 'enrollment-error-content',
                            confirmButton: 'enrollment-error-button'
                        },
                        buttonsStyling: false,
                        allowOutsideClick: false
                    });
                } else {
                    // Regular error - use toast
                    Toast.error(response.message || 'Failed to enroll in class');
                }
                
                // ✅ Re-enable all enroll buttons on error
                $allEnrollBtns.prop('disabled', false);
                $btn.innerHTML = originalHtml;
                initializeLucideIcons();
            }
        },
        error: function (xhr, status, error) {
            // ✅ CRITICAL FIX: Release global lock on error
            isEnrollmentInProgress = false;
            console.error('Enrollment error:', error);
            console.error('Response:', xhr.responseText);
            console.error('Status:', xhr.status);
            
            let errorMessage = 'An error occurred while enrolling';
            try {
                const response = JSON.parse(xhr.responseText);
                errorMessage = response.message || errorMessage;
            } catch (e) {
                // Use default error message
            }
            
            Toast.error(errorMessage);
            // ✅ Re-enable all enroll buttons on error
            $allEnrollBtns.prop('disabled', false);
            $btn.innerHTML = originalHtml;
            initializeLucideIcons();
        }
    });
}

/**
 * ✅ NEW: Load blocked/rejected classes
 */
function loadBlockedClasses() {
    $.ajax({
        url: '/StudentEnrollment/GetBlockedClasses',
        type: 'GET',
        success: function (response) {
            console.log('GetBlockedClasses response:', response);
            if (response.success) {
                if (response.data && response.data.length > 0) {
                    console.log('First blocked class:', response.data[0]);
                    console.log('Property names:', Object.keys(response.data[0]));
                }
                displayBlockedClasses(response.data);
            }
        },
        error: function () {
            console.error('Error loading blocked classes');
        }
    });
}

/**
 * ✅ NEW: Display blocked/rejected classes
 * ✅ FIXED: Handle both camelCase and PascalCase property names from API
 */
function displayBlockedClasses(blockedClasses) {
    const container = $('#blockedClasses');
    const section = $('#blockedSection');
    
    if (!blockedClasses || blockedClasses.length === 0) {
        section.hide();
        return;
    }
    
    section.show();
    $('#blockedCount').text(`${blockedClasses.length} ${blockedClasses.length === 1 ? 'Class' : 'Classes'}`);
    
    let html = '';
    blockedClasses.forEach(blocked => {
        // ✅ FIX: Handle both camelCase and PascalCase with null/undefined checks
        const blockedAtValue = blocked.BlockedAt || blocked.blockedAt;
        const blockedDate = blockedAtValue ? new Date(blockedAtValue).toLocaleDateString() : 'Invalid Date';
        
        // Get course info - check for actual values vs fallback values
        let courseCode = blocked.CourseCode || blocked.courseCode || '';
        let courseName = blocked.CourseName || blocked.courseName || '';
        
        // If course info is missing or is the fallback value, show a more helpful message
        const isCourseUnknown = !courseCode || courseCode === 'UNKNOWN' || courseCode === '';
        if (isCourseUnknown) {
            courseCode = 'Class #' + (blocked.ClassId || blocked.classId || '?');
            courseName = 'Course information unavailable';
        }
        
        const facultyName = blocked.FacultyName || blocked.facultyName || 'N/A';
        const roomCode = blocked.RoomCode || blocked.roomCode || 'TBA';
        const schedule = blocked.Schedule || blocked.schedule || 'TBA';
        const reason = blocked.BlockReason || blocked.blockReason || 'No reason provided';
        
        html += `
            <div class="enrollment-card blocked-card">
                <div class="enrollment-header">
                    <div class="course-info">
                        <div class="course-code">${courseCode}</div>
                        <h3 class="course-name">${courseName}</h3>
                    </div>
                    <span class="blocked-badge">
                        <i data-lucide="ban" class="icon-sm"></i>
                        Blocked
                    </span>
                </div>
                <div class="enrollment-details">
                    <div class="detail-item">
                        <span class="detail-label">Faculty</span>
                        <span class="detail-value">${facultyName}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Schedule</span>
                        <span class="detail-value">${schedule}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Room</span>
                        <span class="detail-value">${roomCode}</span>
                    </div>
                </div>
                <div class="blocked-reason">
                    <i data-lucide="info" class="icon-sm"></i>
                    <span><strong>Reason:</strong> ${reason}</span>
                    <span class="blocked-date">Blocked on ${blockedDate}</span>
                </div>
            </div>
        `;
    });
    
    container.html(html);
    initializeLucideIcons();
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
