/**
 * Admin Dashboard Overview - Refactored to use common utilities
 * Uses common AJAX utilities and UI helpers
 */

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load dashboard data
    loadStats();
    loadRecentActivity();
    checkDatabaseConnection();
});

/**
 * Load dashboard statistics
 */
function loadStats() {
    ajaxGet('/AdminOverview/GetStats',
        function (response) {
            const stats = response.data;
            $('#totalUsers').text(stats.totalUsers);
            $('#totalCourses').text(stats.totalCourses);
            $('#totalFaculty').text(stats.totalFaculty);
            $('#totalRooms').text(stats.totalRooms);
        },
        function (error) {
            console.error('Error loading stats:', error);
        }
    );
}

/**
 * Load recent activity
 */
function loadRecentActivity() {
    ajaxGet('/AdminOverview/GetRecentActivity',
        function (response) {
            const container = $('#recentActivity');

            if (response.data.length === 0) {
                showEmptyState('recentActivity', 'No Activity', 'No recent activity to display', 'activity');
                return;
            }

            const html = response.data.map(activity => `
                <div class="activity-item">
                    <div class="activity-action">${escapeHtml(activity.action)}</div>
                    <div class="activity-details">${escapeHtml(activity.details || '')}</div>
                    <div class="activity-time">${escapeHtml(activity.timestamp)}</div>
                </div>
            `).join('');

            container.html(html);
        },
        function (error) {
            showErrorState('recentActivity', 'Failed to load recent activity');
        }
    );
}

/**
 * Check database connection status
 */
function checkDatabaseConnection() {
    const $badge = $('#dbStatus');
    $badge.text('Checking...').removeClass().addClass('status-badge');

    ajaxGet('/AdminOverview/CheckDatabaseConnection',
        function (response) {
            if (response.status === 'Online') {
                $badge.text('Online').addClass('status-online');
            } else {
                $badge.text('Offline').addClass('status-offline');
            }
        },
        function (error) {
            $badge.text('Offline').addClass('status-offline');
        }
    );
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
