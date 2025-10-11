/**
 * Room Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let roomsTable;

$(document).ready(function () {
    // Initialize DataTable
    initializeDataTable();

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddRoom').on('click', showAddModal);
    $('#btnSaveRoom').on('click', saveRoom);
    $('#btnUpdateRoom').on('click', updateRoom);
    $('#btnConfirmDelete').on('click', confirmDeleteRoom);

    // Export button handlers
    $('#btnPrint').on('click', function(e) {
        e.preventDefault();
        roomsTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function(e) {
        e.preventDefault();
        roomsTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function(e) {
        e.preventDefault();
        roomsTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function(e) {
        e.preventDefault();
        roomsTable.button('.buttons-pdf').trigger();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events - reinitialize icons when modal is shown
    $('#addRoomModal, #editRoomModal, #deleteRoomModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addRoomModal').on('hidden.bs.modal', function () {
        $('#addRoomForm')[0].reset();
    });
});

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    roomsTable = $('#roomsTable').DataTable({
        ajax: {
            url: '/Room/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load rooms data.');
            }
        },
        columns: [
            { 
                data: 'roomCode',
                width: '30%'
            },
            { 
                data: 'roomDescription',
                width: '55%',
                render: function (data, type, row) {
                    return data || '<span style="color: #9ca3af;">No description</span>';
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.roomId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteRoom(${row.roomId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        buttons: [
            {
                extend: 'excel',
                text: '<i data-lucide="file-spreadsheet" class="icon-sm"></i> Excel',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1]
                }
            },
            {
                extend: 'csv',
                text: '<i data-lucide="file-text" class="icon-sm"></i> CSV',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1]
                }
            },
            {
                extend: 'pdf',
                text: '<i data-lucide="file" class="icon-sm"></i> PDF',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1]
                },
                customize: function (doc) {
                    doc.content[1].table.widths = ['30%', '70%'];
                    doc.styles.tableHeader.fillColor = '#f9fafb';
                    doc.styles.tableHeader.color = '#374151';
                }
            },
            {
                extend: 'print',
                text: '<i data-lucide="printer" class="icon-sm"></i> Print',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1]
                }
            }
        ],
        drawCallback: function () {
            // Reinitialize Lucide icons after table redraw
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search rooms...",
            lengthMenu: "Show _MENU_ rooms",
            info: "Showing _START_ to _END_ of _TOTAL_ rooms",
            infoEmpty: "No rooms available",
            infoFiltered: "(filtered from _MAX_ total rooms)",
            zeroRecords: "No matching rooms found",
            emptyTable: "No rooms available in the system"
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
                setTimeout(function() {
                    if (typeof lucide !== 'undefined') {
                        lucide.createIcons();
                    }
                }, 100);
            }
        }
    });
}

/**
 * Show Add Room Modal
 */
function showAddModal() {
    $('#addRoomForm')[0].reset();
    $('#addRoomModal').modal('show');
}

/**
 * Show Edit Room Modal
 */
function showEditModal(roomId) {
    $.ajax({
        url: `/Room/GetById/${roomId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const room = response.data;
                $('#editRoomId').val(room.roomId);
                $('#editRoomCode').val(room.roomCode);
                $('#editRoomDescription').val(room.roomDescription || '');
                $('#editRoomModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load room data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load room data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New Room
 */
function saveRoom() {
    const form = $('#addRoomForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const roomCode = $('#addRoomCode').val().trim();
    const roomDescription = $('#addRoomDescription').val().trim();

    // Additional validation
    if (!roomCode) {
        Toast.warning('Room code is required.');
        return;
    }

    const roomData = {
        RoomCode: roomCode.toUpperCase(),
        RoomDescription: roomDescription || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnSaveRoom');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/Room/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(roomData),
        success: function (response) {
            if (response.success) {
                Toast.created('Room');
                $('#addRoomModal').modal('hide');
                roomsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create room.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('Room');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save Room');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing Room
 */
function updateRoom() {
    const form = $('#editRoomForm')[0];
    
    // Validate form
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const roomId = $('#editRoomId').val();
    const roomCode = $('#editRoomCode').val().trim();
    const roomDescription = $('#editRoomDescription').val().trim();

    // Additional validation
    if (!roomCode) {
        Toast.warning('Room code is required.');
        return;
    }

    const roomData = {
        RoomId: parseInt(roomId),
        RoomCode: roomCode.toUpperCase(),
        RoomDescription: roomDescription || null
    };

    // Disable button to prevent double submission
    const $btn = $('#btnUpdateRoom');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/Room/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(roomData),
        success: function (response) {
            if (response.success) {
                Toast.updated('Room');
                $('#editRoomModal').modal('hide');
                roomsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update room.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('Room');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update Room');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteRoom(roomId) {
    // Store the room ID
    $('#deleteRoomId').val(roomId);
    
    // Show the confirmation modal
    $('#deleteRoomModal').modal('show');
}

/**
 * Confirm and Delete Room
 */
function confirmDeleteRoom() {
    const roomId = $('#deleteRoomId').val();
    
    if (!roomId) {
        Toast.error('Invalid room ID.');
        return;
    }

    // Disable button to prevent double submission
    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/Room/Delete/${roomId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('Room');
                $('#deleteRoomModal').modal('hide');
                roomsTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete room.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete room. It may be in use by other records.');
            } else {
                Toast.deleteError('Room');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="trash-2" class="icon-sm"></i> Delete');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}
