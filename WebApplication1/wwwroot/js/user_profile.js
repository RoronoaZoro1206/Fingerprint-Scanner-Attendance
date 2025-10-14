/**
 * User Profile Management
 */

let originalData = {};

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load profile data
    loadProfileData();

    // Event handlers
    $('#btnChangePicture').on('click', function () {
        $('#profilePictureInput').click();
    });

    $('#profilePictureInput').on('change', handleProfilePictureChange);
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

    // Update profile picture
    if (data.profileImage) {
        $('#profilePicturePreview').attr('src', `/images/profile/${data.profileImage}`);
    }
}

/**
 * Handle profile picture change
 */
function handleProfilePictureChange(e) {
    const file = e.target.files[0];
    
    if (!file) return;

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
        Toast.error('Invalid file type. Only JPG, PNG, and GIF are allowed.');
        return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
        Toast.error('File size must be less than 5MB');
        return;
    }

    // Preview image
    const reader = new FileReader();
    reader.onload = function (e) {
        $('#profilePicturePreview').attr('src', e.target.result);
    };
    reader.readAsDataURL(file);

    // Upload image
    uploadProfilePicture(file);
}

/**
 * Upload profile picture
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
                // Update sidebar profile image
                $('.sidebar .profile-image').attr('src', `/images/profile/${response.fileName}`);
            } else {
                Toast.error(response.message);
                // Revert preview
                if (originalData.profileImage) {
                    $('#profilePicturePreview').attr('src', `/images/profile/${originalData.profileImage}`);
                }
            }
        },
        error: function (xhr, status, error) {
            console.error('Error uploading profile picture:', error);
            Toast.error('Failed to upload profile picture');
            // Revert preview
            if (originalData.profileImage) {
                $('#profilePicturePreview').attr('src', `/images/profile/${originalData.profileImage}`);
            }
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
                // Update sidebar name
                $('.sidebar .user-name').text(`${formData.firstName} ${formData.lastName}`);
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
