/**
 * Faculty Attendance Reports - Refactored to use common utilities
 * View and export attendance reports with statistics
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
        printData(getReportsData(), 'Attendance Reports', getReportColumns());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('attendance_reports', getReportsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('attendance_reports', getReportsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('attendance_reports', getReportsData(), getReportColumns());
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
        url: '/FacultyReport/GetReportData',
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
            url: '/FacultyReport/GetReportData',
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
                className: 'text-left',  // ✅ FIX: Force left alignment for date column
                render: function (data) {
                    return formatDate(data);
                }
            },
            {
                data: 'courseCode',
                width: '20%',
                className: 'text-left',  // ✅ FIX: Force left alignment for class column
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'present',
                width: '15%',
                render: function (data) {
                    return `<span class="count-badge present">${data}</span>`;
                }
            },
            {
                data: 'late',
                width: '15%',
                render: function (data) {
                    return `<span class="count-badge late">${data}</span>`;
                }
            },
            {
                data: 'absent',
                width: '15%',
                render: function (data) {
                    return `<span class="count-badge absent">${data}</span>`;
                }
            },
            {
                data: 'excused',
                width: '15%',
                render: function (data) {
                    return `<span class="count-badge excused">${data}</span>`;
                }
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
    initializeLucideIcons();
}

/**
 * Get reports data for export
 */
function getReportsData() {
    const data = [];
    attendanceTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Date': formatDate(rowData.date),
            'Class': rowData.courseCode,
            'Present': rowData.present,
            'Late': rowData.late,
            'Absent': rowData.absent,
            'Excused': rowData.excused
        });
    });
    return data;
}

/**
 * Get report columns for export
 */
function getReportColumns() {
    return ['Date', 'Class', 'Present', 'Late', 'Absent', 'Excused'];
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
