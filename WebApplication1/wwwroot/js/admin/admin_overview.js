/**
 * Admin Dashboard Overview - Real-Time Data
 * Loads statistics, system status, and recent activity
 */

document.addEventListener('DOMContentLoaded', function () {
  // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Load all data on page load
    loadSystemStats();
    loadSystemStatus();
    loadRecentActivity();

    // Auto-refresh every 30 seconds
    setInterval(loadSystemStats, 30000);      // Refresh stats every 30s
    setInterval(loadSystemStatus, 10000);     // Refresh status every 10s
    setInterval(loadRecentActivity, 60000);   // Refresh activity every 60s
});

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
  // Fetch top 3 most recent audit logs
        const response = await fetch('/api/admin/recent-activity?limit=3');
     
        if (!response.ok) {
   throw new Error(`HTTP error! status: ${response.status}`);
    }
  
   const data = await response.json();
     const activityList = document.getElementById('recentActivity');
        
if (!activityList) return;
        
 // Clear existing content
        activityList.innerHTML = '';
    
     if (data.activities && data.activities.length > 0) {
    // Display activities
            data.activities.forEach(activity => {
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

        console.log('Recent activity loaded:', data.activities.length, 'items');
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
