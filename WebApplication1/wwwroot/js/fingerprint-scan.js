// Modern Fingerprint Authentication JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const fingerprintScanner = document.getElementById('fingerprintScanner');
    const scanStatusMessage = document.getElementById('scanStatusMessage');

    let isScanning = false;
    let redirectTimeout = null;

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Check scanner status on load
    checkScannerStatus();

    // Click scanner to start authentication
  if (fingerprintScanner) {
   fingerprintScanner.addEventListener('click', async function () {
          if (isScanning) {
      showMessage('Please wait, scanning in progress...', 'info');
  return;
         }

      await authenticateWithFingerprint();
        });
    }

    // Check if scanner is available
    async function checkScannerStatus() {
        try {
      const response = await fetch('/FingerprintAuth/CheckScannerStatus');
            const result = await response.json();

            if (!result.isAvailable) {
           showMessage('Scanner not available. Please connect your fingerprint device.', 'error');
           fingerprintScanner.classList.add('offline');
            } else {
     showMessage('Ready to scan. Click the circle to begin authentication.', 'info');
   fingerprintScanner.classList.remove('offline');
      }
        } catch (error) {
   console.error('Error checking scanner status:', error);
            showMessage('Unable to connect to scanner. Please check your device.', 'error');
        }
    }

    // Authenticate with fingerprint
    async function authenticateWithFingerprint() {
        if (isScanning) return;

      try {
          isScanning = true;
        fingerprintScanner.classList.remove('success', 'error');
        fingerprintScanner.classList.add('scanning');
          showMessage('Please scan your fingerprint now...', 'scanning');

            const response = await fetch('/FingerprintAuth/AuthenticateWithFingerprint', {
        method: 'POST',
        headers: {
 'Content-Type': 'application/json'
  }
 });

     const result = await response.json();

   if (result.success) {
          // Success state
 fingerprintScanner.classList.remove('scanning');
          fingerprintScanner.classList.add('success');

          // Show success message
    showMessage(result.message || '✓ Scan successful! Redirecting to your account...', 'success');

           // Log successful login
           console.log('Successful fingerprint login:', result.userName);
    console.log('Match score:', result.matchScore);

                // Store login flag for fingerprint registration prompt
      sessionStorage.setItem('justLoggedIn', 'true');

     // Add smooth progress bar
   const progressBar = document.createElement('div');
        progressBar.className = 'redirect-progress';
      progressBar.innerHTML = '<div class="progress-bar-inner"></div>';
      scanStatusMessage.appendChild(progressBar);

    // Wait for full animation before redirect (4 seconds total)
       redirectTimeout = setTimeout(() => {
      console.log('Redirecting to:', result.redirectUrl);
        window.location.href = result.redirectUrl || '/Home/Index';
      }, 4000);
     } else {
  // Error state
              fingerprintScanner.classList.remove('scanning');
       fingerprintScanner.classList.add('error');

        // Show error message
                const errorMessage = result.message || '✗ Fingerprint not recognized. Please try again.';
      showMessage(errorMessage, 'error');

       // Reset to ready state after 4 seconds
 setTimeout(() => {
   fingerprintScanner.classList.remove('error');
  showMessage('Ready to scan. Click the circle to begin authentication.', 'info');
     }, 4000);
            }
  } catch (error) {
            console.error('Error during authentication:', error);
   fingerprintScanner.classList.remove('scanning');
 fingerprintScanner.classList.add('error');
   showMessage('✗ Connection error. Please try again.', 'error');

            setTimeout(() => {
       fingerprintScanner.classList.remove('error');
      showMessage('Ready to scan. Click the circle to begin authentication.', 'info');
       }, 4000);
        } finally {
         isScanning = false;
        }
    }

    // Show status message with smooth animations
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

   // Set message content
        scanStatusMessage.textContent = message;
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

        // Smooth fade-in
     scanStatusMessage.style.opacity = '0';
 scanStatusMessage.style.display = 'block';

      requestAnimationFrame(() => {
            scanStatusMessage.style.opacity = '1';
   });
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (redirectTimeout) {
         clearTimeout(redirectTimeout);
 }
    });
});

const style = document.createElement('style');
style.textContent = `
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
        background: #10b981;
 animation: progressAnimation 3.5s linear forwards;
    }
    
 @keyframes progressAnimation {
        from {
      width: 0%;
        }
    to {
       width: 100%;
        }
    }

    /* Enhanced message styling */
    .scan-status-message {
     transition: opacity 0.3s ease, transform 0.3s ease;
  padding: 1rem 1.5rem;
        border-radius: 12px;
        font-weight: 500;
        text-align: center;
    font-size: 1rem;
        line-height: 1.6;
   box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .scan-status-message.success {
  background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
   color: #065f46;
      border: 2px solid #10b981;
      font-weight: 600;
 font-size: 1.1rem;
    }

    .scan-status-message.error {
        background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
  color: #991b1b;
        border: 2px solid #ef4444;
        font-weight: 600;
        animation: errorPulse 0.5s ease-in-out;
    }

    .scan-status-message.info {
background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
        color: #1e40af;
        border: 2px solid #3b82f6;
    }

    @keyframes errorPulse {
        0%, 100% {
            transform: scale(1);
 }
        50% {
            transform: scale(1.02);
        }
    }

    /* Scanner circle enhancements */
    .scanner-circle {
        position: relative;
    }

    .scanner-circle.scanning::before {
        content: '';
    position: absolute;
   inset: -5px;
        border-radius: 50%;
        background: conic-gradient(from 0deg, transparent, #3b82f6, transparent);
      animation: rotate 1.5s linear infinite;
        z-index: -1;
    }

    @keyframes rotate {
        from {
 transform: rotate(0deg);
      }
        to {
            transform: rotate(360deg);
        }
    }
`;
document.head.appendChild(style);
