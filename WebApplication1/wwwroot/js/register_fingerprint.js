/// <reference path="global.d.ts" />

// Fingerprint Registration JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const btnScan = document.getElementById('btnScan');
    const btnRegister = document.getElementById('btnRegister');
    const btnCancel = document.getElementById('btnCancel');
    const btnConfirmCancel = document.getElementById('btnConfirmCancel');
    const scannerStatus = document.getElementById('scannerStatus');
    const registrationStatus = document.getElementById('registrationStatus');
    const fingerprintScanner = document.getElementById('fingerprintScanner');

    let capturedTemplateBase64 = null;
    let capturedImageBase64 = null;
    let isRegistered = false;

    // ✅ NEW: Toast management to prevent overlapping toasts
    let activeToast = null;
    let toastQueue = [];

    // ✅ SYNCHRONIZED TIMING CONSTANT: All error state elements fade out together
    const ERROR_STATE_DURATION = 3000; // 3 seconds - matches LED backend timing

    // Detect user role from URL
    const path = window.location.pathname;
    let baseUrl = '';
    let redirectUrl = '';

    if (path.includes('/Admin/')) {
        baseUrl = '/Admin/AdminRegisterFingerprint';
        redirectUrl = '/Admin/Index';
    } else if (path.includes('/Faculty/')) {
        baseUrl = '/Faculty/FacultyRegisterFingerprint';
        redirectUrl = '/Faculty/Index';
    } else if (path.includes('/Student/')) {
        baseUrl = '/Student/StudentRegisterFingerprint';
  redirectUrl = '/Student/';  // ✅ FIXED: Correct route to Student/Index
    }

    console.log('Base URL:', baseUrl);
    console.log('Redirect URL:', redirectUrl);

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Check device status on load
    checkDeviceStatus();
    checkRegistrationStatus();

    // ✅ Auto-refresh status indicators to keep them current
    setInterval(checkDeviceStatus, 10000);       // Refresh scanner status every 10 seconds
    setInterval(checkRegistrationStatus, 30000); // Refresh registration status every 30 seconds
    
    console.log('✅ Auto-refresh enabled: Scanner status (10s), Registration status (30s)');

    // Scan button click
    if (btnScan) {
        btnScan.addEventListener('click', async function () {
            try {
                console.log(`[${new Date().toISOString()}] [Frontend] Scan button clicked`);
                btnScan.disabled = true;
                btnScan.textContent = 'Scanning...';
                fingerprintScanner.classList.add('scanning');

                // 📝 Show "Scanning in progress" toast
                showToast('Scanning in progress, please wait...', 'info');

                const url = `${baseUrl}/CaptureFingerprint`;
                console.log(`[${new Date().toISOString()}] [Frontend] Fetching: ${url}`);

                // 📝 Track start time for minimum scanning duration
                const scanStartTime = Date.now();
                const MIN_SCAN_DURATION = 2000; // 2 seconds minimum (matches login page feel)

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                console.log(`[${new Date().toISOString()}] [Frontend] Response status: ${response.status}`);
                console.log(`[${new Date().toISOString()}] [Frontend] Response headers:`, response.headers.get('content-type'));

                const result = await response.json();
                console.log(`[${new Date().toISOString()}] [Frontend] Response data:`, result);

                // 📝 Calculate elapsed time and wait if needed
                const scanElapsedTime = Date.now() - scanStartTime;
                const remainingTime = Math.max(0, MIN_SCAN_DURATION - scanElapsedTime);

                if (remainingTime > 0) {
                    console.log(`[${new Date().toISOString()}] [Frontend] Extending scan display by ${remainingTime}ms for better UX`);
                    await new Promise(resolve => setTimeout(resolve, remainingTime));
                }

                if (result.success) {
                    // ✅ SUCCESS PATH (already working)
                    console.log(`[${new Date().toISOString()}] [Frontend] ✅ SUCCESS - Fingerprint captured`);
                    
                    capturedTemplateBase64 = result.templateBase64;
                    capturedImageBase64 = result.imageBase64;

                    // ✅ STEP 1: Display the fingerprint image in the circle FIRST
                    displayFingerprintImage(result.imageBase64, result.imageWidth, result.imageHeight);

                    // ✅ STEP 2: Show success toast SECOND
                    showToast('Fingerprint captured successfully!', 'success', 3000);
                    
                    fingerprintScanner.classList.add('captured');
                    btnRegister.disabled = false;

                    // ✅ STEP 3: Wait for DOM to finish rendering (ensure image and toast are visible)
                    await new Promise(resolve => requestAnimationFrame(() => {
                        requestAnimationFrame(resolve); // Double RAF ensures paint is complete
                    }));

                    // ✅ STEP 4: NOW activate LED after UI is fully updated (SYNCHRONIZED!)
                    if (result.ledControlDeferred) {
                        console.log(`[${new Date().toISOString()}] [Frontend] 🟢 Activating GREEN LED after UI updates (synchronized timing)`);
                        try {
                            const ledResponse = await fetch(`${baseUrl}/ActivateLED`, {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' }
                            });
                            const ledResult = await ledResponse.json();
                            console.log(`[${new Date().toISOString()}] [Frontend] ✅ GREEN LED Response:`, ledResult);
                        } catch (ledError) {
                            console.warn(`[${new Date().toISOString()}] [Frontend] ⚠️ Could not activate GREEN LED:`, ledError);
                        }
                    }

                    // ✅ Refresh status if indicated by server
                    if (result.shouldRefreshStatus) {
                        console.log(`[${new Date().toISOString()}] [Frontend] Refreshing device status after successful capture`);
                        await checkDeviceStatus();
                    }
                } else {
                    // ✅ ERROR PATH (NEW: Frontend-controlled RED LED activation with SYNCHRONIZED timing)
                    console.log(`[${new Date().toISOString()}] [Frontend] ❌ ERROR - Capture failed`);
                    console.log(`[${new Date().toISOString()}] [Frontend] Error type: ${result.errorType}`);
                    console.log(`[${new Date().toISOString()}] [Frontend] Error message: ${result.message}`);
                    
                    // ✅ STEP 1: Display error UI with synchronized timing
                    showToast(result.message || 'Failed to capture fingerprint', 'error', ERROR_STATE_DURATION);
                    
                    fingerprintScanner.classList.add('error');
                    
                    // ✅ SYNCHRONIZED FIX: Remove error class after ERROR_STATE_DURATION (3000ms) to match LED/toast timing
                    setTimeout(() => fingerprintScanner.classList.remove('error'), ERROR_STATE_DURATION);

                    // ✅ STEP 2: Wait for error UI to render
                    await new Promise(resolve => requestAnimationFrame(() => {
                        requestAnimationFrame(resolve);
                    }));

                    // ✅ STEP 3: NOW activate RED LED after error UI is displayed (SYNCHRONIZED!)
                    console.log(`[${new Date().toISOString()}] [Frontend] 🔴 Activating RED LED after error UI updates (synchronized timing - ${ERROR_STATE_DURATION}ms)`);
                    try {
                        const ledResponse = await fetch(`${baseUrl}/ActivateErrorLED`, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' }
                        });
                        const ledResult = await ledResponse.json();
                        console.log(`[${new Date().toISOString()}] [Frontend] ✅ RED LED Response:`, ledResult);
                        
                        if (ledResult.success) {
                            console.log(`[${new Date().toISOString()}] [Frontend] 🔴 RED LED activated successfully on hardware scanner (${ERROR_STATE_DURATION}ms duration)`);
                        } else {
                            console.error(`[${new Date().toISOString()}] [Frontend] ❌ RED LED activation failed:`, ledResult.message);
                        }
                    } catch (ledError) {
                        console.error(`[${new Date().toISOString()}] [Frontend] ❌ Could not activate RED LED:`, ledError);
                    }

                    // ✅ Refresh status even on error (device might have come online)
                    if (result.shouldRefreshStatus) {
                        console.log(`[${new Date().toISOString()}] [Frontend] Refreshing device status after capture error`);
                        await checkDeviceStatus();
                    }
                }
            } catch (error) {
                console.error(`[${new Date().toISOString()}] [Frontend] ❌ Exception during capture:`, error);
                // Show error toast with synchronized timing
                showToast('Error: ' + error.message, 'error', ERROR_STATE_DURATION);
            } finally {
                btnScan.disabled = false;
                btnScan.textContent = 'Scan Fingerprint';
                fingerprintScanner.classList.remove('scanning');
            }
        });
    }

    // Display fingerprint image function
    function displayFingerprintImage(imageBase64, width, height) {
        if (!imageBase64) return;

        // Clear existing content
        fingerprintScanner.innerHTML = '';

        // Create image element
        const img = document.createElement('img');
        img.src = 'data:image/bmp;base64,' + imageBase64;
        img.alt = 'Captured Fingerprint';

        // Style to fit circle naturally without zoom
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.objectFit = 'contain';  // Changed from 'cover' to 'contain' - no zoom
        img.style.borderRadius = '50%';
        // Removed filter: invert(1) - keep natural colors (white bg, black lines)

        fingerprintScanner.appendChild(img);

        console.log(`Fingerprint image displayed: ${width}x${height} (natural colors, no zoom)`);
    }

    // Register/Update button click
    if (btnRegister) {
        btnRegister.addEventListener('click', async function () {
            if (!capturedTemplateBase64) {
                // KEEP TOASTIFY: Simple warning
                showToast('Please scan your fingerprint first', 'warning', 3000);
                return;
            }

            // If already registered, show password confirmation immediately
            if (isRegistered) {
                showPasswordConfirmationModal();
                return;
            }

            // Otherwise, attempt to register
            try {
                console.log('Register button clicked');
                btnRegister.disabled = true;
                btnRegister.innerHTML = '<i data-lucide="loader" class="icon-spin"></i> Registering...';

                const url = `${baseUrl}/RegisterFingerprint`;
                console.log('Fetching:', url);

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        templateBase64: capturedTemplateBase64,
                        templateName: 'Primary Fingerprint'
                    })
                });

                console.log('Response status:', response.status);
                const result = await response.json();
                console.log('Response data:', result);

                if (result.success) {
                    // REPLACE WITH SWEETALERT2: Final success message
                    SwalAlert.success('Good job!', result.message || 'Fingerprint registered successfully!').then(() => {
                        fingerprintScanner.classList.remove('captured');
                        fingerprintScanner.classList.add('registered');
                        capturedTemplateBase64 = null;
                        capturedImageBase64 = null;

                        // ✅ Refresh registration status after successful register
                        checkRegistrationStatus();

                        // Redirect after alert closes
                        window.location.href = redirectUrl;
                    });
                } else if (result.requirePasswordConfirmation) {
                    // User has existing fingerprint, need password confirmation
                    showPasswordConfirmationModal();
                } else {
                    // REPLACE WITH SWEETALERT2: Final error message
                    SwalAlert.error('Oops...', result.message || 'Failed to register fingerprint');

                    // ✅ Refresh status after registration error
                    if (result.shouldRefreshStatus) {
                        console.log('Refreshing registration status after registration error');
                        await checkRegistrationStatus();
                    }
                }
            } catch (error) {
                console.error('Error registering fingerprint:', error);
                // REPLACE WITH SWEETALERT2: Final error message
                SwalAlert.error('Oops...', 'Error: ' + error.message);
            } finally {
                btnRegister.disabled = false;
                updateRegisterButtonText();
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        });
    }

    // Update register button text based on registration status
    function updateRegisterButtonText() {
        if (btnRegister) {
            if (isRegistered) {
                btnRegister.innerHTML = '<i data-lucide="refresh-cw"></i> Update Fingerprint';
            } else {
                btnRegister.innerHTML = '<i data-lucide="shield-check"></i> Register Fingerprint';
            }
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    }

    // Show password confirmation modal for updating fingerprint
    function showPasswordConfirmationModal() {
        const modalHTML = `
        <div class="modal fade" id="passwordConfirmModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
   <div class="modal-content">
    <div class="modal-header border-0 pb-0">
   <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
     </div>
    <div class="modal-body text-center pt-0">
 <div class="mb-3">
               <div class="delete-icon-wrapper">
          <i data-lucide="shield-check" class="delete-icon" style="color: #3b82f6;"></i>
     </div>
 </div>
       <h5 class="modal-title mb-2">Update Fingerprint</h5>
   <p class="text-muted mb-3">You already have a registered fingerprint.<br>Please confirm your password to update it.</p>
         <div class="text-start">
    <label for="updatePassword" class="form-label">Password</label>
         <div class="password-input-container">
    <input type="password" class="form-control" id="updatePassword" placeholder="Enter your password" required>
      <button type="button" class="password-toggle" id="toggleUpdatePassword" aria-label="Toggle password visibility">
         <i data-lucide="eye"></i>
            </button>
       </div>
   <div class="invalid-feedback" id="passwordError"></div>
 </div>
    </div>
           <div class="modal-footer border-0 justify-content-center gap-2">
            <button type="button" class="btn btn-secondary px-4" data-bs-dismiss="modal">Cancel</button>
    <button type="button" class="btn btn-primary px-4" id="btnConfirmUpdate">
 <i data-lucide="shield-check" class="icon-sm"></i>
      Confirm & Update
        </button>
        </div>
 </div>
            </div>
        </div>
        `;

        // Remove existing modal if present
        const existingModal = document.getElementById('passwordConfirmModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Add modal to body
        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // Initialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        // Show modal
        const modalElement = document.getElementById('passwordConfirmModal');
        const modal = new bootstrap.Modal(modalElement);
        modal.show();

        // Initialize password toggle functionality
        initializePasswordToggle();

        // Handle confirm button
        const btnConfirmUpdate = document.getElementById('btnConfirmUpdate');
        const updatePassword = document.getElementById('updatePassword');
        const passwordError = document.getElementById('passwordError');

        btnConfirmUpdate.addEventListener('click', async function () {
            const password = updatePassword.value.trim();

            if (!password) {
                updatePassword.classList.add('is-invalid');
                passwordError.textContent = 'Password is required';
                return;
            }

            try {
                btnConfirmUpdate.disabled = true;
                btnConfirmUpdate.innerHTML = '<i data-lucide="loader" class="icon-spin"></i> Updating...';

                const url = `${baseUrl}/UpdateFingerprint`;
                console.log('Updating fingerprint:', url);

                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        templateBase64: capturedTemplateBase64,
                        templateName: 'Primary Fingerprint',
                        password: password
                    })
                });

                const result = await response.json();
                console.log('Update response:', result);

                if (result.success) {
                    // SUCCESS: Close modal and show success message
                    modal.hide();
                    SwalAlert.success('Success!', result.message || 'Fingerprint updated successfully!').then(() => {
                        fingerprintScanner.classList.remove('captured');
                        fingerprintScanner.classList.add('registered');
                        capturedTemplateBase64 = null;
                        capturedImageBase64 = null;

                        // ✅ Refresh registration status after successful update
                        checkRegistrationStatus();

                        // Redirect after alert closes
                        window.location.href = redirectUrl;
                    });
                } else if (result.isDuplicate) {
                    // DUPLICATE FINGERPRINT: Close modal and show error
                    modal.hide();
                    SwalAlert.error('Duplicate Fingerprint!', result.message || 'This fingerprint is already registered to another user. Please use a different finger.');

                    // Clear captured fingerprint to force re-scan
                    capturedTemplateBase64 = null;
                    capturedImageBase64 = null;
                    fingerprintScanner.classList.remove('captured');
                    fingerprintScanner.innerHTML = '<div class="scanner-instruction">Place finger here</div>';
                    btnRegister.disabled = true;

                    // Log duplicate attempt
                    console.error(`Duplicate fingerprint detected! Match score: ${result.matchScore}`);
                } else {
                    // ✅ INCORRECT PASSWORD: Show SweetAlert2 error + inline validation
                    console.warn('Password validation failed:', result.message);

                    // Close the modal first
                    modal.hide();

                    // Show SweetAlert2 error message
                    SwalAlert.error(
                        'Incorrect Password',
                        result.message || 'The password you entered is incorrect. Please try again.'
                    ).then(() => {
                        // After user acknowledges the error, reopen the modal
                        setTimeout(() => {
                            showPasswordConfirmationModal();
                        }, 300);
                    });
                }

                // ✅ Refresh status after any update attempt
                if (result.shouldRefreshStatus) {
                    console.log('Refreshing registration status after update attempt');
                    await checkRegistrationStatus();
                    await checkDeviceStatus();
                }
            } catch (error) {
                console.error('Error updating fingerprint:', error);
                // NETWORK/SERVER ERROR: Show error
                modal.hide();
                SwalAlert.error('Update Failed', 'An error occurred while updating your fingerprint. Please try again.');
            } finally {
                btnConfirmUpdate.disabled = false;
                btnConfirmUpdate.innerHTML = '<i data-lucide="shield-check" class="icon-sm"></i> Confirm & Update';
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        });

        // Clear error on input
        updatePassword.addEventListener('input', function () {
            updatePassword.classList.remove('is-invalid');
        });

        // Clean up modal on hide
        modalElement.addEventListener('hidden.bs.modal', function () {
            modalElement.remove();
        });
    }

    // Cancel button click - REPLACE WITH SWEETALERT2 CONFIRMATION
    if (btnCancel) {
        btnCancel.addEventListener('click', function () {
            SwalAlert.confirm(
                'Cancel Registration?',
                () => {
                    window.location.href = redirectUrl;
                },
                'Yes, cancel',
                'Stay'
            );
        });
    }

    // Confirm cancel - NO LONGER NEEDED (handled by SweetAlert2)
    // if (btnConfirmCancel) { ... }

    // Check device status
    async function checkDeviceStatus() {
        try {
            const url = `${baseUrl}/GetDeviceStatus`;
            console.log('Checking device status:', url);

            const response = await fetch(url);
            console.log('Device status response:', response.status);

            const result = await response.json();
            console.log('Device status data:', result);

            if (scannerStatus) {
                if (result.isDeviceConnected) {
                    scannerStatus.textContent = 'Online';
                    scannerStatus.classList.remove('offline');
                    scannerStatus.classList.add('online');

                    // Update icon
                    const statusIcon = scannerStatus.closest('.status-card').querySelector('.status-icon');
                    if (statusIcon) {
                        statusIcon.classList.remove('offline');
                        statusIcon.classList.add('online');
                    }
                } else {
                    scannerStatus.textContent = result.status || 'Offline';
                    scannerStatus.classList.remove('online');
                    scannerStatus.classList.add('offline');

                    // Update icon
                    const statusIcon = scannerStatus.closest('.status-card').querySelector('.status-icon');
                    if (statusIcon) {
                        statusIcon.classList.remove('online');
                        statusIcon.classList.add('offline');
                    }

                    // Try to initialize
                    console.log('Device not connected, attempting to initialize...');
                    await initializeDevice();
                }
            }
        } catch (error) {
            console.error('Error checking device status:', error);
            if (scannerStatus) {
                scannerStatus.textContent = 'Error: ' + error.message;
                scannerStatus.classList.add('offline');
            }
        }
    }

    // Initialize device
    async function initializeDevice() {
        try {
            const url = `${baseUrl}/InitializeDevice`;
            console.log('Initializing device:', url);

            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            console.log('Initialize response:', response.status);
            const result = await response.json();
            console.log('Initialize data:', result);

            if (result.success) {
                console.log('Device initialized successfully');
                checkDeviceStatus();
            } else {
                console.error('Failed to initialize device:', result.message);
            }
        } catch (error) {
            console.error('Error initializing device:', error);
        }
    }

    // Check registration status
    async function checkRegistrationStatus() {
        try {
            const url = `${baseUrl}/GetRegistrationStatus`;
            console.log('Checking registration status:', url);

            const response = await fetch(url);
            const result = await response.json();
            console.log('Registration status:', result);

            if (registrationStatus) {
                if (result.isRegistered) {
                    isRegistered = true;
                    registrationStatus.textContent = 'Registered';
                    registrationStatus.classList.remove('pending');
                    registrationStatus.classList.add('registered');

                    // Update icon
                    const statusIcon = registrationStatus.closest('.status-card').querySelector('.status-icon');
                    if (statusIcon) {
                        statusIcon.classList.remove('pending');
                        statusIcon.classList.add('registered');
                    }
                } else {
                    isRegistered = false;
                    registrationStatus.textContent = 'Not Registered';
                    registrationStatus.classList.remove('registered');
                    registrationStatus.classList.add('pending');
                }

                // Update button text
                updateRegisterButtonText();
            }
        } catch (error) {
            console.error('Error checking registration status:', error);
        }
    }

    // ✅ IMPROVED: Toast notification helper with queue management to prevent overlapping
    function showToast(message, type = 'info', duration = 3000) {
        console.log(`Toast: [${type}] ${message} (Duration: ${duration}ms)`);

        // ✅ Dismiss any existing toast before showing new one
        if (activeToast) {
            console.log('Dismissing existing toast before showing new one');
            activeToast.classList.remove('show');
            activeToast.remove();
            activeToast = null;
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.textContent = message;

        // Add to body
        document.body.appendChild(toast);
        activeToast = toast;

        // Show with animation
        setTimeout(() => toast.classList.add('show'), 10);

        // ✅ Remove after specified duration (synchronized with LED timing)
        setTimeout(() => {
            if (activeToast === toast) { // Only remove if it's still the active toast
                toast.classList.remove('show');
                setTimeout(() => {
                    if (toast.parentElement) {
                        toast.remove();
                    }
                    if (activeToast === toast) {
                        activeToast = null;
                        console.log('Toast removed after duration');
                    }
                }, 300);
            }
        }, duration);
    }
});

// Add CSS for toast notifications
const style = document.createElement('style');
style.textContent = `
    .toast-notification {
   position: fixed;
        top: 20px;
        right: 20px;
     padding: 1rem 1.5rem;
        border-radius: 8px;
    color: white;
        font-weight: 500;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
 opacity: 0;
  transform: translateY(-20px);
        transition: all 0.3s ease;
   z-index: 9999;
    }

    .toast-notification.show {
      opacity: 1;
        transform: translateY(0);
    }
  
    .toast-success {
        background-color: #10b981;
    }
    
    .toast-error {
 background-color: #ef4444;
    }
  
    .toast-warning {
        background-color: #f59e0b;
    }
    
    .toast-info {
        background-color: #3b82f6;
 }
`;
document.head.appendChild(style);

// Password toggle functionality for modal
function initializePasswordToggle() {
    const toggleButton = document.getElementById('toggleUpdatePassword');
    const passwordInput = document.getElementById('updatePassword');

    if (!toggleButton || !passwordInput) return;

    // Track current state
    let isVisible = false;

    // Function to update icon
    function updateIcon() {
        const iconName = isVisible ? 'eye-off' : 'eye';
        toggleButton.innerHTML = `<i data-lucide="${iconName}"></i>`;

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    // Initialize icon
    updateIcon();

    // Toggle password visibility
    toggleButton.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        // Toggle state
        isVisible = !isVisible;

        // Update input type
        passwordInput.setAttribute('type', isVisible ? 'text' : 'password');

        // Update icon
        updateIcon();
    });
}
