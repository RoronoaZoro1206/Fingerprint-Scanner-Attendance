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
            error: function () {
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
                render: function (data) {
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
    ajaxGet(`/AdminRoom/GetById/${roomId}`,
        function (response) {
            const room = response.data;
            $('#editRoomId').val(room.roomId);
            $('#editRoomCode').val(room.roomCode);
            $('#editRoomDescription').val(room.roomDescription || '');
            showModal('editRoomModal');
        },
        function () {
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
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminRoom/Create', roomData,
        function () {
            Toast.created('Room');
            hideModal('addRoomModal');
            roomsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
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

    if (!roomCode) {
        Toast.warning('Room code is required.');
        return;
    }

    const roomData = {
        RoomId: parseInt($('#editRoomId').val()),
        RoomCode: roomCode.toUpperCase(),
        RoomDescription: $('#editRoomDescription').val().trim() || null
    };

    const $btn = $('#btnUpdateRoom');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminRoom/Update', roomData,
        function () {
            Toast.updated('Room');
            hideModal('editRoomModal');
            roomsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPut
        }
    );

    resetButton($btn);
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
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminRoom/Delete/${roomId}`,
        function () {
            Toast.deleted('Room');
            hideModal('deleteRoomModal');
            roomsTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get rooms data for export
 */
function getRoomsData() {
    const data = [];
    roomsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Room Code': rowData.roomCode,
            'Description': rowData.roomDescription || 'No description'
        });
    });
    return data;
}
