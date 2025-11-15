/// <reference path="../global.d.ts" />

/**
 * Room Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let roomsTable;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize DataTable
    initializeDataTable();

    // Event Handlers
    $('#btnAddRoom').on('click', showAddModal);
    $('#btnSaveRoom').on('click', saveRoom);
    $('#btnUpdateRoom').on('click', updateRoom);
    $('#btnConfirmDelete').on('click', confirmDeleteRoom);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Rooms', getRoomsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('rooms', getRoomsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('rooms', getRoomsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Rooms', getRoomsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addRoomModal', 'editRoomModal', 'deleteRoomModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addRoomModal', 'addRoomForm');
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    roomsTable = $('#roomsTable').DataTable({
        ajax: {
            url: '/AdminRoom/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTable AJAX Error:', error, thrown);
                Toast.error('Failed to load rooms data.');
            }
        },
        columns: [
            {
                data: 'RoomCode',
                width: '20%',  // ✅ Fixed width for Room Code
                className: 'text-nowrap'  // ✅ Prevent wrapping
            },
            {
                data: 'RoomDescription',
                width: '65%',  // ✅ Fixed width for Description (allows truncation)
                render: function (data, type, row) {
                    // For display, show truncated with full text in title attribute (tooltip)
                    if (type === 'display') {
                        if (!data || data.trim() === '') {
                            return '<span style="color: #9ca3af;">No description</span>';
                        }
                        // Add title attribute for native browser tooltip
                        return `<span title="${data}">${data}</span>`;
                    }
                    // For sorting/filtering, return raw data
                    return data || '';
                }
            },
            {
                data: null,
                width: '15%',  // ✅ Fixed width for Actions
                orderable: false,
                searchable: false,
                className: 'text-center',  // ✅ Center align actions
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.RoomId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteRoom(${row.RoomId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        columnDefs: [
            {
                targets: [0, 2],  // Room Code and Actions columns
                className: 'dt-nowrap'  // ✅ Prevent wrapping in these columns
            }
        ],
        ...commonDataTableConfig,
        language: getDataTableLanguage('rooms', 'room')
    });
}

/**
 * Show Add Room Modal
 */
function showAddModal() {
    $('#addRoomForm')[0].reset();
    showModal('addRoomModal');
}

/**
 * Show Edit Room Modal
 */
function showEditModal(roomId) {
    console.log('Opening edit modal for room ID:', roomId);
    
    ajaxGet(`/AdminRoom/GetById/${roomId}`,
        function (response) {
            console.log('Room data received:', response.data);
            const room = response.data;
            $('#editRoomId').val(room.RoomId);
            $('#editRoomCode').val(room.RoomCode);
            $('#editRoomDescription').val(room.RoomDescription || '');
            showModal('editRoomModal');
        },
        function (error) {
            console.error('Error loading room:', error);
            Toast.error('Failed to load room data.');
        }
    );
}

/**
 * Save New Room
 */
function saveRoom() {
    const form = $('#addRoomForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const roomCode = $('#addRoomCode').val().trim();

    if (!roomCode) {
        Toast.warning('Room code is required.');
        return;
    }

    const roomData = {
        RoomCode: roomCode.toUpperCase(),
        RoomDescription: $('#addRoomDescription').val().trim() || null
    };

    const $btn = $('#btnSaveRoom');
    const originalHtml = $btn.html();
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminRoom/Create', roomData,
        function (response) {
            Toast.created('Room');
            hideModal('addRoomModal');
            roomsTable.ajax.reload(null, false);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        },
        function (error) {
            console.error('Error creating room:', error);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        }
    );
}

/**
 * Update Existing Room
 */
function updateRoom() {
    const form = $('#editRoomForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const roomCode = $('#editRoomCode').val().trim();
    const roomId = $('#editRoomId').val();

    console.log('Updating room - ID:', roomId, 'Code:', roomCode);

    if (!roomCode) {
        Toast.warning('Room code is required.');
        return;
    }

    if (!roomId) {
        Toast.error('Invalid room ID.');
        return;
    }

    const roomData = {
        RoomId: parseInt(roomId),
        RoomCode: roomCode.toUpperCase(),
        RoomDescription: $('#editRoomDescription').val().trim() || null
    };

    console.log('Sending room data:', roomData);

    const $btn = $('#btnUpdateRoom');
    const originalHtml = $btn.html();
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminRoom/Update', roomData,
        function (response) {
            console.log('Update successful:', response);
            Toast.updated('Room');
            hideModal('editRoomModal');
            roomsTable.ajax.reload(null, false);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        },
        function (error) {
            console.error('Error updating room:', error);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        }
    );
}

/**
 * Show Delete Confirmation Modal
 */
function deleteRoom(roomId) {
    $('#deleteRoomId').val(roomId);
    showModal('deleteRoomModal');
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

    const $btn = $('#btnConfirmDelete');
    const originalHtml = $btn.html();
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminRoom/Delete/${roomId}`,
        function (response) {
            Toast.deleted('Room');
            hideModal('deleteRoomModal');
            roomsTable.ajax.reload(null, false);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        },
        function (error) {
            console.error('Error deleting room:', error);
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        }
    );
}

/**
 * Get rooms data for export
 */
function getRoomsData() {
    const data = [];
    roomsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Room Code': rowData.RoomCode,
            'Description': rowData.RoomDescription || 'No description'
        });
    });
    return data;
}
