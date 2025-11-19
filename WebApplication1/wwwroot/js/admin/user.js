/**
 * User Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let usersTable;
let rolesData = [];
let addUserProfileModal;
let editUserProfileModal;
let addUserProfileSelection = null;
let editUserProfileSelection = null;

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Initialize profile picture modals
    addUserProfileModal = createProfilePictureModal(
        'addUserProfilePictureModal',
        'addUserProfilePreview',
        handleAddUserProfileSelected
    );

    editUserProfileModal = createProfilePictureModal(
        'editUserProfilePictureModal',
        'editUserProfilePreview',
        handleEditUserProfileSelected
    );

    // Load roles first, then initialize DataTable
    loadRoles().then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddUser').on('click', showAddModal);
    $('#btnSaveUser').on('click', saveUser);
    $('#btnUpdateUser').on('click', updateUser);
    $('#btnConfirmDelete').on('click', confirmDeleteUser);

    // ✅ FIX: Use event delegation with data attributes
    // This ensures edit buttons work even after DataTable reloads
    $('#usersTable tbody').on('click', '.btn-edit', function(e) {
        e.preventDefault();
        const userId = $(this).data('user-id');
        if (userId) {
            showEditModal(userId);
        } else {
            console.error('No user ID found on edit button');
        }
    });

    // ✅ FIX: Use event delegation for delete buttons too
    $('#usersTable tbody').on('click', '.btn-delete', function(e) {
        e.preventDefault();
        const userId = $(this).data('user-id');
        if (userId) {
            deleteUser(userId);
        } else {
            console.error('No user ID found on delete button');
        }
    });

    // Profile picture handlers for Add User modal - use dual-option modal
    $('#btnAddUserChangePicture').on('click', function () {
        addUserProfileModal.show();
    });

    // Profile picture handlers for Edit User modal - use dual-option modal
    $('#btnEditUserChangePicture').on('click', function () {
        editUserProfileModal.show();
    });

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
    
    // Reset profile selections when modals are hidden
    $('#addUserModal').on('hidden.bs.modal', function () {
        addUserProfileSelection = null;
        $('#addUserProfilePreview').attr('src', '/images/profile/default_profile.png');
    });
    
    $('#editUserModal').on('hidden.bs.modal', function () {
        editUserProfileSelection = null;
    });
});

/**
 * Handle profile picture selection for Add User modal
 */
function handleAddUserProfileSelected(selection) {
    addUserProfileSelection = selection;
    
    // Update preview immediately
    if (selection.type === 'file') {
        const reader = new FileReader();
        reader.onload = function (e) {
            $('#addUserProfilePreview').attr('src', e.target.result);
        };
        reader.readAsDataURL(selection.file);
    } else if (selection.type === 'server') {
        $('#addUserProfilePreview').attr('src', `/images/profile/${selection.imageName}`);
    }
}

/**
 * Handle profile picture selection for Edit User modal
 */
function handleEditUserProfileSelected(selection) {
    editUserProfileSelection = selection;
    
    // Update preview immediately
    if (selection.type === 'file') {
        const reader = new FileReader();
        reader.onload = function (e) {
            $('#editUserProfilePreview').attr('src', e.target.result);
        };
        reader.readAsDataURL(selection.file);
    } else if (selection.type === 'server') {
        $('#editUserProfilePreview').attr('src', `/images/profile/${selection.imageName}`);
    }
}

/**
 * Upload profile picture for a user (file)
 */
function uploadUserProfilePicture(userId, file) {
    return new Promise((resolve, reject) => {
        const formData = new FormData();
        formData.append('profilePicture', file);
        formData.append('userId', userId);

        $.ajax({
            url: '/AdminUser/UploadUserProfilePicture',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    resolve(response);
                } else {
                    reject(response.message || 'Failed to upload profile picture');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error uploading profile picture:', error);
                reject('Failed to upload profile picture');
            }
        });
    });
}

/**
 * Set profile picture for a user (server image)
 */
function setUserProfilePictureFromServer(userId, imageName) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/AdminUser/SetUserProfileImageFromServer',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ userId: userId, imageName: imageName }),
            success: function (response) {
                if (response.success) {
                    resolve(response);
                } else {
                    reject(response.message || 'Failed to set profile picture');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error setting profile picture from server:', error);
                reject('Failed to set profile picture');
            }
        });
    });
}

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
        Toast.error('Failed to load roles data');
    }
}

/**
 * Populate role dropdowns
 * ✅ FIX: Using correct C# property names (RoleId, RoleName)
 */
function populateRoleDropdowns() {
    const addSelect = $('#addRoleId');
    const editSelect = $('#editRoleId');

    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    rolesData.forEach(role => {
        // ✅ FIX: Changed role.roleId to role.RoleId and role.roleName to role.RoleName
        const option = `<option value="${role.RoleId}">${role.RoleName}</option>`;
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
                data: 'FullName',
                width: '30%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        const profileImage = row.UserProfileImage || 'default_profile.png';
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
                data: 'UserEmail',
                width: '25%'
            },
            {
                data: 'UserPhone',
                width: '25%',
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'RoleName',
                width: '12%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        return getRoleBadge(row.RoleId, data);
                    }
                    return data;
                }
            },
            {
                data: 'IsActive',
                width: '10%',
                render: function (data, type, row) {
                    if (type === 'display') {
                        return getStatusBadge(data);
                    }
                    return row.StatusText;
                }
            },
            {
                data: null,
                width: '18%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // ✅ FIX: Store UserId as data attribute on buttons
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" data-user-id="${row.UserId}" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" data-user-id="${row.UserId}" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[0, 'asc']],
        ...commonDataTableConfig,
        language: getDataTableLanguage('users', 'user'),
        // ✅ FIX: Reinitialize icons after each draw
        drawCallback: function() {
            initializeLucideIcons();
        }
    });
}

/**
 * Show Add User Modal
 */
function showAddModal() {
    $('#addUserForm')[0].reset();
    addUserProfileSelection = null;
    $('#addUserProfilePreview').attr('src', '/images/profile/default_profile.png');
    showModal('addUserModal');
}

/**
 * Show Edit User Modal
 */
function showEditModal(userId) {
    ajaxGet(`/AdminUser/GetById/${userId}`,
        function (response) {
            const user = response.data;
            // ✅ FIX: Use PascalCase property names (matches API response)
            $('#editUserId').val(user.UserId);
            $('#editUserFirstName').val(user.UserFirstName);
            $('#editUserMiddleName').val(user.UserMiddleName || '');
            $('#editUserLastName').val(user.UserLastName);
            $('#editUserEmail').val(user.UserEmail);
            $('#editUserPhone').val(user.UserPhone || '');
            $('#editUserGender').val(user.UserGender || '');
            $('#editUserBirthdate').val(user.UserBirthdate || '');
            $('#editUserAddress').val(user.UserAddress || '');
            $('#editRoleId').val(user.RoleId);
            $('#editIsActive').val(user.IsActive.toString());
            $('#editUserPassword').val('');
            
            // Set profile picture
            const profileImage = user.UserProfileImage || 'default_profile.png';
            $('#editUserProfilePreview').attr('src', `/images/profile/${profileImage}`);
            editUserProfileSelection = null;
            
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
        UserMiddleName: $('#addUserMiddleName').val().trim() || null,
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
        async function (response) {
            let message = 'User created successfully';
            
            // If profile picture selected, upload or set it
            if (addUserProfileSelection && response.userId) {
                try {
                    if (addUserProfileSelection.type === 'file') {
                        await uploadUserProfilePicture(response.userId, addUserProfileSelection.file);
                        message = 'User created with profile picture uploaded';
                    } else if (addUserProfileSelection.type === 'server') {
                        await setUserProfilePictureFromServer(response.userId, addUserProfileSelection.imageName);
                        message = 'User created with profile picture selected';
                    }
                } catch (error) {
                    console.error('Profile picture error:', error);
                    Toast.warning('User created but profile picture failed to set');
                }
            }
            
            Toast.success(message);
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
        UserMiddleName: $('#editUserMiddleName').val().trim() || null,
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
        async function () {
            let newProfilePicture = null;
            let message = 'User updated successfully';
            
            // If new profile picture selected, upload or set it
            if (editUserProfileSelection) {
                try {
                    if (editUserProfileSelection.type === 'file') {
                        const uploadResponse = await uploadUserProfilePicture(userData.UserId, editUserProfileSelection.file);
                        newProfilePicture = uploadResponse.fileName;
                        message = 'User updated with new profile picture uploaded';
                    } else if (editUserProfileSelection.type === 'server') {
                        const setResponse = await setUserProfilePictureFromServer(userData.UserId, editUserProfileSelection.imageName);
                        newProfilePicture = setResponse.fileName;
                        message = 'User updated with new profile picture selected';
                    }
                } catch (error) {
                    console.error('Profile picture error:', error);
                    Toast.warning('User updated but profile picture failed to set');
                }
            }
            
            // Update sidebar if editing current logged-in user
            updateSidebarIfCurrentUser(userData.UserId, userData.UserFirstName, userData.UserLastName, newProfilePicture);
            
            Toast.success(message);
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
 * Update sidebar profile picture and name if editing current logged-in user
 */
function updateSidebarIfCurrentUser(editedUserId, firstName, lastName, newProfilePicture) {
    // Check if we're editing the current logged-in user
    // We can check session or compare user details
    const currentUserName = $('.sidebar .user-name').first().text().trim();
    const fullName = `${firstName} ${lastName}`;
    
    // Get current profile image from sidebar
    const currentSidebarImage = $('.sidebar .profile-image').first().attr('src');
    const currentUserId = extractUserIdFromImage(currentSidebarImage);
    
    // Update if it's the same user (match by userId in image path or by name)
    if (currentUserId === editedUserId || currentUserName === fullName) {
        // Update name in both desktop and mobile sidebars
        $('.sidebar .user-name').text(fullName);
        $('#sidebarOffcanvas .user-name').text(fullName);
        
        // Update profile picture if a new one was set
        if (newProfilePicture) {
            const newImagePath = `/images/profile/${newProfilePicture}?v=${Date.now()}`;
            $('.sidebar .profile-image').attr('src', newImagePath);
            $('#sidebarOffcanvas .profile-image').attr('src', newImagePath);
        }
    }
}

/**
 * Extract user ID from image path (e.g., /images/profile/user_1_abc123.jpg -> 1)
 */
function extractUserIdFromImage(imagePath) {
    if (!imagePath) return null;
    const match = imagePath.match(/user_(\d+)_/);
    return match ? parseInt(match[1]) : null;
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
            'Name': rowData.FullName,
            'Email': rowData.UserEmail,
            'Phone': rowData.UserPhone || '-',
            'Role': rowData.RoleName,
            'Status': rowData.IsActive ? 'Active' : 'Inactive'
        });
    });
    return data;
}
