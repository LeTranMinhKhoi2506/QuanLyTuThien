/**
 * Admin Campaigns Management Scripts
 * Quản lý chiến dịch - Admin Panel
 */

// Global variables
let campaignModal;
let campaignsTable;
let categories = [];
let validationState = {
    title: false,
    description: false,
    target: false,
    category: false,
    dates: true
};
let checkTitleTimer;
let uploadedImageFile = null; // Store uploaded file

// API URLs (will be set from view)
let apiUrls = {
    checkTitle: '',
    getCategories: '',
    get: '',
    create: '',
    update: '',
    delete: '',
    approve: '',
    reject: '',
    close: '',
    uploadImage: '' // Add upload endpoint
};

/**
 * Initialize module with API URLs
 */
function initCampaignManagement(urls) {
    apiUrls = { ...apiUrls, ...urls };
    
    campaignModal = new bootstrap.Modal(document.getElementById('campaignModal'));
    loadCategories();
    setupRealTimeValidation();
    initDataTable();
}

/**
 * Initialize DataTable
 */
function initDataTable() {
    campaignsTable = $('#campaignsTable').DataTable({
        order: [[0, 'desc']],
        columnDefs: [
            { targets: [4, 5], className: 'text-end' },
            { targets: [8], orderable: false }
        ],
        dom: "<'row mb-3'<'col-sm-12 col-md-4'l><'col-sm-12 col-md-4 text-center'B><'col-sm-12 col-md-4'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
        buttons: [
            {
                extend: 'excel',
                text: '<i class="bi bi-file-earmark-excel me-1"></i> Excel',
                className: 'btn btn-success btn-sm',
                title: 'Danh sách chiến dịch',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6, 7] }
            },
            {
                extend: 'pdf',
                text: '<i class="bi bi-file-earmark-pdf me-1"></i> PDF',
                className: 'btn btn-danger btn-sm',
                title: 'Danh sách chiến dịch',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6, 7] }
            },
            {
                extend: 'print',
                text: '<i class="bi bi-printer me-1"></i> In',
                className: 'btn btn-secondary btn-sm',
                title: 'Danh sách chiến dịch',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5, 6, 7] }
            }
        ]
    });
}

// ============== CATEGORIES ==============

async function loadCategories() {
    try {
        const response = await fetch(apiUrls.getCategories);
        const res = await response.json();
        if (res.success) {
            categories = res.categories;
            const select = document.getElementById('campaignCategory');
            select.innerHTML = '<option value="">-- Chọn danh mục --</option>';
            categories.forEach(c => {
                select.innerHTML += `<option value="${c.categoryId}">${c.name}</option>`;
            });
        }
    } catch (error) {
        console.error('Error loading categories:', error);
    }
}

// ============== VALIDATION ==============

function setupRealTimeValidation() {
    const titleInput = document.getElementById('campaignTitle');
    const descInput = document.getElementById('campaignDescription');
    const targetInput = document.getElementById('campaignTarget');
    const categorySelect = document.getElementById('campaignCategory');
    const startDateInput = document.getElementById('campaignStartDate');
    const endDateInput = document.getElementById('campaignEndDate');

    titleInput.addEventListener('input', function() { validateTitle(this.value); });
    descInput.addEventListener('input', function() { validateDescription(this.value); });
    targetInput.addEventListener('input', function() {
        let value = this.value.replace(/[^\d]/g, '');
        this.value = value;
        validateTarget(value);
    });
    categorySelect.addEventListener('change', function() { validateCategory(this.value); });
    startDateInput.addEventListener('change', validateDates);
    endDateInput.addEventListener('change', validateDates);
}

function validateCategory(value) {
    const select = document.getElementById('campaignCategory');
    const feedback = document.getElementById('categoryFeedback');
    
    select.classList.remove('is-valid', 'is-invalid');
    
    if (!value) {
        select.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Vui lòng chọn danh mục';
        validationState.category = false;
    } else {
        select.classList.add('is-valid');
        feedback.className = 'text-success';
        feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Đã chọn danh mục';
        validationState.category = true;
    }
    updateSaveButton();
}

async function validateTitle(value) {
    const input = document.getElementById('campaignTitle');
    const feedback = document.getElementById('titleFeedback');
    const counter = document.getElementById('titleCounter');
    const errorDiv = document.getElementById('titleError');
    const campaignId = document.getElementById('campaignId').value;
        
    const length = value.trim().length;
    counter.textContent = `${length}/200`;

    input.classList.remove('is-valid', 'is-invalid');
    errorDiv.style.display = 'none';

    if (length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Tên chiến dịch phải từ 10-200 ký tự';
        counter.className = 'text-muted';
        validationState.title = false;
        updateSaveButton();
        return;
    }
    
    if (length < 10) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Cần thêm ${10 - length} ký tự nữa`;
        counter.className = 'text-danger';
        validationState.title = false;
        updateSaveButton();
        return;
    }
    
    if (length > 200) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Vượt quá ${length - 200} ký tự`;
        counter.className = 'text-danger';
        validationState.title = false;
        updateSaveButton();
        return;
    }

    clearTimeout(checkTitleTimer);
    feedback.className = 'text-info';
    feedback.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Đang kiểm tra...';
    
    checkTitleTimer = setTimeout(async () => {
        try {
            const excludeId = campaignId !== '0' ? `&excludeCampaignId=${campaignId}` : '';
            const response = await fetch(`${apiUrls.checkTitle}?title=${encodeURIComponent(value.trim())}${excludeId}`);
            const res = await response.json();
            
            if (res.exists) {
                input.classList.add('is-invalid');
                feedback.className = 'text-danger';
                feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Tên chiến dịch đã tồn tại';
                counter.className = 'text-danger';
                validationState.title = false;
            } else {
                input.classList.add('is-valid');
                feedback.className = 'text-success';
                feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Tên chiến dịch hợp lệ';
                counter.className = 'text-success';
                validationState.title = true;
            }
        } catch (error) {
            input.classList.add('is-valid');
            feedback.className = 'text-success';
            feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Độ dài hợp lệ';
            counter.className = 'text-success';
            validationState.title = true;
        }
        updateSaveButton();
    }, 500);
}

function validateDescription(value) {
    const input = document.getElementById('campaignDescription');
    const feedback = document.getElementById('descFeedback');
    const counter = document.getElementById('descCounter');
    const errorDiv = document.getElementById('descError');
        
    const length = value.trim().length;
    counter.textContent = `${length} ký tự`;

    input.classList.remove('is-valid', 'is-invalid');
    errorDiv.style.display = 'none';

    if (length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Tối thiểu 50 ký tự. Mô tả rõ ràng giúp tăng độ tin cậy.';
        counter.className = 'text-muted';
        validationState.description = false;
    } else if (length < 50) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Cần thêm ${50 - length} ký tự nữa`;
        counter.className = 'text-danger';
        validationState.description = false;
    } else {
        input.classList.add('is-valid');
        feedback.className = 'text-success';
        feedback.innerHTML = `<i class="bi bi-check-circle me-1"></i>Mô tả đủ chi tiết`;
        counter.className = 'text-success';
        validationState.description = true;
    }

    updateSaveButton();
}

function validateTarget(value) {
    const input = document.getElementById('campaignTarget');
    const feedback = document.getElementById('targetFeedback');
    const formatted = document.getElementById('targetFormatted');
    const errorDiv = document.getElementById('targetError');
        
    const amount = parseInt(value) || 0;

    input.classList.remove('is-valid', 'is-invalid');
    errorDiv.style.display = 'none';

    if (amount === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Tối thiểu 100.000 VNĐ';
        formatted.textContent = '';
        validationState.target = false;
    } else if (amount < 100000) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Số tiền tối thiểu là 100.000 VNĐ`;
        formatted.textContent = formatCurrency(amount);
        formatted.className = 'text-danger fw-semibold';
        validationState.target = false;
    } else if (amount > 100000000000) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Số tiền vượt quá giới hạn`;
        formatted.textContent = formatCurrency(amount);
        formatted.className = 'text-danger fw-semibold';
        validationState.target = false;
    } else {
        input.classList.add('is-valid');
        feedback.className = 'text-success';
        feedback.innerHTML = `<i class="bi bi-check-circle me-1"></i>Số tiền hợp lệ`;
        formatted.textContent = formatCurrency(amount);
        formatted.className = 'text-primary fw-semibold';
        validationState.target = true;
    }

    updateSaveButton();
}

function validateDates() {
    const startInput = document.getElementById('campaignStartDate');
    const endInput = document.getElementById('campaignEndDate');
    const startFeedback = document.getElementById('startDateFeedback');
    const endFeedback = document.getElementById('endDateFeedback');
    const dateError = document.getElementById('dateError');

    const startDate = startInput.value ? new Date(startInput.value) : null;
    const endDate = endInput.value ? new Date(endInput.value) : null;
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    startInput.classList.remove('is-valid', 'is-invalid');
    endInput.classList.remove('is-valid', 'is-invalid');
    dateError.style.display = 'none';
    validationState.dates = true;

    if (startDate) {
        startInput.classList.add('is-valid');
        startFeedback.className = 'text-success';
        startFeedback.innerHTML = `<i class="bi bi-check-circle me-1"></i>${formatDate(startDate)}`;
    } else {
        startFeedback.className = 'text-muted';
        startFeedback.textContent = 'Mặc định là hôm nay';
    }

    if (endDate) {
        if (startDate && endDate <= startDate) {
            endInput.classList.add('is-invalid');
            endFeedback.className = 'text-danger';
            endFeedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Ngày kết thúc phải sau ngày bắt đầu`;
            validationState.dates = false;
        } else if (endDate < today) {
            endInput.classList.add('is-invalid');
            endFeedback.className = 'text-danger';
            endFeedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Ngày kết thúc không thể trong quá khứ`;
            validationState.dates = false;
        } else {
            endInput.classList.add('is-valid');
            endFeedback.className = 'text-success';
            const days = Math.ceil((endDate - (startDate || today)) / (1000 * 60 * 60 * 24));
            endFeedback.innerHTML = `<i class="bi bi-check-circle me-1"></i>Còn ${days} ngày gây quỹ`;
        }
    } else {
        endFeedback.className = 'text-muted';
        endFeedback.textContent = 'Thời hạn gây quỹ (không bắt buộc)';
    }

    updateSaveButton();
}

function updateSaveButton() {
    const saveBtn = document.querySelector('#campaignModal .btn-primary');
    const isValid = validationState.title && validationState.description && validationState.target && validationState.category && validationState.dates;
        
    if (isValid) {
        saveBtn.disabled = false;
        saveBtn.classList.remove('btn-secondary');
        saveBtn.classList.add('btn-primary');
    } else {
        const titleLength = document.getElementById('campaignTitle').value.trim().length;
        const descLength = document.getElementById('campaignDescription').value.trim().length;
        const targetVal = document.getElementById('campaignTarget').value;
        const categoryVal = document.getElementById('campaignCategory').value;
            
        if (titleLength > 0 || descLength > 0 || targetVal || categoryVal) {
            saveBtn.disabled = !isValid;
        }
    }
}

function resetValidationState() {
    validationState = { title: false, description: false, target: false, category: false, dates: true };
        
    ['campaignTitle', 'campaignDescription', 'campaignTarget', 'campaignCategory', 'campaignStartDate', 'campaignEndDate'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.classList.remove('is-valid', 'is-invalid');
    });

    document.getElementById('titleFeedback').className = 'text-muted';
    document.getElementById('titleFeedback').textContent = 'Tên chiến dịch phải từ 10-200 ký tự';
    document.getElementById('titleCounter').className = 'text-muted';
    document.getElementById('titleCounter').textContent = '0/200';

    document.getElementById('descFeedback').className = 'text-muted';
    document.getElementById('descFeedback').textContent = 'Tối thiểu 50 ký tự. Mô tả rõ ràng giúp tăng độ tin cậy.';
    document.getElementById('descCounter').className = 'text-muted';
    document.getElementById('descCounter').textContent = '0 ký tự';
    
    document.getElementById('categoryFeedback').className = 'text-muted';
    document.getElementById('categoryFeedback').textContent = 'Bắt buộc chọn danh mục để phân loại';

    document.getElementById('targetFeedback').className = 'text-muted';
    document.getElementById('targetFeedback').textContent = 'Tối thiểu 100.000 VNĐ';
    document.getElementById('targetFormatted').textContent = '';

    document.getElementById('startDateFeedback').className = 'text-muted';
    document.getElementById('startDateFeedback').textContent = 'Mặc định là hôm nay';
    document.getElementById('endDateFeedback').className = 'text-muted';
    document.getElementById('endDateFeedback').textContent = 'Thời hạn gây quỹ (không bắt buộc)';

    const saveBtn = document.querySelector('#campaignModal .btn-primary');
    saveBtn.disabled = false;
}

// ============== HELPERS ==============

function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

function formatDate(date) {
    return date.toLocaleDateString('vi-VN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
}

// ============== CRUD OPERATIONS ==============

function showCreateCampaignModal() {
    document.getElementById('campaignModalTitle').textContent = 'Thêm chiến dịch mới';
    document.getElementById('campaignForm').reset();
    document.getElementById('campaignId').value = '0';
    document.getElementById('statusRow').style.display = 'none';
    document.getElementById('campaignStartDate').value = new Date().toISOString().split('T')[0];
    
    // Reset image upload
    removeCampaignImage();
    
    resetValidationState();
    campaignModal.show();
}

async function editCampaign(id) {
    Swal.fire({ title: 'Đang tải...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    try {
        const response = await fetch(`${apiUrls.get}?id=${id}`);
        const res = await response.json();
        Swal.close();

        if (res.success) {
            const c = res.campaign;
            document.getElementById('campaignModalTitle').textContent = 'Sửa chiến dịch';
            document.getElementById('campaignId').value = c.campaignId;
            document.getElementById('campaignTitle').value = c.title;
            document.getElementById('campaignDescription').value = c.description;
            document.getElementById('campaignTarget').value = c.targetAmount;
            document.getElementById('campaignCategory').value = c.categoryId || '';
            document.getElementById('campaignStartDate').value = c.startDate || '';
            document.getElementById('campaignEndDate').value = c.endDate || '';
            document.getElementById('campaignThumbnailUrl').value = c.thumbnailUrl || '';
            document.getElementById('campaignExcessOption').value = c.excessFundOption || 'next_case';
            document.getElementById('campaignStatus').value = c.status || 'active';
            document.getElementById('statusRow').style.display = 'flex';
            
            // Show existing image
            if (c.thumbnailUrl) {
                document.getElementById('thumbnailPreviewImg').src = c.thumbnailUrl;
                document.getElementById('thumbnailPreview').style.display = 'block';
            } else {
                removeCampaignImage();
            }
            
            validateTitle(c.title);
            validateDescription(c.description);
            validateTarget(c.targetAmount.toString());
            validateCategory(c.categoryId || '');
            validateDates();
            
            campaignModal.show();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Không thể tải thông tin chiến dịch', confirmButtonColor: '#dc3545' });
    }
}

async function saveCampaign() {
    const id = document.getElementById('campaignId').value;
    const isEdit = id !== '0';

    const title = document.getElementById('campaignTitle').value.trim();
    const description = document.getElementById('campaignDescription').value.trim();
    const targetAmount = document.getElementById('campaignTarget').value;

    validateTitle(title);
    validateDescription(description);
    validateTarget(targetAmount);
    validateDates();

    if (!validationState.title || !validationState.description || !validationState.target || !validationState.dates) {
        let errorMessages = [];
        if (!validationState.title) errorMessages.push('Tên chiến dịch phải từ 10-200 ký tự');
        if (!validationState.description) errorMessages.push('Mô tả chiến dịch phải có ít nhất 50 ký tự');
        if (!validationState.target) errorMessages.push('Mục tiêu quyên góp tối thiểu là 100.000đ');
        if (!validationState.dates) errorMessages.push('Ngày kết thúc phải sau ngày bắt đầu');
        
        Swal.fire({
            icon: 'warning',
            title: 'Thông tin không hợp lệ',
            html: `<ul class="text-start">${errorMessages.map(m => `<li>${m}</li>`).join('')}</ul>`,
            confirmButtonColor: '#ffc107'
        });
        return;
    }

    Swal.fire({ title: 'Đang lưu...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    const formData = new FormData();
    if (isEdit) formData.append('campaignId', id);
    formData.append('title', title);
    formData.append('description', description);
    formData.append('targetAmount', targetAmount);
        
    const categoryId = document.getElementById('campaignCategory').value;
    if (categoryId) formData.append('categoryId', categoryId);
        
    const startDate = document.getElementById('campaignStartDate').value;
    const endDate = document.getElementById('campaignEndDate').value;
    if (startDate) formData.append('startDate', startDate);
    if (endDate) formData.append('endDate', endDate);
    
    // Handle image upload
    if (uploadedImageFile) {
        formData.append('thumbnailImage', uploadedImageFile);
    } else {
        // Use existing URL if no new file uploaded
        const existingUrl = document.getElementById('campaignThumbnailUrl').value;
        if (existingUrl) formData.append('thumbnailUrl', existingUrl);
    }
        
    formData.append('excessFundOption', document.getElementById('campaignExcessOption').value);
        
    if (isEdit) formData.append('status', document.getElementById('campaignStatus').value);
        
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    try {
        const url = isEdit ? apiUrls.update : apiUrls.create;
        const response = await fetch(url, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            campaignModal.hide();
            await Swal.fire({ icon: 'success', title: 'Thành công!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        console.error('Save campaign error:', error);
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

async function deleteCampaign(campaignId, title) {
    const result = await Swal.fire({
        icon: 'warning',
        title: 'Xóa chiến dịch',
        html: `Bạn có chắc chắn muốn xóa chiến dịch "<strong>${title}</strong>"?<br><small class="text-muted">Hành động này không thể hoàn tác!</small>`,
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-trash me-1"></i>Xóa',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        reverseButtons: true
    });

    if (!result.isConfirmed) return;

    Swal.fire({ title: 'Đang xóa...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    const formData = new FormData();
    formData.append('campaignId', campaignId);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    try {
        const response = await fetch(apiUrls.delete, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            await Swal.fire({ icon: 'success', title: 'Đã xóa!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Không thể xóa', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

// ============== APPROVAL ACTIONS ==============

async function approveCampaign(campaignId) {
    const result = await Swal.fire({
        icon: 'question',
        title: 'Phê duyệt chiến dịch',
        text: 'Bạn có chắc chắn muốn PHÊ DUYỆT chiến dịch này?',
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-check-lg me-1"></i>Phê duyệt',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#198754',
        cancelButtonColor: '#6c757d',
        reverseButtons: true
    });

    if (!result.isConfirmed) return;
    
    Swal.fire({ title: 'Đang xử lý...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    const formData = new FormData();
    formData.append('campaignId', campaignId);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
    
    try {
        const response = await fetch(apiUrls.approve, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            await Swal.fire({ icon: 'success', title: 'Đã phê duyệt!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

async function rejectCampaign(campaignId) {
    const { value: note } = await Swal.fire({
        icon: 'warning',
        title: 'Từ chối chiến dịch',
        input: 'textarea',
        inputLabel: 'Lý do từ chối',
        inputPlaceholder: 'Nhập lý do từ chối chiến dịch...',
        inputAttributes: { 'aria-label': 'Lý do từ chối' },
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-x-lg me-1"></i>Từ chối',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        reverseButtons: true,
        inputValidator: (value) => {
            if (!value || value.trim() === '') return 'Vui lòng nhập lý do từ chối!';
        }
    });

    if (!note) return;
    
    Swal.fire({ title: 'Đang xử lý...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    const formData = new FormData();
    formData.append('campaignId', campaignId);
    formData.append('note', note);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
    
    try {
        const response = await fetch(apiUrls.reject, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            await Swal.fire({ icon: 'success', title: 'Đã từ chối!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

async function closeCampaign(campaignId) {
    const { value: note } = await Swal.fire({
        icon: 'warning',
        title: 'Đóng chiến dịch',
        input: 'textarea',
        inputLabel: 'Lý do đóng chiến dịch',
        inputPlaceholder: 'Nhập lý do đóng chiến dịch...',
        inputAttributes: { 'aria-label': 'Lý do đóng' },
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-pause-circle me-1"></i>Đóng chiến dịch',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#ffc107',
        cancelButtonColor: '#6c757d',
        reverseButtons: true,
        inputValidator: (value) => {
            if (!value || value.trim() === '') return 'Vui lòng nhập lý do đóng chiến dịch!';
        }
    });

    if (!note) return;
    
    Swal.fire({ title: 'Đang xử lý...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

    const formData = new FormData();
    formData.append('campaignId', campaignId);
    formData.append('note', note);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
    
    try {
        const response = await fetch(apiUrls.close, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            await Swal.fire({ icon: 'success', title: 'Đã đóng chiến dịch!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

// ============== IMAGE UPLOAD FUNCTIONS ==============

/**
 * Preview campaign image before upload
 */
function previewCampaignImage(input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        
        // Validate file type
        const validTypes = ['image/jpeg', 'image/jpg', 'image/png'];
        if (!validTypes.includes(file.type)) {
            Swal.fire({
                icon: 'error',
                title: 'Định dạng không hợp lệ',
                text: 'Vui lòng chọn file ảnh JPG hoặc PNG',
                confirmButtonColor: '#dc3545'
            });
            input.value = '';
            return;
        }
        
        // Validate file size (5MB)
        if (file.size > 5 * 1024 * 1024) {
            Swal.fire({
                icon: 'error',
                title: 'File quá lớn',
                text: 'Kích thước ảnh không được vượt quá 5MB',
                confirmButtonColor: '#dc3545'
            });
            input.value = '';
            return;
        }
        
        // Store file for upload
        uploadedImageFile = file;
        
        // Show preview
        const reader = new FileReader();
        reader.onload = function(e) {
            document.getElementById('thumbnailPreviewImg').src = e.target.result;
            document.getElementById('thumbnailPreview').style.display = 'block';
        };
        reader.readAsDataURL(file);
    }
}

/**
 * Remove campaign image
 */
function removeCampaignImage() {
    document.getElementById('campaignThumbnailFile').value = '';
    document.getElementById('campaignThumbnailUrl').value = '';
    document.getElementById('thumbnailPreview').style.display = 'none';
    document.getElementById('thumbnailPreviewImg').src = '';
    uploadedImageFile = null;
}
