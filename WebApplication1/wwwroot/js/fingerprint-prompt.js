/**
 * Fingerprint Registration Notification
 * Shows a subtle toast notification to admins/faculty who haven't registered their fingerprint
 */

(function () {
    'use strict';

    const STORAGE_KEY = 'fingerprintPromptShown';
    const DELAY_MS = 400;
    const AUTO_HIDE_MS = 10000; // Auto-hide after 10 seconds

    // Check if user is admin, faculty, or student
    function shouldShowPrompt() {
        const roleId = document.body.dataset.roleId;
        return roleId === '1' || roleId === '2' || roleId === '3'; // 1 = Admin, 2 = Faculty, 3 = Student
    }

    // Check if prompt was already shown in this login session
    function wasPromptShownThisSession() {
        return sessionStorage.getItem(STORAGE_KEY) === 'true';
    }

    // Mark prompt as shown for this login session only
    function markPromptAsShown() {
        sessionStorage.setItem(STORAGE_KEY, 'true');
    }

    // Check fingerprint registration status
    async function checkFingerprintStatus() {
        try {
            const response = await fetch('/api/fingerprint/check-status', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to check fingerprint status');
            }

            const data = await response.json();
            return data;
        } catch (error) {
            console.error('Error checking fingerprint status:', error);
            return { hasFingerprint: true }; // Assume registered on error to avoid annoying users
        }
    }

    // Create and show the notification
    function showFingerprintNotification() {
        const roleId = document.body.dataset.roleId;
        let registerUrl;
        let message;

        if (roleId === '1') {
            registerUrl = '/Admin/AdminRegisterFingerprint';
            message = 'Register your fingerprint for secure and convenient login';
        } else if (roleId === '2') {
            registerUrl = '/Faculty/FacultyRegisterFingerprint';
            message = 'Register your fingerprint for secure and convenient login';
        } else if (roleId === '3') {
            registerUrl = '/Student/StudentRegisterFingerprint';
            message = 'Register your fingerprint for attendance tracking and secure login';
        }

        // Create notification HTML
        const notificationHTML = `
            <div class="fingerprint-notification" id="fingerprintNotification">
                <div class="fingerprint-notification-content">
                    <div class="fingerprint-notification-icon">
                        <i data-lucide="fingerprint"></i>
                    </div>
                    <div class="fingerprint-notification-text">
                        <div class="fingerprint-notification-title">Fingerprint Not Registered</div>
                        <div class="fingerprint-notification-message">${message}</div>
                    </div>
                    <div class="fingerprint-notification-actions">
                        <button type="button" class="btn-notification btn-notification-secondary" id="btnNotificationLater">
                            Later
                        </button>
                        <button type="button" class="btn-notification btn-notification-primary" id="btnNotificationRegister">
                            Register Now
                        </button>
                    </div>
                    <button type="button" class="btn-notification-close" id="btnNotificationClose">
                        <i data-lucide="x"></i>
                    </button>
                </div>
            </div>
        `;

        // Insert notification into body
        document.body.insertAdjacentHTML('beforeend', notificationHTML);

        // Initialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        // Get elements
        const notification = document.getElementById('fingerprintNotification');
        const btnLater = document.getElementById('btnNotificationLater');
        const btnRegister = document.getElementById('btnNotificationRegister');
        const btnClose = document.getElementById('btnNotificationClose');

        // Add event listeners
        btnLater.addEventListener('click', () => {
            markPromptAsShown();
            closeNotification(notification);
        });

        btnRegister.addEventListener('click', () => {
            window.location.href = registerUrl;
        });

        btnClose.addEventListener('click', () => {
            markPromptAsShown();
            closeNotification(notification);
        });

        // Show notification with animation
        setTimeout(() => {
            notification.classList.add('show');
        }, 10);

        // Auto-hide after delay
        setTimeout(() => {
            if (notification && notification.parentElement) {
                markPromptAsShown();
                closeNotification(notification);
            }
        }, AUTO_HIDE_MS);
    }

    // Close notification with animation
    function closeNotification(notification) {
        notification.classList.remove('show');
        notification.classList.add('hide');
        setTimeout(() => {
            if (notification && notification.parentElement) {
                notification.remove();
            }
        }, 300);
    }

    // Initialize the prompt
    async function init() {
        // Check if we should show the prompt
        if (!shouldShowPrompt()) {
            return;
        }

        // Check if already shown in this login session
        if (wasPromptShownThisSession()) {
            return;
        }

        // Check fingerprint status
        const status = await checkFingerprintStatus();

        if (!status.hasFingerprint) {
            // Mark as shown for this session
            markPromptAsShown();

            // Show notification after delay
            setTimeout(() => {
                showFingerprintNotification();
            }, DELAY_MS);
        }
    }

    // Run when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
