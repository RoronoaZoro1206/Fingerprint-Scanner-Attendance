/**
 * User Profile Management
 */

let originalData = {};
let profilePictureModal;

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Initialize profile picture modal
    profilePictureModal = createProfilePictureModal(
        'userProfilePictureModal',
        'profilePicturePreview',
        handleProfilePictureSelected
    );

    // Load profile data
    loadProfileData();

    // ✅ FIX CAMERA ICON BUG: Remove existing handler first to prevent duplicate attachments
    $(document).off('click', '#btnChangePicture').on('click', '#btnChangePicture', function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        console.log('[Camera Icon] User Profile camera icon clicked');
        if (profilePictureModal) {
            profilePictureModal.show();
        } else {
            console.error('[Camera Icon] Profile picture modal not initialized');
        }
        return false;
    });

    // Event handlers
    $('#profileForm').on('submit', handleProfileSubmit);
    $('#btnCancel').on('click', handleCancel);
});

/**
 * Load profile data
 */
function loadProfileData() {
    $.ajax({
        url: '/UserProfile/GetProfileData',
        type: 'GET',
        success: function (response) {
            if (response.success && response.data) {
                populateForm(response.data);
                originalData = { ...response.data };
            } else {
                Toast.error(response.message || 'Failed to load profile data');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading profile data:', error);
            Toast.error('Failed to load profile data');
        }
    });
}

/**
 * Populate form with user data
 */
function populateForm(data) {
    $('#firstName').val(data.firstName || '');
    $('#middleName').val(data.middleName || '');
    $('#lastName').val(data.lastName || '');
    $('#gender').val(data.gender || '');
    $('#phone').val(data.phone || '');
    $('#address').val(data.address || '');
    $('#birthdate').val(data.birthdate || '');
    $('#email').val(data.email || '');

    // ✅ FIX: Update profile picture with proper fallback to templates/default_profile.png
    const profileImagePath = data.profileImage && data.profileImage !== 'templates/default_profile.png' 
        ? `/images/profile/${data.profileImage}?v=${Date.now()}` 
        : '/images/profile/templates/default_profile.png';
    
    $('#profilePicturePreview').attr('src', profileImagePath);
    console.log('[Profile] Setting profile picture:', profileImagePath);

    // Update header display
    const fullName = `${data.firstName || ''} ${data.middleName || ''} ${data.lastName || ''}`.trim();
    $('#profileDisplayName').text(fullName || 'User');
    $('#profileDisplayEmail').text(data.email || '');
}

/**
 * Handle profile picture selection from modal
 */
function handleProfilePictureSelected(selection) {
    if (selection.type === 'file') {
        // Upload from local device
        uploadProfilePicture(selection.file);
    } else if (selection.type === 'server') {
        // Set from server image
        setProfilePictureFromServer(selection.imageName);
    }
}

/**
 * Upload profile picture from file
 */
function uploadProfilePicture(file) {
    const formData = new FormData();
    formData.append('profilePicture', file);

    const $btn = $('#btnChangePicture');
    const originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Uploading...');

    $.ajax({
        url: '/UserProfile/UploadProfilePicture',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                // Update profile picture preview
                $('#profilePicturePreview').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
                // Update sidebar profile image (both desktop and mobile)
                $('.sidebar .profile-image').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
                $('#sidebarOffcanvas .profile-image').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
            } else {
                Toast.error(response.message);
                // ✅ FIX: Revert to original image or templates/default_profile.png
                const fallbackImage = originalData.profileImage && originalData.profileImage !== 'templates/default_profile.png'
                    ? `/images/profile/${originalData.profileImage}`
                    : '/images/profile/templates/default_profile.png';
                $('#profilePicturePreview').attr('src', fallbackImage);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error uploading profile picture:', error);
            Toast.error('Failed to upload profile picture');
            // ✅ FIX: Revert to original image or templates/default_profile.png
            const fallbackImage = originalData.profileImage && originalData.profileImage !== 'templates/default_profile.png'
                ? `/images/profile/${originalData.profileImage}`
                : '/images/profile/templates/default_profile.png';
            $('#profilePicturePreview').attr('src', fallbackImage);
        },
        complete: function () {
            $btn.prop('disabled', false).html(originalHtml);
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Set profile picture from server image
 */
function setProfilePictureFromServer(imageName) {
    const $btn = $('#btnChangePicture');
    const originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');

    $.ajax({
        url: '/UserProfile/SetProfileImageFromServer',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ imageName: imageName }),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                // Update profile picture preview
                $('#profilePicturePreview').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
                // Update sidebar profile image (both desktop and mobile)
                $('.sidebar .profile-image').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
                $('#sidebarOffcanvas .profile-image').attr('src', `/images/profile/${response.fileName}?v=${Date.now()}`);
            } else {
                Toast.error(response.message);
                // ✅ FIX: Revert to original image or templates/default_profile.png
                const fallbackImage = originalData.profileImage && originalData.profileImage !== 'templates/default_profile.png'
                    ? `/images/profile/${originalData.profileImage}`
                    : '/images/profile/templates/default_profile.png';
                $('#profilePicturePreview').attr('src', fallbackImage);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error setting profile picture from server:', error);
            Toast.error('Failed to update profile picture');
            // ✅ FIX: Revert to original image or templates/default_profile.png
            const fallbackImage = originalData.profileImage && originalData.profileImage !== 'templates/default_profile.png'
                ? `/images/profile/${originalData.profileImage}`
                : '/images/profile/templates/default_profile.png';
            $('#profilePicturePreview').attr('src', fallbackImage);
        },
        complete: function () {
            $btn.prop('disabled', false).html(originalHtml);
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Handle profile form submit
 */
function handleProfileSubmit(e) {
    e.preventDefault();

    const formData = {
        firstName: $('#firstName').val().trim(),
        middleName: $('#middleName').val().trim(),
        lastName: $('#lastName').val().trim(),
        gender: $('#gender').val(),
        phone: $('#phone').val().trim(),
        address: $('#address').val().trim(),
        birthdate: $('#birthdate').val()
    };

    // Validate required fields
    if (!formData.firstName || !formData.lastName) {
        Toast.error('First name and last name are required');
        return;
    }

    const $btn = $('#btnSave');
    const originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/UserProfile/UpdateProfile',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                originalData = { ...originalData, ...formData };
                // Update sidebar name (both desktop and mobile)
                const fullName = `${formData.firstName} ${formData.lastName}`;
                $('.sidebar .user-name').text(fullName);
                $('#sidebarOffcanvas .user-name').text(fullName);
                // Update header display
                const fullNameWithMiddle = `${formData.firstName} ${formData.middleName || ''} ${formData.lastName}`.trim();
                $('#profileDisplayName').text(fullNameWithMiddle);
            } else {
                Toast.error(response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error updating profile:', error);
            Toast.error('Failed to update profile');
        },
        complete: function () {
            $btn.prop('disabled', false).html(originalHtml);
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });
}

/**
 * Handle cancel button
 */
function handleCancel() {
    if (confirm('Are you sure you want to discard your changes?')) {
        populateForm(originalData);
    }
}
