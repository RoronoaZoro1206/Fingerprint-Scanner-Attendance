let schedulesTable;
let classesData = [];
let schedulesData = [];

$(document).ready(function () {
    loadClasses().then(() => {
        loadSchedulesData().then(() => {
            initializeDataTable();
        });
    });

    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    $('#btnAddSchedule').on('click', showAddModal);
    $('#btnSaveSchedule').on('click', saveSchedules);
    $('#btnUpdateSchedule').on('click', updateSchedule);
    $('#btnConfirmDelete').on('click', confirmDelete);
    
    // Use event delegation for dynamically added button
    $(document).on('click', '#btnAddScheduleEntry', addScheduleEntry);

    $(document).on('click', '.btn-remove-schedule', function() {
        $(this).closest('.schedule-entry').remove();
        updateRemoveButtons();
    });

    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        printSchedules();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        exportToExcel();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        exportToCSV();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        exportToPDF();
    });

    $('#addScheduleModal, #editScheduleModal, #deleteScheduleModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });

    $('#addScheduleModal').on('hidden.bs.modal', function () {
        $('#addScheduleForm')[0].reset();
        $('#scheduleEntries').html(getScheduleEntryHTML());
        updateRemoveButtons();
    });
});

async function loadClasses() {
    const response = await $.ajax({ url: '/ClassSchedule/GetClasses', type: 'GET' });
    if (response.success) {
        classesData = response.data;
        populateClassDropdowns();
    }
}

async function loadSchedulesData() {
    const response = await $.ajax({ url: '/ClassSchedule/GetAll', dataSrc: 'data' });
    schedulesData = response.data || [];
}

function populateClassDropdowns() {
    const addSelect = $('#addClassId');
    const editSelect = $('#editClassId');
    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();
    classesData.forEach(cls => {
        const option = `<option value="${cls.classId}">${cls.courseName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

function initializeDataTable() {
    const groupedData = groupSchedulesByClass();
    
    schedulesTable = $('#schedulesTable').DataTable({
        data: groupedData,
        columns: [
            {
                className: 'dt-control',
                orderable: false,
                data: null,
                defaultContent: '<i data-lucide="chevron-right" class="expand-icon"></i>'
            },
            { data: 'courseName' },
            { 
                data: 'scheduleCount',
                render: function(data) {
                    return `${data} schedule(s)`;
                }
            },
            {
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModalForClass(${row.classId})" title="Add/Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[1, 'asc']],
        pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],
        autoWidth: false,
        drawCallback: function () {
            if (typeof lucide !== 'undefined') lucide.createIcons();
        },
        language: {
            search: "",
            searchPlaceholder: "Search schedules...",
            lengthMenu: "Show _MENU_ schedules",
            emptyTable: "No schedules available"
        },
        dom: '<"table-controls"<"left-controls"l><"right-controls"f>>rt<"d-flex justify-content-center"p><"d-none"i>'
    });

    $('#schedulesTable tbody').on('click', 'td.dt-control', function () {
        const tr = $(this).closest('tr');
        const row = schedulesTable.row(tr);
        
        if (row.child.isShown()) {
            row.child.hide();
            tr.find('.expand-icon').attr('data-lucide', 'chevron-right');
        } else {
            row.child(formatChildRow(row.data())).show();
            tr.find('.expand-icon').attr('data-lucide', 'chevron-down');
        }
        if (typeof lucide !== 'undefined') lucide.createIcons();
    });
}

function groupSchedulesByClass() {
    const grouped = {};
    schedulesData.forEach(s => {
        if (!grouped[s.classId]) {
            grouped[s.classId] = {
                classId: s.classId,
                courseName: s.courseName,
                schedules: [],
                scheduleCount: 0
            };
        }
        grouped[s.classId].schedules.push(s);
        grouped[s.classId].scheduleCount++;
    });
    return Object.values(grouped);
}

function formatChildRow(d) {
    let html = '<table class="child-table"><thead><tr><th>Day</th><th>Start</th><th>End</th><th>Actions</th></tr></thead><tbody>';
    d.schedules.forEach(s => {
        html += `<tr>
            <td>${s.schedDayOfWeek}</td>
            <td>${s.schedStartTime}</td>
            <td>${s.schedEndTime}</td>
            <td>
                <button class="btn-action btn-edit" onclick="showEditModal(${s.schedId})" title="Edit">
                    <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                </button>
                <button class="btn-action btn-delete" onclick="deleteSchedule(${s.schedId})" title="Delete">
                    <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                </button>
            </td>
        </tr>`;
    });
    html += '</tbody></table>';
    return html;
}

function getScheduleEntryHTML() {
    return `<div class="schedule-entry mb-2">
        <div class="row g-2">
            <div class="col-md-4">
                <select class="form-select schedule-day" required>
                    <option value="">Day...</option>
                    <option value="Monday">Monday</option>
                    <option value="Tuesday">Tuesday</option>
                    <option value="Wednesday">Wednesday</option>
                    <option value="Thursday">Thursday</option>
                    <option value="Friday">Friday</option>
                    <option value="Saturday">Saturday</option>
                    <option value="Sunday">Sunday</option>
                </select>
            </div>
            <div class="col-md-3">
                <input type="time" class="form-control schedule-start" required>
            </div>
            <div class="col-md-3">
                <input type="time" class="form-control schedule-end" required>
            </div>
            <div class="col-md-2">
                <button type="button" class="btn btn-danger btn-sm w-100 btn-remove-schedule">
                    <i data-lucide="x" class="icon-sm"></i>
                </button>
            </div>
        </div>
    </div>`;
}

function addScheduleEntry(e) {
    if (e) e.preventDefault();
    $('#scheduleEntries').append(getScheduleEntryHTML());
    updateRemoveButtons();
    if (typeof lucide !== 'undefined') lucide.createIcons();
}

function updateRemoveButtons() {
    const entries = $('.schedule-entry');
    if (entries.length === 1) {
        entries.find('.btn-remove-schedule').hide();
    } else {
        entries.find('.btn-remove-schedule').show();
    }
}

function showAddModal() {
    $('#addScheduleForm')[0].reset();
    $('#scheduleEntries').html(getScheduleEntryHTML());
    updateRemoveButtons();
    $('#addScheduleModal').modal('show');
}

function showEditModalForClass(classId) {
    $('#addClassId').val(classId);
    $('#addScheduleModal').modal('show');
}

function showEditModal(schedId) {
    const sched = schedulesData.find(s => s.schedId === schedId);
    if (sched) {
        $('#editSchedId').val(sched.schedId);
        $('#editClassId').val(sched.classId);
        $('#editDayOfWeek').val(sched.schedDayOfWeek);
        $('#editStartTime').val(sched.schedStartTime);
        $('#editEndTime').val(sched.schedEndTime);
        $('#editScheduleModal').modal('show');
    }
}

function saveSchedules() {
    const form = $('#addScheduleForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const classId = parseInt($('#addClassId').val());
    if (!classId) {
        Toast.warning('Please select a class.');
        return;
    }

    const schedules = [];
    
    $('.schedule-entry').each(function() {
        const day = $(this).find('.schedule-day').val();
        const start = $(this).find('.schedule-start').val();
        const end = $(this).find('.schedule-end').val();
        
        if (day && start && end) {
            schedules.push({
                ClassId: classId,
                SchedDayOfWeek: day,
                SchedStartTime: start,
                SchedEndTime: end
            });
        }
    });

    if (schedules.length === 0) {
        Toast.warning('Please add at least one schedule.');
        return;
    }

    console.log('Sending schedules:', schedules);

    const $btn = $('#btnSaveSchedule');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/ClassSchedule/CreateMultiple',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(schedules),
        success: function (response) {
            console.log('Response:', response);
            if (response.success) {
                Toast.success(response.message);
                $('#addScheduleModal').modal('hide');
                location.reload();
            } else {
                Toast.error(response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error:', xhr.responseText);
            Toast.error('Failed to create schedules: ' + error);
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Schedules');
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }
    });
}

function updateSchedule() {
    const form = $('#editScheduleForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const scheduleData = {
        SchedId: parseInt($('#editSchedId').val()),
        ClassId: parseInt($('#editClassId').val()),
        SchedDayOfWeek: $('#editDayOfWeek').val(),
        SchedStartTime: $('#editStartTime').val(),
        SchedEndTime: $('#editEndTime').val()
    };

    const $btn = $('#btnUpdateSchedule');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/ClassSchedule/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(scheduleData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Schedule');
                $('#editScheduleModal').modal('hide');
                location.reload();
            } else {
                Toast.error(response.message);
            }
        },
        error: function () {
            Toast.updateError('Schedule');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update');
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }
    });
}

function deleteSchedule(schedId) {
    $('#deleteSchedId').val(schedId);
    $('#deleteScheduleModal').modal('show');
}

function confirmDelete() {
    const schedId = $('#deleteSchedId').val();
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/ClassSchedule/Delete/${schedId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Schedule');
                $('#deleteScheduleModal').modal('hide');
                location.reload();
            } else {
                Toast.error(response.message);
            }
        },
        error: function () {
            Toast.deleteError('Schedule');
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }
    });
}

// Export Functions
function getFlatScheduleData() {
    const flatData = [];
    schedulesData.forEach(s => {
        flatData.push({
            'Course': s.courseName,
            'Day': s.schedDayOfWeek,
            'Start Time': s.schedStartTime,
            'End Time': s.schedEndTime
        });
    });
    return flatData;
}

function printSchedules() {
    const printWindow = window.open('', '_blank');
    const data = getFlatScheduleData();
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>Class Schedules</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; }
                h1 { color: #1a1a1a; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: left; border: 1px solid #ddd; }
                th { background-color: #f9fafb; font-weight: 600; }
                tr:nth-child(even) { background-color: #f9fafb; }
                @media print {
                    button { display: none; }
                }
            </style>
        </head>
        <body>
            <h1>Class Schedules</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>
                        <th>Course</th>
                        <th>Day</th>
                        <th>Start Time</th>
                        <th>End Time</th>
                    </tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += `<tr>
            <td>${row.Course}</td>
            <td>${row.Day}</td>
            <td>${row['Start Time']}</td>
            <td>${row['End Time']}</td>
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
    const data = getFlatScheduleData();
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
    link.download = `class_schedules_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

function exportToExcel() {
    const data = getFlatScheduleData();
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
    link.download = `class_schedules_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

function exportToPDF() {
    Toast.info('PDF export requires additional library. Using print instead.');
    printSchedules();
}
