// ============================================================
// Attendance Scan Interface - Main Script
// Integrates with existing AttendanceController and Phase 6 API
// ============================================================

(function() {
    'use strict';

    // DOM Elements
    const fingerprintScanner = document.getElementById('fingerprintScanner');
    const scanStatusMessage = document.getElementById('scanStatusMessage');
    const studentInfoCard = document.getElementById('studentInfoCard');
    const warningMessage = document.getElementById('warningMessage');
    const scannerStatusBadge = document.getElementById('scannerStatusBadge');
    const currentTime = document.getElementById('current-time');
    const currentDate = document.getElementById('current-date');
    const successRateElement = document.querySelector('.scan-stat-card.success h3');
    const todaysScansElement = document.querySelector('.scan-stat-card.total h3');

    // State
    let isScanning = false;
    let scannerOnline = false;
    let autoScanInterval = null;

    // Initialize
    document.addEventListener('DOMContentLoaded', function() {
        console.log('✅ Attendance Scan Interface Initialized');
        
        // Initialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        // Initialize real-time clock
        updateClock();
        setInterval(updateClock, 1000);

        // Load initial statistics
        loadTodayStats();

        // Check scanner status on load
        checkScannerStatus();

        // Load today's summary
        loadTodaySummary();

        // Set up event listeners
        fingerprintScanner.addEventListener('click', handleScanClick);

        // Refresh scanner status every 30 seconds
        setInterval(checkScannerStatus, 30000);

        // Refresh summary every 60 seconds
        setInterval(loadTodaySummary, 60000);
    });

    // ============================================================
    // Scanner Status Check - Uses existing AttendanceController
    // ============================================================
    async function checkScannerStatus() {
        try {
            // Call existing endpoint: /Attendance/CheckScannerStatus
            const response = await fetch('/Attendance/CheckScannerStatus');
            const result = await response.json();

            if (result.success) {
                updateScannerStatus(result.isReady, result.status);
            } else {
                updateScannerStatus(false, 'Error');
            }
        } catch (error) {
            console.error('Error checking scanner status:', error);
            updateScannerStatus(false, 'Connection Error');
        }
    }

    function updateScannerStatus(isOnline, statusText) {
        scannerOnline = isOnline;
        
        const indicator = scannerStatusBadge.querySelector('.status-indicator');
        const text = scannerStatusBadge.querySelector('.status-text');

        indicator.className = 'status-indicator ' + (isOnline ? 'online' : 'offline');
        text.textContent = statusText;

        // Update message if scanner is offline
        if (!isOnline && !isScanning) {
            showMessage('error', '⚠️ Scanner not available. Please contact administrator.');
        } else if (isOnline && !isScanning) {
            showMessage('info', 'Ready to scan. Click the scanner to begin.');
        }
    }

    // ============================================================
    // Scan Process - Uses existing AttendanceController
    // ============================================================
    async function handleScanClick() {
        if (isScanning || !scannerOnline) return;

        try {
            isScanning = true;

            // Reset UI
            hideStudentInfo();
            hideWarning();

            // Start scanning animation
            fingerprintScanner.classList.add('scanning');
            showMessage('info', '🔄 Scanning fingerprint... Please keep your finger steady.');

            // Call existing endpoint: POST /Attendance/RecordAttendance
            const response = await fetch('/Attendance/Scan', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            // Handle response
            if (result.success && result.action !== 'already_completed') {
                await handleScanSuccess(result);
            } else if (result.action === 'already_completed') {
                handleAlreadyCompleted(result);
            } else {
                handleScanError(result.message || 'Scan failed');
            }

        } catch (error) {
            console.error('Scan error:', error);
            handleScanError('Network error. Please try again.');
        } finally {
            isScanning = false;
            setTimeout(() => {
                fingerprintScanner.classList.remove('scanning');
            }, 2000);
        }
    }

    async function handleScanSuccess(data) {
        console.log('✅ Scan successful:', data);

        // Update scanner animation
        fingerprintScanner.classList.remove('scanning');
        fingerprintScanner.classList.add('success');

        // Show success message
        showMessage('success', data.message);

        // Display student info
        displayStudentInfo(data);

        // Show warning if there are incomplete records
        if (data.incompleteRecordsWarning) {
            showWarning(data.incompleteRecordsWarning);
        }

        // Refresh statistics
        await loadTodayStats();

        // Refresh summary
        await loadTodaySummary();

        // Reset scanner state after 3 seconds
        setTimeout(() => {
            fingerprintScanner.classList.remove('success');
            showMessage('info', 'Ready for next scan.');
        }, 3000);
    }

    function handleAlreadyCompleted(data) {
        console.log('⚠️ Already completed:', data);

        // Update scanner animation
        fingerprintScanner.classList.remove('scanning');
        fingerprintScanner.classList.add('error');

        // Show message
        showMessage('error', data.message);

        // Display student info anyway
        displayStudentInfo(data);

        // Reset scanner state after 3 seconds
        setTimeout(() => {
            fingerprintScanner.classList.remove('error');
            showMessage('info', 'Ready for next scan.');
        }, 3000);
    }

    function handleScanError(errorMessage) {
        console.error('❌ Scan error:', errorMessage);

        // Update scanner animation
        fingerprintScanner.classList.remove('scanning');
        fingerprintScanner.classList.add('error');

        // Show error message
        showMessage('error', errorMessage);

        // Hide student info
        hideStudentInfo();

        // Reset scanner state after 2 seconds
        setTimeout(() => {
            fingerprintScanner.classList.remove('error');
            if (scannerOnline) {
                showMessage('info', 'Ready to scan. Try again.');
            }
        }, 2000);
    }

    // ============================================================
    // UI Updates
    // ============================================================
    function showMessage(type, text) {
        const messageIcon = scanStatusMessage.querySelector('.message-icon');
        const messageText = scanStatusMessage.querySelector('.message-text');

        // Update icon based on type
        let iconName = 'info';
        if (type === 'success') iconName = 'check-circle';
        else if (type === 'error') iconName = 'x-circle';
        else if (type === 'info') iconName = 'hand';

        messageIcon.setAttribute('data-lucide', iconName);

        // Update message
        messageText.textContent = text;

        // Update styling
        scanStatusMessage.className = 'scan-status-message ' + type;

        // Refresh icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    function displayStudentInfo(data) {
        if (!data.student) return;

        // Show the card
        studentInfoCard.style.display = 'flex';

        // Update student details
        document.getElementById('studentName').textContent = data.student.fullName || '-';
        document.getElementById('studentNumber').textContent = data.student.studNumber || '-';
        
        const programText = data.student.programName && data.student.sectionName 
            ? `${data.student.programName} - ${data.student.sectionName}`
            : data.student.programName || '-';
        document.getElementById('studentProgram').textContent = programText;

        // Update result badge
        const resultBadge = document.getElementById('resultBadge');
        const resultIcon = resultBadge.querySelector('.result-icon');
        const resultText = document.getElementById('resultText');
        const resultTimestamp = document.getElementById('resultTimestamp');

        if (data.action === 'time_in') {
            resultBadge.className = 'result-badge time-in';
            resultIcon.setAttribute('data-lucide', 'log-in');
            resultText.textContent = 'Time In';
        } else if (data.action === 'time_out') {
            resultBadge.className = 'result-badge time-out';
            resultIcon.setAttribute('data-lucide', 'log-out');
            resultText.textContent = 'Time Out';
        } else if (data.action === 'already_completed') {
            resultBadge.className = 'result-badge time-out';
            resultIcon.setAttribute('data-lucide', 'check-circle');
            resultText.textContent = 'Completed';
        }

        // Format timestamp
        if (data.timestamp) {
            const time = new Date(data.timestamp);
            resultTimestamp.textContent = time.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        }

        // Refresh icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    function hideStudentInfo() {
        studentInfoCard.style.display = 'none';
    }

    function showWarning(text) {
        document.getElementById('warningText').textContent = text;
        warningMessage.style.display = 'flex';
    }

    function hideWarning() {
        warningMessage.style.display = 'none';
    }

    // ============================================================
    // Load Today's Summary - Uses existing AttendanceController
    // ============================================================
    async function loadTodaySummary() {
        try {
            // Call existing endpoint: /Attendance/TodaySummary
            const response = await fetch('/Attendance/TodaySummary');
            const result = await response.json();

            if (result.success && result.data) {
                updateSummaryDisplay(result.data);
            }
        } catch (error) {
            console.error('Error loading summary:', error);
        }
    }

    function updateSummaryDisplay(data) {
        // Update date
        const dateOptions = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
        const dateStr = new Date(data.date).toLocaleDateString('en-US', dateOptions);
        document.getElementById('summaryDate').textContent = dateStr;

        // Update stats
        document.getElementById('statTimedIn').textContent = data.timedIn || 0;
        document.getElementById('statTimedOut').textContent = data.timedOut || 0;
        document.getElementById('statOnCampus').textContent = data.stillOnCampus || 0;
        document.getElementById('statIncomplete').textContent = data.incompleteRecords || 0;
    }

    /**
     * ✅ LOAD TODAY'S STATISTICS from database
     */
    async function loadTodayStats() {
        try {
            const response = await fetch('/Attendance/GetTodayStats', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (successRateElement) {
                successRateElement.textContent = result.successRate || '0%';
            }

            if (todaysScansElement) {
                todaysScansElement.textContent = result.todaysScans?.toLocaleString() || '0';
            }

        } catch (error) {
            console.error('Error loading today stats:', error);
        }
    }

    /**
     * Update real-time clock
     */
    function updateClock() {
        const now = new Date();
        
        if (currentTime) {
            currentTime.textContent = now.toLocaleTimeString('en-US');
        }
        
        if (currentDate) {
            currentDate.textContent = now.toLocaleDateString('en-US');
        }
    }

    /**
     * Show status message with animation
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
        } else {
            scanStatusMessage.classList.add('info');
        }

        scanStatusMessage.style.opacity = '0';
        scanStatusMessage.style.transform = 'translateY(-10px)';
        scanStatusMessage.style.display = 'block';

        requestAnimationFrame(() => {
            scanStatusMessage.style.opacity = '1';
            scanStatusMessage.style.transform = 'translateY(0)';
        });
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (autoScanInterval) {
            clearInterval(autoScanInterval);
        }
    });
})();
