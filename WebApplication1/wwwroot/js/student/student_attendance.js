/**
 * Student My Attendance - Refactored to use common utilities
 * View attendance records with statistics
 */

let attendanceTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load statistics first
    loadStatistics();

    // Initialize DataTable
    initializeDataTable();

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData(getAttendanceData(), 'My Attendance Record', getAttendanceColumns());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('my_attendance', getAttendanceData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('my_attendance', getAttendanceData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('my_attendance', getAttendanceData(), getAttendanceColumns());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        initializeLucideIcons();
    });
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
                className: 'text-left', // Force left alignment for date cells
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
        ...commonDataTableConfig,
        language: getDataTableLanguage('records', 'record')
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
    initializeLucideIcons();
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
 * Get attendance data for export
 */
function getAttendanceData() {
    const data = [];
    attendanceTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Date': formatDate(rowData.date),
            'Course': rowData.courseCode,
            'Status': rowData.status,
            'Time In': rowData.timeIn,
            'Time Out': rowData.timeOut
        });
    });
    return data;
}

/**
 * Get attendance columns for export
 */
function getAttendanceColumns() {
    return ['Date', 'Course', 'Status', 'Time In', 'Time Out'];
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
