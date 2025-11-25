/**
 * Admin Dashboard Overview - Real-Time Data with SignalR
 * ✅ ENHANCED: Now receives real-time attendance notifications via SignalR
 * Loads statistics, system status, and recent activity
 */

document.addEventListener('DOMContentLoaded', async function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load all data on page load
    loadSystemStats();
    loadSystemStatus();
    loadRecentActivity();

    // ✅ NEW: Initialize SignalR connection for real-time updates
    try {
        await signalRConnection.initialize();
        console.log('✅ SignalR connected - Admin Dashboard ready for real-time updates');

        // ✅ Listen for real-time attendance updates
        signalRConnection.onAttendanceUpdate((data) => {
            console.log('📡 Real-time attendance notification received:', data);
            
            // ✅ Add to recent activity feed
            addRealtimeActivityToFeed(data);
            
            // ✅ Show toast notification
            const actionText = data.action === 'time_in' ? 'Time In' : 'Time Out';
            Toast.success(`${data.studentName} - ${actionText} (${data.courseCode})`);
            
            // ✅ Refresh stats (attendance count increased)
            setTimeout(() => {
                loadSystemStats();
            }, 1000);
        });

        console.log('✅ SignalR attendance listener registered');
    } catch (error) {
        console.error('❌ SignalR connection failed (non-critical):', error);
        // Dashboard will still work with polling fallback
    }

    // Auto-refresh every 30 seconds (fallback for missed SignalR events)
    setInterval(loadSystemStats, 30000);      // Refresh stats every 30s
    setInterval(loadSystemStatus, 10000);     // Refresh status every 10s
    setInterval(loadRecentActivity, 60000);   // Refresh activity every 60s
});

/**
 * ✅ NEW: Add real-time activity to the feed
 * Prepends new attendance event to the activity list
 */
function addRealtimeActivityToFeed(data) {
    const activityList = document.getElementById('recentActivity');
    if (!activityList) return;

    // Check if "no activity" placeholder exists
    const noActivity = activityList.querySelector('.no-activity');
    if (noActivity) {
        activityList.innerHTML = ''; // Clear placeholder
    }

    // Create new activity item
    const activityItem = document.createElement('div');
    activityItem.className = 'activity-item realtime-item';
    
    const actionIcon = data.action === 'time_in' ? 'log-in' : 'log-out';
    const actionText = data.action === 'time_in' ? 'Time In' : 'Time Out';
    const actionColor = data.action === 'time_in' ? '#10b981' : '#f59e0b';
    
    activityItem.innerHTML = `
        <div class="activity-icon" style="background-color: ${actionColor}20; color: ${actionColor};">
            <i data-lucide="${actionIcon}"></i>
        </div>
        <div class="activity-content">
            <div class="activity-action">Student ${actionText}</div>
            <div class="activity-details">
                ${data.studentName} (${data.studentNumber}) - ${data.courseCode}
            </div>
            <div class="activity-time">Just now</div>
        </div>
    `;
    
    // Prepend to list (newest first)
    activityList.insertBefore(activityItem, activityList.firstChild);
    
    // Add fade-in animation
    activityItem.style.opacity = '0';
    activityItem.style.transform = 'translateY(-10px)';
    setTimeout(() => {
        activityItem.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        activityItem.style.opacity = '1';
        activityItem.style.transform = 'translateY(0)';
    }, 10);
    
    // Remove realtime-item class after animation
    setTimeout(() => {
        activityItem.classList.remove('realtime-item');
    }, 3000);
    
    // Reinitialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // ✅ FIX: Limit to EXACTLY 3 items (remove oldest when adding new)
    const allItems = activityList.querySelectorAll('.activity-item');
    if (allItems.length > 3) {
        // Remove all items beyond the 3rd one
        for (let i = 3; i < allItems.length; i++) {
            activityList.removeChild(allItems[i]);
        }
    }
}

/**
 * Load real-time system statistics
 */
async function loadSystemStats() {
    try {
        const response = await fetch('/api/admin/stats');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
    
        // Update statistics with animation
        animateValue('totalUsers', 0, data.totalUsers, 1000);
        animateValue('totalCourses', 0, data.totalCourses, 1000);
        animateValue('totalFaculty', 0, data.totalFaculty, 1000);
        animateValue('totalRooms', 0, data.totalRooms, 1000);

        console.log('System stats loaded:', data);
    } catch (error) {
        console.error('Error loading system stats:', error);
    
        // Show error values
        document.getElementById('totalUsers').textContent = '--';
        document.getElementById('totalCourses').textContent = '--';
        document.getElementById('totalFaculty').textContent = '--';
        document.getElementById('totalRooms').textContent = '--';
    }
}

/**
 * Load real-time system status (database and scanner)
 */
async function loadSystemStatus() {
    try {
        const response = await fetch('/api/admin/system-status');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
      
        // Update database status
        const dbStatusBadge = document.getElementById('dbStatus');
        if (dbStatusBadge) {
            dbStatusBadge.textContent = data.database.status;
            dbStatusBadge.className = 'status-badge';
      
            if (data.database.isOnline) {
                dbStatusBadge.classList.add('status-online');
            } else {
                dbStatusBadge.classList.add('status-offline');
            }
        }
        
        // Update scanner status
        const scannerStatusBadge = document.querySelector('.status-item:nth-child(2) .status-badge');
        if (scannerStatusBadge) {
            scannerStatusBadge.textContent = data.scanner.status;
            scannerStatusBadge.className = 'status-badge';
          
            if (data.scanner.isOnline) {
                scannerStatusBadge.classList.add('status-online');
            } else {
                scannerStatusBadge.classList.add('status-offline');
            }
        }

        console.log('System status loaded:', data);
    } catch (error) {
        console.error('Error loading system status:', error);
        
        // Show error status
        const dbStatusBadge = document.getElementById('dbStatus');
        if (dbStatusBadge) {
            dbStatusBadge.textContent = 'Error';
            dbStatusBadge.className = 'status-badge status-offline';
        }
    }
}

/**
 * Load recent activity from administration_audit table (top 3 most recent)
 */
async function loadRecentActivity() {
    try {
        // ✅ FIX: Fetch only top 3 most recent audit logs
        const response = await fetch('/api/admin/recent-activity?limit=3');
     
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
  
        const data = await response.json();
        const activityList = document.getElementById('recentActivity');
        
        if (!activityList) return;
        
        // Clear existing content
        activityList.innerHTML = '';
    
        // ✅ FIX: Declare limitedActivities outside if block
        let limitedActivities = [];
        
        if (data.activities && data.activities.length > 0) {
            // ✅ FIX: Limit to maximum 3 activities (already limited by API, but enforce here too)
            limitedActivities = data.activities.slice(0, 3);
            
            // Display activities
            limitedActivities.forEach(activity => {
                const activityItem = document.createElement('div');
                activityItem.className = 'activity-item';
  
                activityItem.innerHTML = `
                    <div class="activity-icon">
                        <i data-lucide="activity"></i>
                    </div>
                    <div class="activity-content">
                        <div class="activity-action">${escapeHtml(activity.action)}</div>
                        <div class="activity-details">${escapeHtml(activity.details)}</div>
                        <div class="activity-time">${formatTimestamp(activity.timestamp)}</div>
                    </div>
                `;
          
                activityList.appendChild(activityItem);
            });

            // Reinitialize Lucide icons for new content
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        } else {
            // No activities found
            activityList.innerHTML = `
                <div class="no-activity">
                    <i data-lucide="activity" style="width: 48px; height: 48px; color: #9ca3af;"></i>
                    <p>No Activity</p>
                    <span>No recent activity to display</span>
                </div>
            `;
            
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }

        console.log('Recent activity loaded:', limitedActivities.length, 'items');
    } catch (error) {
        console.error('Error loading recent activity:', error);
      
        const activityList = document.getElementById('recentActivity');
        if (activityList) {
            activityList.innerHTML = `
                <div class="no-activity">
                    <i data-lucide="alert-circle" style="width: 48px; height: 48px; color: #ef4444;"></i>
                    <p>Error Loading Activity</p>
                    <span>Failed to fetch recent activity</span>
                </div>
            `;
  
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }
    }
}

/**
 * Animate number from start to end
 */
function animateValue(id, start, end, duration) {
    const element = document.getElementById(id);
    if (!element) return;
    
    const startTimestamp = Date.now();
    const step = () => {
        const now = Date.now();
        const progress = Math.min((now - startTimestamp) / duration, 1);
        const value = Math.floor(progress * (end - start) + start);
        
        element.textContent = value.toLocaleString();
        
        if (progress < 1) {
            requestAnimationFrame(step);
        }
    };
    
    requestAnimationFrame(step);
}

/**
 * Format timestamp to relative time
 */
function formatTimestamp(timestamp) {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 1) {
        return 'Just now';
    } else if (diffMins < 60) {
        return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    } else if (diffHours < 24) {
        return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    } else if (diffDays < 7) {
        return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    } else {
        return date.toLocaleDateString();
    }
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}
