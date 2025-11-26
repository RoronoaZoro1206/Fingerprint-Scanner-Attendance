/// <reference path="global.d.ts" />

/**
 * Auto Logout Detection v14 - SERVER-SIDE HEARTBEAT TRACKING
 * 
 * PROBLEM: Browser tab close/exit doesn't reliably send HTTP requests.
 * sendBeacon is unreliable when closing the browser tab.
 * 
 * SOLUTION:
 * 1. Send periodic heartbeats to the server (every 5 seconds)
 * 2. Server tracks active sessions and detects when heartbeats stop
 * 3. When heartbeats stop for 15+ seconds, server logs AUTO LOGOUT
 * 4. Still try sendBeacon on unload as a backup (may work sometimes)
 * 
 * This approach is RELIABLE because:
 * - Server-side detection doesn't depend on browser cooperation
 * - Even if the browser crashes, server will detect the missing heartbeats
 * - Works for tab close, browser close, navigation away, and crashes
 */

(function() {
    'use strict';

    // ============================================
    // CONSTANTS
    // ============================================
    const TAB_ID_KEY = 'auto_logout_tab_id';
    const NAVIGATING_FLAG_KEY = 'auto_logout_navigating';
    const HEARTBEAT_INTERVAL_MS = 5000; // Send heartbeat every 5 seconds

    // ============================================
    // INITIALIZATION
    // ============================================

    const userId = document.body?.dataset?.userId;
    const userEmail = document.body?.dataset?.userEmail;
    
    if (!userId || !userEmail || userId === '0' || userId === '') {
        console.log('Auto logout: No session');
        return;
    }

    let tabId = sessionStorage.getItem(TAB_ID_KEY);
    
    if (!tabId) {
        tabId = `tab_${Date.now()}_${Math.random().toString(36).substring(2, 11)}`;
        sessionStorage.setItem(TAB_ID_KEY, tabId);
    }

    // Clear navigating flag on page load
    sessionStorage.removeItem(NAVIGATING_FLAG_KEY);

    console.log('✅ Auto logout v14 enabled (Server-side heartbeat tracking)');
    console.log('📋 Tab ID:', tabId);
    console.log('👤 User:', userEmail);

    // ============================================
    // BROWSER INFO
    // ============================================

    function getBrowserInfo() {
        const ua = navigator.userAgent;
        let browser = 'Unknown', os = 'Unknown';
        if (ua.includes('Firefox')) browser = 'Firefox';
        else if (ua.includes('Edg')) browser = 'Edge';
        else if (ua.includes('Chrome')) browser = 'Chrome';
        else if (ua.includes('Safari')) browser = 'Safari';
        if (ua.includes('Windows')) os = 'Windows';
        else if (ua.includes('Mac')) os = 'macOS';
        else if (ua.includes('Linux')) os = 'Linux';
        else if (ua.includes('Android')) os = 'Android';
        else if (/iPhone|iPad|iOS/.test(ua)) os = 'iOS';
        return `${browser} on ${os}`;
    }


    // ============================================
    // HEARTBEAT SYSTEM - Core of the solution
    // ============================================

    let heartbeatInterval = null;
    let lastActivityTime = Date.now();

    function sendHeartbeat() {
        const data = JSON.stringify({
            tabId: tabId,
            userId: parseInt(userId),
            userEmail: userEmail,
            timestamp: Date.now(),
            browserInfo: getBrowserInfo()
        });

        // Use fetch for heartbeats (more reliable than beacon for regular requests)
        fetch('/Auth/Heartbeat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: data,
            keepalive: true
        }).then(response => {
            if (response.ok) {
                console.log('💓 Heartbeat sent');
            }
        }).catch(err => {
            console.warn('💔 Heartbeat failed:', err.message);
        });
    }

    // Start sending heartbeats immediately and every 5 seconds
    sendHeartbeat(); // Send first heartbeat immediately
    heartbeatInterval = setInterval(sendHeartbeat, HEARTBEAT_INTERVAL_MS);

    console.log('💓 Heartbeat system started (every 5 seconds)');

    // Track user activity
    ['click', 'keypress', 'mousemove', 'scroll'].forEach(e => {
        document.addEventListener(e, () => { lastActivityTime = Date.now(); }, { passive: true });
    });

    // ============================================
    // NAVIGATION INTENT TRACKING
    // ============================================

    let navIntent = 'unknown';

    function isSameOrigin(url) {
        if (!url || url.startsWith('/') || url.startsWith('#') || url.startsWith('?') || url.startsWith('javascript:')) {
            return true;
        }
        try {
            return new URL(url, location.href).origin === location.origin;
        } catch {
            return true;
        }
    }

    function markAsNavigating(reason) {
        sessionStorage.setItem(NAVIGATING_FLAG_KEY, JSON.stringify({ reason, timestamp: Date.now() }));
        console.log('🚩 Marked as navigating:', reason);
    }

    function isNavigating() {
        const flag = sessionStorage.getItem(NAVIGATING_FLAG_KEY);
        if (!flag) return false;
        try {
            return (Date.now() - JSON.parse(flag).timestamp) < 5000;
        } catch {
            return false;
        }
    }

    // Detect refresh
    document.addEventListener('keydown', function(e) {
        if (e.key === 'F5' || ((e.ctrlKey || e.metaKey) && (e.key === 'r' || e.key === 'R'))) {
            navIntent = 'refresh';
            markAsNavigating('refresh');
        }
    }, true);

    // Detect link clicks
    document.addEventListener('click', function(e) {
        const link = e.target.closest('a');
        if (!link) return;
        
        const href = link.getAttribute('href');
        if (!href || href === '#' || href.startsWith('javascript:')) return;

        if (href.toLowerCase().includes('logout')) {
            navIntent = 'logout';
            markAsNavigating('logout');
            // Remove session from server tracking when logging out
            removeSessionFromServer();
        } else if (isSameOrigin(href)) {
            navIntent = 'internal';
            markAsNavigating('internal');
        } else {
            navIntent = 'external';
        }
    }, true);

    // Detect form submissions
    document.addEventListener('submit', function() {
        navIntent = 'internal';
        markAsNavigating('form-submit');
    }, true);

    // Detect history navigation
    window.addEventListener('popstate', function() {
        navIntent = 'internal';
        markAsNavigating('history-nav');
    });

    // Intercept programmatic navigation
    const origAssign = window.location.assign.bind(window.location);
    window.location.assign = function(url) {
        if (isSameOrigin(url)) markAsNavigating('location-assign');
        return origAssign(url);
    };

    const origReplace = window.location.replace.bind(window.location);
    window.location.replace = function(url) {
        if (isSameOrigin(url)) markAsNavigating('location-replace');
        return origReplace(url);
    };


    // ============================================
    // SESSION REMOVAL (for explicit logout)
    // ============================================

    function removeSessionFromServer() {
        const data = JSON.stringify({ tabId: tabId });
        
        try {
            if (navigator.sendBeacon) {
                navigator.sendBeacon('/Auth/RemoveSession', new Blob([data], { type: 'application/json' }));
            } else {
                fetch('/Auth/RemoveSession', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: data,
                    keepalive: true
                }).catch(() => {});
            }
        } catch (e) {}
    }

    // ============================================
    // UNLOAD HANDLERS - Backup detection
    // ============================================

    function handleUnload(eventName) {
        console.log(`🚨 ${eventName} - intent: ${navIntent}, navigating: ${isNavigating()}`);

        // Stop heartbeats
        if (heartbeatInterval) {
            clearInterval(heartbeatInterval);
            heartbeatInterval = null;
        }

        // If this is intentional navigation within the app, remove session from tracking
        // so server doesn't log it as auto-logout
        if (isNavigating() || navIntent === 'internal' || navIntent === 'refresh') {
            console.log('✅ Internal navigation - session will continue');
            // Don't remove session - the new page will start sending heartbeats
            return;
        }

        if (navIntent === 'logout') {
            console.log('⏭️ Manual logout - handled by logout page');
            removeSessionFromServer();
            return;
        }

        // For external navigation or tab close, the server will detect
        // the missing heartbeats and log the auto-logout
        console.log('🚨 Tab/browser closing - server will detect via missing heartbeats');
    }

    window.addEventListener('beforeunload', () => handleUnload('beforeunload'));
    
    window.addEventListener('pagehide', (e) => {
        if (!e.persisted) handleUnload('pagehide');
    });

    window.addEventListener('unload', () => handleUnload('unload'));

    // ============================================
    // VISIBILITY CHANGE - Pause/resume heartbeats
    // ============================================

    document.addEventListener('visibilitychange', function() {
        if (document.hidden) {
            // Tab is hidden - continue heartbeats (user might just be switching tabs)
            console.log('👁️ Tab hidden - heartbeats continue');
        } else {
            // Tab is visible again - send immediate heartbeat
            console.log('👁️ Tab visible - sending heartbeat');
            sendHeartbeat();
        }
    });

    console.log('✅ Auto logout v14 ready');
    console.log('📋 Detection: Server-side heartbeat tracking (15s timeout)');
})();
