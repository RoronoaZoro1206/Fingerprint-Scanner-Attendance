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
     * ✅ ENHANCED: Handle successful scan with SweetAlert2 + Info Card Updates
     */
    function handleSuccess(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'error');
        fingerprintScanner.classList.add('success');

        const student = result.student || {};
        const currentClass = result.currentClass || {};
        const action = result.action || 'recorded';
        const timestamp = result.timestamp ? new Date(result.timestamp).toLocaleTimeString() : new Date().toLocaleTimeString();

        // ✅ NEW: Update Info Cards (live display without page refresh)
        updateInfoCards(student, currentClass, result);

        // Build comprehensive message for SweetAlert2
        const studentName = student.fullName || 'Student';
        const programInfo = buildProgramInfo(student);
        const classInfo = buildClassInfo(currentClass);
        
        let icon = action === 'time_in' ? 'success' : 'info';
        let title = action === 'time_in' 
            ? `✅ Welcome, ${studentName}!` 
            : `👋 Goodbye, ${studentName}!`;
        
        let htmlContent = `
            <div style="text-align: left; line-height: 1.8; padding: 10px;">
                <div style="background: #f3f4f6; padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                    <strong>Student ID:</strong> ${student.studNumber || 'N/A'}<br>
                    ${programInfo}
                </div>
                
                ${classInfo}
                
                <div style="margin-top: 15px; padding: 12px; background: ${action === 'time_in' ? '#d1fae5' : '#dbeafe'}; border-radius: 8px;">
                    <strong>${action === 'time_in' ? '⏰ Time In:' : '🚪 Time Out:'}</strong> 
                    <span style="font-size: 18px; font-weight: bold;">${timestamp}</span>
                </div>
        `;

        // Add duration for Time Out
        if (action === 'time_out') {
            const durationMatch = result.message.match(/(\d+ hours? and \d+ minutes?)|(\d+ minutes? and \d+ seconds?)/i);
            if (durationMatch) {
                htmlContent += `
                    <div style="margin-top: 10px; padding: 12px; background: #fef3c7; border-radius: 8px;">
                        <strong>⏱️ Duration:</strong> ${durationMatch[0]}
                    </div>
                `;
            }
        }

        // Add incomplete records warning
        if (result.incompleteRecordsWarning) {
            htmlContent += `
                <div style="margin-top: 15px; padding: 12px; background: #fee2e2; border-left: 3px solid #ef4444; border-radius: 4px;">
                    <strong>⚠️ Warning:</strong><br>
                    ${result.incompleteRecordsWarning}
                </div>
            `;
        }

        htmlContent += `</div>`;

        // Show SweetAlert2 with comprehensive info
        Swal.fire({
            icon: icon,
            title: title,
            html: htmlContent,
            confirmButtonText: 'OK',
            confirmButtonColor: '#3b82f6',
            timer: 8000,
            timerProgressBar: true,
            showClass: {
                popup: 'animate__animated animate__fadeInDown'
            },
            hideClass: {
                popup: 'animate__animated animate__fadeOutUp'
            },
            allowOutsideClick: true,
            backdrop: 'rgba(0,0,0,0.4)'
        });

        updateStats();

        // Reset after 8 seconds (after SweetAlert closes)
        setTimeout(() => {
            resetScanner();
            if (!autoScanInterval) {
                startAutoDetection();
            }
        }, 8000);
    }

    /**
     * ✅ NEW: Update Info Cards with Student/Class Data
     */
    function updateInfoCards(student, currentClass, result) {
        const infoCardsContainer = document.getElementById('studentInfoCards');
        
        if (infoCardsContainer) {
            // Show info cards
            infoCardsContainer.style.display = 'grid';
            
            // Update student name
            const studentNameElement = document.getElementById('currentStudentName');
            const studentNumberElement = document.getElementById('currentStudentNumber');
            if (studentNameElement) {
                studentNameElement.textContent = student.fullName || '-';
            }
            if (studentNumberElement) {
                studentNumberElement.textContent = student.studNumber || '-';
            }
            
            // Update program
            const programElement = document.getElementById('currentProgram');
            const yearSectionElement = document.getElementById('currentYearSection');
            if (programElement) {
                programElement.textContent = student.programName || '-';
            }
            if (yearSectionElement) {
                let yearSection = '';
                if (student.yearLevel) {
                    yearSection = `${student.yearLevel}${getOrdinalSuffix(student.yearLevel)} Year`;
                }
                if (student.sectionName) {
                    yearSection += yearSection ? `, ${student.sectionName}` : student.sectionName;
                }
                yearSectionElement.textContent = yearSection || '-';
            }
            
            // Update course
            const courseElement = document.getElementById('currentCourse');
            const roomElement = document.getElementById('currentRoom');
            if (courseElement && currentClass) {
                courseElement.textContent = currentClass.courseCode 
                    ? `${currentClass.courseCode} - ${currentClass.courseName}` 
                    : '-';
            }
            if (roomElement && currentClass) {
                roomElement.textContent = currentClass.roomCode || '-';
            }
            
            // Update faculty
            const facultyElement = document.getElementById('currentFaculty');
            const scheduleElement = document.getElementById('currentSchedule');
            if (facultyElement && currentClass) {
                facultyElement.textContent = currentClass.facultyName || '-';
            }
            if (scheduleElement && currentClass) {
                scheduleElement.textContent = currentClass.schedule || '-';
            }
        }
    }

    /**
     * ✅ NEW: Build Program Info HTML
     */
    function buildProgramInfo(student) {
        if (!student.programName) return '';
        
        let programDisplay = student.programName;
        if (student.yearLevel) {
            programDisplay += ` - ${student.yearLevel}${getOrdinalSuffix(student.yearLevel)} Year`;
        }
        if (student.sectionName) {
            programDisplay += `, ${student.sectionName}`;
        }
        
        return `<strong>Program:</strong> ${programDisplay}`;
    }

    /**
     * ✅ NEW: Build Class Info HTML
     */
    function buildClassInfo(currentClass) {
        if (!currentClass || !currentClass.courseCode) return '';
        
        return `
            <div style="background: #f9fafb; border: 2px solid #e5e7eb; padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <strong style="font-size: 16px; color: #1f2937;">📚 Current Class</strong><br>
                <div style="margin-top: 8px; line-height: 1.8;">
                    <strong>Course:</strong> ${currentClass.courseCode} - ${currentClass.courseName}<br>
                    <strong>Faculty:</strong> ${currentClass.facultyName}<br>
                    <strong>Room:</strong> ${currentClass.roomCode}<br>
                    <strong>Schedule:</strong> ${currentClass.schedule}
                </div>
            </div>
        `;
    }

    /**
     * ✅ ENHANCED: Handle scan error with SweetAlert2 for critical errors
     */
    function handleError(result) {
        fingerprintScanner.classList.remove('validating', 'scanning', 'success');
        fingerprintScanner.classList.add('error');

        let displayTime = 4000; // Default 4 seconds
        let message = result.message || '❌ Scan failed. Please try again.';
        
        // ✅ CRITICAL ERRORS: Use SweetAlert2 for blocking/important messages
        if (result.messageType === 'already_completed') {
            // Show "Already Completed" as SweetAlert2 modal
            Swal.fire({
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
            
            setTimeout(() => resetScanner(), 10000);
            return;
        }
        
        // ✅ OTHER ERRORS: Show in message box with appropriate icons
        if (result.messageType === 'role_restriction') {
            displayTime = 7000;
            const roleIcon = result.userRole === 'Admin' 
                ? '🛡️' : '👨‍🏫';
            
            message = `
                <div style="text-align: left; padding: 10px; line-height: 1.8;">
                    <p style="font-size: 18px; margin-bottom: 10px;">
                        ${roleIcon} <strong>${result.userRole} Fingerprint Detected</strong>
                    </p>
                    
                    <p>Hello, <strong style="color: #3b82f6;">${result.userName}</strong></p>
                    
                    <p>This is a <strong>${result.userRole}</strong> fingerprint.</p>
                    
                    <p style="margin: 12px 0; padding: 8px; background: #fee2e2; border-left: 3px solid #ef4444; border-radius: 4px;">
                        <strong style="color: #991b1b;">⛔ Only students are allowed to time in and time out.</strong>
                    </p>
                    
                    <p style="font-size: 14px; color: #6b7280;">
                        ${result.userRole === 'Admin' 
                            ? 'Please use the Admin Dashboard to manage the system.' 
                            : 'Please use the Faculty Dashboard to manage your classes.'}
                    </p>
                </div>
            `;
        } else if (result.messageType === 'unregistered') {
            displayTime = 6000;
            message = `
                <div style="text-align: center; padding: 10px;">
                    🔍<br><br>
                    <div style="line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else if (result.messageType === 'no_schedule') {
            displayTime = 6000;
            message = `
                <div style="text-align: center; padding: 10px;">
                    📅<br><br>
                    <div style="line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else if (result.messageType === 'capture_failed') {
            displayTime = 5000;
            message = `
                <div style="text-align: center; padding: 10px;">
                    ⚠️<br><br>
                    <div style="line-height: 1.8;">
                        ${result.message.replace(/\n/g, '<br>')}
                    </div>
                </div>
            `;
        } else {
            displayTime = 5000;
            message = `
                <div style="text-align: center; padding: 10px;">
                    ❌ ${message.replace(/\n/g, '<br>')}
                </div>
            `;
        }

        showMessage(message, 'error');

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
