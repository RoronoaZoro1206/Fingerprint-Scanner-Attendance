/**
 * Faculty Attendance Reports
 */

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load report data
    loadReportData();
});

/**
 * Load report data
 */
function loadReportData() {
    $('#loadingState').show();
    $('#attendanceTable').hide();
    $('#emptyState').hide();

    $.ajax({
        url: '/FacultyReport/GetReportData',
        type: 'GET',
        success: function (response) {
            $('#loadingState').hide();

            if (response.success && response.data) {
                displayStatistics(response.data.statistics);
                
                if (response.data.records && response.data.records.length > 0) {
                    displayAttendanceRecords(response.data.records);
                    $('#attendanceTable').show();
                } else {
                    $('#emptyState').show();
                }
            } else {
                Toast.error(response.message || 'Failed to load report data');
                $('#emptyState').show();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading report data:', error);
            $('#loadingState').hide();
            Toast.error('Failed to load report data');
            $('#emptyState').show();
        }
    });
}

/**
 * Display statistics cards
 */
function displayStatistics(stats) {
    const statsHtml = `
        <div class="stat-card">
            <div class="stat-icon info">
                <i data-lucide="percent"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.averageAttendance}%</div>
                <div class="stat-label">Average Attendance</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon warning">
                <i data-lucide="clock"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.lateArrivalsToday}</div>
                <div class="stat-label">Late Arrivals Today</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon danger">
                <i data-lucide="user-x"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.absentStudents}</div>
                <div class="stat-label">Absent Students</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon success">
                <i data-lucide="users"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.totalStudents}</div>
                <div class="stat-label">Total Students</div>
            </div>
        </div>
    `;

    $('#statsGrid').html(statsHtml);

    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Display attendance records in table
 */
function displayAttendanceRecords(records) {
    const tbody = $('#attendanceTableBody');
    tbody.empty();

    records.forEach(record => {
        const row = `
            <tr>
                <td>${formatDate(record.date)}</td>
                <td><strong>${record.courseCode}</strong></td>
                <td><span class="count-badge present">${record.present}</span></td>
                <td><span class="count-badge late">${record.late}</span></td>
                <td><span class="count-badge absent">${record.absent}</span></td>
                <td><span class="count-badge excused">${record.excused}</span></td>
            </tr>
        `;

        tbody.append(row);
    });

    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Format date to readable format
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
