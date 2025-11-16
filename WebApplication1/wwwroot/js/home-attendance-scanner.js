/// <reference path="global.d.ts" />

/**
 * Home Page Attendance Scanner with AUTOMATIC Fingerprint Detection
 * ✅ ENHANCED: Now automatically detects fingerprints like the login page
 * ✅ NEW: Continuous polling for finger presence - NO CLICKING REQUIRED
 * ✅ NEW: Displays comprehensive student info and class details after scan
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
    let autoScanInterval = null; // ✅ NEW: Auto-detection interval

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Initialize scanner and start polling
    initializeScanner();
    startStatsUpdates();

    // Manual scan trigger (click on circle) - KEPT AS BACKUP
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
     * ✅ NEW: Initialize scanner device and start auto-detection
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
                
                // ✅ NEW: Add pulse animation to indicate readiness
                fingerprintScanner.style.animation = 'pulse 2s infinite';
                
                // ✅ NEW: Start automatic fingerprint detection
                startAutoDetection();
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
     * ✅ NEW: Automatic fingerprint detection (500ms polling)
     * Just like the FingerprintLogin page!
     */
    function startAutoDetection() {
        console.log('✅ Starting automatic fingerprint detection...');
        
        // Poll every 500ms to check if finger is on scanner
        autoScanInterval = setInterval(async () => {
            if (!isScanning && !isCooldown && deviceInitialized) {
                // Check if finger is present and auto-scan
                await tryAutoScan();
            }
        }, 500);
    }

    /**
     * ✅ NEW: Try to scan automatically when finger detected
     */
    async function tryAutoScan() {
        if (isScanning || isCooldown) return;
        
        // Attempt scan (will succeed if finger is on scanner)
        await scanFingerprint(true); // Pass true for auto-scan
    }

    /**
     * Main fingerprint scan function with auto-detection support
     * ✅ ENHANCED: Now supports automatic scanning
     */
    async function scanFingerprint(isAutoScan = false) {
        if (isScanning || isCooldown) return;

        if (!deviceInitialized) {
            if (!isAutoScan) {
                showMessage('Scanner not ready. Please wait...', 'error');
                await initializeScanner();
            }
            return;
        }

        try {
            isScanning = true;

            // STEP 1: Show scanning state
            fingerprintScanner.classList.remove('ready', 'success', 'error');
            fingerprintScanner.classList.add('scanning');
            fingerprintScanner.style.animation = 'none';
            
            // Show scanning message (only if not auto-scan)
            if (!isAutoScan) {
                showMessage('Scanning fingerprint...', 'scanning');
            }

            // STEP 2: Set timeout for scan (10 seconds)
            scanTimeout = setTimeout(() => {
                if (isScanning) {
                    handleScanTimeout();
                }
            }, 10000);

            // ✅ CHANGED: Call /Home/ScanFingerprint instead of /Attendance/RecordAttendance
            const response = await fetch('/Home/ScanFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            clearTimeout(scanTimeout);

            // STEP 4: Add verification delay (1 second) for better UX
            if (!isAutoScan) {
                fingerprintScanner.classList.remove('scanning');
                fingerprintScanner.classList.add('validating');
                showMessage('Verifying fingerprint...', 'validating');
                
                await new Promise(resolve => setTimeout(resolve, 1000));
            }

            // STEP 5: Process result
            const result = await response.json();

            if (result.success) {
                // ✅ NEW: Stop auto-detection on success
                if (autoScanInterval) {
                    clearInterval(autoScanInterval);
                    autoScanInterval = null;
                }
                
                handleSuccess(result);
            } else {
                // ✅ NEW: Handle "no finger detected" silently for auto-scan
                if (isAutoScan && result.message && 
                    (result.message.includes('No fingerprint detected') || 
                     result.message.includes('Failed to capture'))) {
                    // Silent fail - finger not on scanner yet
                    fingerprintScanner.classList.remove('scanning', 'validating');
                    fingerprintScanner.classList.add('ready');
                    fingerprintScanner.style.animation = 'pulse 2s infinite';
                } else {
                    handleError(result);
                }
            }

        } catch (error) {
            clearTimeout(scanTimeout);
            
            // ✅ NEW: Silent fail for auto-scan connection errors
            if (isAutoScan) {
                fingerprintScanner.classList.remove('scanning', 'validating');
                fingerprintScanner.classList.add('ready');
                fingerprintScanner.style.animation = 'pulse 2s infinite';
            } else {
                console.error('Scan error:', error);
                handleError({ message: 'Connection error. Please try again.' });
            }
        } finally {
            isScanning = false;
            
            // Set cooldown to prevent rapid re-scanning (only on actual scan attempts)
            if (!isAutoScan) {
                isCooldown = true;
                setTimeout(() => {
                    isCooldown = false;
                }, 3000);
            }
        }
    }

    /**
     * ✅ ENHANCED: Handle successful scan with comprehensive student information
     */
    function handleSuccess(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'error');
        fingerprintScanner.classList.add('success');

        const student = result.student || {};
        const action = result.action || 'recorded';
        const timestamp = result.timestamp ? new Date(result.timestamp).toLocaleTimeString() : new Date().toLocaleTimeString();

        // ✅ Build comprehensive student information display
        let studentInfo = `
            <strong>${student.fullName || 'Student'}</strong><br>
            <strong>ID:</strong> ${student.studNumber || 'N/A'}<br>
            <strong>Program:</strong> ${student.programName || 'N/A'}${student.sectionName ? ' - ' + student.sectionName : ''}
        `;

        // ✅ Build message based on action (Time In vs Time Out)
        let message = '';
        let emoji = '';
        
        if (action === 'time_in') {
            emoji = '✅';
            message = `
                ${emoji} <strong>Good morning! Time In Recorded</strong><br><br>
                ${studentInfo}<br>
                <strong>Time In:</strong> ${timestamp}
            `;
        } else if (action === 'time_out') {
            emoji = '👋';
            message = `
                ${emoji} <strong>Goodbye! Time Out Recorded</strong><br><br>
                ${studentInfo}<br>
                <strong>Time Out:</strong> ${timestamp}
            `;
        } else {
            message = `✅ ${result.message || 'Attendance recorded successfully'}`;
        }

        // Show incomplete records warning if present
        if (result.incompleteRecordsWarning) {
            message += `<br><br>⚠️ ${result.incompleteRecordsWarning}`;
        }

        showMessage(message, 'success');
        updateStats();

        // ✅ Reset to ready state after 8 seconds (longer to allow reading)
        setTimeout(() => {
            resetScanner();
            // ✅ Restart auto-detection after successful scan
            if (!autoScanInterval) {
                startAutoDetection();
            }
        }, 8000);
    }

    /**
     * ✅ ENHANCED: Handle scan error with better messages
     */
    function handleError(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'success');
        fingerprintScanner.classList.add('error');

        const message = result.message || 'Scan failed. Please try again.';
        
        // Format the error message
        const formattedMessage = `❌ ${message}`;

        showMessage(formattedMessage, 'error');

        // Reset to ready state after 5 seconds
        setTimeout(() => {
            resetScanner();
        }, 5000);
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
     * ✅ ENHANCED: Reset scanner to ready state
     */
    function resetScanner() {
        fingerprintScanner.classList.remove('scanning', 'validating', 'success', 'error');
        fingerprintScanner.classList.add('ready');
        fingerprintScanner.style.animation = 'pulse 2s infinite';
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

            if (successRateElement) {
                successRateElement.textContent = result.successRate || '0%';
            }
            if (todayScansElement) {
                todayScansElement.textContent = result.todayScans || 0;
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
        if (autoScanInterval) {
            clearInterval(autoScanInterval);
        }
    });
});
