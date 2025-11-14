/**
 * Public Homepage Fingerprint Attendance Scanner
 * Integrates with existing AttendanceController and Phase 6 API
 */

(function () {
    'use strict';

    // DOM Elements
    const fingerprintScanner = document.getElementById('fingerprintScanner');
    const scanStatusMessage = document.getElementById('scanStatusMessage');
    const scannerInstructionText = document.querySelector('.scanner-instruction-text');

    // State
    let isScanning = false;
    let scannerOnline = false;
    let autoScanInterval = null;

    // Initialize
    document.addEventListener('DOMContentLoaded', function () {
        console.log('? Public Attendance Scanner Initialized');

        // Initialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        // Update clock
        updateClock();
        setInterval(updateClock, 1000);

        // Load statistics
        loadStatistics();
        setInterval(loadStatistics, 5000);

        // Check scanner status
        checkScannerStatus();

        // Click to scan
        if (fingerprintScanner) {
            fingerprintScanner.addEventListener('click', handleScanClick);
        }

        // Auto-scan every 1 second
        startAutoScan();
    });

    /**
     * Check if scanner is available
     */
    async function checkScannerStatus() {
        try {
            const response = await fetch('/Home/CheckScannerStatus');
            const result = await response.json();

            if (result.success && result.isAvailable) {
                updateScannerState('ready');
                scannerOnline = true;
            } else {
                updateScannerState('offline');
                scannerOnline = false;
            }
        } catch (error) {
            console.error('Error checking scanner status:', error);
            updateScannerState('offline');
            scannerOnline = false;
        }
    }

    /**
     * Handle scan button click
     */
    async function handleScanClick() {
        if (isScanning || !scannerOnline) return;
        await performScan();
    }

    /**
     * Start automatic scanning (checks every second)
     */
    function startAutoScan() {
        autoScanInterval = setInterval(async () => {
            if (!isScanning && scannerOnline) {
                await performScan();
            }
        }, 1000);
    }

    /**
     * Perform fingerprint scan
     */
    async function performScan() {
        if (isScanning) return;

        try {
            isScanning = true;
            updateScannerState('scanning');

            const response = await fetch('/Home/ScanFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (result.success) {
                handleScanSuccess(result);
            } else {
                // Silent fail for "no finger detected"
                if (!result.message.includes('No fingerprint detected')) {
                    handleScanError(result.message);
                }
            }
        } catch (error) {
            // Silent fail for network errors (scanner waiting for finger)
            console.error('Scan error:', error);
        } finally {
            isScanning = false;
            setTimeout(() => {
                if (scannerOnline) {
                    updateScannerState('ready');
                }
            }, 2000);
        }
    }

    /**
     * Handle successful scan
     */
    function handleScanSuccess(data) {
        console.log('? Scan successful:', data);

        // Play success beep
        playBeep('success');

        // Update scanner state
        updateScannerState('success');

        // Show success message
        const actionText = data.action === 'time_in' ? 'TIME IN' : 'TIME OUT';
        const message = `? ${actionText} recorded for ${data.student.fullName}`;
        
        showStatusMessage(message, 'success');

        // Show incomplete warning if present
        if (data.incompleteRecordsWarning) {
            setTimeout(() => {
                showStatusMessage(`?? ${data.incompleteRecordsWarning}`, 'warning');
            }, 3000);
        }

        // Reload statistics
        loadStatistics();

        // Reset to ready state after 4 seconds
        setTimeout(() => {
            updateScannerState('ready');
        }, 4000);
    }

    /**
     * Handle scan error
     */
    function handleScanError(errorMessage) {
        console.error('? Scan error:', errorMessage);

        // Play error beep
        playBeep('error');

        // Update scanner state
        updateScannerState('error');

        // Show error message
        showStatusMessage(errorMessage, 'error');

        // Reset to ready state after 3 seconds
        setTimeout(() => {
            updateScannerState('ready');
        }, 3000);
    }

    /**
     * Update scanner visual state
     */
    function updateScannerState(state) {
        // Remove all state classes
        fingerprintScanner.classList.remove('scanning', 'success', 'error');

        // Update instruction text
        switch (state) {
            case 'ready':
                scannerInstructionText.textContent = 'Place finger here';
                break;
            case 'scanning':
                fingerprintScanner.classList.add('scanning');
                scannerInstructionText.textContent = 'Scanning...';
                break;
            case 'success':
                fingerprintScanner.classList.add('success');
                scannerInstructionText.textContent = 'Success!';
                break;
            case 'error':
                fingerprintScanner.classList.add('error');
                scannerInstructionText.textContent = 'Try again';
                break;
            case 'offline':
                scannerInstructionText.textContent = 'Scanner offline';
                break;
        }
    }

    /**
     * Show status message
     */
    function showStatusMessage(message, type = 'info') {
        if (!scanStatusMessage) return;

        scanStatusMessage.textContent = message;
        scanStatusMessage.className = 'scan-status-message ' + type;
        scanStatusMessage.style.display = 'block';

        // Auto-hide after 5 seconds
        setTimeout(() => {
            scanStatusMessage.style.display = 'none';
        }, 5000);
    }

    /**
     * Load statistics from server
     */
    async function loadStatistics() {
        try {
            const response = await fetch('/Home/GetStatistics');
            const result = await response.json();

            if (result.success) {
                updateStatistics(result.successRate, result.todayScans);
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    /**
     * Update statistics display
     */
    function updateStatistics(successRate, todayScans) {
        const successRateElement = document.querySelector('.scan-stat-card.success h3');
        const todayScansElement = document.querySelector('.scan-stat-card.total h3');

        if (successRateElement) {
            successRateElement.textContent = successRate.toFixed(1) + '%';
        }

        if (todayScansElement) {
            todayScansElement.textContent = todayScans.toLocaleString();
        }
    }

    /**
     * Update real-time clock
     */
    function updateClock() {
        const now = new Date();
        const timeElement = document.getElementById('current-time');
        const dateElement = document.getElementById('current-date');

        if (timeElement) {
            timeElement.textContent = now.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        }

        if (dateElement) {
            dateElement.textContent = now.toLocaleDateString('en-US', {
                month: '2-digit',
                day: '2-digit',
                year: 'numeric'
            });
        }
    }

    /**
     * Play beep sound
     */
    function playBeep(type) {
        const audio = document.getElementById(type === 'success' ? 'successBeep' : 'errorBeep');
        if (audio) {
            audio.currentTime = 0;
            audio.play();
        }
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (autoScanInterval) {
            clearInterval(autoScanInterval);
        }
    });
})();
