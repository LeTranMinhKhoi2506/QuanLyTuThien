// Admin Layout JavaScript

// Toggle Sidebar
function toggleSidebar() {
    const sidebar = document.getElementById('adminSidebar');
    const main = document.getElementById('adminMain');
    sidebar.classList.toggle('collapsed');
    main.classList.toggle('expanded');
    
    // Save state to localStorage
    localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
}

// Toggle Dropdown in Sidebar
function toggleDropdown(element) {
    const dropdown = element.closest('.sidebar-dropdown');
    const sidebar = document.getElementById('adminSidebar');
    
    // If sidebar is collapsed, don't close dropdown on hover
    if (sidebar.classList.contains('collapsed')) {
        dropdown.classList.toggle('open');
        return;
    }
    
    // Close other dropdowns
    document.querySelectorAll('.sidebar-dropdown.open').forEach(d => {
        if (d !== dropdown) d.classList.remove('open');
    });
    
    dropdown.classList.toggle('open');
}

// Toggle Admin Dropdown
function toggleAdminDropdown() {
    const dropdown = document.getElementById('adminDropdown');
    dropdown.classList.toggle('open');
}

// Close dropdown when clicking outside
document.addEventListener('click', function(e) {
    const adminDropdown = document.getElementById('adminDropdown');
    if (adminDropdown && !adminDropdown.contains(e.target)) {
        adminDropdown.classList.remove('open');
    }
    
    const notificationBell = document.getElementById('notificationBell');
    if (notificationBell && !notificationBell.contains(e.target)) {
        notificationBell.classList.remove('open');
    }
});

// Toggle Notification Dropdown
function toggleNotificationDropdown() {
    const bell = document.getElementById('notificationBell');
    bell.classList.toggle('open');
    
    // Refresh notifications when opening
    if (bell.classList.contains('open')) {
        loadNotifications();
    }
}

// Load Notifications from Server
async function loadNotifications() {
    try {
        const response = await fetch('/AdminNotifications/Get');
        const data = await response.json();
        
        const badge = document.getElementById('notificationBadge');
        const list = document.getElementById('notificationList');
        
        if (data.unreadCount > 0) {
            badge.textContent = data.unreadCount > 99 ? '99+' : data.unreadCount;
            badge.style.display = 'flex';
        } else {
            badge.style.display = 'none';
        }
        
        if (data.notifications && data.notifications.length > 0) {
            list.innerHTML = data.notifications.map(n => `
                <a href="${n.url || '#'}" class="notification-item ${n.isRead ? '' : 'unread'}" 
                   onclick="markAsRead(${n.id})">
                    <div class="notif-icon ${n.type}">
                        <i class="bi bi-${getNotifIcon(n.type)}"></i>
                    </div>
                    <div class="notif-content">
                        <div class="notif-title">${n.title}</div>
                        <div class="notif-desc">${n.message}</div>
                        <div class="notif-time">${n.timeAgo}</div>
                    </div>
                </a>
            `).join('');
        } else {
            list.innerHTML = `
                <div class="notification-empty">
                    <i class="bi bi-bell-slash"></i>
                    <div>Không có thông báo mới</div>
                </div>
            `;
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
    }
}

function getNotifIcon(type) {
    const icons = {
        'campaign': 'megaphone',
        'donation': 'cash-coin',
        'disbursement': 'credit-card',
        'report': 'flag',
        'system': 'gear'
    };
    return icons[type] || 'bell';
}

async function markAsRead(notificationId) {
    try {
        await fetch('/AdminNotifications/MarkRead?id=' + notificationId, { method: 'POST' });
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

async function markAllAsRead() {
    try {
        await fetch('/AdminNotifications/MarkAllRead', { method: 'POST' });
        loadNotifications();
        showToast('Thành công', 'Đã đánh dấu tất cả thông báo là đã đọc', 'success');
    } catch (error) {
        console.error('Error marking all notifications as read:', error);
    }
}

// Auto refresh notifications every 30 seconds
setInterval(loadNotifications, 30000);

// Restore sidebar state from localStorage
document.addEventListener('DOMContentLoaded', function() {
    // Load notifications
    loadNotifications();
    
    // Restore sidebar state
    const sidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (sidebarCollapsed) {
        const sidebar = document.getElementById('adminSidebar');
        const main = document.getElementById('adminMain');
        if (sidebar) sidebar.classList.add('collapsed');
        if (main) main.classList.add('expanded');
    }
});

// Toast notification helper
function showToast(title, message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    
    const icons = {
        'success': 'check-circle-fill',
        'error': 'x-circle-fill',
        'warning': 'exclamation-triangle-fill',
        'info': 'info-circle-fill'
    };
    
    const toast = document.createElement('div');
    toast.className = `admin-toast ${type}`;
    toast.innerHTML = `
        <i class="bi bi-${icons[type] || icons.success} toast-icon"></i>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" onclick="closeToast(this)">
            <i class="bi bi-x"></i>
        </button>
    `;
    
    container.appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        toast.classList.add('hiding');
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

function closeToast(btn) {
    const toast = btn.closest('.admin-toast');
    toast.classList.add('hiding');
    setTimeout(() => toast.remove(), 300);
}

// Confirm Modal System
function showConfirm(options) {
    return new Promise((resolve) => {
        const overlay = document.getElementById('confirmModalOverlay');
        const title = document.getElementById('confirmModalTitle');
        const message = document.getElementById('confirmModalMessage');
        const icon = document.getElementById('confirmModalIcon');
        const confirmBtn = document.getElementById('confirmModalConfirm');
        const cancelBtn = document.getElementById('confirmModalCancel');
        
        // Set content
        title.textContent = options.title || 'Xác nhận';
        message.textContent = options.message || 'Bạn có chắc chắn?';
        
        // Set type
        const type = options.type || 'warning';
        icon.className = 'modal-icon ' + type;
        const iconClasses = {
            'warning': 'exclamation-triangle',
            'danger': 'exclamation-octagon',
            'success': 'check-circle',
            'info': 'info-circle'
        };
        icon.innerHTML = `<i class="bi bi-${iconClasses[type] || iconClasses.warning}"></i>`;
        
        // Set button styles
        confirmBtn.className = 'btn ' + (options.confirmClass || (type === 'danger' ? 'btn-danger' : 'btn-primary'));
        confirmBtn.textContent = options.confirmText || 'Xác nhận';
        cancelBtn.textContent = options.cancelText || 'Hủy';
        
        // Show modal
        overlay.classList.add('show');
        
        // Handle confirm
        const handleConfirm = () => {
            overlay.classList.remove('show');
            cleanup();
            resolve(true);
        };
        
        // Handle cancel
        const handleCancel = () => {
            overlay.classList.remove('show');
            cleanup();
            resolve(false);
        };
        
        // Handle overlay click
        const handleOverlayClick = (e) => {
            if (e.target === overlay) {
                handleCancel();
            }
        };
        
        // Handle escape key
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                handleCancel();
            }
        };
        
        // Cleanup function
        const cleanup = () => {
            confirmBtn.removeEventListener('click', handleConfirm);
            cancelBtn.removeEventListener('click', handleCancel);
            overlay.removeEventListener('click', handleOverlayClick);
            document.removeEventListener('keydown', handleEscape);
        };
        
        // Add event listeners
        confirmBtn.addEventListener('click', handleConfirm);
        cancelBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', handleOverlayClick);
        document.addEventListener('keydown', handleEscape);
    });
}

// Convenience functions
async function confirmAction(message, options = {}) {
    return showConfirm({
        title: options.title || 'Xác nhận hành động',
        message: message,
        type: options.type || 'warning',
        confirmText: options.confirmText || 'Đồng ý',
        cancelText: options.cancelText || 'Hủy',
        confirmClass: options.confirmClass
    });
}

async function confirmDelete(itemName = 'mục này') {
    return showConfirm({
        title: 'Xác nhận xóa',
        message: `Bạn có chắc chắn muốn xóa ${itemName}? Hành động này không thể hoàn tác.`,
        type: 'danger',
        confirmText: 'Xóa',
        confirmClass: 'btn-danger'
    });
}

async function confirmApprove(itemName = 'mục này') {
    return showConfirm({
        title: 'Xác nhận phê duyệt',
        message: `Bạn có chắc chắn muốn phê duyệt ${itemName}?`,
        type: 'success',
        confirmText: 'Phê duyệt',
        confirmClass: 'btn-success'
    });
}

async function confirmReject(itemName = 'mục này') {
    return showConfirm({
        title: 'Xác nhận từ chối',
        message: `Bạn có chắc chắn muốn từ chối ${itemName}?`,
        type: 'danger',
        confirmText: 'Từ chối',
        confirmClass: 'btn-danger'
    });
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

// ============================================
// SweetAlert2 Helper Functions
// ============================================
const Swal2 = {
    // Toast notification (góc trên bên phải)
    toast: function(title, icon = 'success') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
        return Toast.fire({ icon, title });
    },

    // Success alert
    success: function(title, text = '') {
        return Swal.fire({
            icon: 'success',
            title: title,
            text: text,
            confirmButtonText: 'OK',
            confirmButtonColor: '#198754'
        });
    },

    // Error alert
    error: function(title, text = '') {
        return Swal.fire({
            icon: 'error',
            title: title,
            text: text,
            confirmButtonText: 'Đóng',
            confirmButtonColor: '#dc3545'
        });
    },

    // Warning alert
    warning: function(title, text = '') {
        return Swal.fire({
            icon: 'warning',
            title: title,
            text: text,
            confirmButtonText: 'OK',
            confirmButtonColor: '#ffc107'
        });
    },

    // Info alert
    info: function(title, text = '') {
        return Swal.fire({
            icon: 'info',
            title: title,
            text: text,
            confirmButtonText: 'OK',
            confirmButtonColor: '#0d6efd'
        });
    },

    // Confirm dialog
    confirm: function(title, text, options = {}) {
        return Swal.fire({
            icon: options.icon || 'question',
            title: title,
            text: text,
            showCancelButton: true,
            confirmButtonText: options.confirmText || 'Xác nhận',
            cancelButtonText: options.cancelText || 'Hủy',
            confirmButtonColor: options.confirmColor || '#0d6efd',
            cancelButtonColor: '#6c757d',
            reverseButtons: true
        });
    },

    // Delete confirm
    confirmDelete: function(itemName = 'mục này') {
        return Swal.fire({
            icon: 'warning',
            title: 'Xác nhận xóa',
            html: `Bạn có chắc chắn muốn xóa <strong>${itemName}</strong>?<br><small class="text-muted">Hành động này không thể hoàn tác.</small>`,
            showCancelButton: true,
            confirmButtonText: '<i class="bi bi-trash me-1"></i>Xóa',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            reverseButtons: true
        });
    },

    // Approve confirm
    confirmApprove: function(itemName = 'mục này') {
        return Swal.fire({
            icon: 'question',
            title: 'Xác nhận phê duyệt',
            text: `Bạn có chắc chắn muốn phê duyệt ${itemName}?`,
            showCancelButton: true,
            confirmButtonText: '<i class="bi bi-check-lg me-1"></i>Phê duyệt',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            reverseButtons: true
        });
    },

    // Reject confirm
    confirmReject: function(itemName = 'mục này') {
        return Swal.fire({
            icon: 'warning',
            title: 'Xác nhận từ chối',
            text: `Bạn có chắc chắn muốn từ chối ${itemName}?`,
            showCancelButton: true,
            confirmButtonText: '<i class="bi bi-x-lg me-1"></i>Từ chối',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            reverseButtons: true
        });
    },

    // Input dialog
    input: function(title, options = {}) {
        return Swal.fire({
            title: title,
            input: options.inputType || 'text',
            inputLabel: options.label || '',
            inputPlaceholder: options.placeholder || '',
            inputValue: options.value || '',
            showCancelButton: true,
            confirmButtonText: options.confirmText || 'Lưu',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#0d6efd',
            inputValidator: options.validator || null
        });
    },

    // Loading
    loading: function(title = 'Đang xử lý...') {
        return Swal.fire({
            title: title,
            allowOutsideClick: false,
            allowEscapeKey: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    },

    // Close loading
    closeLoading: function() {
        Swal.close();
    }
};
