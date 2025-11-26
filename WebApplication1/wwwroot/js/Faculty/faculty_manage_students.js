/**
 * Faculty Manage Students - Dedicated page for managing enrolled students
 * Replaces the modal-based approach with a full page view
 */

// Store all students for filtering
let allStudents = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Validate classId
    if (!classId || classId <= 0) {
        showError('Invalid class ID. Please go back and select a class.');
        return;
    }

    // Load class details and students
    loadClassDetails();
    loadEnrolledStudents();

    // Event handlers
    $('#btnConfirmForceUnenroll').on('click', confirmForceUnenroll);
    $('#searchInput').on('input', filterStudents);

    // Reinitialize icons when modal is shown
    $('#forceUnenrollModal').on('shown.bs.modal', function () {
        initializeLucideIcons();
    });
});

/**
 * Load class details
 */
function loadClassDetails() {
    $.ajax({
        url: `/FacultyClass/GetClassDetails?classId=${classId}`,
        type: 'GET',
        success: function (response) {
            if (response.success && response.data) {
                $('#classCode').text(response.data.courseCode);
                $('#className').text(response.data.courseName);
                $('#classSchedule').html(`<i data-lucide="calendar" class="icon-sm me-1"></i>${response.data.schedule}`);
                initializeLucideIcons();
            } else {
                $('#classCode').text('Class Not Found');
                $('#className').text(response.message || 'Unable to load class details');
            }
        },
        error: function () {
            $('#classCode').text('Error');
            $('#className').text('Failed to load class details');
        }
    });
}

/**
 * Load enrolled students for the class
 */
function loadEnrolledStudents() {
    const container = $('#studentsContainer');

    $.ajax({
        url: `/FacultyClass/GetEnrolledStudents?classId=${classId}`,
        type: 'GET',
        success: function (response) {
            console.log('GetEnrolledStudents response:', response);
            if (response.success) {
                allStudents = response.data || [];
                console.log('Students data:', allStudents);
                if (allStudents.length > 0) {
                    console.log('First student object keys:', Object.keys(allStudents[0]));
                    console.log('First student data:', allStudents[0]);
                }
                displayStudents(allStudents);
                $('#studentCount').text(allStudents.length);
            } else {
                showError(response.message || 'Failed to load students');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading students:', { xhr, status, error });
            showError('Failed to load students. Please try again.');
        }
    });
}


/**
 * Display students list
 */
function displayStudents(students) {
    const container = $('#studentsContainer');

    if (!students || students.length === 0) {
        container.html(`
            <div class="empty-state">
                <i data-lucide="users" class="empty-icon"></i>
                <h4>No Students Enrolled</h4>
                <p>There are no students enrolled in this class yet.</p>
            </div>
        `);
        initializeLucideIcons();
        return;
    }

    let html = '<div class="students-list">';

    students.forEach(student => {
        // Safely handle null/undefined values
        // Note: API returns PascalCase properties (FullName, StudNumber, etc.)
        // Get raw values first
        const rawFullName = student.FullName || student.fullName || '';
        const rawStudNumber = student.StudNumber || student.studNumber || '';
        const rawProgramName = student.ProgramName || student.programName || '';
        const rawYearLevel = student.YearLevel || student.yearLevel || 0;
        
        // Apply display logic - treat 'Unknown Student', 'N/A', empty strings as missing data
        const fullName = rawFullName && rawFullName !== 'Unknown Student' && rawFullName.trim() !== '' 
            ? rawFullName : 'Unknown Student';
        const studNumber = rawStudNumber && rawStudNumber !== 'N/A' && rawStudNumber.trim() !== '' 
            ? rawStudNumber : 'No ID';
        const programName = rawProgramName && rawProgramName !== 'N/A' && rawProgramName.trim() !== '' 
            ? rawProgramName : 'No Program';
        const yearLevel = rawYearLevel && rawYearLevel > 0 ? rawYearLevel : null;
        const yearLevelDisplay = yearLevel ? `Year ${yearLevel}` : 'N/A';
        const profileImage = student.ProfileImage || student.profileImage || 'templates/default_profile.png';
        const enrollId = student.EnrollId || student.enrollId;
        
        let enrollDate = 'N/A';
        const enrolledAtValue = student.EnrolledAt || student.enrolledAt;
        if (enrolledAtValue) {
            try {
                const date = new Date(enrolledAtValue);
                // Check if date is valid and not the minimum date
                if (!isNaN(date.getTime()) && date.getFullYear() > 1970) {
                    enrollDate = date.toLocaleDateString('en-US', {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric'
                    });
                }
            } catch (e) {
                enrollDate = 'N/A';
            }
        }

        // Escape the name for use in onclick handler
        const escapedName = fullName.replace(/'/g, "\\'").replace(/"/g, '\\"');

        html += `
            <div class="student-card" data-student-name="${fullName.toLowerCase()}">
                <div class="student-avatar">
                    <img src="/images/profile/${profileImage}" 
                         alt="${fullName}"
                         onerror="this.onerror=null; this.src='/images/profile/templates/default_profile.png';">
                </div>
                <div class="student-info">
                    <h4 class="student-name">${fullName}</h4>
                    <div class="student-details">
                        <span class="detail-item">
                            <i data-lucide="hash" class="icon-xs"></i>
                            ${studNumber}
                        </span>
                        <span class="detail-item">
                            <i data-lucide="graduation-cap" class="icon-xs"></i>
                            ${programName}
                        </span>
                        <span class="detail-item">
                            <i data-lucide="layers" class="icon-xs"></i>
                            ${yearLevelDisplay}
                        </span>
                    </div>
                    <div class="student-meta">
                        <span class="enrolled-date">
                            <i data-lucide="calendar-check" class="icon-xs"></i>
                            Enrolled: ${enrollDate}
                        </span>
                    </div>
                </div>
                <div class="student-actions">
                    <button class="btn btn-outline-danger btn-sm" 
                            onclick="showForceUnenrollModal(${enrollId}, ${classId}, '${escapedName}')">
                        <i data-lucide="user-x" class="icon-sm"></i>
                        Unenroll
                    </button>
                </div>
            </div>
        `;
    });

    html += '</div>';
    container.html(html);
    initializeLucideIcons();
}

/**
 * Filter students based on search input
 */
function filterStudents() {
    const searchTerm = $('#searchInput').val().toLowerCase().trim();

    if (!searchTerm) {
        displayStudents(allStudents);
        return;
    }

    const filtered = allStudents.filter(student => {
        // Support both PascalCase (API) and camelCase property names
        const fullName = (student.FullName || student.fullName || '').toLowerCase();
        const studNumber = (student.StudNumber || student.studNumber || '').toLowerCase();
        const programName = (student.ProgramName || student.programName || '').toLowerCase();
        
        return fullName.includes(searchTerm) ||
               studNumber.includes(searchTerm) ||
               programName.includes(searchTerm);
    });

    displayStudents(filtered);
}

/**
 * Show error state
 */
function showError(message) {
    $('#studentsContainer').html(`
        <div class="empty-state error">
            <i data-lucide="alert-circle" class="empty-icon text-danger"></i>
            <h4>Error</h4>
            <p>${message}</p>
            <a href="/FacultyClass/Index" class="btn btn-primary mt-3">
                <i data-lucide="arrow-left" class="icon-sm"></i>
                Back to Classes
            </a>
        </div>
    `);
    initializeLucideIcons();
}

/**
 * Show force unenroll confirmation modal
 */
function showForceUnenrollModal(enrollId, classId, studentName) {
    $('#unenrollEnrollId').val(enrollId);
    $('#unenrollClassId').val(classId);
    $('#unenrollStudentName').text(studentName);
    $('#unenrollReason').val('');

    $('#forceUnenrollModal').modal('show');
}

/**
 * Confirm force unenroll action
 */
function confirmForceUnenroll() {
    const enrollId = parseInt($('#unenrollEnrollId').val());
    const unenrollClassId = parseInt($('#unenrollClassId').val());
    const reason = $('#unenrollReason').val().trim();
    const studentName = $('#unenrollStudentName').text();

    const $btn = $('#btnConfirmForceUnenroll');
    const originalHtml = $btn.html();

    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

    $.ajax({
        url: '/FacultyClass/ForceUnenrollStudent',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            EnrollId: enrollId,
            ClassId: unenrollClassId,
            Reason: reason || null
        }),
        success: function (response) {
            if (response.success) {
                $('#forceUnenrollModal').modal('hide');

                // Show success message
                Swal.fire({
                    icon: 'success',
                    title: 'Student Removed',
                    html: `<p><strong>${studentName}</strong> has been removed from this class and blocked from re-enrolling.</p>`,
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#10b981'
                });

                // Reload students list
                loadEnrolledStudents();
            } else {
                Toast.error(response.message || 'Failed to remove student');
            }
        },
        error: function () {
            Toast.error('An error occurred while removing the student');
        },
        complete: function () {
            $btn.prop('disabled', false).html(originalHtml);
            initializeLucideIcons();
        }
    });
}
