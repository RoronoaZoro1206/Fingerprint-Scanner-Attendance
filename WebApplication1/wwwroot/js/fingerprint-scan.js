// Fingerprint scanning functionality
document.addEventListener('DOMContentLoaded', function() {
    const startScanBtn = document.getElementById('startScanBtn');
    const scannerCircle = document.querySelector('.scanner-circle');
    const scannerInstruction = document.querySelector('.scanner-instruction');

    if (startScanBtn && scannerCircle) {
        startScanBtn.addEventListener('click', function() {
            // Disable button during scan
            startScanBtn.disabled = true;
            startScanBtn.textContent = 'Scanning...';

            // Add scanning animation
            scannerCircle.classList.add('scanning');
            scannerInstruction.textContent = 'Scanning in progress...';

            // Simulate fingerprint scan (replace with actual biometric API)
            setTimeout(function() {
                // Simulate successful scan
                scannerCircle.classList.remove('scanning');
                scannerInstruction.textContent = 'Scan complete!';
                startScanBtn.textContent = 'Scan Complete';

                // Redirect or handle authentication
                setTimeout(function() {
                    // Replace with actual authentication logic
                    console.log('Fingerprint authenticated');
                    // window.location.href = '/Dashboard';
                }, 1000);
            }, 3000);
        });
    }
});
