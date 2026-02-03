// ==================== NEWS PAGE JAVASCRIPT ====================

document.addEventListener('DOMContentLoaded', function() {
    console.log('News page loaded');
    initializeNewsPage();
    initializeDynamicFilters();
});

// Initialize news page functionality
function initializeNewsPage() {
    // Auto-submit form on filter change
    const filterSelects = document.querySelectorAll('.filters-section select');
    filterSelects.forEach(select => {
        select.addEventListener('change', function() {
            // Thay vì submit form, g?i AJAX
            loadNewsWithAjax();
        });
    });

    // Add smooth scroll for pagination
    const paginationLinks = document.querySelectorAll('.pagination .page-link');
    paginationLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            // Let the navigation happen, but scroll to top smoothly
            setTimeout(() => {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            }, 100);
        });
    });

    // Add animation to news cards on scroll
    observeNewsCards();

    // Add hover effects
    addHoverEffects();
}

// ==================== DYNAMIC FILTERS (NO PAGE RELOAD) ====================

function initializeDynamicFilters() {
    // Handle quick filter tabs
    const filterTabs = document.querySelectorAll('.quick-filters .filter-tab');
    filterTabs.forEach(tab => {
        tab.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Remove active class from all tabs
            filterTabs.forEach(t => t.classList.remove('active'));
            
            // Add active class to clicked tab
            this.classList.add('active');
            
            // Extract filter parameters from URL
            const url = new URL(this.href);
            const params = new URLSearchParams(url.search);
            
            // Load news with these parameters
            loadNewsWithAjax({
                type: params.get('type') || '',
                search: params.get('search') || '',
                campaignId: params.get('campaignId') || '',
                page: 1
            });
        });
    });

    // Handle search form submission
    const searchForm = document.querySelector('.filters-section form');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            loadNewsWithAjax();
        });
    }

    // Handle pagination clicks
    document.addEventListener('click', function(e) {
        const paginationLink = e.target.closest('.pagination .page-link');
        if (paginationLink && !paginationLink.parentElement.classList.contains('disabled')) {
            e.preventDefault();
            
            const url = new URL(paginationLink.href);
            const params = new URLSearchParams(url.search);
            
            loadNewsWithAjax({
                type: params.get('type') || '',
                search: params.get('search') || '',
                campaignId: params.get('campaignId') || '',
                page: params.get('page') || 1
            });
            
            // Scroll to top smoothly
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        }
    });
}

// Load news with AJAX
async function loadNewsWithAjax(params) {
    try {
        // Show loading state
        showLoadingState();
        
        // Get parameters from form or use provided params
        if (!params) {
            const searchInput = document.querySelector('input[name="search"]');
            const typeSelect = document.querySelector('select[name="type"]');
            const campaignSelect = document.querySelector('select[name="campaignId"]');
            
            params = {
                search: searchInput?.value || '',
                type: typeSelect?.value || '',
                campaignId: campaignSelect?.value || '',
                page: 1
            };
        }
        
        // Build query string
        const queryParams = new URLSearchParams();
        if (params.search) queryParams.set('search', params.search);
        if (params.type) queryParams.set('type', params.type);
        if (params.campaignId) queryParams.set('campaignId', params.campaignId);
        if (params.page) queryParams.set('page', params.page);
        
        // Fetch data
        const response = await fetch(`/News/GetNewsAjax?${queryParams.toString()}`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        
        const data = await response.json();
        
        // Update URL without reload
        const newUrl = `/News/Index?${queryParams.toString()}`;
        window.history.pushState({ path: newUrl }, '', newUrl);
        
        // Render news
        renderNews(data);
        
        // Hide loading state
        hideLoadingState();
        
        // Re-initialize animations
        observeNewsCards();
        
    } catch (error) {
        console.error('Error loading news:', error);
        hideLoadingState();
        showErrorMessage('Có l?i x?y ra khi t?i tin t?c. Vui lòng th? l?i!');
    }
}

// Show loading state
function showLoadingState() {
    const newsGrid = document.querySelector('.news-grid');
    const emptyState = document.querySelector('.empty-state');
    
    if (newsGrid) {
        newsGrid.style.opacity = '0.5';
        newsGrid.style.pointerEvents = 'none';
    }
    
    if (emptyState) {
        emptyState.style.opacity = '0.5';
    }
    
    // Add loading spinner
    const loadingSpinner = document.createElement('div');
    loadingSpinner.id = 'loadingSpinner';
    loadingSpinner.className = 'text-center my-5';
    loadingSpinner.innerHTML = `
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">?ang t?i...</span>
        </div>
        <p class="mt-3 text-muted">?ang t?i tin t?c...</p>
    `;
    
    const newsPage = document.querySelector('.news-page');
    if (newsPage && !document.getElementById('loadingSpinner')) {
        newsPage.appendChild(loadingSpinner);
    }
}

// Hide loading state
function hideLoadingState() {
    const loadingSpinner = document.getElementById('loadingSpinner');
    if (loadingSpinner) {
        loadingSpinner.remove();
    }
    
    const newsGrid = document.querySelector('.news-grid');
    if (newsGrid) {
        newsGrid.style.opacity = '1';
        newsGrid.style.pointerEvents = 'auto';
    }
    
    const emptyState = document.querySelector('.empty-state');
    if (emptyState) {
        emptyState.style.opacity = '1';
    }
}

// Render news data
function renderNews(data) {
    // Update stats
    const statItem = document.querySelector('.stats-summary .stat-item strong');
    if (statItem) {
        statItem.textContent = data.totalUpdates;
    }
    
    // Update filter counts
    const generalOption = document.querySelector('option[value="general"]');
    const financialOption = document.querySelector('option[value="financial_report"]');
    if (generalOption) {
        generalOption.textContent = `Tin t?c chung (${data.generalCount})`;
    }
    if (financialOption) {
        financialOption.textContent = `Báo cáo tài chính (${data.financialCount})`;
    }
    
    // Render news grid or empty state
    const newsPage = document.querySelector('.news-page');
    const existingGrid = document.querySelector('.news-grid');
    const existingEmpty = document.querySelector('.empty-state');
    const existingPagination = document.querySelector('nav[aria-label="Page navigation"]');
    
    // Remove existing content
    if (existingGrid) existingGrid.remove();
    if (existingEmpty) existingEmpty.remove();
    if (existingPagination) existingPagination.remove();
    
    if (data.updates && data.updates.length > 0) {
        // Create news grid
        const gridHtml = renderNewsGrid(data.updates);
        const quickFilters = document.querySelector('.quick-filters');
        quickFilters.insertAdjacentHTML('afterend', gridHtml);
        
        // Create pagination
        if (data.totalPages > 1) {
            const paginationHtml = renderPagination(data.currentPage, data.totalPages, data.type, data.search, data.campaignId);
            const newsGrid = document.querySelector('.news-grid');
            newsGrid.insertAdjacentHTML('afterend', paginationHtml);
        }
    } else {
        // Show empty state
        const emptyHtml = `
            <div class="empty-state text-center py-5">
                <div class="empty-icon mb-4">
                    <i class="bi bi-inbox" style="font-size: 5rem; color: #dee2e6;"></i>
                </div>
                <h4 class="text-muted">Không tìm th?y tin t?c nào</h4>
                <p class="text-muted">Th? thay ??i b? l?c ho?c t? khóa tìm ki?m</p>
                <a href="/News/Index" class="btn btn-primary mt-3">
                    <i class="bi bi-arrow-clockwise me-2"></i>Xem t?t c? tin t?c
                </a>
            </div>
        `;
        const quickFilters = document.querySelector('.quick-filters');
        quickFilters.insertAdjacentHTML('afterend', emptyHtml);
    }
}

// Render news grid HTML
function renderNewsGrid(updates) {
    let html = '<div class="row g-4 news-grid">';
    
    updates.forEach(update => {
        const campaignThumb = update.firstImage || update.campaignThumbnail || '/Images/DefaultPic.png';
        const excerpt = update.excerpt || '';
        const typeBadge = update.type === 'financial_report' 
            ? '<span class="badge badge-financial"><i class="bi bi-file-earmark-bar-graph me-1"></i>Báo cáo TC</span>'
            : '<span class="badge badge-general"><i class="bi bi-newspaper me-1"></i>Tin t?c</span>';
        
        html += `
            <div class="col-lg-4 col-md-6">
                <article class="news-card" onclick="location.href='/News/Details/${update.updateId}'">
                    <div class="news-image-wrapper">
                        <img src="${campaignThumb}" alt="${update.title}" class="news-image"
                             onerror="this.src='/Images/DefaultPic.png'" />
                        <div class="news-badge">
                            ${typeBadge}
                        </div>
                    </div>
                    <div class="news-content">
                        ${update.categoryName ? `
                            <a href="/TrangChu/Details/${update.campaignId}" 
                               class="campaign-tag" onclick="event.stopPropagation()">
                                <i class="bi bi-tag-fill me-1"></i>
                                ${update.categoryName}
                            </a>
                        ` : ''}
                        <h3 class="news-title">${update.title}</h3>
                        <p class="news-excerpt">${excerpt}</p>
                        <div class="news-meta">
                            <div class="meta-item">
                                <i class="bi bi-person-circle me-1"></i>
                                <span>${update.authorName || 'Admin'}</span>
                            </div>
                            <div class="meta-item">
                                <i class="bi bi-calendar3 me-1"></i>
                                <span>${update.createdAt}</span>
                            </div>
                        </div>
                        <div class="news-campaign-link">
                            <i class="bi bi-arrow-right-circle me-2"></i>
                            <span>${update.campaignTitle || ''}</span>
                        </div>
                    </div>
                </article>
            </div>
        `;
    });
    
    html += '</div>';
    return html;
}

// Render pagination HTML
function renderPagination(currentPage, totalPages, type, search, campaignId) {
    let html = '<nav aria-label="Page navigation" class="mt-5"><ul class="pagination justify-content-center">';
    
    // Previous button
    const prevDisabled = currentPage <= 1 ? 'disabled' : '';
    html += `
        <li class="page-item ${prevDisabled}">
            <a class="page-link" href="/News/Index?type=${type || ''}&search=${search || ''}&campaignId=${campaignId || ''}&page=${currentPage - 1}">
                <i class="bi bi-chevron-left"></i>
            </a>
        </li>
    `;
    
    // Page numbers
    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(totalPages, currentPage + 2);
    
    for (let i = startPage; i <= endPage; i++) {
        const active = i === currentPage ? 'active' : '';
        html += `
            <li class="page-item ${active}">
                <a class="page-link" href="/News/Index?type=${type || ''}&search=${search || ''}&campaignId=${campaignId || ''}&page=${i}">
                    ${i}
                </a>
            </li>
        `;
    }
    
    // Next button
    const nextDisabled = currentPage >= totalPages ? 'disabled' : '';
    html += `
        <li class="page-item ${nextDisabled}">
            <a class="page-link" href="/News/Index?type=${type || ''}&search=${search || ''}&campaignId=${campaignId || ''}&page=${currentPage + 1}">
                <i class="bi bi-chevron-right"></i>
            </a>
        </li>
    `;
    
    html += '</ul></nav>';
    return html;
}

// Show error message
function showErrorMessage(message) {
    const toast = document.createElement('div');
    toast.className = 'toast align-items-center text-white bg-danger border-0 position-fixed top-0 end-0 m-3';
    toast.style.zIndex = '9999';
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="bi bi-exclamation-circle me-2"></i>${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    document.body.appendChild(toast);
    
    const bsToast = new bootstrap.Toast(toast, { autohide: true, delay: 3000 });
    bsToast.show();
    
    toast.addEventListener('hidden.bs.toast', function() {
        this.remove();
    });
}

// Observe news cards and animate on scroll
function observeNewsCards() {
    const cards = document.querySelectorAll('.news-card');
    
    if (!cards.length) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = '0';
                    entry.target.style.transform = 'translateY(30px)';
                    entry.target.style.transition = 'all 0.6s ease';
                    
                    requestAnimationFrame(() => {
                        entry.target.style.opacity = '1';
                        entry.target.style.transform = 'translateY(0)';
                    });
                }, index * 100); // Stagger animation
                
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1
    });

    cards.forEach(card => {
        observer.observe(card);
    });
}

// Add hover effects
function addHoverEffects() {
    const cards = document.querySelectorAll('.news-card');
    
    cards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transition = 'all 0.3s ease';
        });
    });
}

// ==================== SHARE FUNCTIONS ====================

function shareFacebook() {
    const url = encodeURIComponent(window.location.href);
    const shareUrl = `https://www.facebook.com/sharer/sharer.php?u=${url}`;
    window.open(shareUrl, 'facebook-share', 'width=600,height=400');
}

function shareTwitter() {
    const url = encodeURIComponent(window.location.href);
    const title = encodeURIComponent(document.title);
    const shareUrl = `https://twitter.com/intent/tweet?url=${url}&text=${title}`;
    window.open(shareUrl, 'twitter-share', 'width=600,height=400');
}

function shareLinkedIn() {
    const url = encodeURIComponent(window.location.href);
    const shareUrl = `https://www.linkedin.com/sharing/share-offsite/?url=${url}`;
    window.open(shareUrl, 'linkedin-share', 'width=600,height=400');
}

function copyLink() {
    const url = window.location.href;
    
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(url).then(() => {
            showCopyNotification('?ã sao chép link vào clipboard!');
        }).catch(err => {
            console.error('Failed to copy:', err);
            fallbackCopyTextToClipboard(url);
        });
    } else {
        fallbackCopyTextToClipboard(url);
    }
}

// Fallback copy method for older browsers
function fallbackCopyTextToClipboard(text) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.top = '0';
    textArea.style.left = '0';
    textArea.style.width = '2em';
    textArea.style.height = '2em';
    textArea.style.padding = '0';
    textArea.style.border = 'none';
    textArea.style.outline = 'none';
    textArea.style.boxShadow = 'none';
    textArea.style.background = 'transparent';
    
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        const successful = document.execCommand('copy');
        if (successful) {
            showCopyNotification('?ã sao chép link vào clipboard!');
        } else {
            showCopyNotification('Không th? sao chép. Vui lòng th? l?i!', 'error');
        }
    } catch (err) {
        console.error('Fallback: Oops, unable to copy', err);
        showCopyNotification('Không th? sao chép. Vui lòng th? l?i!', 'error');
    }
    
    document.body.removeChild(textArea);
}

// Show copy notification
function showCopyNotification(message, type = 'success') {
    // Check if Bootstrap is available for toast
    if (typeof bootstrap !== 'undefined') {
        showBootstrapToast(message, type);
    } else {
        // Fallback to alert
        alert(message);
    }
}

// Show Bootstrap toast notification
function showBootstrapToast(message, type) {
    // Create toast container if doesn't exist
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toastId = 'toast_' + Date.now();
    const bgClass = type === 'success' ? 'bg-success' : 'bg-danger';
    
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    toastContainer.insertAdjacentHTML('beforeend', toastHTML);
    
    // Show toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 3000
    });
    toast.show();
    
    // Remove toast after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        this.remove();
    });
}

// ==================== AJAX FUNCTIONS ====================

// Load more news (for infinite scroll - if needed)
async function loadMoreNews(page) {
    try {
        const params = new URLSearchParams(window.location.search);
        params.set('page', page);
        
        const response = await fetch(`/News/Index?${params.toString()}`);
        const html = await response.text();
        
        // Parse HTML and extract news cards
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const newsCards = doc.querySelectorAll('.news-card');
        
        // Append to grid
        const grid = document.querySelector('.news-grid');
        newsCards.forEach(card => {
            grid.appendChild(card);
        });
        
        // Re-observe new cards
        observeNewsCards();
        
    } catch (error) {
        console.error('Error loading more news:', error);
    }
}

// Get updates by campaign (for campaign detail page)
async function getUpdatesByCampaign(campaignId, page = 1) {
    try {
        const response = await fetch(`/News/GetUpdatesByCampaign?campaignId=${campaignId}&page=${page}`);
        const data = await response.json();
        
        if (data.success) {
            renderCampaignUpdates(data.updates);
            updatePagination(data.currentPage, data.totalPages);
        }
    } catch (error) {
        console.error('Error loading campaign updates:', error);
    }
}

// Render campaign updates
function renderCampaignUpdates(updates) {
    const container = document.getElementById('campaignUpdatesContainer');
    if (!container) return;
    
    container.innerHTML = '';
    
    updates.forEach(update => {
        const card = createUpdateCard(update);
        container.appendChild(card);
    });
}

// Create update card element
function createUpdateCard(update) {
    const div = document.createElement('div');
    div.className = 'update-item';
    
    const date = new Date(update.createdAt);
    const formattedDate = date.toLocaleDateString('vi-VN');
    
    div.innerHTML = `
        <div class="update-header">
            <h5>${update.title}</h5>
            <small class="text-muted">${formattedDate}</small>
        </div>
        <div class="update-body">
            ${update.content}
        </div>
        <a href="/News/Details/${update.updateId}" class="btn btn-sm btn-outline-primary mt-2">
            <i class="bi bi-arrow-right me-1"></i>Xem chi ti?t
        </a>
    `;
    
    return div;
}

// Update pagination
function updatePagination(currentPage, totalPages) {
    const pagination = document.querySelector('.pagination');
    if (!pagination) return;
    
    // Implementation depends on your pagination structure
    console.log(`Current page: ${currentPage}, Total pages: ${totalPages}`);
}

// ==================== UTILITY FUNCTIONS ====================

// Format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

// Truncate text
function truncateText(text, maxLength) {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
}

// Strip HTML tags
function stripHtml(html) {
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || '';
}

// ==================== EXPORT ====================
window.NewsPage = {
    loadMoreNews,
    getUpdatesByCampaign,
    shareFacebook,
    shareTwitter,
    shareLinkedIn,
    copyLink,
    loadNewsWithAjax
};
