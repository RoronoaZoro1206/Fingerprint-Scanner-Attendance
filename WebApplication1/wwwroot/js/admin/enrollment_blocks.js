/**
 * Admin Enrollment Blocks Management
 * Allows admins to view and lift enrollment blocks created by faculty
 */

let enrollmentBlocksTable;
let allBlocksData = [];
let currentFilter = 'active';

$(document).ready(function () {
    initializeLucideIcons();
    initializeDataTable();
    setupEventHandlers();
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    enrollmentBlocksTable = $('#enrollmentBlocksTable').DataTable({
        ajax: {
            url: '/AdminEnrollmentBlock/GetAll',
            dataSrc: function (json) {
                allBlocksData = json.data || [];
                return filterData(allBlocksData, currentFilter);
            }
        },
        columns: [
            { data: 'studentName' },
            { data: 'studentNumber' },
            {
                data: null,
                render: function (data) {
                    return `<span class="fw-medium">${data.courseCode}</span><br>
                            <small class="text-muted">${data.courseName}</small>`;
                }
            },
            { data: 'blockedByName' },
            {
                data: 'blockReason',
                render: function (data) {
                    const maxLen = 30;
                    if (data && data.length > maxLen) {
                        return `<span title="${escapeHtml(data)}">${escapeHtml(data.substring(0, maxLen))}...</span>`;
                    }
                    return escapeHtml(data || 'No reason');
                }
            },
            {
                data: 'blockedAt',
                render: function (data) {
                    return formatDateTime(data);
                }
            },
            {
                data: 'isActive',
                render: function (data, type, row) {
                    if (data) {
                        return '<span class="status-badge status-active">Active</span>';
                    } else {
                        return `<span class="status-badge status-lifted">Lifted</span>
                                <br><small class="text-muted">by ${row.unblockedByName || 'Admin'}</small>`;
                    }
                }
            },
            {
                data: null,
                orderable: false,
                render: function (data) {
                    let actions = `
                        <button class="btn btn-sm btn-outline-primary me-1" onclick="viewBlockDetails(${data.blockId})" title="View Details">
                            <i data-lucide="eye" class="icon-sm"></i>
                        </button>`;
                    
                    if (data.isActive) {
                        actions += `
                            <button class="btn btn-sm btn-outline-success" onclick="showUnblockModal(${data.blockId}, '${escapeHtml(data.studentName)}', '${escapeHtml(data.courseCode)} - ${escapeHtml(data.courseName)}')" title="Lift Block">
                                <i data-lucide="user-check" class="icon-sm"></i>
                            </button>`;
                    }
                    
                    return actions;
                }
            }
        ],
        order: [[5, 'desc']], // Sort by blocked date descending
        pageLength: 10,
        responsive: true,
        language: {
            emptyTable: "No enrollment blocks found",
            zeroRecords: "No matching records found"
        },
        drawCallback: function () {
            initializeLucideIcons();
        }
    });
}

/**
 * Filter data based on status
 */
function filterData(data, filter) {
    if (filter === 'all') return data;
    if (filter === 'active') return data.filter(d => d.isActive === true);
    if (filter === 'lifted') return data.filter(d => d.isActive === false);
    return data;
}

/**
 * Setup event handlers
 */
function setupEventHandlers() {
    // Filter tabs
    $('.btn-filter').on('click', function () {
        $('.btn-filter').removeClass('active');
        $(this).addClass('active');
        currentFilter = $(this).data('filter');
        
        // Refresh table with filtered data
        enrollmentBlocksTable.clear();
        enrollmentBlocksTable.rows.add(filterData(allBlocksData, currentFilter));
        enrollmentBlocksTable.draw();
    });

    // Confirm unblock
    $('#btnConfirmUnblock').on('click', confirmUnblock);

    // Print
    $('#btnPrint').on('click', function () {
        window.print();
    });

    // Export buttons
    $('#btnExportExcel').on('click', function () {
        exportToExcel();
    });

    $('#btnExportCSV').on('click', function () {
        exportToCSV();
    });

    $('#btnExportPDF').on('click', function () {
        exportToPDF();
    });
}

/**
 * View block details
 */
function viewBlockDetails(blockId) {
    const block = allBlocksData.find(b => b.blockId === blockId);
    if (!block) {
        Toast.error('Block not found');
        return;
    }

    $('#viewStudentName').text(block.studentName);
    $('#viewStudentNumber').text(block.studentNumber);
    $('#viewCourseCode').text(block.courseCode);
    $('#viewCourseName').text(block.courseName);
    $('#viewBlockedBy').text(block.blockedByName);
    $('#viewBlockedAt').text(formatDateTime(block.blockedAt));
    $('#viewBlockReason').text(block.blockReason || 'No reason provided');

    if (!block.isActive && block.unblockedAt) {
        $('#unblockInfoSection').show();
        $('#viewUnblockedBy').text(block.unblockedByName || 'Admin');
        $('#viewUnblockedAt').text(formatDateTime(block.unblockedAt));
    } else {
        $('#unblockInfoSection').hide();
    }

    $('#viewBlockModal').modal('show');
    initializeLucideIcons();
}

/**
 * Show unblock confirmation modal
 */
function showUnblockModal(blockId, studentName, className) {
    $('#unblockBlockId').val(blockId);
    $('#unblockStudentName').text(studentName);
    $('#unblockClassName').text(className);
    $('#unblockModal').modal('show');
    initializeLucideIcons();
}

/**
 * Confirm unblock action
 */
function confirmUnblock() {
    const blockId = parseInt($('#unblockBlockId').val());
    const $btn = $('#btnConfirmUnblock');
    const originalHtml = $btn.html();

    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

    $.ajax({
        url: `/AdminEnrollmentBlock/Unblock/${blockId}`,
        type: 'POST',
        success: function (response) {
            if (response.success) {
                $('#unblockModal').modal('hide');
                Toast.success(response.message);
                
                // Reload table
                enrollmentBlocksTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to lift block');
            }
        },
        error: function () {
            Toast.error('An error occurred while lifting the block');
        },
        complete: function () {
            $btn.prop('disabled', false).html(originalHtml);
            initializeLucideIcons();
        }
    });
}

/**
 * Format date time
 */
function formatDateTime(dateStr) {
    if (!dateStr) return 'N/A';
    try {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch {
        return dateStr;
    }
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Export functions
 */
function exportToExcel() {
    const data = filterData(allBlocksData, currentFilter);
    const worksheet = XLSX.utils.json_to_sheet(data.map(formatExportRow));
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Enrollment Blocks');
    XLSX.writeFile(workbook, `enrollment_blocks_${new Date().toISOString().split('T')[0]}.xlsx`);
}

function exportToCSV() {
    const data = filterData(allBlocksData, currentFilter);
    const worksheet = XLSX.utils.json_to_sheet(data.map(formatExportRow));
    const csv = XLSX.utils.sheet_to_csv(worksheet);
    downloadFile(csv, `enrollment_blocks_${new Date().toISOString().split('T')[0]}.csv`, 'text/csv');
}

function exportToPDF() {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();
    
    doc.setFontSize(16);
    doc.text('Enrollment Blocks Report', 14, 20);
    doc.setFontSize(10);
    doc.text(`Generated: ${new Date().toLocaleString()}`, 14, 28);
    doc.text(`Filter: ${currentFilter.charAt(0).toUpperCase() + currentFilter.slice(1)}`, 14, 34);

    const data = filterData(allBlocksData, currentFilter);
    const tableData = data.map(row => [
        row.studentName,
        row.studentNumber,
        row.courseCode,
        row.blockedByName,
        row.isActive ? 'Active' : 'Lifted',
        formatDateTime(row.blockedAt)
    ]);

    doc.autoTable({
        head: [['Student', 'Student #', 'Class', 'Blocked By', 'Status', 'Blocked At']],
        body: tableData,
        startY: 40,
        styles: { fontSize: 8 },
        headStyles: { fillColor: [59, 130, 246] }
    });

    doc.save(`enrollment_blocks_${new Date().toISOString().split('T')[0]}.pdf`);
}

function formatExportRow(row) {
    return {
        'Student Name': row.studentName,
        'Student Number': row.studentNumber,
        'Course Code': row.courseCode,
        'Course Name': row.courseName,
        'Blocked By': row.blockedByName,
        'Reason': row.blockReason,
        'Blocked At': formatDateTime(row.blockedAt),
        'Status': row.isActive ? 'Active' : 'Lifted',
        'Unblocked By': row.unblockedByName || '',
        'Unblocked At': row.unblockedAt ? formatDateTime(row.unblockedAt) : ''
    };
}

function downloadFile(content, filename, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
