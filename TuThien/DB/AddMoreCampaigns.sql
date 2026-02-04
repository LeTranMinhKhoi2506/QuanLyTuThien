-- =============================================
-- Thêm campaigns ?? test hi?n th? top 3
-- =============================================

USE TuThien;
GO

-- Thêm thêm campaigns cho category Giáo d?c (category_id = 2)
INSERT INTO Campaigns (creator_id, category_id, title, description, target_amount, current_amount, start_date, end_date, status, excess_fund_option, thumbnail_url, created_at, updated_at)
VALUES 
-- Campaign 3 - Giáo d?c
(2, 2, N'Xây d?ng th? vi?n cho tr??ng vùng cao', 
 N'Tr??ng ti?u h?c xã M??ng Kim, huy?n M??ng La, t?nh S?n La hi?n ch?a có th? vi?n. Các em h?c sinh ph?i h?c trong ?i?u ki?n thi?u th?n sách v?, tài li?u tham kh?o...', 
 30000000, -- M?c tiêu 30 tri?u
 5000000,  -- ?ã nh?n 5 tri?u
 DATEADD(DAY, -5, GETDATE()), 
 DATEADD(DAY, 25, GETDATE()), 
 'active',
 'next_case',
 '/images/campaigns/library.jpg',
 DATEADD(DAY, -5, GETDATE()),
 GETDATE()),

-- Campaign 4 - Y t?
(2, 1, N'H? tr? chi phí ?i?u tr? ung th? cho bà Lan', 
 N'Bà Nguy?n Th? Lan (68 tu?i) ? ??ng Nai m?c b?nh ung th? ph?i giai ?o?n mu?n, gia ?ình nghèo không có kh? n?ng chi tr? vi?n phí...', 
 80000000,
 15000000,
 DATEADD(DAY, -8, GETDATE()), 
 DATEADD(DAY, 22, GETDATE()), 
 'active',
 'reserve_fund',
 '/images/campaigns/cancer-treatment.jpg',
 DATEADD(DAY, -8, GETDATE()),
 GETDATE()),

-- Campaign 5 - Giáo d?c
(2, 2, N'T?ng máy tính cho h?c sinh nghèo v??t khó', 
 N'Ch??ng trình t?ng máy tính, laptop cho 50 h?c sinh nghèo v??t khó có thành tích h?c t?p xu?t s?c t?i các t?nh mi?n núi phía B?c...', 
 100000000,
 30000000,
 DATEADD(DAY, -12, GETDATE()), 
 DATEADD(DAY, 18, GETDATE()), 
 'active',
 'next_case',
 '/images/campaigns/computers-for-students.jpg',
 DATEADD(DAY, -12, GETDATE()),
 GETDATE()),

-- Campaign 6 - Kh?n c?p
(2, 3, N'C?u tr? l? l?t mi?n Trung 2024', 
 N'M?a l? l?n ?ã gây thi?t h?i n?ng n? t?i các t?nh Qu?ng Bình, Qu?ng Tr?, Th?a Thiên Hu?. Hàng ngàn h? dân b? ng?p l?t, c?n h? tr? kh?n c?p...', 
 200000000,
 120000000,
 DATEADD(DAY, -2, GETDATE()), 
 DATEADD(DAY, 13, GETDATE()), 
 'active',
 'extend',
 '/images/campaigns/flood-relief.jpg',
 DATEADD(DAY, -2, GETDATE()),
 GETDATE());

GO

-- Thêm m?t s? donations cho các campaigns m?i
INSERT INTO Donations (campaign_id, user_id, amount, message, is_anonymous, payment_method, transaction_code, payment_status, donated_at)
VALUES 
-- Donations cho Campaign 3
(3, 3, 3000000, N'?ng h? xây th? vi?n', 0, 'Momo', 'MOMO_LIB_001', 'success', DATEADD(DAY, -4, GETDATE())),
(3, NULL, 2000000, N'Chúc các em h?c gi?i', 1, 'Bank Transfer', 'VCB_LIB_002', 'success', DATEADD(DAY, -3, GETDATE())),

-- Donations cho Campaign 4
(4, 3, 10000000, N'Mong bà mau bình ph?c', 0, 'Bank Transfer', 'VCB_CANCER_001', 'success', DATEADD(DAY, -7, GETDATE())),
(4, NULL, 5000000, N'?ng h? bà Lan', 1, 'Momo', 'MOMO_CANCER_002', 'success', DATEADD(DAY, -5, GETDATE())),

-- Donations cho Campaign 5
(5, 3, 20000000, N'?ng h? các em h?c sinh', 0, 'Bank Transfer', 'VCB_PC_001', 'success', DATEADD(DAY, -10, GETDATE())),
(5, NULL, 10000000, N'H? tr? mua máy tính', 1, 'Momo', 'MOMO_PC_002', 'success', DATEADD(DAY, -9, GETDATE())),

-- Donations cho Campaign 6
(6, 3, 50000000, N'C?u tr? l? l?t', 0, 'Bank Transfer', 'VCB_FLOOD_001', 'success', DATEADD(DAY, -1, GETDATE())),
(6, NULL, 70000000, N'?ng h? ??ng bào mi?n Trung', 1, 'Momo', 'MOMO_FLOOD_002', 'success', GETDATE());

GO

-- C?p nh?t FinancialTransactions
INSERT INTO FinancialTransactions (campaign_id, type, amount, description, reference_id, created_at)
VALUES 
-- Campaign 3
(3, 'in', 3000000, N'Quyên góp t? Nguy?n V?n A', 3, DATEADD(DAY, -4, GETDATE())),
(3, 'in', 2000000, N'Quyên góp ?n danh', 4, DATEADD(DAY, -3, GETDATE())),

-- Campaign 4
(4, 'in', 10000000, N'Quyên góp t? Nguy?n V?n A', 5, DATEADD(DAY, -7, GETDATE())),
(4, 'in', 5000000, N'Quyên góp ?n danh', 6, DATEADD(DAY, -5, GETDATE())),

-- Campaign 5
(5, 'in', 20000000, N'Quyên góp t? Nguy?n V?n A', 7, DATEADD(DAY, -10, GETDATE())),
(5, 'in', 10000000, N'Quyên góp ?n danh', 8, DATEADD(DAY, -9, GETDATE())),

-- Campaign 6
(6, 'in', 50000000, N'Quyên góp t? Nguy?n V?n A', 9, DATEADD(DAY, -1, GETDATE())),
(6, 'in', 70000000, N'Quyên góp ?n danh', 10, GETDATE());

GO

-- Ki?m tra k?t qu?
SELECT 
    c.campaign_id,
    cat.name AS category,
    c.title,
    c.target_amount,
    c.current_amount,
    c.status,
    c.created_at
FROM Campaigns c
INNER JOIN Categories cat ON c.category_id = cat.category_id
WHERE c.status = 'active'
ORDER BY cat.name, c.created_at DESC;

GO

PRINT '?ã thêm thành công 4 campaigns m?i!';
PRINT 'Category Giáo d?c: 3 campaigns';
PRINT 'Category Y t?: 2 campaigns';
PRINT 'Category Kh?n c?p: 2 campaigns';
GO
