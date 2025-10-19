/**
 * User Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let usersTable;
let rolesData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load roles first, then initialize DataTable
    loadRoles().then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddUser').on('click', showAddModal);
    $('#btnSaveUser').on('click', saveUser);
    $('#btnUpdateUser').on('click', updateUser);
    $('#btnConfirmDelete').on('click', confirmDeleteUser);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Users', getUsersData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('users', getUsersData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('users', getUsersData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Users', getUsersData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addUserModal', 'editUserModal', 'deleteUserModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addUserModal', 'addUserForm');
});

/**
 * Load Roles for dropdown
 */
async function loadRoles() {
    try {
        const response = await $.ajax({
            url: '/AdminUser/GetRoles',
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
 * Initialize DataTable
 */
function initializeDataTable() {
    usersTable = $('#usersTable').DataTable({
        ajax: {
            url: '/AdminUser/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load users data.');
            }
        },
        columns: [
            {
                data: 'fullName',
                width: '30%',
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
                width: '25%'
            },
            {
                data: 'userPhone',
                width: '25%',
                render: function (data) {
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
                render: function (data) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${data.userId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteUser(${data.userId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('users', 'user')
    });
}

/**
 * Show Add User Modal
 */
function showAddModal() {
    $('#addUserForm')[0].reset();
    showModal('addUserModal');
}

/**
 * Show Edit User Modal
 */
function showEditModal(userId) {
    ajaxGet(`/AdminUser/GetById/${userId}`,
        function (response) {
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
            $('#editUserPassword').val('');
            showModal('editUserModal');
        },
        function () {
            Toast.error('Failed to load user data.');
        }
    );
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

    const userData = {
        UserFirstName: $('#addUserFirstName').val().trim(),
        UserLastName: $('#addUserLastName').val().trim(),
        UserEmail: $('#addUserEmail').val().trim(),
        UserPhone: $('#addUserPhone').val().trim() || null,
        UserGender: $('#addUserGender').val() || null,
        UserBirthdate: $('#addUserBirthdate').val() || null,
        UserAddress: $('#addUserAddress').val().trim() || null,
        RoleId: parseInt($('#addRoleId').val()),
        UserPassword: $('#addUserPassword').val(),
        IsActive: true
    };

    if (!userData.UserFirstName || !userData.UserLastName || !userData.UserEmail || !userData.RoleId || !userData.UserPassword) {
        Toast.warning('Please fill in all required fields.');
        return;
    }

    const $btn = $('#btnSaveUser');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminUser/Create', userData,
        function () {
            Toast.created('User');
            hideModal('addUserModal');
            usersTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxPost
        }
    );

    resetButton($btn);
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

    const userData = {
        UserId: parseInt($('#editUserId').val()),
        UserFirstName: $('#editUserFirstName').val().trim(),
        UserLastName: $('#editUserLastName').val().trim(),
        UserEmail: $('#editUserEmail').val().trim(),
        UserPhone: $('#editUserPhone').val().trim() || null,
        UserGender: $('#editUserGender').val() || null,
        UserBirthdate: $('#editUserBirthdate').val() || null,
        UserAddress: $('#editUserAddress').val().trim() || null,
        RoleId: parseInt($('#editRoleId').val()),
        UserPassword: $('#editUserPassword').val() || '',
        IsActive: $('#editIsActive').val() === 'true'
    };

    if (!userData.UserFirstName || !userData.UserLastName || !userData.UserEmail || !userData.RoleId) {
        Toast.warning('Please fill in all required fields.');
        return;
    }

    const $btn = $('#btnUpdateUser');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminUser/Update', userData,
        function () {
            Toast.updated('User');
            hideModal('editUserModal');
            usersTable.ajax.reload(null, false);
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
function deleteUser(userId) {
    $('#deleteUserId').val(userId);
    showModal('deleteUserModal');
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
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminUser/Delete/${userId}`,
        function () {
            Toast.deleted('User');
            hideModal('deleteUserModal');
            usersTable.ajax.reload(null, false);
        },
        function () {
            // Error already handled by ajaxDelete
        }
    );

    resetButton($btn);
}

/**
 * Get users data for export
 */
function getUsersData() {
    const data = [];
    usersTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Name': rowData.fullName,
            'Email': rowData.userEmail,
            'Phone': rowData.userPhone || '-',
            'Role': rowData.roleName,
            'Status': rowData.isActive ? 'Active' : 'Inactive'
        });
    });
    return data;
}
