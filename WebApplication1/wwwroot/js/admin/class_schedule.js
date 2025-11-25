/// <reference path="../global.d.ts" />

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
    const response = await $.ajax({ url: '/AdminClassSchedule/GetClasses', type: 'GET' });
    if (response.success) {
        classesData = response.data;
        populateClassDropdowns();
    }
}

async function loadSchedulesData() {
    const response = await $.ajax({ url: '/AdminClassSchedule/GetAll', dataSrc: 'data' });
    schedulesData = response.data || [];
}

function populateClassDropdowns() {
    const addSelect = $('#addClassId');
    const editSelect = $('#editClassId');
    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();
    classesData.forEach(cls => {
        // ✅ FIX: Changed from cls.courseName to cls.CourseName (PascalCase)
        const option = `<option value="${cls.ClassId}">${cls.CourseName}</option>`;
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
            // ✅ FIX: Changed from 'courseName' to 'CourseName' (PascalCase)
            { data: 'CourseName' },
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
                            <button class="btn-action btn-edit" onclick="showEditModalForClass(${row.ClassId})" title="Add/Edit">
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
        // ✅ FIX: Changed from s.classId to s.ClassId (PascalCase)
        if (!grouped[s.ClassId]) {
            grouped[s.ClassId] = {
                // ✅ FIX: All properties using PascalCase
                ClassId: s.ClassId,
                CourseName: s.CourseName,
                schedules: [],
                scheduleCount: 0
            };
        }
        grouped[s.ClassId].schedules.push(s);
        grouped[s.ClassId].scheduleCount++;
    });
    return Object.values(grouped);
}

function formatChildRow(d) {
    let html = '<table class="child-table"><thead><tr><th>Day</th><th>Start</th><th>End</th><th>Actions</th></tr></thead><tbody>';
    d.schedules.forEach(s => {
        // ✅ FIX: Changed to PascalCase properties (SchedId, SchedDayOfWeek, SchedStartTime, SchedEndTime)
        html += `<tr>
            <td>${s.SchedDayOfWeek}</td>
            <td>${s.SchedStartTime}</td>
            <td>${s.SchedEndTime}</td>
            <td>
                <button class="btn-action btn-edit" onclick="showEditModal(${s.SchedId})" title="Edit">
                    <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                </button>
                <button class="btn-action btn-delete" onclick="deleteSchedule(${s.SchedId})" title="Delete">
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
    // ✅ FIX: Changed to PascalCase properties to match ClassScheduleDetailDto
    const sched = schedulesData.find(s => s.SchedId === schedId);
    if (sched) {
        $('#editSchedId').val(sched.SchedId);
        $('#editClassId').val(sched.ClassId);
        $('#editDayOfWeek').val(sched.SchedDayOfWeek);
        $('#editStartTime').val(sched.SchedStartTime);
        $('#editEndTime').val(sched.SchedEndTime);
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
        url: '/AdminClassSchedule/CreateMultiple',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(schedules),
        success: function (response) {
            console.log('Response:', response);
            if (response.success) {
                Toast.success(response.message || 'Schedules created successfully');
                $('#addScheduleModal').modal('hide');
                setTimeout(function() {
                    location.reload();
                }, 1000);
            } else {
                // ✨ FINAL: Standard SweetAlert2 error icon + red OK button + preserved content
                const errorMessage = response.message || 'Failed to create schedules';
                if (errorMessage.includes('\n')) {
                    // Parse error message parts
                    const messageParts = errorMessage.split('\n\n');
                    const conflictHeader = messageParts[0] || '';
                    const conflictDetails = messageParts[1] || '';
                    
                    // ✅ NEW: Extract faculty name if provided in response
                    const facultyName = response.facultyName || 'The faculty member';
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Faculty Schedule Conflict',
                        html: `
                            <div style="text-align: left; margin-top: 0.75rem;">
                                <!-- Compact Conflict Header -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    font-size: 0.9375rem;
                                    font-weight: 600;
                                    color: #991b1b;
                                    line-height: 1.5;
                                ">
                                    ❌ ${conflictHeader.replace('❌ Faculty Schedule Conflict Detected!', 'Faculty Schedule Conflict Detected!')}
                                </p>
                                
                                <!-- Conflict Details with Emoji and Faculty Name -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fef2f2;
                                    border-left: 3px solid #dc2626;
                                    border-radius: 6px;
                                    font-size: 0.9375rem;
                                    line-height: 1.65;
                                    color: #7f1d1d;
                                ">
                                    👨‍🏫 ${conflictDetails}
                                </p>
                                
                                <!-- Solution with Emoji -->
                                <p style="
                                    margin: 0;
                                    padding: 0.875rem;
                                    background: #eff6ff;
                                    border-left: 3px solid #3b82f6;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #1e40af;
                                ">
                                    💡 <strong>Solution:</strong> Choose a different time slot or assign a different faculty member.
                                </p>
                            </div>
                        `,
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#ef4444',
                        width: '520px',
                        padding: '1.75rem 2rem 2rem',
                        customClass: {
                            popup: 'faculty-conflict-standard',
                            title: 'faculty-conflict-title-standard',
                            htmlContainer: 'faculty-conflict-content-standard',
                            confirmButton: 'faculty-conflict-button-standard'
                        },
                        buttonsStyling: false,
                        allowOutsideClick: false
                    });
                } else {
                    // Single-line error - use toast
                    Toast.error(errorMessage);
                }
                $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Schedules');
                if (typeof lucide !== 'undefined') lucide.createIcons();
            }
        },
        error: function (xhr, status, error) {
            console.error('Error:', xhr.responseText);
            Toast.error('Failed to create schedules: ' + error);
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
        url: '/AdminClassSchedule/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(scheduleData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Schedule');
                $('#editScheduleModal').modal('hide');
                setTimeout(function() {
                    location.reload();
                }, 1000);
            } else {
                // ✨ FINAL: Standard SweetAlert2 error icon + red OK button + preserved content
                const errorMessage = response.message || 'Failed to update schedule';
                if (errorMessage.includes('\n')) {
                    // Parse error message parts
                    const messageParts = errorMessage.split('\n\n');
                    const conflictHeader = messageParts[0] || '';
                    const conflictDetails = messageParts[1] || '';
                    
                    // ✅ NEW: Extract faculty name if provided in response
                    const facultyName = response.facultyName || 'The faculty member';
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Faculty Schedule Conflict',
                        html: `
                            <div style="text-align: left; margin-top: 0.75rem;">
                                <!-- Compact Conflict Header -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    font-size: 0.9375rem;
                                    font-weight: 600;
                                    color: #991b1b;
                                    line-height: 1.5;
                                ">
                                    ❌ ${conflictHeader.replace('❌ Faculty Schedule Conflict Detected!', 'Faculty Schedule Conflict Detected!')}
                                </p>
                                
                                <!-- Conflict Details with Emoji -->
                                <p style="
                                    margin: 0 0 1rem 0;
                                    padding: 0.875rem;
                                    background: #fef2f2;
                                    border-left: 3px solid #dc2626;
                                    border-radius: 6px;
                                    font-size: 0.9375rem;
                                    line-height: 1.65;
                                    color: #7f1d1d;
                                ">
                                    👨‍🏫 ${conflictDetails}
                                </p>
                                
                                <!-- Solution with Emoji -->
                                <p style="
                                    margin: 0;
                                    padding: 0.875rem;
                                    background: #eff6ff;
                                    border-left: 3px solid #3b82f6;
                                    border-radius: 6px;
                                    font-size: 0.875rem;
                                    line-height: 1.6;
                                    color: #1e40af;
                                ">
                                    💡 <strong>Solution:</strong> Choose a different time slot or assign a different faculty member.
                                </p>
                            </div>
                        `,
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#ef4444',
                        width: '520px',
                        padding: '1.75rem 2rem 2rem',
                        customClass: {
                            popup: 'faculty-conflict-standard',
                            title: 'faculty-conflict-title-standard',
                            htmlContainer: 'faculty-conflict-content-standard',
                            confirmButton: 'faculty-conflict-button-standard'
                        },
                        buttonsStyling: false,
                        allowOutsideClick: false
                    });
                } else {
                    // Single-line error - use toast
                    Toast.error(errorMessage);
                }
                $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update');
                if (typeof lucide !== 'undefined') lucide.createIcons();
            }
        },
        error: function () {
            Toast.updateError('Schedule');
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
        url: `/AdminClassSchedule/Delete/${schedId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Schedule');
                $('#deleteScheduleModal').modal('hide');
                // Delay reload to allow toast to display
                setTimeout(function() {
                    location.reload();
                }, 1000);
            } else {
                Toast.error(response.message);
                $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
                if (typeof lucide !== 'undefined') lucide.createIcons();
            }
        },
        error: function () {
            Toast.deleteError('Schedule');
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') lucide.createIcons();
        }
    });
}

// Export Functions
function getFlatScheduleData() {
    const flatData = [];
    schedulesData.forEach(s => {
        // ✅ FIX: Changed to PascalCase properties
        flatData.push({
            'Course': s.CourseName,
            'Day': s.SchedDayOfWeek,
            'Start Time': s.SchedStartTime,
            'End Time': s.SchedEndTime
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
