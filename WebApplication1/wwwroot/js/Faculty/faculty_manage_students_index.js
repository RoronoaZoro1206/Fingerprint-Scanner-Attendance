/**
 * Faculty Manage Students Index - Class selection for student management
 * Similar to faculty_class.js but navigates to ManageStudents page
 */

$(document).ready(function () {
    initializeLucideIcons();
    loadMyClasses();
});

/**
 * Load faculty's classes
 */
function loadMyClasses() {
    const container = $('#classesContainer');
    
    container.html(`
        <div class="loading-container text-center py-5">
            <div class="spinner-border text-primary"></div>
            <p class="mt-2 text-muted">Loading classes...</p>
        </div>
    `);

    $.ajax({
        url: '/FacultyClass/GetMyClasses',
        type: 'GET',
        success: function (response) {
            if (response.success && response.data) {
                displayClasses(response.data);
            } else {
                showError(response.message || 'Failed to load classes');
            }
        },
        error: function () {
            showError('Failed to load classes. Please try again.');
        }
    });
}

/**
 * Display classes grid
 */
function displayClasses(classes) {
    const container = $('#classesContainer');

    if (!classes || classes.length === 0) {
        container.html(`
            <div class="empty-state text-center py-5">
                <i data-lucide="book-x" class="empty-icon mb-3" style="width: 64px; height: 64px; color: #9ca3af;"></i>
                <h4>No Classes Found</h4>
                <p class="text-muted">You don't have any classes assigned yet.</p>
            </div>
        `);
        initializeLucideIcons();
        return;
    }

    let html = '';
    classes.forEach(cls => {
        const courseCode = cls.courseCode || 'N/A';
        const courseName = cls.courseName || 'Unknown Course';
        const roomName = cls.roomName || 'TBA';
        const schedule = cls.schedule || 'TBA';
        const studentCount = cls.studentCount || 0;

        html += `
            <div class="class-card">
                <div class="class-header">
                    <h3 class="class-code">${courseCode}</h3>
                    <span class="student-badge">${studentCount} Student${studentCount !== 1 ? 's' : ''}</span>
                </div>
                <div class="class-info">
                    <div class="info-row">
                        <span class="info-label">Course:</span>
                        <span class="info-value">${courseName}</span>
                    </div>
                    <div class="info-row">
                        <span class="info-label">Room:</span>
                        <span class="info-value">${roomName}</span>
                    </div>
                    <div class="info-row">
                        <span class="info-label">Schedule:</span>
                        <span class="info-value">${schedule}</span>
                    </div>
                </div>
                <div class="class-actions">
                    <a href="/Faculty/ManageStudents?classId=${cls.classId}" class="btn-manage-students">
                        <i data-lucide="users"></i>
                        Manage Students
                    </a>
                </div>
            </div>
        `;
    });

    container.html(html);
    initializeLucideIcons();
}

/**
 * Show error state
 */
function showError(message) {
    $('#classesContainer').html(`
        <div class="empty-state text-center py-5">
            <i data-lucide="alert-circle" class="empty-icon mb-3 text-danger" style="width: 64px; height: 64px;"></i>
            <h4>Error</h4>
            <p class="text-muted">${message}</p>
            <button class="btn btn-primary mt-3" onclick="loadMyClasses()">
                <i data-lucide="refresh-cw" class="icon-sm"></i>
                Try Again
            </button>
        </div>
    `);
    initializeLucideIcons();
}
