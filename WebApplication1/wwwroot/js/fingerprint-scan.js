// Modern Fingerprint Authentication JavaScript
// Enhanced with automatic fingerprint detection and Bootstrap Icons
document.addEventListener('DOMContentLoaded', function () {
 const fingerprintScanner = document.getElementById('fingerprintScanner');
  const scanStatusMessage = document.getElementById('scanStatusMessage');

    let isScanning = false;
    let redirectTimeout = null;
    let deviceInitialized = false;
    let autoScanInterval = null;

  // Initialize Lucide icons
  if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Check scanner status and initialize device on load
    initializeDeviceAndCheck();

    // Click scanner to start authentication (manual trigger)
    if (fingerprintScanner) {
      fingerprintScanner.addEventListener('click', async function () {
          if (isScanning) {
    showMessage('<i class="bi bi-info-circle me-2"></i>Please wait, scanning in progress...', 'info');
   return;
            }

 await authenticateWithFingerprint();
        });
    }

 // Initialize device and check status
    async function initializeDeviceAndCheck() {
 try {
  showMessage('<i class="bi bi-hourglass-split me-2"></i>Initializing fingerprint scanner...', 'info');
   
          // First, check if device is available
     const statusResponse = await fetch('/FingerprintAuth/CheckScannerStatus');
     const statusResult = await statusResponse.json();

    if (statusResult.isAvailable) {
    deviceInitialized = true;
 fingerprintScanner.classList.remove('offline');
       showMessage('<i class="bi bi-check-circle me-2"></i>Scanner ready. Place your finger on the scanner to login.', 'success');
      
  // Add pulse animation to indicate readiness
         fingerprintScanner.style.animation = 'pulse 2s infinite';
            
       // Start automatic fingerprint detection
            startAutoDetection();
    } else {
     fingerprintScanner.classList.add('offline');
   showMessage('<i class="bi bi-exclamation-triangle me-2"></i>Scanner not available. Please connect your fingerprint device.', 'error');
      }
   } catch (error) {
            console.error('Error initializing device:', error);
        fingerprintScanner.classList.add('offline');
  showMessage('<i class="bi bi-exclamation-triangle me-2"></i>Unable to connect to scanner. Please check your device.', 'error');
        }
    }

    // NEW: Automatic fingerprint detection
    function startAutoDetection() {
  // Poll every 500ms to check if finger is on scanner
    autoScanInterval = setInterval(async () => {
      if (!isScanning && deviceInitialized) {
      // Check if finger is present and auto-scan
       await tryAutoAuthenticate();
     }
        }, 500);
    }

    // NEW: Try to authenticate automatically when finger detected
    async function tryAutoAuthenticate() {
        if (isScanning) return;
   
    // Attempt authentication (will succeed if finger is on scanner)
  await authenticateWithFingerprint(true); // Pass true for auto-scan
    }

    // Authenticate with fingerprint (enhanced for auto-detection)
    async function authenticateWithFingerprint(isAutoScan = false) {
      if (isScanning) return;

        if (!deviceInitialized) {
         if (!isAutoScan) {
    showMessage('<i class="bi bi-exclamation-triangle me-2"></i>Scanner not ready. Please wait...', 'error');
              await initializeDeviceAndCheck();
         }
  return;
     }

        try {
     isScanning = true;
     
       // Remove previous states
          fingerprintScanner.classList.remove('success', 'error', 'offline');
        fingerprintScanner.classList.add('scanning');
 fingerprintScanner.style.animation = 'none';
 
   // Show scanning message
            if (!isAutoScan) {
    showMessage('<i class="bi bi-fingerprint me-2"></i>Scanning fingerprint... Please hold steady.', 'scanning');
   }

   // Call authentication endpoint
   const response = await fetch('/FingerprintAuth/AuthenticateWithFingerprint', {
        method: 'POST',
       headers: {
    'Content-Type': 'application/json'
   }
       });

     const result = await response.json();

       if (result.success) {
 // Stop auto-detection on success
            if (autoScanInterval) {
       clearInterval(autoScanInterval);
   autoScanInterval = null;
      }

            // SUCCESS - Fingerprint matched in database
         fingerprintScanner.classList.remove('scanning');
fingerprintScanner.classList.add('success');

      // Show success message with user info
    const userName = result.userName || 'User';
         const matchScore = result.matchScore || 'High';
             showMessage(
     `<i class="bi bi-check-circle-fill me-2"></i>Login successful! Welcome ${userName}.<br>Match Score: ${matchScore}<br>Redirecting to your dashboard...`, 
    'success'
    );

     // Log successful login
    console.log('✅ Successful fingerprint authentication:', {
   userName: result.userName,
       matchScore: result.matchScore,
  redirectUrl: result.redirectUrl
          });

     // Store login flag for fingerprint registration prompt
 sessionStorage.setItem('justLoggedIn', 'true');

     // Add smooth progress bar
      const progressBar = document.createElement('div');
         progressBar.className = 'redirect-progress';
      progressBar.innerHTML = '<div class="progress-bar-inner"></div>';
        scanStatusMessage.appendChild(progressBar);

     // Wait for animation to complete before redirect
    redirectTimeout = setTimeout(() => {
   console.log('Redirecting to:', result.redirectUrl);
   window.location.href = result.redirectUrl || '/Home/Index';
           }, 3000);

      } else {
   // FAILURE - Check if it's "no finger detected" vs "not recognized"
 if (result.message && result.message.includes('No fingerprint detected')) {
     // Finger not on scanner - don't show error, just continue auto-detection
      if (!isAutoScan) {
         showMessage('<i class="bi bi-hand-index me-2"></i>Please place your finger on the scanner.', 'info');
              }
         fingerprintScanner.classList.remove('scanning');
       fingerprintScanner.style.animation = 'pulse 2s infinite';
  } else {
      // Fingerprint not recognized - show error
       fingerprintScanner.classList.remove('scanning');
       fingerprintScanner.classList.add('error');

 const errorMessage = result.message || '<i class="bi bi-x-circle-fill me-2"></i>Fingerprint not recognized. This fingerprint is not registered in the system.';
     showMessage(errorMessage, 'error');

          console.error('❌ Authentication failed:', result.message);

           // Reset to ready state after 4 seconds
   setTimeout(() => {
     fingerprintScanner.classList.remove('error');
  fingerprintScanner.style.animation = 'pulse 2s infinite';
     showMessage('<i class="bi bi-hand-index me-2"></i>Ready to scan. Place your finger on the scanner.', 'info');
  }, 4000);
      }
         }

        } catch (error) {
            // CONNECTION ERROR - only show if not auto-scan
 if (!isAutoScan) {
          console.error('Error during authentication:', error);
     fingerprintScanner.classList.remove('scanning');
          fingerprintScanner.classList.add('error');
         showMessage('<i class="bi bi-exclamation-octagon me-2"></i>Connection error. Please try again.', 'error');

      setTimeout(() => {
         fingerprintScanner.classList.remove('error');
    fingerprintScanner.style.animation = 'pulse 2s infinite';
               showMessage('<i class="bi bi-hand-index me-2"></i>Ready to scan. Place your finger on the scanner.', 'info');
  }, 4000);
            } else {
          // Silent fail for auto-scan
       fingerprintScanner.classList.remove('scanning');
       fingerprintScanner.style.animation = 'pulse 2s infinite';
    }

   } finally {
        isScanning = false;
     }
    }

    // Show status message with smooth animations (enhanced)
    function showMessage(message, type = 'info') {
        if (!scanStatusMessage) return;

   // Clear any existing redirect timeout when showing new message
    if (type !== 'success' && redirectTimeout) {
            clearTimeout(redirectTimeout);
            redirectTimeout = null;
        }

        // Remove any existing progress bar
     const existingProgress = scanStatusMessage.querySelector('.redirect-progress');
        if (existingProgress) {
            existingProgress.remove();
        }

        // Set message content using innerHTML to properly render HTML/icons
   scanStatusMessage.innerHTML = message;
        scanStatusMessage.className = 'scan-status-message show';

 // Apply appropriate styling
        if (type === 'success') {
   scanStatusMessage.classList.add('success');
        } else if (type === 'error') {
        scanStatusMessage.classList.add('error');
        } else if (type === 'scanning') {
 scanStatusMessage.classList.add('scanning');
     } else {
            scanStatusMessage.classList.add('info');
        }

     // Smooth fade-in with scale animation
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
      if (redirectTimeout) {
     clearTimeout(redirectTimeout);
 }
    if (autoScanInterval) {
   clearInterval(autoScanInterval);
        }
    });
});

// Enhanced styles for smooth authentication experience
const style = document.createElement('style');
style.textContent = `
    /* Progress bar for redirect */
    .redirect-progress {
        width: 100%;
  height: 4px;
    background: rgba(16, 185, 129, 0.2);
   border-radius: 2px;
        margin-top: 0.75rem;
        overflow: hidden;
    }
    
    .progress-bar-inner {
  width: 0%;
  height: 100%;
      background: linear-gradient(90deg, #10b981, #059669);
    animation: progressAnimation 2.8s ease-in-out forwards;
        box-shadow: 0 0 10px rgba(16, 185, 129, 0.5);
    }
    
    @keyframes progressAnimation {
 from { width: 0%; }
        to { width: 100%; }
    }

  /* Enhanced message styling */
  .scan-status-message {
     transition: all 0.3s ease;
  padding: 1rem 1.5rem;
   border-radius: 12px;
  font-weight: 500;
      text-align: center;
     font-size: 1rem;
        line-height: 1.6;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

    .scan-status-message.success {
        background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
        color: #065f46;
        border: 2px solid #10b981;
        font-weight: 600;
     font-size: 1.1rem;
        animation: successPulse 0.6s ease-in-out;
    }

    .scan-status-message.error {
        background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
   color: #991b1b;
        border: 2px solid #ef4444;
  font-weight: 600;
   animation: errorShake 0.5s ease-in-out;
    }

    .scan-status-message.scanning {
  background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
        color: #1e40af;
        border: 2px solid #3b82f6;
     animation: scanningPulse 1.5s ease-in-out infinite;
    }

    .scan-status-message.info {
    background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
        color: #3730a3;
        border: 2px solid #6366f1;
    }

    @keyframes successPulse {
 0%, 100% { transform: scale(1); }
50% { transform: scale(1.05); }
  }

    @keyframes errorShake {
     0%, 100% { transform: translateX(0); }
        25% { transform: translateX(-10px); }
        75% { transform: translateX(10px); }
    }

    @keyframes scanningPulse {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.8; }
    }

    /* Scanner circle enhancements */
    .scanner-circle {
        position: relative;
  transition: all 0.3s ease;
    }

    /* Scanning state - rotating gradient */
    .scanner-circle.scanning::before {
      content: '';
     position: absolute;
  inset: -5px;
      border-radius: 50%;
        background: conic-gradient(from 0deg, transparent, #3b82f6, transparent);
     animation: rotate 1.5s linear infinite;
        z-index: -1;
    }

  /* Success state - green glow */
    .scanner-circle.success {
        background: linear-gradient(135deg, #d1fae5, #a7f3d0);
      border-color: #10b981;
        box-shadow: 0 0 20px rgba(16, 185, 129, 0.6);
    }

    /* Error state - red glow */
    .scanner-circle.error {
 background: linear-gradient(135deg, #fee2e2, #fecaca);
        border-color: #ef4444;
        box-shadow: 0 0 20px rgba(239, 68, 68, 0.6);
    }

    /* Offline state - gray */
    .scanner-circle.offline {
   opacity: 0.5;
        cursor: not-allowed;
    }

    /* Ready pulse animation */
 @keyframes pulse {
   0%, 100% {
     box-shadow: 0 0 0 0 rgba(59, 130, 246, 0.4);
        }
        50% {
      box-shadow: 0 0 0 10px rgba(59, 130, 246, 0);
        }
    }

    @keyframes rotate {
        from { transform: rotate(0deg); }
   to { transform: rotate(360deg); }
    }

    /* Bootstrap Icons spacing */
    .bi {
        display: inline-block;
        vertical-align: middle;
    }
`;
document.head.appendChild(style);
