/**
 * Section Management - Refactored to use common utilities
 * Uses common AJAX, modal, export, and DataTable utilities
 */

let sectionsTable;
let programsData = [];

$(document).ready(function () {
    // Initialize icons
    initializeLucideIcons();

    // Load programs first, then initialize DataTable
    loadPrograms().then(() => {
        initializeDataTable();
    });

    // Event Handlers
    $('#btnAddSection').on('click', showAddModal);
    $('#btnSaveSection').on('click', saveSection);
    $('#btnUpdateSection').on('click', updateSection);
    $('#btnConfirmDelete').on('click', confirmDeleteSection);

    // Export button handlers
    $('#btnPrint').on('click', function (e) {
        e.preventDefault();
        printData('Sections', getSectionsData());
    });

    $('#btnExportExcel').on('click', function (e) {
        e.preventDefault();
        exportToExcelFile('sections', getSectionsData());
    });

    $('#btnExportCSV').on('click', function (e) {
        e.preventDefault();
        exportToCSVFile('sections', getSectionsData());
    });

    $('#btnExportPDF').on('click', function (e) {
        e.preventDefault();
        exportToPDFFile('Sections', getSectionsData());
    });

    // Reinitialize icons when dropdown is shown
    $('#exportDropdown').on('shown.bs.dropdown', initializeLucideIcons);

    // Setup modal icon reinitialization
    setupModalIconReinit(['addSectionModal', 'editSectionModal', 'deleteSectionModal']);

    // Clear form when modal is hidden
    setupFormClearOnHide('addSectionModal', 'addSectionForm');
});

/**
 * Load Programs for dropdown
 */
async function loadPrograms() {
    try {
        const response = await $.ajax({
            url: '/AdminSection/GetPrograms',
            type: 'GET'
        });

        if (response.success) {
            programsData = response.data;
            populateProgramDropdowns();
        }
    } catch (error) {
        console.error('Error loading programs:', error);
        Toast.error('Failed to load programs.');
    }
}

/**
 * Populate program dropdowns
 */
function populateProgramDropdowns() {
    const addSelect = $('#addProgramId');
    const editSelect = $('#editProgramId');

    // Clear existing options except the first one
    addSelect.find('option:not(:first)').remove();
    editSelect.find('option:not(:first)').remove();

    // Add program options
    programsData.forEach(program => {
        const option = `<option value="${program.programId}">${program.programName}</option>`;
        addSelect.append(option);
        editSelect.append(option);
    });
}

/**
 * Get year level display text
 */
function getYearLevelText(yearLevel) {
    const yearLevels = {
        1: '1st Year',
        2: '2nd Year',
        3: '3rd Year',
        4: '4th Year'
    };
    return yearLevels[yearLevel] || 'N/A';
}

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    sectionsTable = $('#sectionsTable').DataTable({
        ajax: {
            url: '/AdminSection/GetAll',
            dataSrc: 'data',
            error: function () {
                Toast.error('Failed to load sections data.');
            }
        },
        columns: [
            {
                data: 'sectionName',
                width: '25%'
            },
            {
                data: 'programName',
                width: '40%'
            },
            {
                data: 'yearLevel',
                width: '20%',
                render: function (data) {
                    return getYearLevelText(data);
                }
            },
            {
                data: null,
                width: '15%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <div class="action-buttons">
                            <button class="btn-action btn-edit" onclick="showEditModal(${row.sectionId})" title="Edit">
                                <i data-lucide="edit-2" class="icon-sm" style="color: #3b82f6;"></i>
                            </button>
                            <button class="btn-action btn-delete" onclick="deleteSection(${row.sectionId})" title="Delete">
                                <i data-lucide="trash-2" class="icon-sm" style="color: #ef4444;"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        order: [[2, 'asc'], [0, 'asc']], // Order by year level, then section name
        ...commonDataTableConfig,
        language: getDataTableLanguage('sections', 'section')
    });
}

/**
 * Show Add Section Modal
 */
function showAddModal() {
    $('#addSectionForm')[0].reset();
    showModal('addSectionModal');
}

/**
 * Show Edit Section Modal
 */
function showEditModal(sectionId) {
    ajaxGet(`/AdminSection/GetById/${sectionId}`,
        function (response) {
            const section = response.data;
            $('#editSectionId').val(section.sectionId);
            $('#editSectionName').val(section.sectionName);
            $('#editProgramId').val(section.programId);
            $('#editYearLevel').val(section.yearLevel);
            showModal('editSectionModal');
        },
        function () {
            Toast.error('Failed to load section data.');
        }
    );
}

/**
 * Save New Section
 */
function saveSection() {
    const form = $('#addSectionForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const sectionName = $('#addSectionName').val().trim();
    const programId = $('#addProgramId').val();
    const yearLevel = $('#addYearLevel').val();

    if (!sectionName || !programId || !yearLevel) {
        Toast.warning('All fields are required.');
        return;
    }

    const sectionData = {
        SectionName: sectionName,
        ProgramId: parseInt(programId),
        YearLevel: parseInt(yearLevel)
    };

    const $btn = $('#btnSaveSection');
    setButtonLoading($btn, 'Saving...');

    ajaxPost('/AdminSection/Create', sectionData,
        function () {
            Toast.created('Section');
            hideModal('addSectionModal');
            sectionsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Update Existing Section
 */
function updateSection() {
    const form = $('#editSectionForm')[0];

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const sectionName = $('#editSectionName').val().trim();
    const programId = $('#editProgramId').val();
    const yearLevel = $('#editYearLevel').val();

    if (!sectionName || !programId || !yearLevel) {
        Toast.warning('All fields are required.');
        return;
    }

    const sectionData = {
        SectionId: parseInt($('#editSectionId').val()),
        SectionName: sectionName,
        ProgramId: parseInt(programId),
        YearLevel: parseInt(yearLevel)
    };

    const $btn = $('#btnUpdateSection');
    setButtonLoading($btn, 'Updating...');

    ajaxPut('/AdminSection/Update', sectionData,
        function () {
            Toast.updated('Section');
            hideModal('editSectionModal');
            sectionsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Show Delete Confirmation Modal
 */
function deleteSection(sectionId) {
    $('#deleteSectionId').val(sectionId);
    showModal('deleteSectionModal');
}

/**
 * Confirm and Delete Section
 */
function confirmDeleteSection() {
    const sectionId = $('#deleteSectionId').val();

    if (!sectionId) {
        Toast.error('Invalid section ID.');
        return;
    }

    const $btn = $('#btnConfirmDelete');
    setButtonLoading($btn, 'Deleting...');

    ajaxDelete(`/AdminSection/Delete/${sectionId}`,
        function () {
            Toast.deleted('Section');
            hideModal('deleteSectionModal');
            sectionsTable.ajax.reload(null, false);
            resetButton($btn);
        },
        function () {
            resetButton($btn);
        }
    );
}

/**
 * Get sections data for export
 */
function getSectionsData() {
    const data = [];
    sectionsTable.rows({ search: 'applied' }).every(function () {
        const rowData = this.data();
        data.push({
            'Section': rowData.sectionName,
            'Program': rowData.programName,
            'Year Level': getYearLevelText(rowData.yearLevel)
        });
    });
    return data;
}
