# H??ng d?n c?u hình Email ?? g?i mã xác nh?n quên m?t kh?u

## ?? C?u hình Gmail SMTP

### B??c 1: B?t xác th?c 2 b??c cho Gmail
1. Truy c?p https://myaccount.google.com/security
2. Tìm "Xác minh 2 b??c" và b?t nó lên

### B??c 2: T?o App Password (M?t kh?u ?ng d?ng)
1. Truy c?p https://myaccount.google.com/apppasswords
2. Ch?n "App" ? "Mail"
3. Ch?n "Device" ? "Other" và ??t tên (ví d?: "TuThien App")
4. Nh?n "Generate"
5. Copy m?t kh?u 16 ký t? (??nh d?ng: xxxx xxxx xxxx xxxx)

### B??c 3: C?p nh?t appsettings.json

M? file `appsettings.json` và c?p nh?t ph?n EmailSettings:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@gmail.com",          // ? Email Gmail c?a b?n
  "SmtpPassword": "xxxx xxxx xxxx xxxx",       // ? App Password v?a t?o (không có kho?ng tr?ng)
  "FromEmail": "your-email@gmail.com",         // ? Email ng??i g?i
  "FromName": "H? th?ng T? Thi?n"
}
```

**Ví d?:**
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "tuthien2024@gmail.com",
  "SmtpPassword": "abcdxyzw12345678",
  "FromEmail": "tuthien2024@gmail.com",
  "FromName": "H? th?ng T? Thi?n"
}
```

### B??c 4: Kh?i ??ng l?i ?ng d?ng
Sau khi c?p nh?t appsettings.json, kh?i ??ng l?i ?ng d?ng ?? áp d?ng c?u hình m?i.

---

## ?? S? d?ng nhà cung c?p email khác

### Outlook/Hotmail:
```json
"EmailSettings": {
  "SmtpHost": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@outlook.com",
  "SmtpPassword": "your-password"
}
```

### Custom SMTP Server:
```json
"EmailSettings": {
  "SmtpHost": "smtp.yourdomain.com",
  "SmtpPort": "587",
  "SmtpUser": "noreply@yourdomain.com",
  "SmtpPassword": "your-password"
}
```

---

## ?? Ki?m tra email có ho?t ??ng

1. Ch?y ?ng d?ng
2. Vào trang ??ng nh?p ? "Quên m?t kh?u?"
3. Nh?p email ?ã ??ng ký
4. Ki?m tra h?p th? (inbox ho?c spam)
5. N?u không nh?n ???c email, ki?m tra logs trong Visual Studio Output

---

## ?? L?u ý quan tr?ng

1. **Không commit file appsettings.json** ch?a thông tin email th?t lên Git/GitHub
2. S? d?ng **appsettings.Development.json** cho môi tr??ng dev
3. S? d?ng **User Secrets** ho?c **Environment Variables** cho production
4. App Password có th? b? vô hi?u n?u ??i m?t kh?u Gmail
5. N?u v?n không g?i ???c, ki?m tra:
   - Firewall/Antivirus có ch?n port 587 không
   - K?t n?i internet
   - Logs trong Visual Studio Output

---

## ?? Ch?c n?ng ?ã hoàn thành

? ViewModel: `ForgotPasswordViewModel`  
? Controller Actions: `ForgotPassword (GET/POST)`, `ResetPassword (GET/POST)`  
? Views: `ForgotPassword.cshtml`, `ResetPassword.cshtml`  
? Email Service: `SendPasswordResetEmailAsync`  
? Mã xác nh?n 6 ch? s?  
? Session timeout 15 phút  
? M?t kh?u ???c hash b?ng BCrypt  

---

## ?? Flow ho?t ??ng

1. User nh?n "Quên m?t kh?u?" ? Nh?p email
2. H? th?ng t?o mã 6 ch? s? ng?u nhiên
3. G?i email ch?a mã xác nh?n
4. User nh?p mã + m?t kh?u m?i
5. H? th?ng xác th?c mã và ??t l?i m?t kh?u
6. User ??ng nh?p v?i m?t kh?u m?i

---

N?u g?p v?n ??, hãy ki?m tra Output logs trong Visual Studio!
