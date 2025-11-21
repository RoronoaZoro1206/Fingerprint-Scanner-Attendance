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
    loadPrograms();
    loadSections();

    // Form submission
    $('#profileForm').on('submit', function (e) {
        e.preventDefault();
        saveProfile();
    });

    // Filter sections by program and year level
    $('#program, #yearLevel').on('change', filterSections);

    // Confirm lock button in modal
    $('#btnConfirmLock').on('click', function () {
        hideModal('profileLockModal');
        submitProfileUpdate();
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
    if (data.userProfileImage) {
        // Check if it's a user-specific image or template
        const imagePath = data.userProfileImage.includes('/') 
            ? `/images/profile/${data.userProfileImage}?v=${Date.now()}` 
            : `/images/profile/${data.userProfileImage}?v=${Date.now()}`;
        $('#profileImage').attr('src', imagePath);
    } else {
        // No profile image - use default from templates folder
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

    // Academic information
    if (data.programId) {
        $('#program').val(data.programId);
    }
    if (data.studYearLevel) {
        $('#yearLevel').val(data.studYearLevel);
    }
    if (data.sectionId) {
        $('#section').val(data.sectionId);
    }

    // Lock program field if profile is submitted (year level and section remain editable)
    if (data.profileSubmitted) {
        $('#program').prop('disabled', true);
        $('#yearLevel').prop('disabled', false);
        $('#section').prop('disabled', false);
        
        // Add visual indicator
        if (!$('#academicLockNotice').length) {
            $('.form-section:has(#program)').prepend(`
                <div id="academicLockNotice" class="alert alert-info" style="margin-bottom: 1rem;">
                    <i data-lucide="lock" style="width: 16px; height: 16px; vertical-align: middle;"></i>
                    <strong>Program Locked</strong><br>
                    Your program was locked on ${new Date(data.profileSubmittedAt).toLocaleString()}.
                    You can still update your year level and section as you progress.
                    Contact administration if you need to change your program.
                </div>
            `);
            initializeLucideIcons();
        }
    } else {
        $('#program').prop('disabled', false);
        $('#yearLevel').prop('disabled', false);
        $('#section').prop('disabled', false);
        $('#academicLockNotice').remove();
    }

    // Filter sections after loading
    filterSections();
}

/**
 * Load programs
 */
function loadPrograms() {
    $.ajax({
        url: '/StudentProfile/GetPrograms',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                programsData = response.data;
                populateProgramDropdown();
            }
        },
        error: function () {
            console.error('Failed to load programs');
        }
    });
}

/**
 * Populate program dropdown
 */
function populateProgramDropdown() {
    const $select = $('#program');
    $select.find('option:not(:first)').remove();

    programsData.forEach(program => {
        $select.append(`<option value="${program.programId}">${program.programName} (${program.programCode})</option>`);
    });

    // Restore selected value if exists
    if (profileData && profileData.programId) {
        $select.val(profileData.programId);
    }
}

/**
 * Load sections
 */
function loadSections() {
    $.ajax({
        url: '/StudentProfile/GetSections',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                sectionsData = response.data;
                filterSections();
            }
        },
        error: function () {
            console.error('Failed to load sections');
        }
    });
}

/**
 * Filter sections by program and year level
 */
function filterSections() {
    const $select = $('#section');
    const selectedProgramId = parseInt($('#program').val()) || null;
    const selectedYearLevel = parseInt($('#yearLevel').val()) || null;
    const currentSectionId = profileData?.sectionId;

    $select.find('option:not(:first)').remove();

    let filteredSections = sectionsData;

    // Filter by program
    if (selectedProgramId) {
        filteredSections = filteredSections.filter(s => s.programId === selectedProgramId);
    }

    // Filter by year level
    if (selectedYearLevel) {
        filteredSections = filteredSections.filter(s => s.yearLevel === selectedYearLevel);
    }

    // Populate dropdown
    filteredSections.forEach(section => {
        $select.append(`<option value="${section.sectionId}">${section.sectionName}</option>`);
    });

    // Restore selected value if exists and is in filtered list
    if (currentSectionId) {
        const sectionExists = filteredSections.some(s => s.sectionId === currentSectionId);
        if (sectionExists) {
            $select.val(currentSectionId);
        }
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

    const programId = $('#program').val();
    if (!programId) {
        Toast.warning('Please select a program. This is required for class enrollment.');
        return;
    }

    // Show confirmation modal if this is the first submission
    if (!profileData.profileSubmitted && programId) {
        showModal('profileLockModal');
        return;
    }

    // If already submitted or no modal needed, proceed with save
    submitProfileUpdate();
}

/**
 * Submit profile update to server
 */
function submitProfileUpdate() {
    const profileUpdate = {
        UserFirstName: $('#firstName').val().trim(),
        UserMiddleName: $('#middleName').val().trim() || null,
        UserLastName: $('#lastName').val().trim(),
        UserPhone: $('#phone').val().trim() || null,
        UserGender: $('#gender').val() || null,
        UserBirthdate: $('#birthdate').val() || null,
        UserAddress: $('#address').val().trim() || null,
        StudNumber: profileData?.studNumber || null,
        ProgramId: $('#program').val() ? parseInt($('#program').val()) : null,
        StudYearLevel: $('#yearLevel').val() ? parseInt($('#yearLevel').val()) : null,
        SectionId: $('#section').val() ? parseInt($('#section').val()) : null
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
