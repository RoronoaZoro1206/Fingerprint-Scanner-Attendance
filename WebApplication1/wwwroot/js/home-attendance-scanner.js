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
    let lastScanAttempt = 0;
    const SCAN_COOLDOWN_MS = 1500; // Minimum 1.5 seconds between scan attempts

    // Initialize
    document.addEventListener('DOMContentLoaded', function () {
        console.log('✅ Public Attendance Scanner Initialized');

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

        // Auto-scan every 2 seconds (reduced frequency to prevent excessive polling)
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
                console.log('✅ Scanner is online and ready');
            } else {
                updateScannerState('offline');
                scannerOnline = false;
                console.warn('⚠️ Scanner is offline');
                showStatusMessage('Scanner not available. Please connect the fingerprint device.', 'error');
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
     * Start automatic scanning (checks every 2 seconds)
     */
    function startAutoScan() {
        autoScanInterval = setInterval(async () => {
            // Only attempt scan if:
            // 1. Not currently scanning
            // 2. Scanner is online
            // 3. Enough time has passed since last attempt (cooldown)
            const now = Date.now();
            if (!isScanning && scannerOnline && (now - lastScanAttempt) >= SCAN_COOLDOWN_MS) {
                await performScan();
            }
        }, 2000); // Reduced from 1000ms to 2000ms
    }

    /**
     * Perform fingerprint scan
     */
    async function performScan() {
        // Prevent concurrent scans
        if (isScanning) {
            console.log('⏸️ Scan already in progress, skipping...');
            return;
        }

        // Enforce cooldown period
        const now = Date.now();
        if ((now - lastScanAttempt) < SCAN_COOLDOWN_MS) {
            console.log('⏸️ Cooldown period active, skipping scan...');
            return;
        }

        try {
            isScanning = true;
            lastScanAttempt = now;
            updateScannerState('scanning');
            console.log('🔍 Starting fingerprint scan...');

            const response = await fetch('/Home/ScanFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📥 Scan result:', result);

            if (result.success) {
                handleScanSuccess(result);
            } else {
                // Check if it's a "no finger detected" error (should not show error state)
                const message = result.message || '';
                
                if (message.includes('No fingerprint detected') || 
                    message.includes('no finger detected') ||
                    message.includes('Place your finger')) {
                    // Silent fail - just reset to ready state without showing error
                    console.log('ℹ️ No finger detected - waiting for finger placement');
                    updateScannerState('ready');
                } else {
                    // Actual error - show error state
                    handleScanError(message);
                }
            }
        } catch (error) {
            // Network or unexpected error
            console.error('❌ Scan error:', error);
            handleScanError('Connection error. Please try again.');
        } finally {
            // Always reset scanning state after a delay
            setTimeout(() => {
                isScanning = false;
                if (scannerOnline && fingerprintScanner && !fingerprintScanner.classList.contains('success')) {
                    updateScannerState('ready');
                }
            }, 800);
        }
    }

    /**
     * Handle successful scan
     */
    function handleScanSuccess(data) {
        console.log('✅ Scan successful:', data);

        // Stop auto-scanning temporarily
        if (autoScanInterval) {
            clearInterval(autoScanInterval);
            autoScanInterval = null;
        }

        // Play success beep
        playBeep('success');

        // Update scanner state
        updateScannerState('success');

        // Show success message
        const actionText = data.action === 'time_in' ? 'TIME IN' : 'TIME OUT';
        const studentName = data.student?.fullName || 'Student';
        const message = `✅ ${actionText} recorded for ${studentName}`;
        
        showStatusMessage(message, 'success');

        // Show incomplete warning if present
        if (data.incompleteRecordsWarning) {
            setTimeout(() => {
                showStatusMessage(`⚠️ ${data.incompleteRecordsWarning}`, 'warning');
            }, 3000);
        }

        // Reload statistics
        loadStatistics();

        // Reset to ready state and restart auto-scan after 5 seconds
        setTimeout(() => {
            updateScannerState('ready');
            // Restart auto-scanning
            startAutoScan();
        }, 5000);
    }

    /**
     * Handle scan error
     */
    function handleScanError(errorMessage) {
        console.error('❌ Scan error:', errorMessage);

        // Play error beep
        playBeep('error');

        // Update scanner state
        updateScannerState('error');

        // Show error message
        showStatusMessage(errorMessage, 'error');

        // Reset to ready state after 3 seconds
        setTimeout(() => {
            if (scannerOnline) {
                updateScannerState('ready');
            }
        }, 3000);
    }

    /**
     * Update scanner visual state
     */
    function updateScannerState(state) {
        if (!fingerprintScanner) return;

        // Remove all state classes
        fingerprintScanner.classList.remove('scanning', 'success', 'error');

        // Update instruction text
        if (scannerInstructionText) {
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
            audio.play().catch(err => console.warn('Audio play failed:', err));
        }
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (autoScanInterval) {
            clearInterval(autoScanInterval);
        }
    });
})();
