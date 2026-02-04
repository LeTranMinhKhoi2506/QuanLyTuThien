INSERT INTO Users (username, email, password_hash, phone_number, role, status)
VALUES 
('hoichuthapdo', 'info@redcross.org.vn', 'hash_1', '02438224030', 'charity_org', 'active'),
('quy_hi_vong', 'hope@fpt.com.vn', 'hash_2', '0987654321', 'charity_org', 'active'),
('manh_thuong_quan_2', 'tranvanb@gmail.com', 'hash_3', '0912345678', 'user', 'active');

INSERT INTO UserProfiles (user_id, full_name, address, bio)
VALUES 
(4, N'Hội Chữ Thập Đỏ Việt Nam', N'82 Nguyễn Du, Hà Nội', N'Tổ chức nhân đạo chuyên nghiệp'),
(5, N'Quỹ Hy Vọng (Hope Foundation)', N'Tòa nhà FPT, Cầu Giấy', N'Hỗ trợ trẻ em vùng cao và hoàn cảnh khó khăn'),
(6, N'Trần Văn B', N'Quận 7, TP.HCM', N'Nhà hảo tâm cá nhân');

INSERT INTO Campaigns (creator_id, category_id, title, description, target_amount, current_amount, start_date, end_date, status, excess_fund_option, thumbnail_url)
VALUES 
-- Chiến dịch 2: Giáo dục vùng cao
(5, 2, N'Xây trường mới cho trẻ em Mường Nhé', 
 N'Ngôi trường cũ tại xã Leng Su Sìn đã xuống cấp nghiêm trọng, mái dột nát mỗi khi mưa về...', 
 450000000, 120500000, GETDATE(), DATEADD(MONTH, 3, GETDATE()), 'active', 'next_case', '/img/muongnhe.jpg'),
 (4, 3, N'Cứu trợ khẩn cấp lũ lụt miền Trung 2025', 
 N'Hỗ trợ nhu yếu phẩm, phao cứu sinh và thuốc men cho bà con vùng lũ...', 
 1000000000, 1005000000, DATEADD(MONTH, -2, GETDATE()), DATEADD(DAY, -5, GETDATE()), 'completed', 'reserve_fund', '/img/lut.jpg'),

-- Chiến dịch 4: Y tế (Hỗ trợ người già)
(2, 1, N'Ánh sáng cho người nghèo bị đục thủy tinh thể', 
 N'Chương trình mổ mắt miễn phí cho 100 cụ già có hoàn cảnh khó khăn tại vùng sâu vùng xa...', 
 200000000, 45000000, GETDATE(), DATEADD(MONTH, 2, GETDATE()), 'active', 'next_case', '/img/mat.jpg'),

-- Chiến dịch 5: Giáo dục (Học bổng)
(5, 2, N'Học bổng "Tiếp sức đến trường" 2026', 
 N'Trao 500 suất học bổng cho học sinh nghèo học giỏi trên toàn quốc...', 
 500000000, 0, DATEADD(DAY, 5, GETDATE()), DATEADD(MONTH, 4, GETDATE()), 'pending_approval', 'reserve_fund', '/img/hocbong.jpg'),

-- Chiến dịch 6: Y tế (Xây trạm xá)
(4, 1, N'Xây trạm y tế xã biên giới Việt - Lào', 
 N'Dự án cải thiện cơ sở hạ tầng y tế cơ bản cho người dân tộc thiểu số...', 
 800000000, 250000000, DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 5, GETDATE()), 'active', 'next_case', '/img/tramxa.jpg'),

-- Chiến dịch 7: Khẩn cấp (Cháy nhà)
(2, 3, N'Hỗ trợ gia đình nạn nhân vụ cháy tại Quận 8', 
 N'Gia đình anh H bị cháy rụi tài sản, cần sự hỗ trợ để ổn định cuộc sống tạm thời...', 
 30000000, 30000000, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, 10, GETDATE()), 'paused', 'next_case', '/img/chay.jpg');

 INSERT INTO CampaignMilestones (campaign_id, title, amount_needed, deadline, status)
VALUES 
(2, N'Giai đoạn 1: San lấp mặt bằng', 100000000, DATEADD(MONTH, 1, GETDATE()), 'pending'),
(2, N'Giai đoạn 2: Xây thô và lợp mái', 250000000, DATEADD(MONTH, 2, GETDATE()), 'pending'),
(4, N'Giai đoạn 1: Khám sàng lọc', 50000000, DATEADD(DAY, 15, GETDATE()), 'in_progress');

INSERT INTO CampaignDocuments (campaign_id, file_url, file_type, description)
VALUES 
(2, '/docs/giay_phep_xay_dung.pdf', 'pdf', N'Giấy phép xây dựng trường học'),
(3, '/docs/danh_sach_ho_tro.pdf', 'pdf', N'Danh sách hộ dân nhận cứu trợ'),
(6, '/docs/ban_ve_thiet_ke.jpg', 'image', N'Bản vẽ thiết kế trạm y tế');

INSERT INTO Donations (campaign_id, user_id, amount, message, is_anonymous, payment_method, transaction_code, payment_status)
VALUES 
(2, 6, 50000000, N'Góp gạch xây trường cho các con', 0, 'Bank Transfer', 'VCB111', 'success'),
(2, 3, 10000000, N'Chúc dự án sớm hoàn thành', 0, 'Momo', 'MOMO222', 'success'),
(4, NULL, 2000000, N'Hỗ trợ các cụ', 1, 'ZaloPay', 'ZALO333', 'success'),
(3, 6, 100000000, N'Chia sẻ khó khăn miền Trung', 0, 'Bank Transfer', 'VCB444', 'success');

INSERT INTO FinancialTransactions (campaign_id, type, amount, description, reference_id)
VALUES 
(2, 'in', 50000000, N'Ủng hộ từ Trần Văn B', 3),
(2, 'in', 10000000, N'Ủng hộ từ Nguyễn Văn A', 4),
(3, 'in', 100000000, N'Ủng hộ từ Trần Văn B', 6);