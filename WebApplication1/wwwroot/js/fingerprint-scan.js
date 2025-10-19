// Fingerprint scanning functionality
document.addEventListener('DOMContentLoaded', function() {
    const fingerprintScanner = document.getElementById('fingerprintScanner');
    const scanStatusMessage = document.getElementById('scanStatusMessage');
    let isScanning = false;

    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    if (fingerprintScanner) {
        // Add click event to scanner circle
        fingerprintScanner.addEventListener('click', function(e) {
            if (isScanning) return;

            // Create ripple effect
            createRipple(e, this);

            // Start scanning
            startScan();
        });
    }

    function createRipple(event, element) {
        const ripple = document.createElement('span');
        ripple.classList.add('ripple');
        
        const rect = element.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = event.clientX - rect.left - size / 2;
        const y = event.clientY - rect.top - size / 2;
        
        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = x + 'px';
        ripple.style.top = y + 'px';
        
        element.appendChild(ripple);
        
        setTimeout(() => ripple.remove(), 600);
    }

    function startScan() {
        isScanning = true;

        // Add scanning state
        fingerprintScanner.classList.add('scanning');
        showStatusMessage('Scanning fingerprint...', 'scanning');

        // Simulate fingerprint scan (replace with actual biometric API)
        setTimeout(function() {
            // Simulate random success/failure for demo
            const isSuccess = Math.random() > 0.3; // 70% success rate

            fingerprintScanner.classList.remove('scanning');

            if (isSuccess) {
                // Success state
                fingerprintScanner.classList.add('success');
                showStatusMessage('Fingerprint recognized! Logging in...', 'success');

                // Redirect after success
                setTimeout(function() {
                    // Replace with actual authentication logic
                    console.log('Fingerprint authenticated');
                    // window.location.href = '/Dashboard';
                    
                    // Reset for demo purposes
                    resetScanner();
                }, 2000);
            } else {
                // Error state
                fingerprintScanner.classList.add('error');
                showStatusMessage('Fingerprint not recognized. Please try again.', 'error');

                // Reset after error
                setTimeout(function() {
                    resetScanner();
                }, 2000);
            }
        }, 2500);
    }

    function showStatusMessage(message, type) {
        scanStatusMessage.textContent = message;
        scanStatusMessage.className = 'scan-status-message show ' + type;
    }

    function resetScanner() {
        fingerprintScanner.classList.remove('scanning', 'success', 'error');
        scanStatusMessage.classList.remove('show');
        isScanning = false;
    }
});
