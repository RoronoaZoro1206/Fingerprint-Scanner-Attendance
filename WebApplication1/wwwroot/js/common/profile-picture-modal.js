/**
 * Profile Picture Upload Modal - Dual Option
 * Handles both local file upload and server image selection
 */

class ProfilePictureModal {
    constructor(modalId, previewElementId, onImageSelected) {
        this.modalId = modalId;
        this.previewElementId = previewElementId;
        this.onImageSelected = onImageSelected; // Callback function when image is selected
        this.currentTab = 'upload'; // 'upload' or 'server'
        this.selectedFile = null;
        this.selectedServerImage = null;
        this.serverImages = [];
        
        this.initModal();
    }

    /**
     * Initialize the modal HTML
     */
    initModal() {
        const modalHtml = `
            <div class="modal fade" id="${this.modalId}" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog image-selection-modal">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Select Profile Picture</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <!-- Tabs -->
                            <div class="image-source-tabs">
                                <button class="image-source-tab active" data-tab="upload">
                                    <i data-lucide="upload"></i>
                                    <span>Upload from Device</span>
                                </button>
                                <button class="image-source-tab" data-tab="server">
                                    <i data-lucide="folder-open"></i>
                                    <span>Choose from Gallery</span>
                                </button>
                            </div>

                            <!-- Upload Tab Content -->
                            <div class="tab-content-section active" data-content="upload">
                                <!-- ✅ FIX: Hidden file input at the top level for easier access -->
                                <input type="file" id="${this.modalId}-file-input" accept="image/jpeg,image/jpg,image/png,image/gif" style="display: none;">
                                
                                <div class="upload-area" id="${this.modalId}-upload-area" tabindex="0" role="button" aria-label="Click to select or drag and drop an image">
                                    <i data-lucide="image-plus" class="upload-icon"></i>
                                    <h3>Drop image here or click to browse</h3>
                                    <p>Select an image from your computer</p>
                                </div>
                                
                                <div class="upload-preview" id="${this.modalId}-upload-preview">
                                    <img id="${this.modalId}-preview-img" src="" alt="Preview">
                                    <div class="upload-preview-actions">
                                        <button type="button" class="btn btn-sm btn-secondary" id="${this.modalId}-change-file">
                                            <i data-lucide="rotate-ccw" class="icon-sm"></i>
                                            Change File
                                        </button>
                                    </div>
                                </div>

                                <div class="file-requirements">
                                    <h4>Requirements:</h4>
                                    <ul>
                                        <li>File types: JPG, PNG, GIF</li>
                                        <li>Maximum size: 5MB</li>
                                        <li>Recommended: Square images (1:1 aspect ratio)</li>
                                    </ul>
                                </div>
                            </div>

                            <!-- Server Images Tab Content -->
                            <div class="tab-content-section" data-content="server">
                                <div class="server-images-search">
                                    <div class="search-icon-wrapper">
                                        <i data-lucide="search" class="search-icon"></i>
                                        <input type="text" class="search-input" id="${this.modalId}-search-input" placeholder="Search images...">
                                    </div>
                                </div>
                                
                                <div class="loading-images" id="${this.modalId}-loading" style="display: none;">
                                    <div class="loading-spinner"></div>
                                    <p>Loading images...</p>
                                </div>

                                <div class="server-images-grid" id="${this.modalId}-server-images"></div>
                                
                                <div class="no-images-message" id="${this.modalId}-no-images" style="display: none;">
                                    <i data-lucide="image-off" class="no-images-icon"></i>
                                    <p>No images found</p>
                                </div>

                                <div class="selected-image-name" id="${this.modalId}-selected-name" style="display: none;"></div>
                            </div>
                        </div>
                        <div class="modal-footer modal-footer-actions">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary" id="${this.modalId}-confirm-btn" disabled>
                                <i data-lucide="check" class="icon-sm"></i>
                                Set as Profile Picture
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal if present
        $(`#${this.modalId}`).remove();
        
        // Append to body
        $('body').append(modalHtml);
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        this.attachEventListeners();
    }

    /**
     * Attach event listeners
     */
    attachEventListeners() {
        const self = this;

        // Tab switching
        $(`#${this.modalId} .image-source-tab`).on('click', function() {
            const tab = $(this).data('tab');
            self.switchTab(tab);
        });

        // ✅ FIX: Multiple event handlers for upload area click to ensure compatibility
        // Method 1: jQuery click handler
        $(`#${this.modalId}-upload-area`).on('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('[Click Method 1] Upload area clicked via jQuery');
            self.triggerFileInput();
        });

        // ✅ FIX: Method 2: Native DOM click handler (backup)
        const uploadArea = document.getElementById(`${this.modalId}-upload-area`);
        if (uploadArea) {
            uploadArea.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                console.log('[Click Method 2] Upload area clicked via native DOM');
                self.triggerFileInput();
            }, { passive: false });
        }

        // ✅ FIX: Method 3: Keyboard accessibility (Enter/Space)
        $(`#${this.modalId}-upload-area`).on('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                e.stopPropagation();
                console.log('[Keyboard] Upload area activated via keyboard');
                self.triggerFileInput();
            }
        });

        // ✅ FIX: File input change handler with comprehensive logging
        $(`#${this.modalId}-file-input`).on('change', function(e) {
            console.log('[File Input] Change event triggered');
            console.log('[File Input] Files selected:', e.target.files.length);
            
            if (e.target.files && e.target.files.length > 0) {
                const file = e.target.files[0];
                console.log('[File Input] File details:', {
                    name: file.name,
                    type: file.type,
                    size: file.size,
                    lastModified: new Date(file.lastModified).toISOString()
                });
                self.handleFileSelect(file);
            } else {
                console.warn('[File Input] No files selected or file selection cancelled');
            }
        });

        // ✅ FIX: Change file button with enhanced functionality
        $(`#${this.modalId}-change-file`).on('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('[Change File] Button clicked');
            self.triggerFileInput();
        });

        // ✅ FIX: Improved drag and drop handlers
        if (uploadArea) {
            // Prevent default drag behaviors on the entire modal
            uploadArea.addEventListener('dragenter', (e) => {
                e.preventDefault();
                e.stopPropagation();
                $(uploadArea).addClass('dragging');
                console.log('[Drag] Enter event');
            });

            uploadArea.addEventListener('dragover', (e) => {
                e.preventDefault();
                e.stopPropagation();
                $(uploadArea).addClass('dragging');
            });

            uploadArea.addEventListener('dragleave', (e) => {
                e.preventDefault();
                e.stopPropagation();
                // Only remove dragging class if leaving the upload area itself (not child elements)
                if (e.target === uploadArea) {
                    $(uploadArea).removeClass('dragging');
                    console.log('[Drag] Leave event');
                }
            });

            uploadArea.addEventListener('drop', (e) => {
                e.preventDefault();
                e.stopPropagation();
                $(uploadArea).removeClass('dragging');
                console.log('[Drop] File(s) dropped');
                
                const files = e.dataTransfer?.files;
                if (files && files.length > 0) {
                    const file = files[0];
                    console.log('[Drop] Processing dropped file:', file.name);
                    self.handleFileSelect(file);
                } else {
                    console.warn('[Drop] No valid files in drop event');
                }
            });
        }

        // Server image search
        $(`#${this.modalId}-search-input`).on('input', function() {
            self.filterServerImages($(this).val());
        });

        // Confirm button
        $(`#${this.modalId}-confirm-btn`).on('click', function() {
            self.confirmSelection();
        });

        // Modal hidden event - reset state
        $(`#${this.modalId}`).on('hidden.bs.modal', function() {
            self.resetModal();
        });

        // Modal shown event - load server images
        $(`#${this.modalId}`).on('shown.bs.modal', function() {
            console.log('[Modal] Shown, current tab:', self.currentTab);
            // Reinitialize icons after modal is shown
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
            // Load server images if on server tab and not loaded yet
            if (self.currentTab === 'server' && self.serverImages.length === 0) {
                self.loadServerImages();
            }
        });
    }

    /**
     * ✅ NEW: Centralized function to trigger file input
     * This ensures consistent behavior across all trigger methods
     */
    triggerFileInput() {
        console.log('[Trigger] Attempting to open file input dialog');
        
        const fileInput = document.getElementById(`${this.modalId}-file-input`);
        
        if (!fileInput) {
            console.error('[Trigger] File input element not found!');
            return;
        }

        try {
            // Reset the file input value to allow selecting the same file again
            fileInput.value = '';
            
            // Trigger the click event
            console.log('[Trigger] Triggering click on file input element');
            fileInput.click();
            
            console.log('[Trigger] File input click triggered successfully');
        } catch (error) {
            console.error('[Trigger] Error triggering file input:', error);
            Toast.error('Failed to open file selector. Please try again.');
        }
    }

    /**
     * Switch between tabs
     */
    switchTab(tab) {
        console.log('[Tab] Switching to:', tab);
        this.currentTab = tab;

        // Update tab buttons
        $(`#${this.modalId} .image-source-tab`).removeClass('active');
        $(`#${this.modalId} .image-source-tab[data-tab="${tab}"]`).addClass('active');

        // Update tab content
        $(`#${this.modalId} .tab-content-section`).removeClass('active');
        $(`#${this.modalId} .tab-content-section[data-content="${tab}"]`).addClass('active');

        // Update confirm button state
        this.updateConfirmButton();

        // Load server images if switching to server tab
        if (tab === 'server' && this.serverImages.length === 0) {
            this.loadServerImages();
        }

        // Reinitialize icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    /**
     * Handle file selection from local device
     */
    handleFileSelect(file) {
        console.log('[File Select] Processing file:', file);
        
        if (!file) {
            console.warn('[File Select] No file provided');
            return;
        }

        console.log('[File Select] File details:', {
            name: file.name,
            type: file.type,
            size: `${(file.size / 1024 / 1024).toFixed(2)} MB`,
            lastModified: new Date(file.lastModified).toLocaleString()
        });

        // Validate file type
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        if (!allowedTypes.includes(file.type)) {
            console.error('[File Select] Invalid file type:', file.type);
            Toast.error('Invalid file type. Only JPG, PNG, and GIF are allowed.');
            return;
        }

        // Validate file size (5MB)
        const maxSize = 5 * 1024 * 1024; // 5MB in bytes
        if (file.size > maxSize) {
            console.error('[File Select] File too large:', file.size, 'bytes');
            Toast.error(`File size must be less than 5MB. Your file is ${(file.size / 1024 / 1024).toFixed(2)}MB`);
            return;
        }

        console.log('[File Select] ✅ File validation passed');
        this.selectedFile = file;
        this.selectedServerImage = null;

        // Preview image
        const reader = new FileReader();
        
        reader.onload = (e) => {
            console.log('[File Select] FileReader loaded successfully');
            $(`#${this.modalId}-preview-img`).attr('src', e.target.result);
            $(`#${this.modalId}-upload-preview`).addClass('show');
            $(`#${this.modalId}-upload-area`).hide();
            console.log('[File Select] Preview displayed');
        };
        
        reader.onerror = (e) => {
            console.error('[File Select] FileReader error:', e);
            Toast.error('Failed to read file. Please try again.');
        };
        
        reader.onloadstart = () => {
            console.log('[File Select] FileReader started reading file');
        };
        
        reader.readAsDataURL(file);

        this.updateConfirmButton();
    }

    /**
     * Load available images from server
     */
    async loadServerImages() {
        const self = this;
        console.log('[Server Images] Loading from server...');
        
        $(`#${this.modalId}-loading`).show();
        $(`#${this.modalId}-server-images`).hide();
        $(`#${this.modalId}-no-images`).hide();

        try {
            const response = await $.ajax({
                url: '/UserProfile/GetAvailableProfileImages',
                type: 'GET'
            });

            console.log('[Server Images] Response:', response);

            if (response.success && response.images) {
                self.serverImages = response.images;
                console.log(`[Server Images] Loaded ${self.serverImages.length} images`);
                self.renderServerImages(response.images);
            } else {
                console.warn('[Server Images] No images found or request unsuccessful');
                self.showNoImages();
            }
        } catch (error) {
            console.error('[Server Images] Error loading:', error);
            Toast.error('Failed to load available images');
            self.showNoImages();
        } finally {
            $(`#${this.modalId}-loading`).hide();
        }
    }

    /**
     * Render server images grid
     */
    renderServerImages(images) {
        const gridContainer = $(`#${this.modalId}-server-images`);
        gridContainer.empty();

        if (!images || images.length === 0) {
            this.showNoImages();
            return;
        }

        console.log('[Server Images] Rendering', images.length, 'images');

        images.forEach(imageName => {
            const imageItem = $(`
                <div class="server-image-item" data-image="${imageName}">
                    <img src="/images/profile/${imageName}" alt="${imageName}" loading="lazy" 
                         onerror="this.onerror=null; this.src='/images/profile/default_profile.png';">
                    <div class="server-image-overlay">
                        <i data-lucide="check-circle" class="overlay-checkmark"></i>
                    </div>
                </div>
            `);

            imageItem.on('click', () => {
                this.selectServerImage(imageName);
            });

            gridContainer.append(imageItem);
        });

        gridContainer.show();
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    /**
     * Show no images message
     */
    showNoImages() {
        console.log('[Server Images] Showing no images message');
        $(`#${this.modalId}-server-images`).hide();
        $(`#${this.modalId}-no-images`).show();
        
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }

    /**
     * Select a server image
     */
    selectServerImage(imageName) {
        console.log('[Server Images] Selected:', imageName);
        this.selectedServerImage = imageName;
        this.selectedFile = null;

        // Update visual selection
        $(`#${this.modalId} .server-image-item`).removeClass('selected');
        $(`#${this.modalId} .server-image-item[data-image="${imageName}"]`).addClass('selected');

        // Show selected image name
        $(`#${this.modalId}-selected-name`).text(`Selected: ${imageName}`).show();

        this.updateConfirmButton();
    }

    /**
     * Filter server images based on search query
     */
    filterServerImages(query) {
        console.log('[Server Images] Filtering with query:', query);
        const filteredImages = this.serverImages.filter(imageName =>
            imageName.toLowerCase().includes(query.toLowerCase())
        );
        console.log('[Server Images] Filtered results:', filteredImages.length, 'images');
        this.renderServerImages(filteredImages);
    }

    /**
     * Update confirm button state
     */
    updateConfirmButton() {
        const hasSelection = (this.currentTab === 'upload' && this.selectedFile) ||
                            (this.currentTab === 'server' && this.selectedServerImage);
        
        console.log('[Confirm Button] Updating state:', {
            hasSelection,
            currentTab: this.currentTab,
            selectedFile: !!this.selectedFile,
            selectedServerImage: !!this.selectedServerImage
        });
        
        $(`#${this.modalId}-confirm-btn`).prop('disabled', !hasSelection);
    }

    /**
     * Confirm and apply selection
     */
    confirmSelection() {
        console.log('[Confirm] Confirming selection. Tab:', this.currentTab);
        
        if (this.currentTab === 'upload' && this.selectedFile) {
            console.log('[Confirm] File upload:', this.selectedFile.name);
            // Return file object for upload
            this.onImageSelected({
                type: 'file',
                file: this.selectedFile
            });
        } else if (this.currentTab === 'server' && this.selectedServerImage) {
            console.log('[Confirm] Server image:', this.selectedServerImage);
            // Return server image name
            this.onImageSelected({
                type: 'server',
                imageName: this.selectedServerImage
            });
        } else {
            console.warn('[Confirm] No selection to confirm');
            Toast.warning('Please select an image first');
            return;
        }

        // Close modal
        $(`#${this.modalId}`).modal('hide');
    }

    /**
     * Reset modal state
     */
    resetModal() {
        console.log('[Reset] Resetting modal state');
        this.selectedFile = null;
        this.selectedServerImage = null;
        
        // Reset upload tab
        $(`#${this.modalId}-upload-area`).show();
        $(`#${this.modalId}-upload-preview`).removeClass('show');
        $(`#${this.modalId}-preview-img`).attr('src', '');
        $(`#${this.modalId}-file-input`).val('');

        // Reset server tab
        $(`#${this.modalId} .server-image-item`).removeClass('selected');
        $(`#${this.modalId}-selected-name`).hide();
        $(`#${this.modalId}-search-input`).val('');

        // Switch back to upload tab
        this.switchTab('upload');
    }

    /**
     * Show the modal
     */
    show() {
        console.log('[Modal] Showing modal:', this.modalId);
        $(`#${this.modalId}`).modal('show');
    }

    /**
     * Hide the modal
     */
    hide() {
        console.log('[Modal] Hiding modal:', this.modalId);
        $(`#${this.modalId}`).modal('hide');
    }
}

// Global helper function to create profile picture modal
function createProfilePictureModal(modalId, previewElementId, onImageSelected) {
    console.log('[Factory] Creating profile picture modal:', modalId);
    return new ProfilePictureModal(modalId, previewElementId, onImageSelected);
}
