/**
 * Student My Attendance
 */

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load attendance data
    loadAttendanceData();
});

/**
 * Load attendance data
 */
function loadAttendanceData() {
    $('#loadingState').show();
    $('#attendanceTable').hide();
    $('#emptyState').hide();

    $.ajax({
        url: '/StudentAttendance/GetAttendanceData',
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
                Toast.error(response.message || 'Failed to load attendance data');
                $('#emptyState').show();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading attendance data:', error);
            $('#loadingState').hide();
            Toast.error('Failed to load attendance data');
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
            <div class="stat-icon success">
                <i data-lucide="check-circle"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.classesAttended}</div>
                <div class="stat-label">Class Attended</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon warning">
                <i data-lucide="clock"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.lateArrivals}</div>
                <div class="stat-label">Late Arrivals</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon danger">
                <i data-lucide="x-circle"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.absences}</div>
                <div class="stat-label">Absences</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon info">
                <i data-lucide="trending-up"></i>
            </div>
            <div class="stat-content">
                <div class="stat-value">${stats.attendanceRate}%</div>
                <div class="stat-label">Attendance Rate</div>
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
        const statusBadge = getStatusBadge(record.status);
        
        const row = `
            <tr>
                <td>${formatDate(record.date)}</td>
                <td><strong>${record.courseCode}</strong></td>
                <td>${statusBadge}</td>
                <td>${record.timeIn}</td>
                <td>${record.timeOut}</td>
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
 * Get status badge HTML
 */
function getStatusBadge(status) {
    const statusLower = status.toLowerCase();
    let icon = '';
    
    switch (statusLower) {
        case 'present':
            icon = '<i data-lucide="check-circle" class="status-icon"></i>';
            break;
        case 'late':
            icon = '<i data-lucide="clock" class="status-icon"></i>';
            break;
        case 'absent':
            icon = '<i data-lucide="x-circle" class="status-icon"></i>';
            break;
        case 'excused':
            icon = '<i data-lucide="info" class="status-icon"></i>';
            break;
        default:
            icon = '<i data-lucide="minus-circle" class="status-icon"></i>';
    }

    return `<span class="status-badge ${statusLower}">${icon} ${status}</span>`;
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
