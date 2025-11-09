/**
 * User Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let usersTable;
let rolesData = [];
let addUserProfileFile = null;
let editUserProfileFile = null;

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

    // Profile picture handlers for Add User modal
    $('#btnAddUserChangePicture').on('click', function () {
        $('#addUserProfileInput').click();
    });
    $('#addUserProfileInput').on('change', handleAddUserProfilePictureChange);

    // Profile picture handlers for Edit User modal
    $('#btnEditUserChangePicture').on('click', function () {
        $('#editUserProfileInput').click();
    });
    $('#editUserProfileInput').on('change', handleEditUserProfilePictureChange);

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
    
    // Reset profile pictures when modals are hidden
    $('#addUserModal').on('hidden.bs.modal', function () {
        addUserProfileFile = null;
        $('#addUserProfilePreview').attr('src', '/images/profile/default_profile.png');
    });
    
    $('#editUserModal').on('hidden.bs.modal', function () {
        editUserProfileFile = null;
    });
});

/**
 * Handle profile picture change for Add User modal
 */
function handleAddUserProfilePictureChange(e) {
    const file = e.target.files[0];
    
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
        Toast.error('Invalid file type. Only JPG, PNG, and GIF are allowed.');
        $('#addUserProfileInput').val('');
        return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
        Toast.error('File size must be less than 5MB');
        $('#addUserProfileInput').val('');
        return;
    }

    // Store file for upload
    addUserProfileFile = file;

    // Preview image
    const reader = new FileReader();
    reader.onload = function (e) {
        $('#addUserProfilePreview').attr('src', e.target.result);
    };
    reader.readAsDataURL(file);
}

/**
 * Handle profile picture change for Edit User modal
 */
function handleEditUserProfilePictureChange(e) {
    const file = e.target.files[0];
    
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
        Toast.error('Invalid file type. Only JPG, PNG, and GIF are allowed.');
        $('#editUserProfileInput').val('');
        return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
        Toast.error('File size must be less than 5MB');
        $('#editUserProfileInput').val('');
        return;
    }

    // Store file for upload
    editUserProfileFile = file;

    // Preview image
    const reader = new FileReader();
    reader.onload = function (e) {
        $('#editUserProfilePreview').attr('src', e.target.result);
    };
    reader.readAsDataURL(file);
}

/**
 * Upload profile picture for a user
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
    addUserProfileFile = null;
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
            $('#editUserId').val(user.userId);
            $('#editUserFirstName').val(user.userFirstName);
            $('#editUserMiddleName').val(user.userMiddleName || '');
            $('#editUserLastName').val(user.userLastName);
            $('#editUserEmail').val(user.userEmail);
            $('#editUserPhone').val(user.userPhone || '');
            $('#editUserGender').val(user.userGender || '');
            $('#editUserBirthdate').val(user.userBirthdate || '');
            $('#editUserAddress').val(user.userAddress || '');
            $('#editRoleId').val(user.roleId);
            $('#editIsActive').val(user.isActive.toString());
            $('#editUserPassword').val('');
            
            // Set profile picture
            const profileImage = user.userProfileImage || 'default_profile.png';
            $('#editUserProfilePreview').attr('src', `/images/profile/${profileImage}`);
            editUserProfileFile = null;
            
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
            // If profile picture selected, upload it
            if (addUserProfileFile && response.userId) {
                try {
                    await uploadUserProfilePicture(response.userId, addUserProfileFile);
                    Toast.created('User with profile picture');
                } catch (error) {
                    Toast.warning('User created but profile picture upload failed');
                }
            } else {
                Toast.created('User');
            }
            
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
            let profilePictureUpdated = false;
            let newProfilePicture = null;
            
            // If new profile picture selected, upload it
            if (editUserProfileFile) {
                try {
                    const uploadResponse = await uploadUserProfilePicture(userData.UserId, editUserProfileFile);
                    profilePictureUpdated = true;
                    newProfilePicture = uploadResponse.fileName;
                    Toast.updated('User with profile picture');
                } catch (error) {
                    Toast.warning('User updated but profile picture upload failed');
                }
            } else {
                Toast.updated('User');
            }
            
            // Update sidebar if editing current logged-in user
            updateSidebarIfCurrentUser(userData.UserId, userData.UserFirstName, userData.UserLastName, newProfilePicture);
            
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
        
        // Update profile picture if a new one was uploaded
        if (newProfilePicture) {
            const newImagePath = `/images/profile/${newProfilePicture}`;
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
            'Name': rowData.fullName,
            'Email': rowData.userEmail,
            'Phone': rowData.userPhone || '-',
            'Role': rowData.roleName,
            'Status': rowData.isActive ? 'Active' : 'Inactive'
        });
    });
    return data;
}
