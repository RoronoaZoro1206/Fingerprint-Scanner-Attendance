/**
 * Profile Picture Upload Modal - Dual Option
 * Handles both local file upload and server template selection
 * ✅ ENHANCED: Supports templates/users folder structure with comprehensive deletion rules
 * ✅ COMPREHENSIVE: Template protection, role-based permissions, and in-use checks
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
        this.templatesFolder = 'templates'; // ✅ NEW: Templates folder name
        this.userRole = null; // ✅ NEW: User's role
        
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

        // ✅ FIX BUG #1: Single unified click handler to prevent double file dialog
        $(`#${this.modalId}-upload-area`).on('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('[Upload Area Click] User clicked upload area');
            self.triggerFileInput();
            return false;
        });

        // ✅ FIX BUG #1: Single file input change handler
        const fileInput = document.getElementById(`${this.modalId}-file-input`);
        if (fileInput) {
            // Remove any existing event listeners first
            const newFileInput = fileInput.cloneNode(true);
            fileInput.parentNode.replaceChild(newFileInput, fileInput);
            
            // Attach single change event listener
            $(`#${this.modalId}-file-input`).on('change', function(e) {
                console.log('[File Input Change] Event triggered');
                console.log('[File Input Change] Files count:', e.target.files?.length || 0);
                
                if (e.target.files && e.target.files.length > 0) {
                    const file = e.target.files[0];
                    console.log('[File Input Change] Processing file:', file.name);
                    self.handleFileSelect(file);
                } else {
                    console.log('[File Input Change] No files selected (user cancelled)');
                }
            });
        }

        // ✅ FIX: Change file button
        $(`#${this.modalId}-change-file`).on('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('[Change File Button] Clicked');
            self.triggerFileInput();
            return false;
        });

        // ✅ FIX: Improved drag and drop handlers
        const uploadArea = document.getElementById(`${this.modalId}-upload-area`);
        if (uploadArea) {
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
     * ✅ FIX BUG #1: Centralized function to trigger file input
     */
    triggerFileInput() {
        console.log('[Trigger] Opening file dialog');
        
        const fileInput = document.getElementById(`${this.modalId}-file-input`);
        
        if (!fileInput) {
            console.error('[Trigger] File input element not found!');
            Toast.error('File selector not found. Please refresh the page.');
            return;
        }

        try {
            // ✅ Reset the file input value to allow selecting the same file again
            fileInput.value = '';
            
            // ✅ Trigger the click event ONCE
            console.log('[Trigger] Triggering file input click');
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
     * Load available template images from server
     */
    async loadServerImages() {
        const self = this;
        console.log('[Server Images] Loading templates from server...');
        
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
                self.templatesFolder = response.folder || 'templates';
                console.log(`[Server Images] Loaded ${self.serverImages.length} template images from ${self.templatesFolder}`);
                self.renderServerImages(response.images);
            } else {
                console.warn('[Server Images] No templates found or request unsuccessful');
                self.showNoImages();
            }
        } catch (error) {
            console.error('[Server Images] Error loading:', error);
            Toast.error('Failed to load template images');
            self.showNoImages();
        } finally {
            $(`#${self.modalId}-loading`).hide();
        }
    }

    /**
     * ✅ COMPREHENSIVE: Render server template images with role-based delete button visibility
     */
    renderServerImages(images) {
        const gridContainer = $(`#${this.modalId}-server-images`);
        gridContainer.empty();

        if (!images || images.length === 0) {
            this.showNoImages();
            return;
        }

        console.log('[Server Images] Rendering', images.length, 'template images');

        // ✅ RULE 1: Templates can NEVER be deleted (no delete button shown for templates)
        images.forEach(imageName => {
            const imageItem = $(`
                <div class="server-image-item" data-image="${imageName}">
                    <img src="/images/profile/${this.templatesFolder}/${imageName}" alt="${imageName}" loading="lazy" 
                         onerror="this.onerror=null; this.src='/images/profile/${this.templatesFolder}/default_profile.png';">
                    <div class="server-image-overlay">
                        <i data-lucide="check-circle" class="overlay-checkmark"></i>
                    </div>
                </div>
            `);

            imageItem.on('click', (e) => {
                if (!$(e.target).closest('.btn-delete-gallery-image').length) {
                    this.selectServerImage(imageName);
                }
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
     * Confirm template deletion with SweetAlert2
     * ✅ NOTE: This function will never be called for templates (delete button hidden)
     * Kept for consistency, but templates are protected
     */
    confirmDeleteImage(imageName) {
        const self = this;
        
        Swal.fire({
            title: 'Delete Template?',
            html: `
                <div style="text-align: left; margin-top: 1rem;">
                    <p style="color: #dc2626; font-size: 0.875rem; margin-bottom: 0.75rem; background: #fee2e2; padding: 0.75rem; border-radius: 0.375rem; border-left: 3px solid #dc2626;">
                        <i data-lucide="shield" style="width: 16px; height: 16px; vertical-align: middle; margin-right: 0.5rem;"></i>
                        <strong>Templates are protected and cannot be deleted.</strong>
                    </p>
                    <p style="color: #6b7280; margin-bottom: 0;">Templates are shared resources used by all users and are managed at the system level.</p>
                </div>
            `,
            icon: 'error',
            confirmButtonText: 'OK',
            confirmButtonColor: '#dc2626',
            didOpen: () => {
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }
        });
    }

    /**
     * Delete template image from server
     * ✅ COMPREHENSIVE: Role check and template protection
     */
    async deleteImage(imageName) {
        const self = this;
        console.log('[Delete Image] Attempting to delete:', imageName);

        try {
            Swal.fire({
                title: 'Deleting Image...',
                html: 'Please wait while the image is being deleted.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const response = await $.ajax({
                url: '/UserProfile/DeleteGalleryImage',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ imageName: imageName })
            });

            if (response.success) {
                self.serverImages = self.serverImages.filter(img => img !== imageName);
                
                $(`#${self.modalId} .server-image-item[data-image="${imageName}"]`).fadeOut(300, function() {
                    $(this).remove();
                    
                    if (self.serverImages.length === 0) {
                        self.showNoImages();
                    }
                });

                if (self.selectedServerImage === imageName) {
                    self.selectedServerImage = null;
                    self.updateConfirmButton();
                    $(`#${self.modalId}-selected-name`).hide();
                }

                Swal.fire({
                    icon: 'success',
                    title: 'Image Deleted!',
                    html: `
                        <div style="text-align: center; padding: 0.5rem;">
                            <p style="color: #6b7280; margin-bottom: 0;">Image has been deleted successfully.</p>
                        </div>
                    `,
                    timer: 3000,
                    showConfirmButton: false
                });

                console.log('[Delete Image] ✅ Image deleted successfully');
            } else if (response.permissionDenied) {
                // ✅ RULE 3: Non-admin attempted deletion
                Swal.fire({
                    icon: 'warning',
                    title: 'Permission Denied',
                    text: response.message || 'Only administrators can delete profile pictures from the gallery',
                    confirmButtonColor: '#3b82f6'
                });
            } else if (response.isTemplate) {
                // ✅ RULE 1: Template protection
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Delete Template',
                    html: `
                        <div style="text-align: left; margin-top: 1rem;">
                            <p style="color: #6b7280; margin-bottom: 0.5rem;">${response.message}</p>
                            <p style="color: #3b82f6; font-size: 0.875rem; margin-top: 0.75rem; background: #eff6ff; padding: 0.75rem; border-radius: 0.375rem; border-left: 3px solid #3b82f6;">
                                <i data-lucide="info" style="width: 16px; height: 16px; vertical-align: middle; margin-right: 0.5rem;"></i>
                                Templates are shared resources used by multiple users.
                            </p>
                        </div>
                    `,
                    confirmButtonColor: '#dc2626',
                    didOpen: () => {
                        if (typeof lucide !== 'undefined') {
                            lucide.createIcons();
                        }
                    }
                });
            } else if (response.inUse) {
                // ✅ RULE 2: Image is in use
                const usersList = response.users.map(u => `<li><strong>${u.Name}</strong> (${u.Role})</li>`).join('');
                
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Delete Image',
                    html: `
                        <div style="text-align: left; margin-top: 1rem;">
                            <p style="color: #6b7280; margin-bottom: 0.75rem;">This image is currently in use by:</p>
                            <ul style="text-align: left; margin-bottom: 0.75rem; padding-left: 1.5rem;">
                                ${usersList}
                            </ul>
                            <p style="color: #dc2626; font-size: 0.875rem; background: #fee2e2; padding: 0.75rem; border-radius: 0.375rem; border-left: 3px solid #dc2626;">
                                <i data-lucide="alert-triangle" style="width: 16px; height: 16px; vertical-align: middle;"></i>
                                Please change their profile pictures first before deleting this image.
                            </p>
                        </div>
                    `,
                    confirmButtonColor: '#dc2626',
                    didOpen: () => {
                        if (typeof lucide !== 'undefined') {
                            lucide.createIcons();
                        }
                    }
                });
            } else {
                throw new Error(response.message || 'Failed to delete image');
            }
        } catch (error) {
            console.error('[Delete Image] Error:', error);
            
            Swal.fire({
                icon: 'error',
                title: 'Deletion Failed',
                text: error.message || 'Failed to delete image. Please try again.',
                confirmButtonColor: '#dc2626'
            });
        }
    }

    /**
     * Select server template image
     */
    selectServerImage(imageName) {
        console.log('[Select Template]', imageName);
        
        this.selectedServerImage = imageName;
        this.selectedFile = null;

        $(`#${this.modalId} .server-image-item`).removeClass('selected');
        $(`#${this.modalId} .server-image-item[data-image="${imageName}"]`).addClass('selected');

        $(`#${this.modalId}-selected-name`).text(`Selected template: ${imageName}`).show();

        this.updateConfirmButton();
    }

    /**
     * Confirm selection
     */
    confirmSelection() {
        console.log('[Confirm Selection]');

        if (this.currentTab === 'upload' && this.selectedFile) {
            console.log('[Confirm] Uploading file:', this.selectedFile.name);
            if (this.onImageSelected) {
                this.onImageSelected({
                    type: 'file',
                    file: this.selectedFile
                });
            }
            this.hideModal();
        } else if (this.currentTab === 'server' && this.selectedServerImage) {
            console.log('[Confirm] Using server template:', this.selectedServerImage);
            if (this.onImageSelected) {
                this.onImageSelected({
                    type: 'server',
                    imageName: this.selectedServerImage
                });
            }
            this.hideModal();
        } else {
            console.warn('[Confirm] No selection made');
            Toast.warning('Please select a template or upload an image');
        }
    }

    /**
     * Filter server images
     */
    filterServerImages(searchTerm) {
        const term = searchTerm.toLowerCase().trim();
        console.log('[Filter] Search term:', term);

        if (!term) {
            // Show all images
            $(`#${this.modalId} .server-image-item`).show();
            return;
        }

        // Filter images
        $(`#${this.modalId} .server-image-item`).each(function() {
            const imageName = $(this).data('image').toLowerCase();
            if (imageName.includes(term)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    /**
     * Update confirm button state
     */
    updateConfirmButton() {
        const $btn = $(`#${this.modalId}-confirm-btn`);

        if ((this.currentTab === 'upload' && this.selectedFile) ||
            (this.currentTab === 'server' && this.selectedServerImage)) {
            $btn.prop('disabled', false);
        } else {
            $btn.prop('disabled', true);
        }
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

        // Reset server tab
        $(`#${this.modalId} .server-image-item`).removeClass('selected');
        $(`#${this.modalId}-selected-name`).hide();
        $(`#${this.modalId}-search-input`).val('');

        // Reset confirm button
        this.updateConfirmButton();
    }

    /**
     * Show no images message
     */
    showNoImages() {
        $(`#${this.modalId}-server-images`).hide();
        $(`#${this.modalId}-no-images`).show();
    }

    /**
     * Show modal
     */
    show() {
        console.log('[Show Modal]', this.modalId);
        const modal = new bootstrap.Modal(document.getElementById(this.modalId));
        modal.show();
    }

    /**
     * Hide modal
     */
    hideModal() {
        console.log('[Hide Modal]', this.modalId);
        $(`#${this.modalId}`).modal('hide');
    }
}

/**
 * Global helper function to create profile picture modal
 */
function createProfilePictureModal(modalId, previewElementId, onImageSelected) {
    console.log('[Factory] Creating profile picture modal:', modalId);
    return new ProfilePictureModal(modalId, previewElementId, onImageSelected);
}
