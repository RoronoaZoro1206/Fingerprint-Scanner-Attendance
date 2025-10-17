/**
 * Student Profile Management
 */

let profileData = null;
let programsData = [];
let sectionsData = [];

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

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
        // Close the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('profileLockModal'));
        modal.hide();
        
        // Proceed with save
        submitProfileUpdate();
    });
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

    if (data.userProfileImage && data.userProfileImage !== 'default_profile.png') {
        $('#profileImage').attr('src', `/images/profile/${data.userProfileImage}`);
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

    // Lock academic fields if profile is submitted
    if (data.profileSubmitted) {
        $('#program').prop('disabled', true);
        $('#yearLevel').prop('disabled', true);
        $('#section').prop('disabled', true);
        
        // Add visual indicator
        if (!$('#academicLockNotice').length) {
            $('.form-section:has(#program)').prepend(`
                <div id="academicLockNotice" class="alert alert-info" style="margin-bottom: 1rem;">
                    <i data-lucide="lock" style="width: 16px; height: 16px; vertical-align: middle;"></i>
                    <strong>Academic Information Locked</strong><br>
                    Your academic information was submitted on ${new Date(data.profileSubmittedAt).toLocaleString()}.
                    Contact administration if you need to make changes.
                </div>
            `);
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
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
        const modal = new bootstrap.Modal(document.getElementById('profileLockModal'));
        modal.show();
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
    const originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: '/StudentProfile/UpdateProfile',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(profileUpdate),
        success: function (response) {
            if (response.success) {
                Toast.success(response.message);
                loadProfile(); // Reload to show updated data
            } else {
                Toast.error(response.message);
            }
        },
        error: function () {
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
