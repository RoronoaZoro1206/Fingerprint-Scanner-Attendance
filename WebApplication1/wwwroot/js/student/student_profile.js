/**
 * Student Profile Management - Refactored to use common utilities
 * Manage student profile with program lock functionality
 */

let profileData = null;
let programsData = [];
let sectionsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load profile data
    loadProfile();
    // ✅ SECURITY FIX: Removed loadPrograms() and loadSections() - students cannot edit academic info

    // Form submission
    $('#profileForm').on('submit', function (e) {
        e.preventDefault();
        saveProfile();
    });

    // Setup modal icon reinitialization
    setupModalIconReinit(['profileLockModal']);
});

/**
 * Load student profile
 */
function loadProfile() {
    $.ajax({
        url: '/StudentProfile/GetProfile',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                profileData = response.data;
                displayProfile(response.data);
            } else {
                Toast.error(response.message);
            }
        },
        error: function () {
            Toast.error('Failed to load profile');
        }
    });
}

/**
 * Display profile data
 */
function displayProfile(data) {
    // Update header
    const fullName = `${data.userFirstName} ${data.userMiddleName || ''} ${data.userLastName}`.replace(/\s+/g, ' ');
    $('#profileName').text(fullName);
    $('#profileEmail').text(data.userEmail);
    
    if (data.studNumber) {
        $('#profileStudentNumber').text(data.studNumber).show();
    } else {
        $('#profileStudentNumber').hide();
    }

    // ✅ FIX: Properly handle profile image with correct default path
    if (data.userProfileImage && data.userProfileImage !== 'default_profile.png' && data.userProfileImage !== 'templates/default_profile.png') {
        // User has a custom profile image
        const imagePath = `/images/profile/${data.userProfileImage}?v=${Date.now()}`;
        $('#profileImage').attr('src', imagePath);
    } else {
        // No profile image or using default - use default from templates folder
        $('#profileImage').attr('src', '/images/profile/templates/default_profile.png');
    }

    // Fill form fields
    $('#firstName').val(data.userFirstName);
    $('#middleName').val(data.userMiddleName || '');
    $('#lastName').val(data.userLastName);
    $('#gender').val(data.userGender || '');
    $('#birthdate').val(data.userBirthdate || '');
    $('#phone').val(data.userPhone || '');
    $('#address').val(data.userAddress || '');

    // ✅ SECURITY FIX: Academic information is READ-ONLY for students
    // Display values but fields remain disabled
    if (data.programId) {
        $('#program').append(`<option value="${data.programId}" selected>${data.programName || 'Program'}</option>`);
    }
    if (data.studYearLevel) {
        $('#yearLevel').val(data.studYearLevel);
    }
    if (data.sectionId) {
        $('#section').append(`<option value="${data.sectionId}" selected>${data.sectionName || 'Section'}</option>`);
    }
}

/**
 * Save profile
 */
function saveProfile() {
    const form = $('#profileForm')[0];
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // ✅ SECURITY FIX: Removed program validation - students cannot edit academic info
    // Just save personal information only
    submitProfileUpdate();
}

/**
 * Submit profile update to server
 */
function submitProfileUpdate() {
    // ✅ SECURITY FIX: Only send personal information - exclude academic fields
    const profileUpdate = {
        UserFirstName: $('#firstName').val().trim(),
        UserMiddleName: $('#middleName').val().trim() || null,
        UserLastName: $('#lastName').val().trim(),
        UserPhone: $('#phone').val().trim() || null,
        UserGender: $('#gender').val() || null,
        UserBirthdate: $('#birthdate').val() || null,
        UserAddress: $('#address').val().trim() || null
        // ✅ REMOVED: ProgramId, StudYearLevel, SectionId - students cannot edit these
    };

    const $btn = $('#btnSaveProfile');
    setButtonLoading($btn, 'Saving...');

    ajaxPut('/StudentProfile/UpdateProfile', profileUpdate,
        function (response) {
            Toast.success(response.message);
            loadProfile(); // Reload to show updated data
            updateSidebarName(profileUpdate.UserFirstName, profileUpdate.UserLastName);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Update sidebar username in real-time
 */
function updateSidebarName(firstName, lastName) {
    const fullName = `${firstName} ${lastName}`;
    
    // Update desktop sidebar
    $('.sidebar .user-name').text(fullName);
    
    // Update mobile offcanvas sidebar
    $('#sidebarOffcanvas .user-name').text(fullName);
    
    // Update navbar if it has user name display
    $('.navbar .user-name').text(fullName);
}
