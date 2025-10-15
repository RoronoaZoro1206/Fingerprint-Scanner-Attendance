/**
 * Student My Attendance with DataTables
 */

let attendanceTable;

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load statistics first
    loadStatistics();

    // Initialize DataTable
    initializeDataTable();
});

/**
 * Load statistics data
 */
function loadStatistics() {
    $.ajax({
        url: '/StudentAttendance/GetAttendanceData',
        type: 'GET',
        success: function (response) {
            if (response.success && response.data) {
                displayStatistics(response.data.statistics);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading statistics:', error);
        }
    });
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    attendanceTable = $('#attendanceTable').DataTable({
        ajax: {
            url: '/StudentAttendance/GetAttendanceData',
            dataSrc: function (json) {
                if (json.success && json.data && json.data.records) {
                    return json.data.records;
                }
                return [];
            },
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load attendance data.');
            }
        },
        columns: [
            {
                data: 'date',
                width: '15%',
                render: function (data) {
                    return formatDate(data);
                }
            },
            {
                data: 'courseCode',
                width: '20%',
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'status',
                width: '20%',
                render: function (data) {
                    return getStatusBadge(data);
                }
            },
            {
                data: 'timeIn',
                width: '20%'
            },
            {
                data: 'timeOut',
                width: '20%'
            }
        ],
        order: [[0, 'desc']],
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
            searchPlaceholder: "Search records...",
            lengthMenu: "Show _MENU_ records",
            info: "Showing _START_ to _END_ of _TOTAL_ records",
            infoEmpty: "No records found",
            infoFiltered: "(filtered from _MAX_ total records)",
            zeroRecords: "No matching records found",
            emptyTable: "No attendance records available"
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
