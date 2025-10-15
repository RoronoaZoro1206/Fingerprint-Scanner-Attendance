/**
 * Student Register Fingerprint
 */

let isScanning = false;
let scanCount = 0;
const requiredScans = 3;

$(document).ready(function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Event handlers
    $('#fingerprintScanner').on('click', handleScannerClick);
    $('#btnScan').on('click', handleScannerClick);
    $('#btnRegister').on('click', handleRegister);
    $('#btnCancel').on('click', handleCancel);
    $('#btnConfirmCancel').on('click', confirmCancel);

    // Modal events - reinitialize icons when modal is shown
    $('#cancelModal').on('shown.bs.modal', function () {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    });

    // Disable register button initially
    $('#btnRegister').prop('disabled', true);
});

/**
 * Handle scanner click
 */
function handleScannerClick(e) {
    if (isScanning) return;

    const $scanner = $('#fingerprintScanner');
    
    // Create ripple effect
    if (e.clientX) {
        createRipple(e, $scanner);
    }

    // Start scan
    startScan();
}

/**
 * Create ripple effect
 */
function createRipple(e, $element) {
    const $ripple = $('<span class="ripple"></span>');
    const rect = $element[0].getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = e.clientX - rect.left - size / 2;
    const y = e.clientY - rect.top - size / 2;

    $ripple.css({
        width: size + 'px',
        height: size + 'px',
        left: x + 'px',
        top: y + 'px'
    });

    $element.append($ripple);

    setTimeout(() => {
        $ripple.remove();
    }, 600);
}

/**
 * Start scan
 */
function startScan() {
    isScanning = true;
    const $scanner = $('#fingerprintScanner');
    const $btnScan = $('#btnScan');

    // Update UI
    $scanner.removeClass('success error').addClass('scanning');
    $btnScan.prop('disabled', true).text('Scanning...');

    // Simulate scan (2 seconds)
    setTimeout(() => {
        completeScan();
    }, 2000);
}

/**
 * Complete scan
 */
function completeScan() {
    const $scanner = $('#fingerprintScanner');
    const $btnScan = $('#btnScan');

    // 90% success rate
    const isSuccess = Math.random() > 0.1;

    if (isSuccess) {
        scanCount++;
        $scanner.removeClass('scanning').addClass('success');
        
        if (scanCount >= requiredScans) {
            Toast.success('All scans completed!');
            $btnScan.text('Scan Complete');
            $('#btnRegister').prop('disabled', false);
        } else {
            Toast.success(`Scan ${scanCount}/${requiredScans} complete`);
            $btnScan.prop('disabled', false).text('Scan Fingerprint');
        }

        setTimeout(() => {
            $scanner.removeClass('success');
        }, 1000);
    } else {
        $scanner.removeClass('scanning').addClass('error');
        Toast.error('Scan failed. Please try again.');
        $btnScan.prop('disabled', false).text('Scan Fingerprint');

        setTimeout(() => {
            $scanner.removeClass('error');
        }, 1000);
    }

    isScanning = false;
}

/**
 * Handle register
 */
function handleRegister() {
    if (scanCount < requiredScans) {
        Toast.error(`Please complete ${requiredScans} scans first.`);
        return;
    }

    const $btn = $('#btnRegister');
    const originalHtml = $btn.html();
    
    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Registering...');

    // Simulate registration
    setTimeout(() => {
        Toast.success('Fingerprint registered successfully!');
        
        // Reset
        scanCount = 0;
        $('#fingerprintScanner').removeClass('scanning success error');
        $('#btnScan').prop('disabled', false).text('Scan Fingerprint');
        $btn.prop('disabled', true).html(originalHtml);
        
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }, 2000);
}

/**
 * Handle cancel - show confirmation modal
 */
function handleCancel() {
    // Show the confirmation modal
    $('#cancelModal').modal('show');
}

/**
 * Confirm cancel - reset the registration
 */
function confirmCancel() {
    scanCount = 0;
    isScanning = false;
    $('#fingerprintScanner').removeClass('scanning success error');
    $('#btnScan').prop('disabled', false).text('Scan Fingerprint');
    $('#btnRegister').prop('disabled', true);
    
    // Hide the modal
    $('#cancelModal').modal('hide');
    
    Toast.info('Registration cancelled');
}
