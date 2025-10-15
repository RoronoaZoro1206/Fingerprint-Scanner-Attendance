$(document).ready(function() {
    loadStats();
    loadRecentActivity();
    checkDatabaseConnection();
    
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
});

async function loadStats() {
    try {
        const response = await fetch('/AdminOverview/GetStats');
        const result = await response.json();
        
        if (result.success) {
            const stats = result.data;
            document.getElementById('totalUsers').textContent = stats.totalUsers;
            document.getElementById('totalCourses').textContent = stats.totalCourses;
            document.getElementById('totalFaculty').textContent = stats.totalFaculty;
            document.getElementById('totalRooms').textContent = stats.totalRooms;
        }
    } catch (error) {
        console.error('Error loading stats:', error);
    }
}

async function loadRecentActivity() {
    try {
        const response = await fetch('/AdminOverview/GetRecentActivity');
        const result = await response.json();
        
        if (result.success) {
            const container = document.getElementById('recentActivity');
            
            if (result.data.length === 0) {
                container.innerHTML = '<p style="color: #9ca3af; text-align: center;">No recent activity</p>';
                return;
            }
            
            container.innerHTML = result.data.map(activity => `
                <div class="activity-item">
                    <div class="activity-action">${escapeHtml(activity.action)}</div>
                    <div class="activity-details">${escapeHtml(activity.details || '')}</div>
                    <div class="activity-time">${escapeHtml(activity.timestamp)}</div>
                </div>
            `).join('');
        }
    } catch (error) {
        console.error('Error loading activity:', error);
    }
}

async function checkDatabaseConnection() {
    try {
        const response = await fetch('/AdminOverview/CheckDatabaseConnection');
        const result = await response.json();
        
        const badge = document.getElementById('dbStatus');
        if (result.success && result.status === 'Online') {
            badge.textContent = 'Online';
            badge.className = 'status-badge status-online';
        } else {
            badge.textContent = 'Offline';
            badge.className = 'status-badge status-offline';
        }
    } catch (error) {
        const badge = document.getElementById('dbStatus');
        badge.textContent = 'Offline';
        badge.className = 'status-badge status-offline';
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
