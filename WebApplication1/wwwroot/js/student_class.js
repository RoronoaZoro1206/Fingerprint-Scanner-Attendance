/**
 * Student My Classes
 */

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load classes
    loadMyClasses();
});

/**
 * Load student's enrolled classes
 */
function loadMyClasses() {
    $('#loadingState').show();
    $('#classesContainer').hide();
    $('#emptyState').hide();

    $.ajax({
        url: '/StudentClass/GetMyClasses',
        type: 'GET',
        success: function (response) {
            $('#loadingState').hide();

            if (response.success) {
                if (response.data && response.data.length > 0) {
                    displayClasses(response.data);
                } else {
                    $('#emptyState').show();
                }
            } else {
                Toast.error(response.message || 'Failed to load classes');
                $('#emptyState').show();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading classes:', error);
            $('#loadingState').hide();
            Toast.error('Failed to load classes');
            $('#emptyState').show();
        }
    });
}

/**
 * Display classes in grid
 */
function displayClasses(classes) {
    const container = $('#classesContainer');
    container.empty();

    classes.forEach(classItem => {
        const attendanceClass = getAttendanceClass(classItem.attendanceRate);
        const barClass = getAttendanceBarClass(classItem.attendanceRate);

        const card = `
            <div class="class-card">
                <div class="class-header">
                    <div class="class-info">
                        <div class="class-code">${classItem.courseCode}</div>
                        <p class="class-name">${classItem.courseName}</p>
                    </div>
                    <span class="class-units">${classItem.courseUnits} units</span>
                </div>

                <div class="class-details">
                    <div class="detail-row">
                        <span class="detail-label">Faculty:</span>
                        <span class="detail-value">${classItem.facultyName}</span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">Room:</span>
                        <span class="detail-value">${classItem.roomCode}</span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">Schedule:</span>
                        <span class="detail-value">${classItem.schedule || 'TBA'}</span>
                    </div>
                </div>

                <div class="attendance-section">
                    <span class="attendance-label">Attendance Rate:</span>
                    <div class="attendance-info">
                        <div class="attendance-bar-container">
                            <div class="attendance-bar ${barClass}" style="width: ${classItem.attendanceRate}%"></div>
                        </div>
                        <span class="attendance-percentage ${attendanceClass}">${classItem.attendanceRate}%</span>
                    </div>
                </div>
            </div>
        `;

        container.append(card);
    });

    container.show();

    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Get attendance percentage text class
 */
function getAttendanceClass(rate) {
    if (rate >= 90) return 'high';
    if (rate >= 75) return 'medium';
    return 'low';
}

/**
 * Get attendance bar color class
 */
function getAttendanceBarClass(rate) {
    if (rate >= 90) return '';
    if (rate >= 75) return 'warning';
    return 'danger';
}
