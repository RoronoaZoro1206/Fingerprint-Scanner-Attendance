/**
 * Auto Logout Detection - Logs user logout when browser/tab is closed
 * Uses Beacon API to reliably send logout signal even during page unload
 * 
 * ? FIXED: Only triggers on REAL logout (browser close/exit)
 * ? DOES NOT trigger on internal navigation (sidebar clicks, page changes)
 * ? ENHANCED: Tracks session duration, last activity, and device info
 */

(function() {
    'use strict';

    // Only run if user is logged in (has session)
    const userId = document.body.dataset.userId;
    const userEmail = document.body.dataset.userEmail;
    
    if (!userId || !userEmail) {
        console.log('No active session detected - auto logout detection disabled');
        return;
    }

    console.log('? Auto logout detection enabled for user:', userEmail);

    // ? NEW: Track session start time and last activity
    const sessionStartTime = new Date();
    let lastActivityTime = new Date();

    /**
     * ? NEW: Get browser and OS information
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
     * ? NEW: Calculate session duration
     */
    function getSessionDuration() {
        const duration = new Date() - sessionStartTime;
        const minutes = Math.floor(duration / 60000);
        const seconds = Math.floor((duration % 60000) / 1000);
        return `${minutes}m ${seconds}s`;
    }

    /**
     * ? NEW: Format last activity time
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
     * ? NEW: Get client IP address (best effort - requires server support)
     * Note: This will be resolved server-side from request headers
     */
    function getClientIP() {
        // IP will be determined server-side from HttpContext
        return 'server-determined';
    }

    /**
     * Send logout beacon to server with enhanced details
     * Uses Beacon API which is designed to work even during page unload
     */
    function sendLogoutBeacon() {
        try {
            const logoutUrl = '/Auth/AutoLogout';
            
            // ? ENHANCED: Include session duration, last activity, and device info
            const data = JSON.stringify({
                userId: parseInt(userId),
                userEmail: userEmail,
                reason: 'Browser closed or tab navigated away',
                sessionDuration: getSessionDuration(),
                lastActivityTime: getLastActivityTime(),
                browserInfo: getBrowserInfo(),
                // Server will add IP address from HttpContext
            });

            // Beacon API - guaranteed to send even if page is closing
            if (navigator.sendBeacon) {
                const blob = new Blob([data], { type: 'application/json' });
                const sent = navigator.sendBeacon(logoutUrl, blob);
                console.log('?? Enhanced logout beacon sent:', sent);
                return sent;
            } else {
                // Fallback to synchronous XHR (not recommended but works)
                const xhr = new XMLHttpRequest();
                xhr.open('POST', logoutUrl, false); // false = synchronous
                xhr.setRequestHeader('Content-Type', 'application/json');
                xhr.send(data);
                console.log('?? Fallback XHR logout sent');
                return true;
            }
        } catch (error) {
            console.error('? Error sending logout beacon:', error);
            return false;
        }
    }

    /**
     * Check if navigation is internal (same domain and path structure)
     */
    function isInternalNavigation(url) {
        try {
            // If it's a relative URL (no protocol), it's internal
            if (!url || url.startsWith('#') || url.startsWith('/')) {
                return true;
            }
            
            // Check if it's same domain
            const currentDomain = window.location.hostname;
            const linkUrl = new URL(url, window.location.href);
            
            // Same domain = internal navigation
            return linkUrl.hostname === currentDomain;
        } catch (error) {
            // If URL parsing fails, assume it's internal
            return true;
        }
    }

    /**
     * Track if user is leaving the site intentionally
     */
    let isLeavingSite = false;
    let hasLoggedOut = false; // Prevent duplicate logout logs
    let isInternalClick = false; // Track internal clicks

    /**
     * ? NEW: Update last activity time on user interaction
     */
    function updateLastActivity() {
        lastActivityTime = new Date();
    }

    // Track user activity
    ['click', 'keypress', 'scroll', 'mousemove'].forEach(event => {
        document.addEventListener(event, updateLastActivity, { passive: true, once: false });
    });

    /**
     * Detect when user clicks a link
     */
    document.addEventListener('click', function(e) {
        const target = e.target.closest('a');
        
        if (target) {
            const href = target.getAttribute('href');
            
            // Check if it's manual logout
            if (href && (href.includes('/Auth/Logout') || href.includes('/Logout'))) {
                console.log('?? Manual logout detected - disabling auto logout');
                hasLoggedOut = true; // Prevent auto logout from firing
                return;
            }
            
            // Check if it's internal navigation (sidebar, menu, etc.)
            if (href && isInternalNavigation(href)) {
                console.log('?? Internal navigation detected:', href, '- auto logout will NOT trigger');
                isInternalClick = true;
                // Reset flag after a short delay
                setTimeout(() => {
                    isInternalClick = false;
                }, 100);
                return;
            }
            
            // Check if it's an external link
            if (href && !isInternalNavigation(href)) {
                console.log('?? External link clicked - preparing auto logout');
                isLeavingSite = true;
            }
        }
    }, true); // Use capture phase to catch events early

    /**
     * Detect when user is leaving the page
     * This fires BEFORE the page unloads
     * 
     * ? FIX: Only send logout if NOT internal navigation
     */
    window.addEventListener('beforeunload', function(e) {
        // DON'T log if:
        // 1. User clicked manual logout button
        // 2. Already logged out
        // 3. Currently navigating to logout page
        // 4. Just clicked an internal link (sidebar, menu, etc.)
        
        if (hasLoggedOut) {
            console.log('?? Already logged out - skipping auto logout');
            return;
        }
        
        if (window.location.pathname.includes('/Auth/Logout') || window.location.pathname.includes('/Logout')) {
            console.log('?? Navigating to logout page - skipping auto logout');
            return;
        }
        
        if (isInternalClick) {
            console.log('?? Internal navigation - skipping auto logout');
            return;
        }
        
        // Only send logout if it's a REAL exit (browser close, external link, etc.)
        console.log('?? beforeunload detected - checking if real exit...');
        console.log(`?? Session Info: Duration: ${getSessionDuration()}, Last Activity: ${getLastActivityTime()}`);
        
        // Small delay to check if it's internal navigation
        // If page unloads immediately, it's probably browser close
        setTimeout(() => {
            if (!hasLoggedOut && !isInternalClick) {
                console.log('? Confirmed: User is exiting - sending enhanced logout beacon');
                sendLogoutBeacon();
                hasLoggedOut = true;
            }
        }, 10);
    });

    /**
     * Detect when page visibility changes (tab switch, minimize, etc.)
     * 
     * ? FIX: More conservative - only log if REALLY closing
     */
    document.addEventListener('visibilitychange', function() {
        // Only trigger if:
        // 1. Page becomes hidden
        // 2. Not already logged out
        // 3. Not internal navigation
        // 4. Page stays hidden for a while (likely closing)
        
        if (document.visibilityState === 'hidden' && !hasLoggedOut && !isInternalClick) {
            // Wait 2 seconds - if page is still hidden, it's probably closing
            setTimeout(function() {
                if (document.visibilityState === 'hidden' && !hasLoggedOut && !isInternalClick) {
                    console.log('??? Page hidden for 2 seconds - likely closing, sending enhanced logout beacon');
                    sendLogoutBeacon();
                    hasLoggedOut = true;
                }
            }, 2000); // Increased delay to avoid false positives
        }
    });

    /**
     * Page unload event (last resort fallback)
     * 
     * ? FIX: Only if NOT internal navigation
     */
    window.addEventListener('unload', function() {
        if (!hasLoggedOut && !isInternalClick) {
            console.log('?? unload event - final attempt to send enhanced logout');
            sendLogoutBeacon();
            hasLoggedOut = true;
        }
    });

    console.log('? Enhanced auto logout detection initialized successfully');
    console.log('?? Tracking: Session duration, last activity, device info');
    console.log('?? Will ONLY log on: browser close, external navigation, or manual logout');
    console.log('?? Will NOT log on: sidebar clicks, internal page navigation');
})();
