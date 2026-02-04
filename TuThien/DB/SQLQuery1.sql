-- Tạo Database
CREATE DATABASE TuThien;
GO
USE TuThien;
GO


-- =============================================
-- 1. BẢNG NGƯỜI DÙNG & XÁC THỰC (Users & Auth)
-- Node 1.0, 1.1, 1.2
-- =============================================
CREATE TABLE Users (
    user_id INT PRIMARY KEY IDENTITY(1,1),
    username NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    phone_number VARCHAR(15),
    -- Thay thế ENUM bằng CHECK constraint
    role VARCHAR(20) DEFAULT 'user' 
        CHECK (role IN ('admin', 'user', 'charity_org')), 
    status VARCHAR(20) DEFAULT 'active' 
        CHECK (status IN ('active', 'locked', 'pending_verification')),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE UserProfiles (
    profile_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT UNIQUE,
    full_name NVARCHAR(100) NOT NULL,
    address NVARCHAR(MAX),
    avatar_url NVARCHAR(255),
    bio NVARCHAR(MAX),
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- Node 1.3: Xác thực danh tính (KYC)
CREATE TABLE IdentityVerifications (
    verification_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    document_type VARCHAR(50) NOT NULL 
        CHECK (document_type IN ('personal_id', 'organization_license')),
    document_url_front NVARCHAR(255) NOT NULL,
    document_url_back NVARCHAR(255),
    status VARCHAR(20) DEFAULT 'pending' 
        CHECK (status IN ('pending', 'approved', 'rejected')),
    admin_note NVARCHAR(MAX),
    reviewed_by INT,
    submitted_at DATETIME2 DEFAULT GETDATE(),
    reviewed_at DATETIME2,
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (reviewed_by) REFERENCES Users(user_id)
);
GO

-- =============================================
-- 2. BẢNG CHIẾN DỊCH TỪ THIỆN (Campaigns)
-- Node 2.0, Workflow 1
-- =============================================
CREATE TABLE Categories (
    category_id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX)
);

CREATE TABLE Campaigns (
    campaign_id INT PRIMARY KEY IDENTITY(1,1),
    creator_id INT NOT NULL,
    category_id INT,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NOT NULL,
    target_amount DECIMAL(18, 2) NOT NULL,
    current_amount DECIMAL(18, 2) DEFAULT 0,
    start_date DATETIME2,
    end_date DATETIME2,
    thumbnail_url NVARCHAR(255),
    
    -- Node 3.0: Xử lý tiền dư (Sung công quỹ hoặc Chuyển hoàn cảnh khác) [cite: 89, 90, 91]
    excess_fund_option VARCHAR(20) DEFAULT 'next_case' 
        CHECK (excess_fund_option IN ('reserve_fund', 'next_case')),
    
    -- Trạng thái chiến dịch (Workflow 1) [cite: 13, 104]
    status VARCHAR(20) DEFAULT 'draft' 
        CHECK (status IN ('draft', 'pending_approval', 'active', 'paused', 'completed', 'rejected', 'locked')),
    
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (creator_id) REFERENCES Users(user_id),
    FOREIGN KEY (category_id) REFERENCES Categories(category_id)
);

-- Node 2.1.1 & 2.1.4: Các giai đoạn gây quỹ
CREATE TABLE CampaignMilestones (
    milestone_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    title NVARCHAR(150),
    amount_needed DECIMAL(18, 2) NOT NULL,
    deadline DATETIME2,
    status VARCHAR(20) DEFAULT 'pending' 
        CHECK (status IN ('pending', 'in_progress', 'completed')),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id) ON DELETE CASCADE
);

-- Node 2.2.1: Tài liệu chứng minh
CREATE TABLE CampaignDocuments (
    document_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    file_url NVARCHAR(255) NOT NULL,
    file_type VARCHAR(10) NOT NULL 
        CHECK (file_type IN ('image', 'pdf', 'doc')),
    description NVARCHAR(255),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id) ON DELETE CASCADE
);
GO

-- =============================================
-- 3. BẢNG TÀI CHÍNH & QUYÊN GÓP (Donations)
-- Node 3.0, Workflow 2
-- =============================================
CREATE TABLE Donations (
    donation_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    user_id INT, -- NULL nếu quyên góp vãng lai
    amount DECIMAL(18, 2) NOT NULL CHECK (amount > 0),
    message NVARCHAR(MAX),
    is_anonymous BIT DEFAULT 0, -- 0: False, 1: True
    
    -- Thông tin thanh toán
    payment_method NVARCHAR(50), 
    transaction_code VARCHAR(100) UNIQUE,
    payment_status VARCHAR(20) DEFAULT 'pending' 
        CHECK (payment_status IN ('pending', 'success', 'failed')),
    
    donated_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Node 3.5: Lịch sử sao kê (Xuất Excel giống ngân hàng) [cite: 59, 93]
CREATE TABLE FinancialTransactions (
    transaction_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    type VARCHAR(10) NOT NULL CHECK (type IN ('in', 'out')), -- IN: Tiền vào, OUT: Tiền ra
    amount DECIMAL(18, 2) NOT NULL,
    description NVARCHAR(255),
    reference_id INT, -- ID của Donation hoặc DisbursementRequest
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id)
);
GO

-- =============================================
-- 4. BẢNG GIẢI NGÂN & MINH BẠCH (Disbursements)
-- Node 3.1, 3.2, Workflow 3
-- =============================================
CREATE TABLE DisbursementRequests (
    request_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    requester_id INT NOT NULL,
    amount DECIMAL(18, 2) NOT NULL CHECK (amount > 0),
    reason NVARCHAR(MAX) NOT NULL,
    
    -- Lưu danh sách ảnh hóa đơn dưới dạng JSON [cite: 116, 128]
    proof_images NVARCHAR(MAX), 
    
    status VARCHAR(20) DEFAULT 'pending' 
        CHECK (status IN ('pending', 'approved', 'rejected')),
    admin_note NVARCHAR(MAX),
    approved_by INT,
    approved_at DATETIME2,
    created_at DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id),
    FOREIGN KEY (requester_id) REFERENCES Users(user_id),
    FOREIGN KEY (approved_by) REFERENCES Users(user_id),
    
    -- Ràng buộc kiểm tra định dạng JSON cho proof_images
    CONSTRAINT [CK_Disbursement_Json] CHECK (ISJSON(proof_images) = 1)
);
GO

-- =============================================
-- 5. BẢNG TIN TỨC & TƯƠNG TÁC
-- Node 4.0, 4.1, 4.2
-- =============================================
CREATE TABLE CampaignUpdates (
    update_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    author_id INT NOT NULL,
    title NVARCHAR(255),
    content NVARCHAR(MAX) NOT NULL,
    
    type VARCHAR(20) DEFAULT 'general' 
        CHECK (type IN ('general', 'financial_report')),
    
    image_urls NVARCHAR(MAX), -- JSON array ảnh
    
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id),
    FOREIGN KEY (author_id) REFERENCES Users(user_id),
    CONSTRAINT [CK_Update_Images_Json] CHECK (ISJSON(image_urls) = 1 OR image_urls IS NULL)
);

CREATE TABLE Comments (
    comment_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    user_id INT NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Node 4.4: Báo cáo sai phạm [cite: 78]
CREATE TABLE Reports (
    report_id INT PRIMARY KEY IDENTITY(1,1),
    reporter_id INT NOT NULL,
    target_id INT NOT NULL,
    target_type VARCHAR(20) NOT NULL 
        CHECK (target_type IN ('campaign', 'user', 'comment')),
    reason NVARCHAR(MAX) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending' 
        CHECK (status IN ('pending', 'resolved', 'dismissed')),
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (reporter_id) REFERENCES Users(user_id)
);
GO

-- =============================================
-- 6. BẢNG QUẢN TRỊ & NHẬT KÝ HỆ THỐNG (Audit Logs)
-- Node 5.0, 5.4 - Quan trọng cho chống gian lận [cite: 81, 83]
-- =============================================
CREATE TABLE AuditLogs (
    log_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    action NVARCHAR(50) NOT NULL, -- Vd: 'APPROVE_DISBURSEMENT'
    table_name NVARCHAR(50) NOT NULL, 
    record_id INT NOT NULL,
    
    -- Lưu dữ liệu cũ/mới dạng JSON để đối chiếu
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    
    ip_address VARCHAR(45),
    user_agent NVARCHAR(255),
    created_at DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    CONSTRAINT [CK_Audit_Old_Json] CHECK (ISJSON(old_value) = 1 OR old_value IS NULL),
    CONSTRAINT [CK_Audit_New_Json] CHECK (ISJSON(new_value) = 1 OR new_value IS NULL)
);

CREATE TABLE Notifications (
    notification_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL,
    title NVARCHAR(255),
    message NVARCHAR(MAX),
    is_read BIT DEFAULT 0,
    type VARCHAR(20) DEFAULT 'system' 
        CHECK (type IN ('system', 'campaign_update', 'payment')),
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);
GO

-- =============================================
-- 7. TRIGGERS TỰ ĐỘNG CẬP NHẬT updated_at
-- (SQL Server không có ON UPDATE CURRENT_TIMESTAMP)
-- =============================================

CREATE TRIGGER trg_UpdateUsers
ON Users
AFTER UPDATE
AS
BEGIN
    UPDATE Users
    SET updated_at = GETDATE()
    FROM Users u
    INNER JOIN inserted i ON u.user_id = i.user_id;
END;
GO

CREATE TRIGGER trg_UpdateCampaigns
ON Campaigns
AFTER UPDATE
AS
BEGIN
    UPDATE Campaigns
    SET updated_at = GETDATE()
    FROM Campaigns c
    INNER JOIN inserted i ON c.campaign_id = i.campaign_id;
END;
GO

-- =============================================
-- 1. TẠO DỮ LIỆU NGƯỜI DÙNG (Users & Profiles)
-- =============================================
-- 1.1 Tạo Admin
INSERT INTO Users (username, email, password_hash, phone_number, role, status)
VALUES ('admin_system', 'admin@charity.com', 'hashed_pass_123', '0909000001', 'admin', 'active');

-- 1.2 Tạo Tổ chức từ thiện
INSERT INTO Users (username, email, password_hash, phone_number, role, status)
VALUES ('quytuthien_hcm', 'contact@quytuthien.com', 'hashed_pass_456', '0909000002', 'charity_org', 'active');

-- 1.3 Tạo Người ủng hộ (Mạnh thường quân)
INSERT INTO Users (username, email, password_hash, phone_number, role, status)
VALUES ('manhthuongquan_01', 'nguyenvana@gmail.com', 'hashed_pass_789', '0909000003', 'user', 'active');

-- Thêm Profile chi tiết
INSERT INTO UserProfiles (user_id, full_name, address, bio)
VALUES 
(1, N'Quản Trị Viên Hệ Thống', N'Hà Nội', N'Admin quản lý hệ thống'),
(2, N'Quỹ Từ Thiện TP.HCM', N'Quận 1, TP.HCM', N'Chuyên hỗ trợ các hoàn cảnh bệnh hiểm nghèo'),
(3, N'Nguyễn Văn A', N'Đà Nẵng', N'Thích làm việc thiện');
GO

-- =============================================
-- 2. TẠO DANH MỤC & CHIẾN DỊCH (Campaigns)
-- =============================================
-- 2.1 Danh mục
INSERT INTO Categories (name, description)
VALUES 
(N'Y tế', N'Hỗ trợ viện phí, phẫu thuật'),
(N'Giáo dục', N'Xây trường, tặng học bổng'),
(N'Khẩn cấp', N'Cứu trợ thiên tai, bão lũ');

-- 2.2 Tạo Chiến dịch: "Phẫu thuật tim cho bé An" (ID creator = 2 là Quỹ từ thiện)
INSERT INTO Campaigns (creator_id, category_id, title, description, target_amount, current_amount, start_date, end_date, status)
VALUES 
(2, 1, N'Phẫu thuật tim khẩn cấp cho bé An', 
 N'<h3>Hoàn cảnh:</h3><p>Bé An (5 tuổi) bị tim bẩm sinh cần mổ gấp...</p>', 
 50000000, -- Mục tiêu 50 triệu
 20000000, -- Đã có 20 triệu (số này sẽ khớp với bảng Donations)
 DATEADD(DAY, -10, GETDATE()), -- Bắt đầu 10 ngày trước
 DATEADD(DAY, 20, GETDATE()),  -- Kết thúc sau 20 ngày
 'active');

-- 2.3 Các giai đoạn gây quỹ (Milestones)
INSERT INTO CampaignMilestones (campaign_id, title, amount_needed, deadline, status)
VALUES 
(1, N'Giai đoạn 1: Phẫu thuật', 30000000, DATEADD(DAY, 5, GETDATE()), 'in_progress'),
(1, N'Giai đoạn 2: Hồi sức sau mổ', 20000000, DATEADD(DAY, 20, GETDATE()), 'pending');

-- 2.4 Tài liệu chứng minh (Giấy báo viện phí)
INSERT INTO CampaignDocuments (campaign_id, file_url, file_type, description)
VALUES 
(1, N'/uploads/docs/giay_bao_mo.jpg', 'image', N'Giấy chỉ định mổ của bệnh viện Tim');
GO

-- =============================================
-- 3. GHI NHẬN QUYÊN GÓP (Donations - Tiền vào)
-- =============================================
-- 3.1 Mạnh thường quân ủng hộ 15 triệu
INSERT INTO Donations (campaign_id, user_id, amount, message, is_anonymous, payment_method, transaction_code, payment_status)
VALUES 
(1, 3, 15000000, N'Mong bé sớm khỏe lại', 0, 'Momo', 'MOMO12345678', 'success');

-- 3.2 Một người ẩn danh ủng hộ 5 triệu
INSERT INTO Donations (campaign_id, user_id, amount, message, is_anonymous, payment_method, transaction_code, payment_status)
VALUES 
(1, NULL, 5000000, N'Của ít lòng nhiều', 1, 'Bank Transfer', 'VCB987654321', 'success');

-- Cập nhật vào bảng sao kê dòng tiền (Transaction Type = IN)
INSERT INTO FinancialTransactions (campaign_id, type, amount, description, reference_id)
VALUES 
(1, 'in', 15000000, N'Ủng hộ từ Nguyễn Văn A', 1),
(1, 'in', 5000000, N'Ủng hộ ẩn danh', 2);
GO

-- =============================================
-- 4. YÊU CẦU GIẢI NGÂN & MINH BẠCH (Disbursement - Tiền ra)
-- =============================================
-- 4.1 Quỹ tạo yêu cầu rút 10 triệu để đóng tạm ứng viện phí
-- Lưu ý cột proof_images dùng định dạng JSON
INSERT INTO DisbursementRequests (campaign_id, requester_id, amount, reason, proof_images, status, approved_by, approved_at)
VALUES 
(1, 2, 10000000, N'Đóng tạm ứng viện phí đợt 1', 
 N'[{"url": "/uploads/bill/bill_tam_ung.jpg", "desc": "Hóa đơn đỏ bệnh viện"}, {"url": "/uploads/img/be_an_nhap_vien.jpg"}]', 
 'approved', 1, GETDATE()); -- Đã được Admin (ID 1) duyệt

-- Cập nhật vào bảng sao kê dòng tiền (Transaction Type = OUT)
INSERT INTO FinancialTransactions (campaign_id, type, amount, description, reference_id)
VALUES 
(1, 'out', 10000000, N'Chi tạm ứng viện phí đợt 1 (Xem hóa đơn tại mục Minh bạch)', 1);
GO

-- =============================================
-- 5. TIN TỨC CẬP NHẬT (Updates)
-- =============================================
INSERT INTO CampaignUpdates (campaign_id, author_id, title, content, type, image_urls)
VALUES 
(1, 2, N'Bé An đã nhập viện chờ mổ', 
 N'Cảm ơn các MTQ, sáng nay bé đã làm thủ tục nhập viện...', 
 'general', 
 N'["/uploads/update/nhap_vien.jpg"]'),
 
(1, 2, N'Báo cáo tài chính đợt 1', 
 N'Chúng tôi đã rút 10tr để đóng viện phí, chi tiết xem sao kê.', 
 'financial_report', NULL);
GO

-- =============================================
-- 6. GHI LOG HỆ THỐNG (Audit Logs - Chống gian lận)
-- =============================================
-- Ghi lại hành động Admin duyệt chi tiền
INSERT INTO AuditLogs (user_id, action, table_name, record_id, old_value, new_value, ip_address, user_agent)
VALUES 
(1, 'APPROVE_DISBURSEMENT', 'DisbursementRequests', 1, 
 N'{"status": "pending"}', 
 N'{"status": "approved", "approved_by": 1}', 
 '192.168.1.10', 'Chrome/Windows 10');
GO