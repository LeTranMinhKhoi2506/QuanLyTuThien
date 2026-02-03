-- Script để cập nhật password_hash cho các user hiện có
-- Sử dụng BCrypt để hash password

USE TuThien;
GO

-- Cập nhật password cho admin (password: admin123)
-- BCrypt hash của "admin123"
UPDATE Users 
SET password_hash = '$2a$11$5Z7qQX4KqP6HlQy5Y8Z3qOs9v3Pq5Y8Z3qOs9v3Pq5Y8Z3qOs9v3Pq'
WHERE username = 'admin_system';

-- Cập nhật password cho quỹ từ thiện (password: charity123)
UPDATE Users 
SET password_hash = '$2a$11$6A8rRY5LrQ7ImRz6Z9A4rOt0w4Qr6Z9A4rOt0w4Qr6Z9A4rOt0w4Qr'
WHERE username = 'quytuthien_hcm';

-- Cập nhật password cho user thường (password: user123)
UPDATE Users 
SET password_hash = '$2a$11$7B9sS Z6MsR8JnSa7A0B5sP1x5Rs7A0B5sP1x5Rs7A0B5sP1x5Rs'
WHERE username = 'manhthuongquan_01';

GO

-- Hoặc bạn có thể tạo user mới với password đã hash:
/*
-- Tạo user test với password là "123456"
INSERT INTO Users (username, email, password_hash, phone_number, role, status, created_at, updated_at)
VALUES 
('testuser', 'test@example.com', '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOP', '0909000004', 'user', 'active', GETDATE(), GETDATE());

-- Tạo profile cho user test
INSERT INTO UserProfiles (user_id, full_name, address, bio)
VALUES 
(SCOPE_IDENTITY(), N'Người dùng Test', N'Hà Nội', N'Tài khoản test');
*/

PRINT N'Đã cập nhật password_hash cho các user. 
Lưu ý: Các hash trên chỉ là ví dụ. 
Để có hash thực, bạn cần đăng ký user mới qua form Register hoặc dùng BCrypt.Net trong C# để tạo hash.';
GO
