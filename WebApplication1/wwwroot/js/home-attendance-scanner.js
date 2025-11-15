/// <reference path="global.d.ts" />

/**
 * Home Page Attendance Scanner with Intelligent Course Validation
 * Standardized fingerprint workflow + schedule checking
 * Prevents infinite loops with proper state management
 */

document.addEventListener('DOMContentLoaded', function () {
    const fingerprintScanner = document.getElementById('fingerprintScanner');
    const scanStatusMessage = document.getElementById('scanStatusMessage');
    const successRateElement = document.querySelector('.scan-stat-card.success h3');
    const todayScansElement = document.querySelector('.scan-stat-card.total h3');
    
    let isScanning = false;
    let isCooldown = false;
    let deviceInitialized = false;
    let scanTimeout = null;
    let statsUpdateInterval = null;

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Initialize scanner and start polling
    initializeScanner();
    startStatsUpdates();

    // Manual scan trigger (click on circle)
    if (fingerprintScanner) {
        fingerprintScanner.addEventListener('click', async function () {
            if (isScanning || isCooldown) {
                showMessage('Please wait...', 'info');
                return;
            }
            await scanFingerprint();
        });
    }

    /**
     * Initialize scanner device
     */
    async function initializeScanner() {
        try {
            showMessage('Initializing scanner...', 'scanning');
            fingerprintScanner.classList.add('scanning');

            const response = await fetch('/Home/InitializeScanner', {
                method: 'POST'
            });

            const result = await response.json();

            if (result.success) {
                deviceInitialized = true;
                fingerprintScanner.classList.remove('scanning');
                fingerprintScanner.classList.add('ready');
                showMessage('Scanner ready. Place finger to scan.', 'success');
            } else {
                deviceInitialized = false;
                fingerprintScanner.classList.remove('scanning');
                fingerprintScanner.classList.add('error');
                showMessage('Scanner not available. Please check device connection.', 'error');
            }
        } catch (error) {
            console.error('Error initializing scanner:', error);
            deviceInitialized = false;
            fingerprintScanner.classList.remove('scanning');
            fingerprintScanner.classList.add('error');
            showMessage('Failed to initialize scanner', 'error');
        }
    }

    /**
     * Main fingerprint scan function with standardized workflow
     */
    async function scanFingerprint() {
        if (isScanning || isCooldown) return;

        if (!deviceInitialized) {
            showMessage('Scanner not ready. Please wait...', 'error');
            await initializeScanner();
            return;
        }

        try {
            isScanning = true;

            // STEP 1: Show scanning state
            fingerprintScanner.classList.remove('ready', 'success', 'error');
            fingerprintScanner.classList.add('scanning');
            showMessage('Scanning fingerprint...', 'scanning');

            // STEP 2: Set timeout for scan (10 seconds)
            scanTimeout = setTimeout(() => {
                if (isScanning) {
                    handleScanTimeout();
                }
            }, 10000);

            // STEP 3: Call backend endpoint
            const response = await fetch('/Home/ScanFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            clearTimeout(scanTimeout);

            // STEP 4: Add verification delay (2 seconds) for better UX
            fingerprintScanner.classList.remove('scanning');
            fingerprintScanner.classList.add('validating');
            showMessage('Verifying fingerprint...', 'validating');
            
            await new Promise(resolve => setTimeout(resolve, 2000));

            // STEP 5: Process result
            const result = await response.json();

            if (result.success) {
                handleSuccess(result);
            } else {
                handleError(result);
            }

        } catch (error) {
            clearTimeout(scanTimeout);
            console.error('Scan error:', error);
            handleError({ message: 'Connection error. Please try again.' });
        } finally {
            isScanning = false;
            // Set cooldown to prevent rapid re-scanning
            isCooldown = true;
            setTimeout(() => {
                isCooldown = false;
            }, 3000);
        }
    }

    /**
     * Handle successful scan
     */
    function handleSuccess(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'error');
        fingerprintScanner.classList.add('success');

        const student = result.student || {};
        const action = result.action || 'recorded';
        const timestamp = result.timestamp || new Date().toLocaleTimeString();

        let message = '';
        if (action === 'Time In') {
            message = `✅ Time In Recorded<br>
                      Student: ${student.fullName || 'Student'}<br>
                      Time: ${timestamp}`;
        } else if (action === 'Time Out') {
            message = `✅ Time Out Recorded<br>
                      Student: ${student.fullName || 'Student'}<br>
                      Time: ${timestamp}`;
        } else {
            message = `✅ ${result.message || 'Attendance recorded successfully'}`;
        }

        // Show incomplete records warning if present
        if (result.incompleteRecordsWarning) {
            message += `<br><br>⚠️ ${result.incompleteRecordsWarning}`;
        }

        showMessage(message, 'success');
        updateStats();

        // Reset to ready state after 5 seconds
        setTimeout(() => {
            resetScanner();
        }, 5000);
    }

    /**
     * Handle scan error (including schedule validation failures)
     */
    function handleError(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'success');
        fingerprintScanner.classList.add('error');

        const message = result.message || 'Scan failed. Please try again.';
        showMessage(`❌ ${message}`, 'error');

        // Reset to ready state after 4 seconds
        setTimeout(() => {
            resetScanner();
        }, 4000);
    }

    /**
     * Handle scan timeout
     */
    function handleScanTimeout() {
        isScanning = false;
        fingerprintScanner.classList.remove('scanning', 'validating');
        fingerprintScanner.classList.add('error');
        showMessage('Scan timeout. Please try again.', 'error');

        setTimeout(() => {
            resetScanner();
        }, 3000);
    }

    /**
     * Reset scanner to ready state
     */
    function resetScanner() {
        fingerprintScanner.classList.remove('scanning', 'validating', 'success', 'error');
        fingerprintScanner.classList.add('ready');
        showMessage('Scanner ready. Place finger to scan.', 'info');
    }

    /**
     * Show status message
     */
    function showMessage(message, type = 'info') {
        if (!scanStatusMessage) return;

        scanStatusMessage.innerHTML = message;
        scanStatusMessage.className = 'scan-status-message show';

        if (type === 'success') {
            scanStatusMessage.classList.add('success');
        } else if (type === 'error') {
            scanStatusMessage.classList.add('error');
        } else if (type === 'scanning') {
            scanStatusMessage.classList.add('scanning');
        } else if (type === 'validating') {
            scanStatusMessage.classList.add('validating');
        } else {
            scanStatusMessage.classList.add('info');
        }

        // Smooth fade-in
        scanStatusMessage.style.opacity = '0';
        scanStatusMessage.style.transform = 'translateY(-10px)';
        requestAnimationFrame(() => {
            scanStatusMessage.style.opacity = '1';
            scanStatusMessage.style.transform = 'translateY(0)';
        });
    }

    /**
     * Update statistics display
     */
    async function updateStats() {
        try {
            const response = await fetch('/Home/GetStatistics');
            const result = await response.json();

            if (result.success) {
                if (successRateElement) {
                    successRateElement.textContent = result.successRate + '%';
                }
                if (todayScansElement) {
                    todayScansElement.textContent = result.todayScans;
                }
            }
        } catch (error) {
            console.error('Error updating stats:', error);
        }
    }

    /**
     * Start periodic stats updates
     */
    function startStatsUpdates() {
        updateStats();
        statsUpdateInterval = setInterval(updateStats, 30000); // Update every 30 seconds
    }

    /**
     * Cleanup on page unload
     */
    window.addEventListener('beforeunload', function () {
        if (scanTimeout) {
            clearTimeout(scanTimeout);
        }
        if (statsUpdateInterval) {
            clearInterval(statsUpdateInterval);
        }
    });
});
