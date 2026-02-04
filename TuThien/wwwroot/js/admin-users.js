/**
 * Admin Users Management Scripts
 * Quản lý người dùng - Admin Panel
 */

// Global variables
let userModal;
let usersTable;
let validationState = {
    username: false,
    email: false,
    password: false,
    phone: true
};
let checkUsernameTimer, checkEmailTimer;

// API URLs (will be set from view)
let apiUrls = {
    checkUsername: '',
    checkEmail: '',
    get: '',
    create: '',
    update: '',
    delete: '',
    updateStatus: '',
    updateRole: ''
};

/**
 * Initialize module with API URLs
 */
function initUserManagement(urls) {
    apiUrls = { ...apiUrls, ...urls };
    
    userModal = new bootstrap.Modal(document.getElementById('userModal'));
    setupUserValidation();
    initDataTable();
}

/**
 * Initialize DataTable
 */
function initDataTable() {
    usersTable = $('#usersTable').DataTable({
        order: [[0, 'desc']],
        columnDefs: [
            { targets: [6], orderable: false },
            { targets: [3, 4], orderable: true }
        ],
        dom: "<'row mb-3'<'col-sm-12 col-md-4'l><'col-sm-12 col-md-4 text-center'B><'col-sm-12 col-md-4'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
        buttons: [
            {
                extend: 'excel',
                text: '<i class="bi bi-file-earmark-excel me-1"></i> Excel',
                className: 'btn btn-success btn-sm',
                title: 'Danh sách người dùng',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            },
            {
                extend: 'pdf',
                text: '<i class="bi bi-file-earmark-pdf me-1"></i> PDF',
                className: 'btn btn-danger btn-sm',
                title: 'Danh sách người dùng',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            },
            {
                extend: 'print',
                text: '<i class="bi bi-printer me-1"></i> In',
                className: 'btn btn-secondary btn-sm',
                title: 'Danh sách người dùng',
                exportOptions: { columns: [0, 1, 2, 3, 4, 5] }
            }
        ],
        drawCallback: function() {
            attachStatusButtonHandlers();
            attachRoleSelectHandlers();
        }
    });
}

// ============== VALIDATION ==============

function setupUserValidation() {
    const usernameInput = document.getElementById('userName');
    const emailInput = document.getElementById('userEmail');
    const passwordInput = document.getElementById('userPassword');
    const phoneInput = document.getElementById('userPhone');

    usernameInput.addEventListener('input', function() { validateUsername(this.value); });
    emailInput.addEventListener('input', function() { validateEmail(this.value); });
    passwordInput.addEventListener('input', function() { validatePassword(this.value); });
    phoneInput.addEventListener('input', function() { validatePhone(this.value); });
}

async function validateUsername(value) {
    const input = document.getElementById('userName');
    const feedback = document.getElementById('usernameFeedback');
    const counter = document.getElementById('usernameCounter');
    const userId = document.getElementById('userId').value;
        
    value = value.trim();
    const length = value.length;
    counter.textContent = `${length}/50`;

    input.classList.remove('is-valid', 'is-invalid');

    if (length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = '3-50 ký tự, chỉ chữ cái, số và dấu gạch dưới';
        counter.className = 'text-muted';
        validationState.username = false;
        updateUserSaveButton();
        return;
    }

    if (length < 3) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Cần thêm ${3 - length} ký tự nữa`;
        counter.className = 'text-danger';
        validationState.username = false;
        updateUserSaveButton();
        return;
    }

    if (!/^[a-zA-Z0-9_]+$/.test(value)) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Chỉ được chứa chữ cái, số và dấu gạch dưới';
        validationState.username = false;
        updateUserSaveButton();
        return;
    }

    clearTimeout(checkUsernameTimer);
    feedback.className = 'text-info';
    feedback.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Đang kiểm tra...';
        
    checkUsernameTimer = setTimeout(async () => {
        try {
            const excludeId = userId !== '0' ? `&excludeUserId=${userId}` : '';
            const response = await fetch(`${apiUrls.checkUsername}?username=${encodeURIComponent(value)}${excludeId}`);
            const res = await response.json();
                
            if (res.exists) {
                input.classList.add('is-invalid');
                feedback.className = 'text-danger';
                feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Tên đăng nhập đã tồn tại';
                validationState.username = false;
            } else {
                input.classList.add('is-valid');
                feedback.className = 'text-success';
                feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Tên đăng nhập hợp lệ';
                counter.className = 'text-success';
                validationState.username = true;
            }
        } catch (error) {
            feedback.className = 'text-warning';
            feedback.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>Không thể kiểm tra';
            validationState.username = true;
        }
        updateUserSaveButton();
    }, 500);
}

async function validateEmail(value) {
    const input = document.getElementById('userEmail');
    const feedback = document.getElementById('emailFeedback');
    const userId = document.getElementById('userId').value;
        
    value = value.trim();
    input.classList.remove('is-valid', 'is-invalid');

    if (value.length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Email hợp lệ và chưa được sử dụng';
        validationState.email = false;
        updateUserSaveButton();
        return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(value)) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Email không đúng định dạng';
        validationState.email = false;
        updateUserSaveButton();
        return;
    }

    clearTimeout(checkEmailTimer);
    feedback.className = 'text-info';
    feedback.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Đang kiểm tra...';
        
    checkEmailTimer = setTimeout(async () => {
        try {
            const excludeId = userId !== '0' ? `&excludeUserId=${userId}` : '';
            const response = await fetch(`${apiUrls.checkEmail}?email=${encodeURIComponent(value)}${excludeId}`);
            const res = await response.json();
                
            if (res.exists) {
                input.classList.add('is-invalid');
                feedback.className = 'text-danger';
                feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Email đã được sử dụng';
                validationState.email = false;
            } else {
                input.classList.add('is-valid');
                feedback.className = 'text-success';
                feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Email hợp lệ';
                validationState.email = true;
            }
        } catch (error) {
            feedback.className = 'text-warning';
            feedback.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>Không thể kiểm tra';
            validationState.email = true;
        }
        updateUserSaveButton();
    }, 500);
}

function validatePassword(value) {
    const input = document.getElementById('userPassword');
    const feedback = document.getElementById('passwordFeedback');
    const strength = document.getElementById('passwordStrength');
    const userId = document.getElementById('userId').value;
    const isEdit = userId !== '0';

    input.classList.remove('is-valid', 'is-invalid');
    strength.textContent = '';

    if (isEdit && value.length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Để trống nếu không muốn đổi mật khẩu';
        validationState.password = true;
        updateUserSaveButton();
        return;
    }

    if (value.length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Tối thiểu 6 ký tự';
        validationState.password = false;
        updateUserSaveButton();
        return;
    }

    if (value.length < 6) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = `<i class="bi bi-x-circle me-1"></i>Cần thêm ${6 - value.length} ký tự nữa`;
        validationState.password = false;
        updateUserSaveButton();
        return;
    }

    let strengthLevel = 0;
    if (value.length >= 8) strengthLevel++;
    if (/[a-z]/.test(value) && /[A-Z]/.test(value)) strengthLevel++;
    if (/\d/.test(value)) strengthLevel++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(value)) strengthLevel++;

    input.classList.add('is-valid');
    feedback.className = 'text-success';
    feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Mật khẩu hợp lệ';

    const strengthMap = {
        0: { class: 'text-danger', text: 'Yếu' },
        1: { class: 'text-danger', text: 'Yếu' },
        2: { class: 'text-warning', text: 'Trung bình' },
        3: { class: 'text-info', text: 'Khá' },
        4: { class: 'text-success', text: 'Mạnh' }
    };
    
    strength.className = `${strengthMap[strengthLevel].class} fw-semibold`;
    strength.textContent = strengthMap[strengthLevel].text;

    validationState.password = true;
    updateUserSaveButton();
}

function validatePhone(value) {
    const input = document.getElementById('userPhone');
    const feedback = document.getElementById('phoneFeedback');

    input.classList.remove('is-valid', 'is-invalid');

    if (value.length === 0) {
        feedback.className = 'text-muted';
        feedback.textContent = 'Số điện thoại Việt Nam (không bắt buộc)';
        validationState.phone = true;
        updateUserSaveButton();
        return;
    }

    const phoneRegex = /^(0|\+84)[0-9]{9,10}$/;
    if (!phoneRegex.test(value.replace(/\s/g, ''))) {
        input.classList.add('is-invalid');
        feedback.className = 'text-danger';
        feedback.innerHTML = '<i class="bi bi-x-circle me-1"></i>Số điện thoại không hợp lệ';
        validationState.phone = false;
    } else {
        input.classList.add('is-valid');
        feedback.className = 'text-success';
        feedback.innerHTML = '<i class="bi bi-check-circle me-1"></i>Số điện thoại hợp lệ';
        validationState.phone = true;
    }
    updateUserSaveButton();
}

function togglePassword() {
    const input = document.getElementById('userPassword');
    const icon = document.getElementById('togglePasswordIcon');
    if (input.type === 'password') {
        input.type = 'text';
        icon.className = 'bi bi-eye-slash';
    } else {
        input.type = 'password';
        icon.className = 'bi bi-eye';
    }
}

function updateUserSaveButton() {
    const saveBtn = document.querySelector('#userModal .btn-primary');
    const isValid = validationState.username && validationState.email && validationState.password && validationState.phone;
    saveBtn.disabled = !isValid;
}

function resetUserValidationState(isEdit = false) {
    validationState = { 
        username: false, 
        email: false, 
        password: isEdit,
        phone: true 
    };
        
    ['userName', 'userEmail', 'userPassword', 'userPhone'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.classList.remove('is-valid', 'is-invalid');
    });

    document.getElementById('usernameFeedback').className = 'text-muted';
    document.getElementById('usernameFeedback').textContent = '3-50 ký tự, chỉ chữ cái, số và dấu gạch dưới';
    document.getElementById('usernameCounter').className = 'text-muted';
    document.getElementById('usernameCounter').textContent = '0/50';

    document.getElementById('emailFeedback').className = 'text-muted';
    document.getElementById('emailFeedback').textContent = 'Email hợp lệ và chưa được sử dụng';

    document.getElementById('passwordFeedback').className = 'text-muted';
    document.getElementById('passwordFeedback').textContent = 'Tối thiểu 6 ký tự';
    document.getElementById('passwordStrength').textContent = '';

    document.getElementById('phoneFeedback').className = 'text-muted';
    document.getElementById('phoneFeedback').textContent = 'Số điện thoại Việt Nam (không bắt buộc)';

    const saveBtn = document.querySelector('#userModal .btn-primary');
    saveBtn.disabled = true;
}

// ============== CRUD OPERATIONS ==============

function showCreateUserModal() {
    document.getElementById('userModalTitle').textContent = 'Thêm người dùng mới';
    document.getElementById('userForm').reset();
    document.getElementById('userId').value = '0';
    document.getElementById('userPassword').required = true;
    document.getElementById('passwordLabel').innerHTML = '<i class="bi bi-key me-1"></i>Mật khẩu <span class="text-danger">*</span>';
    document.getElementById('passwordHint').style.display = 'none';
    document.getElementById('statusGroup').style.display = 'none';
    resetUserValidationState(false);
    userModal.show();
}

async function editUser(id) {
    Swal.fire({
        title: 'Đang tải...',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    try {
        const response = await fetch(`${apiUrls.get}?id=${id}`);
        const res = await response.json();
        Swal.close();

        if (res.success) {
            const u = res.user;
            document.getElementById('userModalTitle').textContent = 'Sửa người dùng';
            document.getElementById('userId').value = u.userId;
            document.getElementById('userName').value = u.username;
            document.getElementById('userEmail').value = u.email;
            document.getElementById('userPassword').value = '';
            document.getElementById('userPassword').required = false;
            document.getElementById('passwordLabel').innerHTML = '<i class="bi bi-key me-1"></i>Mật khẩu mới';
            document.getElementById('passwordHint').style.display = 'block';
            document.getElementById('userPhone').value = u.phoneNumber || '';
            document.getElementById('userRole').value = u.role || 'user';
            document.getElementById('userStatus').value = u.status || 'active';
            document.getElementById('statusGroup').style.display = 'block';
            
            resetUserValidationState(true);
            validateUsername(u.username);
            validateEmail(u.email);
            validatePassword('');
            if (u.phoneNumber) validatePhone(u.phoneNumber);
            
            userModal.show();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Không thể tải thông tin người dùng', confirmButtonColor: '#dc3545' });
    }
}

async function saveUser() {
    const id = document.getElementById('userId').value;
    const isEdit = id !== '0';

    const username = document.getElementById('userName').value.trim();
    const email = document.getElementById('userEmail').value.trim();
    const password = document.getElementById('userPassword').value;
    const phone = document.getElementById('userPhone').value.trim();

    await validateUsername(username);
    await validateEmail(email);
    validatePassword(password);
    if (phone) validatePhone(phone);

    if (!validationState.username || !validationState.email || !validationState.password || !validationState.phone) {
        let errorMessages = [];
        if (!validationState.username) errorMessages.push('Tên đăng nhập không hợp lệ');
        if (!validationState.email) errorMessages.push('Email không hợp lệ');
        if (!validationState.password) errorMessages.push('Mật khẩu không hợp lệ');
        if (!validationState.phone) errorMessages.push('Số điện thoại không hợp lệ');
        
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
    if (isEdit) formData.append('userId', id);
    formData.append('username', username);
    formData.append('email', email);
    if (password) formData.append('password', password);
    if (phone) formData.append('phoneNumber', phone);
    formData.append('role', document.getElementById('userRole').value);
    if (isEdit) formData.append('status', document.getElementById('userStatus').value);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    try {
        const url = isEdit ? apiUrls.update : apiUrls.create;
        const response = await fetch(url, { method: 'POST', body: formData });
        const res = await response.json();
        
        if (res.success) {
            userModal.hide();
            await Swal.fire({ icon: 'success', title: 'Thành công!', text: res.message, confirmButtonColor: '#198754' });
            location.reload();
        } else {
            Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
        }
    } catch (error) {
        console.error('Save user error:', error);
        Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
    }
}

async function deleteUser(userId, username) {
    const result = await Swal.fire({
        icon: 'warning',
        title: 'Xóa người dùng',
        html: `Bạn có chắc chắn muốn xóa người dùng "<strong>${username}</strong>"?<br><small class="text-muted">Hành động này không thể hoàn tác!</small>`,
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
    formData.append('userId', userId);
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

// ============== STATUS & ROLE HANDLERS ==============

function attachStatusButtonHandlers() {
    document.querySelectorAll('.btn-status').forEach(btn => {
        btn.removeEventListener('click', btn._statusHandler);
        
        btn._statusHandler = async function() {
            const userId = this.dataset.userId;
            const status = this.dataset.status;
            const actionText = status === 'locked' ? 'khóa' : 'mở khóa';
            
            const result = await Swal.fire({
                icon: status === 'locked' ? 'warning' : 'question',
                title: `${status === 'locked' ? 'Khóa' : 'Mở khóa'} tài khoản`,
                text: `Bạn có chắc chắn muốn ${actionText} tài khoản này?`,
                showCancelButton: true,
                confirmButtonText: status === 'locked' ? '<i class="bi bi-lock me-1"></i>Khóa' : '<i class="bi bi-unlock me-1"></i>Mở khóa',
                cancelButtonText: 'Hủy',
                confirmButtonColor: status === 'locked' ? '#dc3545' : '#198754',
                cancelButtonColor: '#6c757d',
                reverseButtons: true
            });

            if (!result.isConfirmed) return;
            
            Swal.fire({ title: 'Đang xử lý...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

            const formData = new FormData();
            formData.append('userId', userId);
            formData.append('status', status);
            formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
            
            try {
                const response = await fetch(apiUrls.updateStatus, { method: 'POST', body: formData });
                const res = await response.json();
                
                if (res.success) {
                    await Swal.fire({ icon: 'success', title: 'Thành công!', text: res.message, confirmButtonColor: '#198754' });
                    location.reload();
                } else {
                    Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
                }
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
            }
        };
        
        btn.addEventListener('click', btn._statusHandler);
    });
}

function attachRoleSelectHandlers() {
    document.querySelectorAll('.role-select').forEach(select => {
        const originalValue = select.value;
        
        select.removeEventListener('change', select._roleHandler);
        
        select._roleHandler = async function() {
            const userId = this.dataset.userId;
            const role = this.value;
            const roleText = { 'user': 'Người dùng', 'charity_org': 'Tổ chức từ thiện', 'admin': 'Admin' }[role] || role;
            
            const result = await Swal.fire({
                icon: 'question',
                title: 'Thay đổi vai trò',
                html: `Bạn có chắc chắn muốn thay đổi vai trò thành <strong>${roleText}</strong>?`,
                showCancelButton: true,
                confirmButtonText: 'Xác nhận',
                cancelButtonText: 'Hủy',
                confirmButtonColor: '#0d6efd',
                cancelButtonColor: '#6c757d',
                reverseButtons: true
            });

            if (!result.isConfirmed) {
                this.value = originalValue;
                return;
            }
            
            Swal.fire({ title: 'Đang cập nhật...', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

            const formData = new FormData();
            formData.append('userId', userId);
            formData.append('role', role);
            formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
            
            try {
                const response = await fetch(apiUrls.updateRole, { method: 'POST', body: formData });
                const res = await response.json();
                
                if (res.success) {
                    Swal.fire({ icon: 'success', title: 'Đã cập nhật!', text: res.message, toast: true, position: 'top-end', showConfirmButton: false, timer: 3000 });
                } else {
                    Swal.fire({ icon: 'error', title: 'Lỗi', text: res.message, confirmButtonColor: '#dc3545' });
                    location.reload();
                }
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'Lỗi kết nối', text: 'Đã xảy ra lỗi, vui lòng thử lại', confirmButtonColor: '#dc3545' });
                location.reload();
            }
        };
        
        select.addEventListener('change', select._roleHandler);
    });
}
