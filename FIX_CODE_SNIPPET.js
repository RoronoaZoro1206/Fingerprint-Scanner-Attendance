// ✅ ENHANCED FIX: Replace the swalOptions object in showSweetAlertNotification()
// Location: Line ~280-295 in home-attendance-scanner.js

// REPLACE THIS:
const swalOptions = {
    willOpen: () => {
        if (statusMessageElement && statusMessageElement.classList.contains('show')) {
            console.log('✅ SweetAlert2 opened - changing status message background to white');
            statusMessageElement.classList.add('swal-active');
        }
    },
    didClose: () => {
        if (statusMessageElement && statusMessageElement.classList.contains('show')) {
            console.log('✅ SweetAlert2 closed - restoring status message green background');
            statusMessageElement.classList.remove('swal-active');
            
            setTimeout(() => {
                statusMessageElement.classList.remove('success', 'error', 'warning', 'scanning', 'validating');
                statusMessageElement.classList.add('info');
                statusMessageElement.innerHTML = 'Scanner ready. Place finger to scan.';
            }, 100);
        }
    }
};

// WITH THIS (ENHANCED VERSION):
const swalOptions = {
    willOpen: () => {
        // ✅ Change background to white when SweetAlert2 modal opens
        if (statusMessageElement && statusMessageElement.classList.contains('show')) {
            console.log('✅ SweetAlert2 opened - changing status message background to white');
            statusMessageElement.classList.add('swal-active');
        }
    },
    didClose: () => {
        // ✅ ENHANCED: Multi-layer validation before restoring green background
        // Layer 1: Extended delay to ensure SweetAlert2 cleanup is complete
        setTimeout(() => {
            if (!statusMessageElement || !statusMessageElement.classList.contains('show')) {
                return;
            }
            
            console.log('✅ SweetAlert2 closed - validating before restoring green background');
            
            // Layer 2: Check DOM for any visible SweetAlert2 modals
            const visibleSwalPopup = document.querySelector('.swal2-popup:not([style*="display: none"])');
            const visibleSwalContainer = document.querySelector('.swal2-container:not(.swal2-container-hidden)');
            const swalShown = document.querySelector('.swal2-shown');
            
            // Layer 3: Check if SweetAlert2 is animating
            const isAnimating = document.querySelector('.swal2-animate-show-class') ||
                               document.querySelector('.swal2-animate-hide-class') ||
                               document.querySelector('.swal2-show') ||
                               document.querySelector('.swal2-hide');
            
            // Layer 4: Check SweetAlert2 internal state
            const swalIsOpen = window.Swal && typeof window.Swal.isVisible === 'function' && window.Swal.isVisible();
            
            // Log validation results for debugging
            console.log('Validation checks:', {
                visiblePopup: !!visibleSwalPopup,
                visibleContainer: !!visibleSwalContainer,
                swalShown: !!swalShown,
                isAnimating: !!isAnimating,
                swalIsOpen: !!swalIsOpen
            });
            
            // ✅ CRITICAL: Only restore green if ALL checks confirm no alert is visible
            const isAnySwalVisible = visibleSwalPopup || visibleSwalContainer || swalShown || isAnimating || swalIsOpen;
            
            if (!isAnySwalVisible) {
                console.log('✅ All checks passed - restoring green background');
                statusMessageElement.classList.remove('swal-active');
                
                // ✅ ADDITIONAL FIX: Force restore "ready" state with green background
                // Wait additional 50ms to ensure smooth transition after modal closes
                setTimeout(() => {
                    if (statusMessageElement.classList.contains('show')) {
                        statusMessageElement.classList.remove('success', 'error', 'warning', 'scanning', 'validating');
                        statusMessageElement.classList.add('info');
                        statusMessageElement.innerHTML = 'Scanner ready. Place finger to scan.';
                    }
                }, 50);
            } else {
                console.warn('⚠️ SweetAlert2 still visible - keeping white background');
            }
        }, 300); // Increased delay from 100ms to 300ms for better reliability
    }
};
