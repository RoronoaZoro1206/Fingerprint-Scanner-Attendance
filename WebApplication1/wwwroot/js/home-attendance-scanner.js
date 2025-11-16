/// <reference path="global.d.ts" />

/**
 * Home Page Attendance Scanner with AUTOMATIC Fingerprint Detection
 * ✅ ENHANCED: Proper timing for message display and scanner reset
 * ✅ NEW: Success messages stay visible for 7 seconds
 * ✅ NEW: Error messages stay visible for 5 seconds
 * ✅ NEW: Smooth state transitions with adequate reading time
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
    let autoScanInterval = null;

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
     * ✅ Initialize scanner device and start auto-detection
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
                
                fingerprintScanner.style.animation = 'pulse 2s infinite';
                
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
     * ✅ Automatic fingerprint detection (500ms polling)
     */
    function startAutoDetection() {
        console.log('✅ Starting automatic fingerprint detection...');
        
        autoScanInterval = setInterval(async () => {
            if (!isScanning && !isCooldown && deviceInitialized) {
                await tryAutoScan();
            }
        }, 500);
    }

    /**
     * ✅ Try to scan automatically when finger detected
     */
    async function tryAutoScan() {
        if (isScanning || isCooldown) return;
        
        await scanFingerprint(true);
    }

    /**
     * ✅ ENHANCED: Main fingerprint scan function with proper timing
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
            
            if (!isAutoScan) {
                showMessage('Scanning fingerprint...', 'scanning');
            }

            // STEP 2: Set timeout for scan (10 seconds)
            scanTimeout = setTimeout(() => {
                if (isScanning) {
                    handleScanTimeout();
                }
            }, 10000);

            // STEP 3: Call scan endpoint
            const response = await fetch('/Home/ScanFingerprint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            clearTimeout(scanTimeout);

            // STEP 4: ✅ ENHANCED: Add proper verification delay (2 seconds)
            if (!isAutoScan) {
                fingerprintScanner.classList.remove('scanning');
                fingerprintScanner.classList.add('validating');
                showMessage('Verifying fingerprint...', 'validating');
                
                await new Promise(resolve => setTimeout(resolve, 2000)); // 2 seconds for verification
            }

            // STEP 5: Process result
            const result = await response.json();

            if (result.success) {
                if (autoScanInterval) {
                    clearInterval(autoScanInterval);
                    autoScanInterval = null;
                }
                
                handleSuccess(result);
            } else {
                // Handle "no finger detected" silently for auto-scan
                if (isAutoScan && result.message && 
                    (result.message.includes('No fingerprint detected') || 
                     result.message.includes('No finger detected') ||
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
            
            // Set cooldown to prevent rapid re-scanning
            if (!isAutoScan) {
                isCooldown = true;
                setTimeout(() => {
                    isCooldown = false;
                }, 8000); // ✅ INCREASED: 8 second cooldown (matches message display time)
            }
        }
    }

    /**
     * ✅ ENHANCED: Handle successful scan with 7-second display time
     */
    function handleSuccess(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'error');
        fingerprintScanner.classList.add('success');

        const student = result.student || {};
        const action = result.action || 'recorded';
        const timestamp = result.timestamp ? new Date(result.timestamp).toLocaleTimeString() : new Date().toLocaleTimeString();

        // Build comprehensive student information display
        let studentInfo = `
            <strong>${student.fullName || 'Student'}</strong><br>
            <strong>ID:</strong> ${student.studNumber || 'N/A'}<br>
            <strong>Program:</strong> ${student.programName || 'N/A'}${student.sectionName ? ' - ' + student.sectionName : ''}
        `;

        // Build message based on action (Time In vs Time Out)
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

        // ✅ ENHANCED: Reset after 7 seconds (adequate time to read full details)
        setTimeout(() => {
            resetScanner();
            // Restart auto-detection after successful scan
            if (!autoScanInterval) {
                startAutoDetection();
            }
        }, 7000); // ✅ INCREASED: 7 seconds for success messages
    }

    /**
     * ✅ ENHANCED: Handle scan error with role-based messaging
     */
    function handleError(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'success');
        fingerprintScanner.classList.add('error');

        let displayTime = 4000; // Default 4 seconds
        let message = result.message || '❌ Scan failed. Please try again.';
        
        // ✅ ENHANCED: Handle different message types with appropriate display times
        if (result.messageType === 'role_restriction') {
            // Role restriction messages need more time to read
            displayTime = 7000; // 7 seconds for role restriction messages
            
            const roleIcon = result.userRole === 'Admin' 
                ? '<i data-lucide="shield-alert" style="width:32px;height:32px;color:#ef4444;"></i>' 
                : '<i data-lucide="user-cog" style="width:32px;height:32px;color:#f59e0b;"></i>';
            
            message = `
                <div class="role-restriction-alert" style="text-align: left; padding: 10px;">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 12px;">
                        ${roleIcon}
                        <strong style="font-size: 18px; color: #1f2937;">
                            ${result.userRole} Fingerprint Detected
                        </strong>
                    </div>
                    
                    <div style="margin-left: 0; line-height: 1.8;">
                        <p style="margin: 8px 0; font-size: 15px;">
                            Hello, <strong style="color: #3b82f6;">${result.userName}</strong>
                        </p>
                        
                        <p style="margin: 8px 0;">
                            This is a <strong>${result.userRole}</strong> fingerprint.
                        </p>
                        
                        <p style="margin: 12px 0; padding: 8px; background: #fee2e2; border-left: 3px solid #ef4444; border-radius: 4px;">
                            <strong style="color: #991b1b;">⛔ Only students are allowed to time in and time out.</strong>
                        </p>
                        
                        <p style="margin: 8px 0; font-size: 14px; color: #6b7280;">
                            ${result.userRole === 'Admin' 
                                ? 'Please use the Admin Dashboard to manage the system.' 
                                : 'Please use the Faculty Dashboard to manage your classes.'}
                        </p>
                    </div>
                </div>
            `;
        } else if (result.messageType === 'unregistered') {
            // Unregistered fingerprint messages
            displayTime = 6000; // 6 seconds
            message = `
                <div style="text-align: center; padding: 10px;">
                    <i data-lucide="alert-circle" style="width:32px;height:32px;color:#ef4444;"></i>
                    <div style="margin-top: 12px; line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else if (result.messageType === 'no_schedule') {
            // No schedule messages
            displayTime = 6000; // 6 seconds
            message = `
                <div style="text-align: center; padding: 10px;">
                    <i data-lucide="calendar-x" style="width:32px;height:32px;color:#f59e0b;"></i>
                    <div style="margin-top: 12px; line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else if (result.messageType === 'capture_failed') {
            // Capture failure messages
            displayTime = 5000; // 5 seconds
            message = `
                <div style="text-align: center; padding: 10px;">
                    <i data-lucide="fingerprint" style="width:32px;height:32px;color:#ef4444;"></i>
                    <div style="margin-top: 12px; line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else {
            // Generic error messages
            displayTime = 5000; // 5 seconds
            const formattedMessage = `❌ ${message}`;
            message = `
                <div style="text-align: center; padding: 10px;">
                    ${formattedMessage.replace(/\n/g, '<br>')}
                </div>
            `;
        }

        showMessage(message, 'error');

        // ✅ Reset scanner after appropriate display time
        setTimeout(() => {
            resetScanner();
        }, displayTime);
    }

    /**
     * ✅ ENHANCED: Handle scan timeout with proper display time
     */
    function handleScanTimeout() {
        isScanning = false;
        fingerprintScanner.classList.remove('scanning', 'validating');
        fingerprintScanner.classList.add('error');
        showMessage('⏱️ Scan timeout. No finger detected. Please try again.', 'error');

        // ✅ ENHANCED: Give user time to read timeout message
        setTimeout(() => {
            resetScanner();
        }, 4000); // 4 seconds for timeout messages
    }

    /**
     * ✅ ENHANCED: Reset scanner to ready state with smooth transition
     */
    function resetScanner() {
        // Fade out current message first
        if (scanStatusMessage) {
            scanStatusMessage.style.opacity = '0';
            scanStatusMessage.style.transform = 'translateY(-10px)';
        }

        // Wait for fade animation, then reset
        setTimeout(() => {
            fingerprintScanner.classList.remove('scanning', 'validating', 'success', 'error');
            fingerprintScanner.classList.add('ready');
            fingerprintScanner.style.animation = 'pulse 2s infinite';
            showMessage('Scanner ready. Place finger to scan.', 'info');
        }, 500); // 500ms for smooth transition
    }

    /**
     * ✅ ENHANCED: Show status message with better visibility
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
        scanStatusMessage.style.display = 'block'; // Ensure visible
        
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
