/**
 * Faculty Attendance Reports with DataTables
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

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printReports();
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcel();
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSV();
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDF();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
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

// Export Functions
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

function printReports() {
    const printWindow = window.open('', '_blank');
    const data = getReportsData();

    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Attendance Reports</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; }
                h1 { color: #1a1a1a; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: left; border: 1px solid #ddd; }
                th { background-color: #f9fafb; font-weight: 600; }
                tr:nth-child(even) { background-color: #f9fafb; }
                .badge { padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; }
                .present { background: #d1fae5; color: #065f46; }
                .late { background: #fef3c7; color: #92400e; }
                .absent { background: #fee2e2; color: #991b1b; }
                .excused { background: #dbeafe; color: #1e40af; }
                @media print {
                    button { display: none; }
                }
            </style>
        </head>
        <body>
            <h1>Attendance Reports</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Class</th>
                        <th>Present</th>
                        <th>Late</th>
                        <th>Absent</th>
                        <th>Excused</th>
                    </tr>
                </thead>
                <tbody>`;

    data.forEach(row => {
        html += `<tr>
            <td>${row.Date}</td>
            <td><strong>${row.Class}</strong></td>
            <td><span class="badge present">${row.Present}</span></td>
            <td><span class="badge late">${row.Late}</span></td>
            <td><span class="badge absent">${row.Absent}</span></td>
            <td><span class="badge excused">${row.Excused}</span></td>
        </tr>`;
    });

    html += `
                </tbody>
            </table>
            <script>
                window.onload = function() { window.print(); }
            </script>
        </body>
        </html>`;

    printWindow.document.write(html);
    printWindow.document.close();
}

function exportToCSV() {
    const data = getReportsData();
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }

    const headers = Object.keys(data[0]);
    let csv = headers.join(',') + '\n';

    data.forEach(row => {
        const values = headers.map(header => {
            const value = row[header] || '';
            return `"${value.toString().replace(/"/g, '""')}"`;
        });
        csv += values.join(',') + '\n';
    });

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `attendance_reports_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getReportsData();
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }

    const headers = Object.keys(data[0]);
    let html = '<table><thead><tr>';
    headers.forEach(header => {
        html += `<th>${header}</th>`;
    });
    html += '</tr></thead><tbody>';

    data.forEach(row => {
        html += '<tr>';
        headers.forEach(header => {
            html += `<td>${row[header] || ''}</td>`;
        });
        html += '</tr>';
    });
    html += '</tbody></table>';

    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `attendance_reports_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printReports();
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
