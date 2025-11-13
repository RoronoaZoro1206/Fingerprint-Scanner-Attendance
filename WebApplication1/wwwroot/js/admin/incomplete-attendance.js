// ============================================================
// Admin Incomplete Attendance Management
// Integrates with existing AdminIncompleteAttendanceController
// ============================================================

(function() {
    'use strict';

    let incompleteRecords = [];
    let currentRecordId = null;
    let allRecords = []; // Store all records for search filtering

    document.addEventListener('DOMContentLoaded', function() {
        console.log('? Admin Incomplete Attendance Initialized');

        // Initialize Lucide icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        // Load records
        loadIncompleteRecords();

        // Event listeners
        document.getElementById('btnRefresh').addEventListener('click', loadIncompleteRecords);
        document.getElementById('searchInput').addEventListener('input', handleSearch);
        document.getElementById('modalClose').addEventListener('click', closeModal);
        document.getElementById('modalOverlay').addEventListener('click', closeModal);
        document.getElementById('btnCancelResolve').addEventListener('click', closeModal);
        document.getElementById('btnConfirmResolve').addEventListener('click', handleResolve);
    });

    // ============================================================
    // Load Incomplete Records - Uses existing controller
    // ============================================================
    async function loadIncompleteRecords() {
        showLoading();

        try {
            // Call existing endpoint: /Admin/AdminIncompleteAttendance/GetIncompleteRecords
            const response = await fetch('/Admin/AdminIncompleteAttendance/GetIncompleteRecords');
            const result = await response.json();

            if (result.success && result.data) {
                incompleteRecords = result.data;
                allRecords = [...result.data]; // Store copy for filtering
                renderRecordsTable(incompleteRecords);
                updateStatistics(incompleteRecords);
            } else {
                showError('Failed to load records: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading records:', error);
            showError('Failed to load records. Please try again.');
        }
    }

    // ============================================================
    // Render Table
    // ============================================================
    function renderRecordsTable(records) {
        const tbody = document.getElementById('recordsTableBody');
        const emptyState = document.getElementById('emptyState');
        const tableContainer = document.querySelector('.table-container');

        if (!records || records.length === 0) {
            tableContainer.style.display = 'none';
            emptyState.style.display = 'block';
            
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
            return;
        }

        tableContainer.style.display = 'block';
        emptyState.style.display = 'none';

        tbody.innerHTML = records.map(record => `
            <tr>
                <td><strong>${escapeHtml(record.studentName)}</strong></td>
                <td>${escapeHtml(record.studNumber)}</td>
                <td>${formatDate(record.attendDate)}</td>
                <td>${formatTime(record.timeIn)}</td>
                <td><span class="badge badge-warning">${escapeHtml(record.daysAgo)}</span></td>
                <td>
                    <button class="btn-action btn-resolve" onclick="window.openResolveModal(${record.dailyAttendId})">
                        <i data-lucide="check"></i>
                        Resolve
                    </button>
                </td>
            </tr>
        `).join('');

        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    function showLoading() {
        const tbody = document.getElementById('recordsTableBody');
        const emptyState = document.getElementById('emptyState');
        const tableContainer = document.querySelector('.table-container');

        tableContainer.style.display = 'block';
        emptyState.style.display = 'none';

        tbody.innerHTML = `
            <tr class="loading-row">
                <td colspan="6">
                    <div class="loading-container">
                        <div class="loading-spinner"></div>
                        <span>Loading records...</span>
                    </div>
                </td>
            </tr>
        `;
    }

    // ============================================================
    // Open Resolve Modal
    // ============================================================
    window.openResolveModal = function(dailyAttendId) {
        const record = allRecords.find(r => r.dailyAttendId === dailyAttendId);
        if (!record) {
            console.error('Record not found:', dailyAttendId);
            return;
        }

        currentRecordId = dailyAttendId;
        
        document.getElementById('modalStudentName').textContent = record.studentName;
        document.getElementById('modalDate').textContent = formatDate(record.attendDate);
        document.getElementById('modalTimeIn').textContent = formatTime(record.timeIn);

        // Set default time out (same day at 17:00)
        const timeOutDefault = new Date(record.attendDate);
        timeOutDefault.setHours(17, 0, 0, 0);
        document.getElementById('manualTimeOut').value = formatDateTimeLocal(timeOutDefault);

        document.getElementById('resolveModal').classList.add('active');
        
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    };

    // ============================================================
    // Handle Resolve - Uses existing controller
    // ============================================================
    async function handleResolve() {
        const manualTimeOut = document.getElementById('manualTimeOut').value;
        
        if (!manualTimeOut) {
            alert('Please select a time out');
            return;
        }

        const btnConfirm = document.getElementById('btnConfirmResolve');
        btnConfirm.disabled = true;
        btnConfirm.innerHTML = '<i data-lucide="loader"></i> Resolving...';

        try {
            // Call existing endpoint: POST /Admin/AdminIncompleteAttendance/ResolveRecord
            const response = await fetch('/Admin/AdminIncompleteAttendance/ResolveRecord', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    dailyAttendId: currentRecordId,
                    manualTimeOut: new Date(manualTimeOut).toISOString()
                })
            });

            const result = await response.json();

            if (result.success) {
                closeModal();
                showSuccess('Record resolved successfully');
                await loadIncompleteRecords();
            } else {
                showError(result.message || 'Failed to resolve record');
            }
        } catch (error) {
            console.error('Error resolving record:', error);
            showError('Error resolving record. Please try again.');
        } finally {
            btnConfirm.disabled = false;
            btnConfirm.innerHTML = '<i data-lucide="check"></i> Resolve Record';
            
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    }

    // ============================================================
    // Close Modal
    // ============================================================
    function closeModal() {
        document.getElementById('resolveModal').classList.remove('active');
        currentRecordId = null;
        document.getElementById('manualTimeOut').value = '';
    }

    // ============================================================
    // Update Statistics
    // ============================================================
    function updateStatistics(records) {
        const total = records.length;
        
        // Calculate recent (last 7 days)
        const recent = records.filter(r => {
            const daysAgoText = r.daysAgo.toLowerCase();
            
            if (daysAgoText.includes('today') || daysAgoText.includes('yesterday')) {
                return true;
            }
            
            const match = daysAgoText.match(/(\d+)\s+days?\s+ago/i);
            if (match) {
                const days = parseInt(match[1]);
                return days <= 7;
            }
            
            return false;
        }).length;

        document.getElementById('statTotalIncomplete').textContent = total;
        document.getElementById('statRecentIncomplete').textContent = recent;
    }

    // ============================================================
    // Search Functionality
    // ============================================================
    function handleSearch(e) {
        const searchTerm = e.target.value.toLowerCase().trim();
        
        if (!searchTerm) {
            incompleteRecords = [...allRecords];
        } else {
            incompleteRecords = allRecords.filter(record => 
                record.studentName.toLowerCase().includes(searchTerm) ||
                record.studNumber.toLowerCase().includes(searchTerm)
            );
        }
        
        renderRecordsTable(incompleteRecords);
    }

    // ============================================================
    // Utility Functions
    // ============================================================
    function formatDate(dateStr) {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { 
            month: 'short', 
            day: 'numeric', 
            year: 'numeric' 
        });
    }

    function formatTime(timeStr) {
        const time = new Date(timeStr);
        return time.toLocaleTimeString('en-US', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: true
        });
    }

    function formatDateTimeLocal(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hours}:${minutes}`;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showSuccess(message) {
        // Use SweetAlert2 if available, otherwise alert
        if (typeof SwalAlert !== 'undefined') {
            SwalAlert.success('Success!', message);
        } else {
            alert(message);
        }
    }

    function showError(message) {
        // Use SweetAlert2 if available, otherwise alert
        if (typeof SwalAlert !== 'undefined') {
            SwalAlert.error('Error', message);
        } else {
            alert(message);
        }
    }

})();
