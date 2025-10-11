/**
 * User Management - CRUD Operations with DataTables
 * Uses AJAX for all operations and toast notifications
 */

let usersTable;
let rolesData = [];

$(document).ready(function () {
    // Load roles first, then initialize DataTable
    loadRoles().then(() => {
        initializeDataTable();
    });

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event Handlers
    $('#btnAddUser').on('click', showAddModal);
    $('#btnSaveUser').on('click', saveUser);
    $('#btnUpdateUser').on('click', updateUser);
    $('#btnConfirmDelete').on('click', confirmDeleteUser);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        usersTable.button('.buttons-print').trigger();
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        usersTable.button('.buttons-excel').trigger();
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        usersTable.button('.buttons-csv').trigger();
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        usersTable.button('.buttons-pdf').trigger();
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Modal events
    $('#addUserModal, #editUserModal, #deleteUserModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Clear form when modal is hidden
    $('#addUserModal').on('hidden.bs.modal', function () {
        $('#addUserForm')[0].reset();
    });
});

/**
 * Load Roles for dropdown
 */
async function loadRoles() {
    try {
        const response = await $.ajax({
            url: '/User/GetRoles',
            type: 'GET'
        });

        if (response.success) {
            rolesData = response.data;
            populateRoleDropdowns();
        }
    } catch (error) {
        console.error('Error loading roles:', error);
    }
}

/**
 * Populate role dropdowns
 */
function populateRoleDropdowns() {
    const addSelect = $('#addRoleId');
    const editSelect = $('#editRoleId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    rolesData.forEach(role => {
        const option = `<option value="${role.roleId}">${role.roleName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Get role badge HTML
 */
function getRoleBadge(roleId, roleName) {
    let badgeClass = 'badge-admin';
    if (roleId === 2) badgeClass = 'badge-faculty';
    else if (roleId === 3) badgeClass = 'badge-student';

    return `<span class="badge ${badgeClass}">${roleName}</span>`;
}

/**
 * Get status badge HTML
 */
function getStatusBadge(isActive) {
    const badgeClass = isActive ? 'badge-active' : 'badge-inactive';
    const text = isActive ? 'Active' : 'Inactive';
    return `<span class="badge ${badgeClass}">${text}</span>`;
}

/**
 * Initialize DataTable with export options
 */
function initializeDataTable() {
    usersTable = $('#usersTable').DataTable({
        ajax: {
            url: '/User/GetAll',
            dataSrc: 'data',
            error: function (xhr, error, thrown) {
                console.error('DataTables error:', error);
                Toast.error('Failed to load users data.');
            }
        },
        columns: [
            {
                data: 'fullName',
                width: '25%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        const profileImage = row.userProfileImage || 'default_profile.png';
                        const imagePath = `/images/profile/${profileImage}`;
                        return `
                            <div class="d-flex align-items-center">
                                <img src="${imagePath}" alt="${data}" class="user-avatar me-2" 
                                     onerror="this.src='/images/profile/default_profile.png'">
                                <span>${data}</span>
                            </div>
                        `;
                    }
                    return data;
                }
            },
            {
                data: 'userEmail',
                width: '20%'
            },
            {
                data: 'userPhone',
                width: '15%',
                render: function (data, type, row) {
                    return data || '-';
                }
            },
            {
                data: 'roleName',
                width: '12%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        return getRoleBadge(row.roleId, data);
                    }
                    return data;
                }
            },
            {
                data: 'isActive',
                width: '10%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        return getStatusBadge(data);
                    }
                    return row.statusText;
                }
            },
            {
                data: null,
                width: '18%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.userId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteUser(${row.userId})" title="Delete">
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
                    columns: [0, 1, 2, 3, 4]
                }
            },
            {
                extend: 'csv',
                text: '<i data-lucide="file-text" class="icon-sm"></i> CSV',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3, 4]
                }
            },
            {
                extend: 'pdf',
                text: '<i data-lucide="file" class="icon-sm"></i> PDF',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3, 4]
                }
            },
            {
                extend: 'print',
                text: '<i data-lucide="printer" class="icon-sm"></i> Print',
                className: 'btn-export',
                exportOptions: {
                    columns: [0, 1, 2, 3, 4]
                }
            }
        ],
        drawCallback: function () {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        },
        language: {
            search: "",
            searchPlaceholder: "Search users...",
            lengthMenu: "Show _MENU_ users",
            info: "Showing _START_ to _END_ of _TOTAL_ users",
            infoEmpty: "No users available",
            infoFiltered: "(filtered from _MAX_ total users)",
            zeroRecords: "No matching users found",
            emptyTable: "No users available in the system"
        },
        initComplete: function () {
            const searchInput = $('.dataTables_filter input');

            if (searchInput.parent().find('.search-icon').length === 0) {
                searchInput.wrap('<div class="search-input-wrapper"></div>');
                searchInput.before('<i data-lucide="search" class="search-icon"></i>');

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
 * Show Add User Modal
 */
function showAddModal() {
    $('#addUserForm')[0].reset();
    $('#addUserModal').modal('show');
}

/**
 * Show Edit User Modal
 */
function showEditModal(userId) {
    $.ajax({
        url: `/User/GetById/${userId}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                const user = response.data;
                $('#editUserId').val(user.userId);
                $('#editUserFirstName').val(user.userFirstName);
                $('#editUserLastName').val(user.userLastName);
                $('#editUserEmail').val(user.userEmail);
                $('#editUserPhone').val(user.userPhone || '');
                $('#editUserGender').val(user.userGender || '');
                $('#editUserBirthdate').val(user.userBirthdate || '');
                $('#editUserAddress').val(user.userAddress || '');
                $('#editRoleId').val(user.roleId);
                $('#editIsActive').val(user.isActive.toString());
                $('#editUserPassword').val(''); // Clear password field
                $('#editUserModal').modal('show');
            } else {
                Toast.error(response.message || 'Failed to load user data.');
            }
        },
        error: function (xhr) {
            Toast.error('Failed to load user data.');
            console.error('Error:', xhr);
        }
    });
}

/**
 * Save New User
 */
function saveUser() {
    const form = $('#addUserForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const firstName = $('#addUserFirstName').val().trim();
    const lastName = $('#addUserLastName').val().trim();
    const email = $('#addUserEmail').val().trim();
    const phone = $('#addUserPhone').val().trim();
    const gender = $('#addUserGender').val();
    const birthdate = $('#addUserBirthdate').val();
    const address = $('#addUserAddress').val().trim();
    const roleId = $('#addRoleId').val();
    const password = $('#addUserPassword').val();

    if (!firstName || !lastName || !email || !roleId || !password) {
        Toast.warning('Please fill in all required fields.');
        return;
    }

    const userData = {
        UserFirstName: firstName,
        UserLastName: lastName,
        UserEmail: email,
        UserPhone: phone || null,
        UserGender: gender || null,
        UserBirthdate: birthdate || null,
        UserAddress: address || null,
        RoleId: parseInt(roleId),
        UserPassword: password,
        IsActive: true
    };

    const $btn = $('#btnSaveUser');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/User/Create',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(userData),
        success: function (response) {
            if (response.success) {
                Toast.created('User');
                $('#addUserModal').modal('hide');
                usersTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to create user.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.createError('User');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Save User');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Update Existing User
 */
function updateUser() {
    const form = $('#editUserForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const userId = $('#editUserId').val();
    const firstName = $('#editUserFirstName').val().trim();
    const lastName = $('#editUserLastName').val().trim();
    const email = $('#editUserEmail').val().trim();
    const phone = $('#editUserPhone').val().trim();
    const gender = $('#editUserGender').val();
    const birthdate = $('#editUserBirthdate').val();
    const address = $('#editUserAddress').val().trim();
    const roleId = $('#editRoleId').val();
    const isActive = $('#editIsActive').val() === 'true';
    const password = $('#editUserPassword').val();

    if (!firstName || !lastName || !email || !roleId) {
        Toast.warning('Please fill in all required fields.');
        return;
    }

    const userData = {
        UserId: parseInt(userId),
        UserFirstName: firstName,
        UserLastName: lastName,
        UserEmail: email,
        UserPhone: phone || null,
        UserGender: gender || null,
        UserBirthdate: birthdate || null,
        UserAddress: address || null,
        RoleId: parseInt(roleId),
        UserPassword: password || '', // Empty string if not changing password
        IsActive: isActive
    };

    const $btn = $('#btnUpdateUser');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/User/Update',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(userData),
        success: function (response) {
            if (response.success) {
                Toast.updated('User');
                $('#editUserModal').modal('hide');
                usersTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to update user.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 400) {
                Toast.error('Invalid data provided. Please check your input.');
            } else {
                Toast.updateError('User');
            }
        },
        complete: function () {
            $btn.prop('disabled', false).html('<i data-lucide="save" class="icon-sm"></i> Update User');
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Show Delete Confirmation Modal
 */
function deleteUser(userId) {
    $('#deleteUserId').val(userId);
    $('#deleteUserModal').modal('show');
}

/**
 * Confirm and Delete User
 */
function confirmDeleteUser() {
    const userId = $('#deleteUserId').val();

    if (!userId) {
        Toast.error('Invalid user ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Deleting...');

    $.ajax({
        url: `/User/Delete/${userId}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                Toast.deleted('User');
                $('#deleteUserModal').modal('hide');
                usersTable.ajax.reload(null, false);
            } else {
                Toast.error(response.message || 'Failed to delete user.');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            if (xhr.status === 409) {
                Toast.error('Cannot delete user. It may be in use by other records.');
            } else {
                Toast.deleteError('User');
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
