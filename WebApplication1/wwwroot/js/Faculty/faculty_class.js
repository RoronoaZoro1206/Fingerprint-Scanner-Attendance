/**
 * Faculty Classes - Refactored to use common utilities
 * Display faculty's assigned classes in a grid layout
 */

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load classes
    loadClasses();
});

/**
 * Load faculty classes
 */
function loadClasses() {
    const container = $('#classesContainer');
    
    // Show loading state
    container.html(`
        <div class="loading-container">
            <div class="spinner-border"></div>
        </div>
    `);

    $.ajax({
        url: '/FacultyClass/GetMyClasses',
        type: 'GET',
        success: function (response) {
            if (response.success && response.data && response.data.length > 0) {
                displayClasses(response.data);
            } else {
                showEmptyState();
            }
        },
        error: function (xhr) {
            console.error('Error loading classes:', xhr);
            Toast.error('Failed to load classes');
            showEmptyState();
        }
    });
}

/**
 * Display classes in grid
 */
function displayClasses(classes) {
    const container = $('#classesContainer');
    container.empty();

    classes.forEach(cls => {
        const classCard = createClassCard(cls);
        container.append(classCard);
    });

    // Reinitialize Lucide icons
    initializeLucideIcons();
}

/**
 * Create class card HTML
 */
function createClassCard(cls) {
    return `
        <div class="class-card">
            <div class="class-header">
                <h3 class="class-code">${cls.courseCode || 'N/A'}</h3>
                <span class="student-badge">${cls.studentCount} Student${cls.studentCount !== 1 ? 's' : ''}</span>
            </div>
            
            <div class="class-info">
                <div class="info-row">
                    <span class="info-label">Course:</span>
                    <span class="info-value">${cls.courseName || 'N/A'}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Room:</span>
                    <span class="info-value">${cls.roomName || 'N/A'}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Schedule:</span>
                    <span class="info-value">${cls.schedule || 'Not scheduled'}</span>
                </div>
            </div>
            
            <div class="class-actions">
                <button class="btn-attendance" onclick="goToAttendance(${cls.classId})">
                    <i data-lucide="user-check"></i>
                    Attendance
                </button>
            </div>
        </div>
    `;
}

/**
 * Show empty state when no classes
 */
function showEmptyState() {
    const container = $('#classesContainer');
    container.html(`
        <div class="empty-state">
            <i data-lucide="book-open"></i>
            <h3>No Classes Assigned</h3>
            <p>You don't have any classes assigned yet. Please contact administration.</p>
        </div>
    `);

    // Reinitialize Lucide icons
    initializeLucideIcons();
}

/**
 * Navigate to attendance page for a class
 */
function goToAttendance(classId) {
    window.location.href = `/FacultyAttendance/Index?classId=${classId}`;
}
