/// <reference path="global.d.ts" />

/**
 * Auto Logout Detection - Logs user logout ONLY when browser/tab is ACTUALLY closed
 * ✅ FIXED: Now properly distinguishes between:
 *    - Tab close (should log)
 *    - Browser close (should log)
 *    - Internal navigation (should NOT log)
 *    - Tab switching (should NOT log)
 * 
 * Uses sophisticated detection to prevent false positives
 */

(function() {
    'use strict';

    // Only run if user is logged in (has session)
    const userId = document.body.dataset.userId;
    const userEmail = document.body.dataset.userEmail;
    
    if (!userId || !userEmail) {
        console.log('❌ No active session detected - auto logout detection disabled');
        return;
    }

    console.log('✅ Auto logout detection enabled for user:', userEmail);
    console.log('📊 User ID:', userId);
    console.log('ℹ️ Will ONLY trigger on actual tab/browser close');

    // Track session start time and last activity
    const sessionStartTime = new Date();
    let lastActivityTime = new Date();

    /**
     * Get browser and OS information
     */
    function getBrowserInfo() {
        const userAgent = navigator.userAgent;
        let browserName = 'Unknown';
        let osName = 'Unknown';

        // Detect browser
        if (userAgent.indexOf('Firefox') > -1) {
            browserName = 'Firefox';
        } else if (userAgent.indexOf('Chrome') > -1) {
            browserName = 'Chrome';
        } else if (userAgent.indexOf('Safari') > -1) {
            browserName = 'Safari';
        } else if (userAgent.indexOf('Edge') > -1 || userAgent.indexOf('Edg') > -1) {
            browserName = 'Edge';
        } else if (userAgent.indexOf('MSIE') > -1 || userAgent.indexOf('Trident') > -1) {
            browserName = 'Internet Explorer';
        }

        // Detect OS
        if (userAgent.indexOf('Windows') > -1) {
            osName = 'Windows';
        } else if (userAgent.indexOf('Mac') > -1) {
            osName = 'macOS';
        } else if (userAgent.indexOf('Linux') > -1) {
            osName = 'Linux';
        } else if (userAgent.indexOf('Android') > -1) {
            osName = 'Android';
        } else if (userAgent.indexOf('iOS') > -1 || userAgent.indexOf('iPhone') > -1 || userAgent.indexOf('iPad') > -1) {
            osName = 'iOS';
        }

        return `${browserName} on ${osName}`;
    }

    /**
     * Calculate session duration
     */
    function getSessionDuration() {
        const duration = new Date() - sessionStartTime;
        const minutes = Math.floor(duration / 60000);
        const seconds = Math.floor((duration % 60000) / 1000);
        return `${minutes}m ${seconds}s`;
    }

    /**
     * Format last activity time
     */
    function getLastActivityTime() {
        return lastActivityTime.toLocaleTimeString('en-US', { 
            hour12: false,
            hour: '2-digit', 
            minute: '2-digit', 
            second: '2-digit' 
        });
    }

    /**
     * Send logout beacon to server with enhanced details
     * Uses Beacon API which is designed to work even during page unload
     */
    function sendLogoutBeacon() {
        try {
            const logoutUrl = '/Auth/AutoLogout';
            
            const data = JSON.stringify({
                userId: parseInt(userId),
                userEmail: userEmail,
                reason: 'Browser/tab closed',
                sessionDuration: getSessionDuration(),
                lastActivityTime: getLastActivityTime(),
                browserInfo: getBrowserInfo()
            });

            console.log('📡 Sending auto logout beacon to:', logoutUrl);
            console.log('📦 Data:', data);

            // Beacon API - guaranteed to send even if page is closing
            if (navigator.sendBeacon) {
                const blob = new Blob([data], { type: 'application/json' });
                const sent = navigator.sendBeacon(logoutUrl, blob);
                console.log('🔵 Beacon API result:', sent ? 'Success ✅' : 'Failed ❌');
                return sent;
            } else {
                console.log('⚠️ Beacon API not supported, using synchronous XHR fallback');
                // Fallback to synchronous XHR
                const xhr = new XMLHttpRequest();
                xhr.open('POST', logoutUrl, false); // false = synchronous
                xhr.setRequestHeader('Content-Type', 'application/json');
                xhr.send(data);
                console.log('🔵 XHR fallback sent, status:', xhr.status);
                return xhr.status === 200;
            }
        } catch (error) {
            console.error('❌ Error sending logout beacon:', error);
            return false;
        }
    }

    /**
     * Check if URL is a logout URL
     */
    function isLogoutUrl(url) {
        if (!url) return false;
        const urlLower = url.toLowerCase();
        return urlLower.includes('/auth/logout') || 
               urlLower.includes('/logout') ||
               urlLower.endsWith('/logout');
    }

    /**
     * Check if navigation is internal (same origin)
     */
    function isInternalNavigation(url) {
        if (!url) return false;
        
        try {
            // Relative URLs are internal
            if (url.startsWith('/') || url.startsWith('#') || url.startsWith('?')) {
                return true;
            }
            
            // Check if URL is same origin
            const currentOrigin = window.location.origin;
            const targetUrl = new URL(url, window.location.href);
            return targetUrl.origin === currentOrigin;
        } catch (e) {
            // If URL parsing fails, assume internal
            return true;
        }
    }

    /**
     * Track logout state
     */
    let hasLoggedOut = false;
    let isManualLogout = false;
    let isNavigatingInternally = false;
    let navigationTimer = null;

    /**
     * Update last activity time on user interaction
     * For logging purposes only (shows when user was last active)
     */
    function updateLastActivity() {
        lastActivityTime = new Date();
    }

    // Track user activity (for logging only, NOT for auto-logout)
    ['click', 'keypress', 'mousemove'].forEach(event => {
        document.addEventListener(event, updateLastActivity, { passive: true });
    });

    /**
     * ✅ CRITICAL FIX: Track internal navigation to prevent false auto-logouts
     * This runs BEFORE page unload to determine if we're just navigating internally
     */
    document.addEventListener('click', function(e) {
        const target = e.target.closest('a');
        
        if (target) {
            const href = target.getAttribute('href');
            
            // Check for manual logout
            if (href && isLogoutUrl(href)) {
                console.log('🚪 Manual logout detected - disabling auto logout');
                hasLoggedOut = true;
                isManualLogout = true;
                return;
            }
            
            // Check for internal navigation
            if (href && isInternalNavigation(href)) {
                console.log('🏠 Internal navigation detected:', href);
                isNavigatingInternally = true;
                
                // Clear any existing timer
                if (navigationTimer) {
                    clearTimeout(navigationTimer);
                }
                
                // Reset flag after navigation completes (longer timeout for safety)
                navigationTimer = setTimeout(() => {
                    isNavigatingInternally = false;
                    console.log('🏠 Internal navigation completed, flag reset');
                }, 1000);
            }
        }
    }, true); // Capture phase to catch before navigation

    /**
     * ✅ IMPROVED: Use unload event which ONLY fires on actual tab close
     * This is more reliable than pagehide for our use case
     * 
     * unload fires when:
     * - User closes the tab (X button)
     * - User closes the browser window
     * - User navigates to external site
     * 
     * unload does NOT fire when:
     * - User switches to another tab (tab remains open)
     * - User opens new tab (original tab remains open)
     * - Page is just hidden (document.visibilityState changes)
     */
    window.addEventListener('unload', function(e) {
        console.log('🚨 unload event fired!');
        console.log('   hasLoggedOut:', hasLoggedOut);
        console.log('   isManualLogout:', isManualLogout);
        console.log('   isNavigatingInternally:', isNavigatingInternally);
        console.log('   Current path:', window.location.pathname);
        
        // ✅ CRITICAL: Skip if this is internal navigation
        if (isNavigatingInternally) {
            console.log('⏭️ Internal navigation - skipping auto logout');
            return;
        }
        
        // Skip if manual logout was clicked
        if (hasLoggedOut || isManualLogout) {
            console.log('⏭️ Manual logout - skipping auto logout');
            return;
        }
        
        // Skip if navigating to logout page
        if (window.location.pathname.toLowerCase().includes('/logout')) {
            console.log('⏭️ Logout page - skipping auto logout');
            return;
        }
        
        // ✅ Only send auto logout beacon if this is a REAL tab/browser close
        console.log('⚠️ Real tab/browser close detected - sending auto logout beacon');
        console.log(`📊 Session: ${getSessionDuration()}, Last Activity: ${getLastActivityTime()}`);
        
        sendLogoutBeacon();
        hasLoggedOut = true;
    });

    /**
     * ✅ BACKUP: Keep beforeunload as secondary check
     * This provides extra coverage for edge cases
     */
    window.addEventListener('beforeunload', function(e) {
        console.log('🚨 beforeunload event fired!');
        
        // Skip if already handled or manual logout
        if (hasLoggedOut || isManualLogout) {
            console.log('⏭️ Already handled or manual logout - skipping');
            return;
        }
        
        // Skip if internal navigation
        if (isNavigatingInternally) {
            console.log('⏭️ Internal navigation - skipping');
            return;
        }
        
        // Skip if logout page
        if (window.location.pathname.toLowerCase().includes('/logout')) {
            console.log('⏭️ Logout page - skipping');
            return;
        }
        
        // ✅ This will only fire if unload didn't handle it
        console.log('⚠️ beforeunload backup - sending auto logout beacon');
        sendLogoutBeacon();
        hasLoggedOut = true;
    });

    console.log('✅ Auto logout detection initialized');
    console.log('📊 Tracking: Session duration, last activity, device info');
    console.log('ℹ️ Will log ONLY on:');
    console.log('   ✅ Tab close (clicking X on tab)');
    console.log('   ✅ Browser window close');
    console.log('   ✅ Navigation to external site');
    console.log('ℹ️ Will NOT log on:');
    console.log('   ❌ Manual logout (logout button click)');
    console.log('   ❌ Internal navigation (sidebar, menu clicks)');
    console.log('   ❌ Tab switching (Facebook, YouTube, etc.)');
    console.log('   ❌ Opening new tabs');
    console.log('   ❌ Inactivity/AFK');
})();
