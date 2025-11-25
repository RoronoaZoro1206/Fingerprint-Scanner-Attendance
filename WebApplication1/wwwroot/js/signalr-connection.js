/**
 * SignalR Connection Manager
 * Manages real-time connection to AttendanceHub
 * Provides automatic reconnection and event handling
 */

/**
 * SignalR Connection Manager
 * ✅ ENHANCED: Now includes visual connection status indicator
 * Singleton pattern for managing SignalR connection across the application
 */

const signalRConnection = (() => {
    let connection = null;
    let reconnectAttempts = 0;
    const maxReconnectAttempts = 5;
    const statusIndicator = document.getElementById('signalr-status');

    /**
     * ✅ NEW: Show connection status in UI
     */
    function showStatus(message, type = 'connected') {
        if (!statusIndicator) return;

        statusIndicator.textContent = message;
        statusIndicator.className = `signalr-status ${type}`;

        // Auto-hide success messages after 3 seconds
        if (type === 'connected') {
            setTimeout(() => {
                statusIndicator.className = 'signalr-status';
                statusIndicator.textContent = '';
            }, 3000);
        }
    }

    /**
     * Initialize SignalR connection
     */
    async function initialize() {
        if (connection) {
            console.log('SignalR connection already initialized');
            return connection;
        }

        try {
            console.log('📡 SignalR connecting to /attendanceHub...');
            showStatus('Connecting...', 'reconnecting');

            connection = new signalR.HubConnectionBuilder()
                .withUrl('/attendanceHub')
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Connection event handlers
            connection.onreconnecting((error) => {
                console.warn('⚠️ SignalR reconnecting...', error);
                reconnectAttempts++;
                showStatus(`Reconnecting... (Attempt ${reconnectAttempts})`, 'reconnecting');
            });

            connection.onreconnected((connectionId) => {
                console.log('✅ SignalR reconnected:', connectionId);
                reconnectAttempts = 0;
                showStatus('Reconnected', 'connected');
            });

            connection.onclose((error) => {
                console.error('❌ SignalR connection closed:', error);
                
                if (reconnectAttempts >= maxReconnectAttempts) {
                    console.error('❌ Max reconnection attempts reached. Please refresh the page.');
                    showStatus('Connection lost - Please refresh', 'disconnected');
                } else {
                    showStatus('Disconnected', 'disconnected');
                }
            });

            // Start connection
            await connection.start();
            console.log('✅ SignalR connected successfully');
            showStatus('Connected', 'connected');

            return connection;
        } catch (error) {
            console.error('❌ SignalR connection failed:', error);
            showStatus('Connection failed', 'disconnected');
            throw error;
        }
    }

    /**
     * Listen for attendance updates (global broadcast)
     */
    function onAttendanceUpdate(callback) {
        if (!connection) {
            console.error('SignalR not initialized. Call initialize() first.');
            return;
        }

        connection.on('ReceiveAttendance', (data) => {
            console.log('📡 Received attendance update:', data);
            
            // ✅ Add pulse animation to status indicator
            if (statusIndicator) {
                statusIndicator.classList.add('pulse-animation');
                setTimeout(() => {
                    statusIndicator.classList.remove('pulse-animation');
                }, 500);
            }
            
            callback(data);
        });
    }

    /**
     * Listen for class-specific attendance updates
     */
    function onClassAttendanceUpdate(callback) {
        if (!connection) {
            console.error('SignalR not initialized. Call initialize() first.');
            return;
        }

        connection.on('ReceiveClassAttendance', (data) => {
            console.log('📡 Received class attendance update:', data);
            
            // ✅ Add pulse animation to status indicator
            if (statusIndicator) {
                statusIndicator.classList.add('pulse-animation');
                setTimeout(() => {
                    statusIndicator.classList.remove('pulse-animation');
                }, 500);
            }
            
            callback(data);
        });
    }

    /**
     * Join a class-specific SignalR group
     */
    async function joinClassGroup(classId) {
        if (!connection) {
            console.error('SignalR not initialized. Call initialize() first.');
            return false;
        }

        try {
            await connection.invoke('JoinClassGroup', classId);
            console.log(`✅ Joined class group: Class_${classId}`);
            return true;
        } catch (error) {
            console.error('Failed to join class group:', error);
            return false;
        }
    }

    /**
     * Leave a class-specific SignalR group
     */
    async function leaveClassGroup(classId) {
        if (!connection) {
            console.error('SignalR not initialized. Call initialize() first.');
            return false;
        }

        try {
            await connection.invoke('LeaveClassGroup', classId);
            console.log(`✅ Left class group: Class_${classId}`);
            return true;
        } catch (error) {
            console.error('Failed to leave class group:', error);
            return false;
        }
    }

    /**
     * Get current connection state
     */
    function getConnectionState() {
        if (!connection) {
            return {
                isConnected: false,
                connectionId: null,
                reconnectAttempts: 0,
                state: 'Not Initialized'
            };
        }

        return {
            isConnected: connection.state === signalR.HubConnectionState.Connected,
            connectionId: connection.connectionId,
            reconnectAttempts: reconnectAttempts,
            state: connection.state
        };
    }

    /**
     * Manually disconnect
     */
    async function disconnect() {
        if (connection) {
            await connection.stop();
            console.log('✅ SignalR disconnected');
            showStatus('Disconnected', 'disconnected');
        }
    }

    // Public API
    return {
        initialize,
        onAttendanceUpdate,
        onClassAttendanceUpdate,
        joinClassGroup,
        leaveClassGroup,
        getConnectionState,
        disconnect,
        showStatus // ✅ NEW: Expose for manual status updates
    };
})();

// ✅ Cleanup on page unload
window.addEventListener('beforeunload', async () => {
    await signalRConnection.disconnect();
});
