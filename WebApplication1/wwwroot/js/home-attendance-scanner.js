/// <reference path="global.d.ts" />

/**
 * Home Page Attendance Scanner with AUTOMATIC Fingerprint Detection
 * ✅ ENHANCED: Proper timing for message display and scanner reset
 * ✅ NEW: Success messages stay visible for 7 seconds
 * ✅ NEW: Error messages stay visible for 5 seconds
 * ✅ NEW: Smooth state transitions with adequate reading time
 * ✅ ROBUST: Counter-based alert state management for rapid sequential alerts
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

    // ✅ NEW: Global state for managing multiple overlapping alerts
    let activeAlertCount = 0;
    let backgroundRestoreTimeout = null;

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
     * ✅ NEW: Centralized status background update function
     * Handles background color based on active alert count
     */
    function updateStatusBackground() {
        if (!scanStatusMessage || !scanStatusMessage.classList.contains('show')) {
            return;
        }

        if (activeAlertCount > 0) {
            // At least one alert is active - force white background
            if (!scanStatusMessage.classList.contains('swal-active')) {
                console.log(`✅ Activating white background (Active alerts: ${activeAlertCount})`);
                scanStatusMessage.classList.add('swal-active');
            }
        } else {
            // No alerts active - restore green background
            if (scanStatusMessage.classList.contains('swal-active')) {
                console.log('✅ Restoring green background (No active alerts)');
                scanStatusMessage.classList.remove('swal-active');

                // Force restore "ready" state
                setTimeout(() => {
                    if (scanStatusMessage.classList.contains('show')) {
                        scanStatusMessage.classList.remove('success', 'error', 'warning', 'scanning', 'validating');
                        scanStatusMessage.classList.add('info');
                        scanStatusMessage.innerHTML = 'Scanner ready. Place finger to scan.';
                    }
                }, 50);
            }
        }
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
     * ✅ Automatic fingerprint detection (1000ms polling - increased from 500ms)
     */
    function startAutoDetection() {
        console.log('✅ Starting automatic fingerprint detection...');

        // ✅ FIX: Increased polling interval to 1000ms (1 second) to reduce rapid retry errors
        autoScanInterval = setInterval(async () => {
            if (!isScanning && !isCooldown && deviceInitialized) {
                await tryAutoScan();
            }
        }, 1000); // Changed from 500ms to 1000ms
    }

    /**
     * ✅ Try to scan automatically when finger detected
     */
    async function tryAutoScan() {
        if (isScanning || isCooldown) return;

        await scanFingerprint(true);
    }

    /**
     * ✅ ENHANCED: Main fingerprint scan function with proper timing and better error handling
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
                showMessage('Place your finger firmly on the scanner...', 'scanning');
            }

            // STEP 2: Set timeout for scan (15 seconds - increased for better reliability)
            scanTimeout = setTimeout(() => {
                if (isScanning) {
                    handleScanTimeout();
                }
            }, 15000); // Increased from 10 to 15 seconds

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
                // ✅ ENHANCED: Better handling for "no finger detected" errors
                if (isAutoScan && result.message &&
                    (result.message.includes('No fingerprint detected') ||
                        result.message.includes('No finger detected') ||
                        result.message.includes('Failed to capture') ||
                        result.message.includes('Place your finger'))) {
                    // Silent fail - finger not on scanner yet, just continue polling
                    fingerprintScanner.classList.remove('scanning', 'validating');
                    fingerprintScanner.classList.add('ready');
                    fingerprintScanner.style.animation = 'pulse 2s infinite';

                    // Show a subtle hint for manual scans only
                    if (!isAutoScan) {
                        showMessage('Waiting for finger... Place your finger firmly on the scanner.', 'info');
                    }
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

            // ✅ ENHANCED: Minimal cooldown for successful scans, longer for manual failures
            if (!isAutoScan && !result?.success) {
                isCooldown = true;
                setTimeout(() => {
                    isCooldown = false;
                }, 2000); // Reduced to 2 seconds for better UX
            } else {
                // ✅ No cooldown for successful scans - allow immediate time-out
                isCooldown = false;
            }
        }
    }

    /**
     * ✅ NEW: Update live scan info cards
     */
    function updateLiveScanInfo(result) {
        const container = document.getElementById('liveScanInfoContainer');
        const badge = document.getElementById('scanActionBadge');
        const actionText = document.getElementById('scanActionText');
        const actionTime = document.getElementById('scanActionTime');
        const studentName = document.getElementById('liveStudentName');
        const studentNumber = document.getElementById('liveStudentNumber');
        const studentId = document.getElementById('liveStudentId');
        const programYear = document.getElementById('liveProgramYear');
        const courseName = document.getElementById('liveCourseName');
        const facultyName = document.getElementById('liveFacultyName');

        if (!container) return;

        // Show container
        container.style.display = 'block';

        // Update badge - Green for TIME IN, Yellow for TIME OUT
        if (result.action === 'time_in') {
            badge.className = 'scan-action-badge time-in';
            actionText.textContent = 'TIME IN';
        } else if (result.action === 'time_out') {
            badge.className = 'scan-action-badge time-out';
            actionText.textContent = 'TIME OUT';
        }

        // Format timestamp
        const timestamp = result.timestamp ? new Date(result.timestamp) : new Date();
        actionTime.textContent = timestamp.toLocaleTimeString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
        });

        // Update student info
        const student = result.student || {};
        const currentClass = result.currentClass || {};

        // Student Name and Number
        studentName.textContent = student.fullName || '-';
        studentNumber.textContent = student.studNumber || '-';

        // ✅ NEW: Student ID (same as studNumber)
        studentId.textContent = student.studNumber || '-';

        // ✅ NEW: Program / Year
        let programYearText = '-';
        if (student.programName && student.yearLevel) {
            programYearText = `${student.programName} - ${student.yearLevel}${getOrdinalSuffix(student.yearLevel)} Year`;
        } else if (student.programName) {
            programYearText = student.programName;
        }
        programYear.textContent = programYearText;

        // Format course name
        if (currentClass.courseCode && currentClass.courseName) {
            courseName.textContent = `${currentClass.courseCode} - ${currentClass.courseName}`;
        } else {
            courseName.textContent = '-';
        }

        // Format faculty name
        if (currentClass.facultyName) {
            facultyName.textContent = currentClass.facultyName;
        } else {
            facultyName.textContent = '-';
        }

        // Reinitialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    /**
     * ✅ ENHANCED: Show SweetAlert2 notification with ROBUST counter-based background control
     * Handles rapid sequential alerts without race conditions
     */
    function showSweetAlertNotification(result) {
        if (!window.Swal) {
            console.warn('SweetAlert2 not loaded');
            return;
        }

        const statusMessageElement = document.getElementById('scanStatusMessage');

        const swalOptions = {
            willOpen: () => {
                // ✅ CRITICAL: Increment counter when alert opens
                activeAlertCount++;
                console.log(`[${new Date().toLocaleTimeString()}] Alert opened. Counter: ${activeAlertCount}`);

                // ✅ Cancel any pending green restore
                if (backgroundRestoreTimeout) {
                    clearTimeout(backgroundRestoreTimeout);
                    backgroundRestoreTimeout = null;
                    console.log('Cancelled pending green restore');
                }

                // ✅ Immediately update to white
                updateStatusBackground();
            },
            didClose: () => {
                // ✅ CRITICAL: Decrement counter when alert closes
                activeAlertCount--;
                console.log(`[${new Date().toLocaleTimeString()}] Alert closed. Counter: ${activeAlertCount}`);

                // ✅ Cancel any existing timer
                if (backgroundRestoreTimeout) {
                    clearTimeout(backgroundRestoreTimeout);
                }

                // ✅ Set new timer to restore green with multi-layer validation
                backgroundRestoreTimeout = setTimeout(() => {
                    // Layer 1: Check counter
                    if (activeAlertCount !== 0) {
                        console.warn(`⚠️ Green restore blocked: Counter still at ${activeAlertCount}`);
                        return;
                    }

                    // Layer 2: Check DOM for any visible SweetAlert2 modals
                    const visibleSwalPopup = document.querySelector('.swal2-popup:not([style*="display: none"])');
                    const visibleSwalContainer = document.querySelector('.swal2-container:not(.swal2-container-hidden)');
                    const swalShown = document.querySelector('.swal2-shown');

                    // Layer 3: Check if SweetAlert2 is animating
                    const isAnimating = document.querySelector('.swal2-animate-show-class') ||
                        document.querySelector('.swal2-animate-hide-class') ||
                        document.querySelector('.swal2-show') ||
                        document.querySelector('.swal2-hide');

                    // Layer 4: Check SweetAlert2 internal state
                    const swalIsOpen = window.Swal && typeof window.Swal.isVisible === 'function' && window.Swal.isVisible();

                    // Log validation results for debugging
                    console.log(`[${new Date().toLocaleTimeString()}] Restore check:`, {
                        counter: activeAlertCount,
                        visiblePopup: !!visibleSwalPopup,
                        visibleContainer: !!visibleSwalContainer,
                        swalShown: !!swalShown,
                        isAnimating: !!isAnimating,
                        swalIsOpen: !!swalIsOpen
                    });

                    // ✅ CRITICAL: Only restore green if ALL checks confirm no alert is visible
                    const isAnySwalVisible = visibleSwalPopup || visibleSwalContainer || swalShown || isAnimating || swalIsOpen;

                    if (!isAnySwalVisible) {
                        console.log('✅ All checks passed - restoring green background');
                        updateStatusBackground();
                    } else {
                        console.warn('⚠️ SweetAlert2 still visible - keeping white background');
                    }
                }, 300); // Extended delay for SweetAlert2 cleanup
            }
        };

        if (!result.success) {
            // Error notifications
            if (result.messageType === 'already_completed') {
                Swal.fire({
                    ...swalOptions,
                    icon: 'warning',
                    title: '⚠️ Attendance Already Completed',
                    html: `
                        <div style="text-align: left; line-height: 1.8;">
                            <p>Hello, <strong>${result.studentName}</strong></p>
                            
                            <p style="margin-top: 15px;">You have already completed your attendance for:</p>
                            <div style="background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 10px 0;">
                                <strong>📚 ${result.courseName}</strong><br>
                                👨‍🏫 Prof. ${result.facultyName}
                            </div>
                            
                            <div style="margin-top: 15px;">
                                <strong>✅ Time In:</strong> ${result.timeIn}<br>
                                <strong>✅ Time Out:</strong> ${result.timeOut}
                            </div>
                            
                            <p style="margin-top: 15px; color: #dc2626;">
                                <strong>You cannot time in again for this class today.</strong>
                            </p>
                        </div>
                    `,
                    confirmButtonText: 'Understood',
                    confirmButtonColor: '#3b82f6',
                    timer: 10000,
                    timerProgressBar: true,
                    allowOutsideClick: false
                });
            } else if (result.messageType === 'no_schedule') {
                // ✅ FIX ISSUE #1 & #2: Display complete student name, day, and time
                const studentGreeting = result.studentName ? `Hello, ${result.studentName}` : 'Hello';

                // ✅ CRITICAL FIX: Parse the message to extract day and time
                // Message format: "📅 No class scheduled at this time.\n\nYou have no subject/course assigned for Monday at 9:27 am."
                const fullMessage = result.message || '';

                // Extract the complete sentence about day and time
                // Look for pattern: "You have no subject\/course assigned for [Day] at [Time]."
                const dayTimeMatch = fullMessage.match(/You have no subject\/course assigned for (.+?)\./i);
                const dayTimeText = dayTimeMatch ? dayTimeMatch[1] : '';

                Swal.fire({
                    ...swalOptions,
                    icon: 'error',
                    title: '❌ Cannot Record Attendance',
                    html: `
                        <div style="text-align: left; line-height: 1.8;">
                            <p style="font-size: 16px; color: #6b7280; margin-bottom: 10px;">📅 No class scheduled at this time.</p>
                            <p style="font-weight: 600; font-size: 18px; margin-bottom: 15px;">${studentGreeting}</p>
                            ${dayTimeText ? `<p style="color: #374151;">You have no subject/course assigned for <strong>${dayTimeText}</strong>.</p>` : ''}

                            <p style="margin-top: 15px; color: #dc2626;">
                                <strong>Please check your class schedule or contact the administration.</strong>
                            </p>
                        </div>
                    `,
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#ef4444',
                    timer: 6000,
                    timerProgressBar: true,
                    allowOutsideClick: true,
                    customClass: {
                        popup: 'student-error-subtle'  // ✅ SUBTLE ENHANCEMENT: Blue top border for student error
                    }
                });
            } else if (result.messageType === 'class_ended') {
                // ✅ NEW: Handle "class has ended, cannot time out" error
                Swal.fire({
                    ...swalOptions,
                    icon: 'warning',
                    title: '⚠️ Class Has Ended',
                    html: `
                        <div style="text-align: left; line-height: 1.8;">
                            <p>Hello, <strong>${result.studentName}</strong></p>
                            
                            <p style="margin-top: 15px;">The class has already ended:</p>
                            <div style="background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 10px 0;">
                                <strong>📚 ${result.courseName}</strong><br>
                                👨‍🏫 Prof. ${result.facultyName}
                            </div>
                            
                            <div style="margin-top: 15px;">
                                <strong>🕒 Class ended at:</strong> ${result.classEndTime}<br>
                                <strong>✅ Your Time In:</strong> ${result.timeIn}
                            </div>
                            
                            <p style="margin-top: 15px; padding: 12px; background: #fef3c7; border-radius: 8px; color: #92400e;">
                                <strong>⚠️ You forgot to time out before the class ended.</strong><br>
                                Your attendance has been automatically marked as <strong>ABSENT</strong>.
                            </p>
                            
                            <p style="margin-top: 15px; color: #6b7280; font-size: 14px;">
                                Please contact your faculty if you need to appeal this status.
                            </p>
                        </div>
                    `,
                    confirmButtonText: 'Understood',
                    confirmButtonColor: '#f59e0b',
                    timer: 12000,
                    timerProgressBar: true,
                    allowOutsideClick: false,
                    customClass: {
                        popup: 'student-error-subtle'
                    }
                });
            } else if (result.messageType === 'role_restriction') {
                // ✅ SUBTLE ENHANCEMENT: Handle role restriction errors with colored borders
                const userRole = result.userRole || 'Unknown';
                const userName = result.userName || 'User';

                // Determine role emoji and custom class
                let roleEmoji = '🛡️';
                let customClass = 'admin-error-subtle'; // Default

                if (userRole === 'Faculty') {
                    roleEmoji = '👨‍🏫';
                    customClass = 'faculty-error-subtle'; // ✅ Orange border
                } else if (userRole === 'Admin') {
                    roleEmoji = '🛡️';
                    customClass = 'admin-error-subtle'; // ✅ Red border
                }

                // Parse the message to extract details
                const fullMessage = result.message || '';

                Swal.fire({
                    ...swalOptions,
                    icon: 'error',
                    title: '❌ Cannot Record Attendance',
                    html: fullMessage.replace(/\n\n/g, '<br><br>').replace(/\n/g, '<br>'),
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#ef4444',
                    timer: 7000,
                    timerProgressBar: true,
                    allowOutsideClick: true,
                    customClass: {
                        popup: customClass  // ✅ SUBTLE ENHANCEMENT: Role-based colored border
                    }
                });
            } else {
                // Generic error
                Swal.fire({
                    ...swalOptions,
                    icon: 'error',
                    title: '❌ Cannot Record Attendance',
                    html: `
                        <div style="text-align: left; line-height: 1.8;">
                            <p style="font-size: 16px; color: #6b7280; margin-bottom: 10px;">🔍 Fingerprint not recognized.</p>
                            <p style="color: #374151;">This fingerprint is not registered in the system.</p>
                            <p style="color: #374151;">Please contact the administration office to register your fingerprint.</p>
                        </div>
                    `,
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#ef4444',
                    timer: 6000,
                    timerProgressBar: true,
                    allowOutsideClick: true,
                    customClass: {
                        popup: 'generic-error-refined'  // ✅ Apply refined styling
                    }
                });
            }
        } else {
            // Success notifications
            const student = result.student || {};
            const currentClass = result.currentClass || {};

            const title = result.action === 'time_in'
                ? '👋 Welcome!'
                : '👋 Goodbye!';

            const studentName = student.fullName || 'Student';
            const programInfo = student.programName || '';
            const yearSection = student.yearLevel && student.sectionName
                ? `${student.yearLevel}${getOrdinalSuffix(student.yearLevel)} Year, ${student.sectionName}`
                : '';

            const courseDisplay = currentClass.courseCode && currentClass.courseName
                ? `${currentClass.courseCode} - ${currentClass.courseName}`
                : '-';

            const facultyDisplay = currentClass.facultyName || '-';

            const timestamp = result.timestamp
                ? new Date(result.timestamp).toLocaleTimeString('en-US', {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    hour12: true
                })
                : '';

            let html = `
                <div style="text-align: left; line-height: 1.8; padding: 10px;">
                    <p style="font-size: 18px; font-weight: 700; margin-bottom: 5px;">${studentName}</p>
            `;

            if (programInfo) {
                html += `<p style="color: #6b7280; margin-bottom: 3px;">${programInfo}`;
                if (yearSection) {
                    html += ` - ${yearSection}`;
                }
                html += `</p>`;
            }

            html += `
                    <div style="background: #f9fafb; border: 2px solid #e5e7eb; padding: 15px; border-radius: 8px; margin: 15px 0;">
                        <div style="margin-bottom: 10px;">
                            <strong style="font-size: 14px; color: #1f2937;">📚 Course</strong><br>
                            <span style="color: #374151;">${courseDisplay}</span>
                        </div>
                        <div>
                            <strong style="font-size: 14px; color: #1f2937;">👨‍🏫 Faculty</strong><br>
                            <span style="color: #374151;">${facultyDisplay}</span>
                        </div>
            `;

            if (currentClass.roomCode) {
                html += `
                        <div style="margin-top: 10px;">
                            <strong style="font-size: 14px; color: #1f2937;">🚪 Room</strong><br>
                            <span style="color: #374151;">${currentClass.roomCode}</span>
                        </div>
                `;
            }

            html += `</div>`;

            html += `
                    <div style="margin-top: 15px; padding: 12px; background: ${result.action === 'time_in' ? '#d1fae5' : '#dbeafe'}; border-radius: 8px;">
                        <strong>${result.action === 'time_in' ? '✅ Time In:' : '👋 Time Out:'}</strong>
                        <span style="font-size: 18px; font-weight: bold; margin-left: 10px;">${timestamp}</span>
                    </div>
            `;

            // Add duration for time out
            if (result.action === 'time_out' && result.message) {
                const durationMatch = result.message.match(/(\d+ hours? and \d+ minutes?)|(\d+ minutes? and \d+ seconds?)/i);
                if (durationMatch) {
                    html += `
                        <div style="margin-top: 10px; padding: 12px; background: #fef3c7; border-radius: 8px;">
                            <strong>⏱️ Duration:</strong> ${durationMatch[0]}
                        </div>
                    `;
                }
            }

            html += `</div>`;

            Swal.fire({
                ...swalOptions,
                icon: 'success',
                title: title,
                html: html,
                confirmButtonText: 'OK',
                confirmButtonColor: '#10b981',
                timer: 8000,
                timerProgressBar: true,
                allowOutsideClick: true
            });
        }
    }

    /**
     * ✅ NEW: Helper function to get ordinal suffix (1st, 2nd, 3rd, 4th)
     */
    function getOrdinalSuffix(num) {
        const j = num % 10;
        const k = num % 100;
        if (j === 1 && k !== 11) return "st";
        if (j === 2 && k !== 12) return "nd";
        if (j === 3 && k !== 13) return "rd";
        return "th";
    }

    /**
     * ✅ ENHANCED: Handle successful scan - ONLY SweetAlert2, NO inline message
     */
    function handleSuccess(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'error');
        fingerprintScanner.classList.add('success');

        // ✅ UPDATE: Show live info cards in the RIGHT SIDE LIST
        updateLiveScanInfo(result);

        // ✅ UPDATE: Show SweetAlert2 notification ONLY (no inline message)
        showSweetAlertNotification(result);

        // ✅ Update statistics
        updateStats();

        // ✅ CRITICAL FIX: Reset scanner immediately after 2 seconds to allow time-out scan
        setTimeout(() => {
            resetScanner();
            // ✅ CRITICAL FIX: Ensure auto-detection is running for immediate time-out capability
            if (!autoScanInterval) {
                console.log('✅ Restarting auto-detection after successful scan');
                startAutoDetection();
            } else {
                console.log('✅ Auto-detection already running');
            }
        }, 2000); // Reduced from 6 to 2 seconds for faster time-out capability
    }
    
    /**
     * ✅ Reset scanner to ready state
     */
    function resetScanner() {
        console.log('🔄 Resetting scanner to ready state');
        fingerprintScanner.classList.remove('scanning', 'success', 'error', 'validating');
        fingerprintScanner.classList.add('ready');
        fingerprintScanner.style.animation = 'pulse 2s infinite';
        
        // ✅ CRITICAL FIX: Reset scanning flags to allow immediate next scan
        isScanning = false;
        isCooldown = false;
        
        console.log('✅ Scanner reset complete - Ready for next scan');
    }
    
    /**
     * ✅ Handle scan error
     */
    function handleError(result) {
        console.log('❌ Handling scan error');
        fingerprintScanner.classList.remove('scanning', 'validating', 'success');
        fingerprintScanner.classList.add('error');
        
        showSweetAlertNotification(result);
        
        setTimeout(() => {
            resetScanner();
            // ✅ CRITICAL FIX: Ensure auto-detection restarts after error
            if (!autoScanInterval) {
                console.log('✅ Restarting auto-detection after error');
                startAutoDetection();
            }
        }, 3000);
    }
    
    /**
     * ✅ Handle scan timeout
     */
    function handleScanTimeout() {
        fingerprintScanner.classList.remove('scanning', 'validating');
        fingerprintScanner.classList.add('error');
        
        showMessage('Scan timeout. Please try again.', 'error');
        
        isScanning = false;
        
        setTimeout(() => {
            resetScanner();
            if (!autoScanInterval) {
                startAutoDetection();
            }
        }, 3000);
    }
    
    /**
     * ✅ Show status message
     */
    function showMessage(message, type) {
        if (!scanStatusMessage) return;
        
        scanStatusMessage.classList.remove('success', 'error', 'warning', 'scanning', 'validating', 'info');
        scanStatusMessage.classList.add(type, 'show');
        scanStatusMessage.innerHTML = message;
        
        if (type === 'error' || type === 'warning') {
            setTimeout(() => {
                scanStatusMessage.classList.remove('show');
            }, 5000);
        }
    }

    /**
     * ✅ Start auto-updating statistics
     */
    function startStatsUpdates() {
        // Update stats immediately
        updateStats();
        
        // Then update every 5 seconds
        statsUpdateInterval = setInterval(updateStats, 5000);
    }

    /**
     * ✅ Update statistics from server
     * ✅ ENHANCED: Added error handling to prevent UI freeze
     */
    async function updateStats() {
        try {
            const response = await fetch('/Home/GetStatistics');
            
            // ✅ FIX: Check if response is OK before parsing
            if (!response.ok) {
                console.warn('Statistics endpoint returned error status:', response.status);
                return; // Silently fail - don't break UI
            }
            
            const data = await response.json();
            
            if (data && data.success !== false) {
                // Update Success Rate
                const successRateElement = document.getElementById('successRateValue');
                if (successRateElement && data.successRate !== undefined) {
                    successRateElement.textContent = data.successRate + '%';
                }
                
                // Update Today's Scans
                const todayScansElement = document.getElementById('todayScansValue');
                if (todayScansElement && data.todayScans !== undefined) {
                    todayScansElement.textContent = data.todayScans;
                }
            }
        } catch (error) {
            console.error('Error updating statistics:', error);
            // ✅ FIX: Don't throw - just log and continue
            // UI will continue functioning with old stats
        }
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
