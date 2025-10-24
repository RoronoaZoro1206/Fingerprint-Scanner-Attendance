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

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Check device status on load
    checkDeviceStatus();
    checkRegistrationStatus();

    // Scan button click
    btnScan.addEventListener('click', async function () {
        try {
            btnScan.disabled = true;
            btnScan.textContent = 'Scanning...';
            fingerprintScanner.classList.add('scanning');

            const response = await fetch('/Student/StudentRegisterFingerpront/CaptureFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success) {
                capturedTemplateBase64 = result.templateBase64;
                showToast('Fingerprint captured successfully!', 'success');
                fingerprintScanner.classList.add('captured');
                btnRegister.disabled = false;
            } else {
                showToast(result.message || 'Failed to capture fingerprint', 'error');
                fingerprintScanner.classList.add('error');
                setTimeout(() => fingerprintScanner.classList.remove('error'), 1000);
            }
        } catch (error) {
            console.error('Error capturing fingerprint:', error);
            showToast('Error capturing fingerprint', 'error');
        } finally {
            btnScan.disabled = false;
            btnScan.textContent = 'Scan Fingerprint';
            fingerprintScanner.classList.remove('scanning');
        }
    });

    // Register button click
    btnRegister.addEventListener('click', async function () {
        if (!capturedTemplateBase64) {
            showToast('Please scan your fingerprint first', 'warning');
            return;
        }

        try {
            btnRegister.disabled = true;
            btnRegister.innerHTML = '<i data-lucide="loader" class="icon-spin"></i> Registering...';

            const response = await fetch('/Student/StudentRegisterFingerpront/RegisterFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    templateBase64: capturedTemplateBase64,
                    templateName: 'Primary Fingerprint'
                })
            });

            const result = await response.json();

            if (result.success) {
                showToast(result.message || 'Fingerprint registered successfully!', 'success');
                fingerprintScanner.classList.remove('captured');
                fingerprintScanner.classList.add('registered');
                capturedTemplateBase64 = null;
                checkRegistrationStatus();
                
                // Redirect after 2 seconds
                setTimeout(() => {
                    window.location.href = '/Student/StudentProfile';
                }, 2000);
            } else {
                showToast(result.message || 'Failed to register fingerprint', 'error');
            }
        } catch (error) {
            console.error('Error registering fingerprint:', error);
            showToast('Error registering fingerprint', 'error');
        } finally {
            btnRegister.disabled = false;
            btnRegister.innerHTML = '<i data-lucide="shield-check"></i> Register Fingerprint';
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    });

    // Cancel button click
    btnCancel.addEventListener('click', function () {
        const modal = new bootstrap.Modal(document.getElementById('cancelModal'));
        modal.show();
    });

    // Confirm cancel
    btnConfirmCancel.addEventListener('click', function () {
        window.location.href = '/Student/StudentProfile';
    });

    // Check device status
    async function checkDeviceStatus() {
        try {
            const response = await fetch('/Student/StudentRegisterFingerpront/GetDeviceStatus');
            const result = await response.json();

            if (result.isDeviceConnected) {
                scannerStatus.textContent = 'Online';
                scannerStatus.classList.remove('offline');
                scannerStatus.classList.add('online');
            } else {
                scannerStatus.textContent = 'Offline';
                scannerStatus.classList.remove('online');
                scannerStatus.classList.add('offline');
                
                // Try to initialize
                await initializeDevice();
            }
        } catch (error) {
            console.error('Error checking device status:', error);
            scannerStatus.textContent = 'Error';
            scannerStatus.classList.add('offline');
        }
    }

    // Initialize device
    async function initializeDevice() {
        try {
            const response = await fetch('/Student/StudentRegisterFingerpront/InitializeDevice', {
                method: 'POST'
            });
            const result = await response.json();
            
            if (result.success) {
                checkDeviceStatus();
            }
        } catch (error) {
            console.error('Error initializing device:', error);
        }
    }

    // Check registration status
    async function checkRegistrationStatus() {
        try {
            const response = await fetch('/Student/StudentRegisterFingerpront/GetRegistrationStatus');
            const result = await response.json();

            if (result.isRegistered) {
                registrationStatus.textContent = 'Registered';
                registrationStatus.classList.remove('pending');
                registrationStatus.classList.add('registered');
            } else {
                registrationStatus.textContent = 'Not Registered';
                registrationStatus.classList.remove('registered');
                registrationStatus.classList.add('pending');
            }
        } catch (error) {
            console.error('Error checking registration status:', error);
        }
    }

    // Toast notification helper
    function showToast(message, type = 'info') {
        // You can implement your own toast notification here
        // For now, using alert as fallback
        if (typeof Toastify !== 'undefined') {
            Toastify({
                text: message,
                duration: 3000,
                gravity: 'top',
                position: 'right',
                backgroundColor: type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'
            }).showToast();
        } else {
            alert(message);
        }
    }
});
