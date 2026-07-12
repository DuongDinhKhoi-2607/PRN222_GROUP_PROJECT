<<<<<<< HEAD
# Trợ Lý Học Tập RAG AI Assistant - Hướng Dẫn Cài Đặt và Sử Dụng

Chào mừng bạn đến với **RAG AI Assistant**, ứng dụng web hỗ trợ học tập thông minh sử dụng mô hình ngôn ngữ lớn (LLM) và kỹ thuật RAG (Retrieval-Augmented Generation). Dự án được phát triển trên nền tảng **C# .NET Core** với kiến trúc 3 lớp (Three-Layer Architecture): Presentation, Business và Data Access.

Tài liệu này sẽ hướng dẫn bạn (hoặc khách hàng của bạn) cách thiết lập cơ sở dữ liệu, lấy khóa API, cấu hình và chạy ứng dụng một cách nhanh chóng và dễ dàng.

---

## 📌 Yêu Cầu Hệ Thống (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các phần mềm sau:
1. **.NET 10.0 SDK** (Hoặc phiên bản .NET mới nhất).
2. **Microsoft SQL Server** (Bản Express, Developer hoặc LocalDB).
3. **SQL Server Management Studio (SSMS)** hoặc Azure Data Studio (dùng để chạy file script database).
4. **Trình duyệt web** hiện đại (Chrome, Edge, Firefox).

---

## 🛠️ Bước 1: Thiết Lập Cơ Sở Dữ Liệu (SQL Server)

Hệ thống đi kèm với file script SQL đầy đủ để khởi tạo cấu trúc cơ sở dữ liệu.

1. Mở **SQL Server Management Studio (SSMS)** và kết nối với SQL Server của bạn.
2. Mở file [RAGChatbotDB_Simple_CreateDatabase_CreateTables.sql](file:///e:/PRN222/Assignment2/RAGChatbotDB_Simple_CreateDatabase_CreateTables.sql) trong SSMS (hoặc sao chép nội dung của file).
3. Nhấp **Execute** (hoặc nhấn `F5`) để chạy script.
   - Script này sẽ tự động tạo cơ sở dữ liệu có tên là `RAGChatbotDB`.
   - Các bảng, chỉ mục (indexes), khóa ngoại (foreign keys) và các ràng buộc kiểm tra định dạng dữ liệu (check constraints) sẽ được thiết lập tự động.

---

## 🔑 Bước 2: Lấy Các Khóa API Cần Thiết

### 1. Khóa Google Gemini API (Để chatbot trả lời câu hỏi)
Ứng dụng sử dụng mô hình Gemini của Google để phân tích và trả lời câu hỏi dựa trên ngữ cảnh tài liệu.
1. Truy cập trang **[Google AI Studio](https://aistudio.google.com/)**.
2. Đăng nhập bằng tài khoản Google của bạn.
3. Bấm vào nút **"Get API Key"** ở góc trên bên trái.
4. Chọn **"Create API Key"** (Tạo khóa API mới) và sao chép mã khóa được tạo (có dạng `AIzaSy...`).

### 2. Cấu hình Email SMTP (Để gửi email kích hoạt tài khoản Giảng viên)
Ứng dụng sử dụng dịch vụ SMTP để gửi link kích hoạt và mật khẩu tạm thời cho Giảng viên khi Admin tạo tài khoản.
Để cấu hình thông qua **Gmail**:
1. Đăng nhập vào tài khoản Gmail gửi thư của bạn.
2. Truy cập **Google Account Settings** (Quản lý tài khoản Google) -> **Security** (Bảo mật).
3. Đảm bảo đã bật **2-Step Verification** (Xác minh 2 bước).
4. Tìm kiếm mục **App Passwords** (Mật khẩu ứng dụng).
5. Tạo một mật khẩu ứng dụng mới cho danh mục "Thư" hoặc ứng dụng tùy chỉnh, đặt tên (ví dụ: `RAG Assistant`), sau đó sao chép mật khẩu 16 chữ số được cấp (bỏ qua dấu cách).

> 💡 **LƯU Ý KHI KIỂM THỬ LOCAL (Không cần cài SMTP):**
> Nếu bạn không muốn cài đặt SMTP hoặc không có mạng, hệ thống hỗ trợ **Local Logging**. Tất cả các email kích hoạt gửi đi sẽ tự động được ghi lại thông tin (bao gồm liên kết kích hoạt và mật khẩu tạm thời) trong file log cục bộ [verification_emails.log](file:///e:/PRN222/Assignment2/verification_emails.log) tại thư mục gốc của dự án. Bạn có thể mở file này để lấy link kích hoạt tài khoản.

---

## ⚙️ Bước 3: Cấu Hình Ứng Dụng (`appsettings.json`)

Mở file cấu hình [appsettings.json](file:///e:/PRN222/Assignment2/PresentationLayer/appsettings.json) bằng một trình soạn thảo văn bản (như VS Code hoặc Notepad) và cập nhật các thông số sau:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RAGChatbotDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Gemini": {
    "ApiKey": "ĐIỀN_API_KEY_GEMINI_CỦA_BẠN_VÀO_ĐÂY"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "RAG AI Assistant",
    "SenderEmail": "ĐIỀN_EMAIL_GỬI_THƯ_CỦA_BẠN",
    "Password": "ĐIỀN_MẬT_KHẨU_ỨNG_DỤNG_GMAIL_16_KÝ_TỰ"
  }
}
```

*Lưu ý về Connection String:*
- Nếu sử dụng SQL Server LocalDB, chỉnh Server thành `(localdb)\\MSSQLLocalDB`.
- Nếu đăng nhập SQL Server bằng tài khoản sa, đổi sang: `Server=YOUR_SERVER;Database=RAGChatbotDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;`.

---

## 🚀 Bước 4: Khởi Chạy Ứng Dụng

Bạn có thể chạy ứng dụng bằng cách sử dụng Terminal/Command Prompt:

1. Mở Terminal (CMD / PowerShell / Git Bash).
2. Di chuyển thư mục làm việc đến thư mục gốc của dự án (`Assignment2`).
3. Chạy lệnh sau để build dự án:
   ```bash
   dotnet build
   ```
4. Di chuyển vào thư mục PresentationLayer hoặc chạy trực tiếp bằng lệnh sau từ thư mục gốc:
   ```bash
   dotnet run --project PresentationLayer
   ```
5. Khi terminal hiển thị thông báo ứng dụng đã chạy thành công, hãy mở trình duyệt và truy cập vào địa chỉ mặc định (thường là `https://localhost:7080` hoặc cổng HTTP được hiển thị trên console).

---

## 🔑 Tài Khoản Mặc Định và Kịch Bản Sử Dụng

Hệ thống sẽ tự động khởi tạo (seed) dữ liệu ban đầu khi bạn chạy lần đầu tiên:

### 1. Đăng nhập với vai trò Admin (Quản trị viên)
- **Email:** `demo@ragassistant.local`
- **Mật khẩu:** `admin123`
- **Chức năng chính:** Quản lý môn học, tạo tài khoản Giảng viên (Lecturer), quản lý quyền tải tài liệu của giảng viên.

### 2. Luồng kích hoạt tài khoản Giảng viên (Lecturer)
1. Đăng nhập bằng tài khoản Admin ở trên.
2. Vào phần quản lý Giảng viên và bấm **Tạo mới Giảng viên**. Nhập tên và email của Giảng viên.
3. Hệ thống sẽ gửi email chứa liên kết kích hoạt (hoặc bạn có thể mở file [verification_emails.log](file:///e:/PRN222/Assignment2/verification_emails.log) ở thư mục gốc của dự án để lấy liên kết này).
4. Truy cập liên kết kích hoạt đó trên trình duyệt. Tài khoản sẽ được kích hoạt và hệ thống sẽ cấp một mật khẩu tạm thời mặc định là `1234@AbcD` (hoặc mật khẩu ngẫu nhiên được hiển thị).
5. Đăng nhập bằng tài khoản Giảng viên mới kích hoạt. 
6. Hệ thống sẽ áp dụng bộ lọc **ForcePasswordChangeFilter**, bắt buộc Giảng viên phải đổi mật khẩu ngay lập tức tại trang `/Auth/ChangePassword` trước khi có thể tiếp tục sử dụng các chức năng khác để đảm bảo tính bảo mật.
7. Sau khi đổi mật khẩu, Giảng viên có thể tải lên tài liệu cho các môn học được phân quyền và hệ thống sẽ tự động phân tách tài liệu thành các khối thông tin (chunks) và nhúng vector (embeddings).

### 3. Đăng ký & Đăng nhập vai trò Sinh viên (Student)
- Sinh viên có thể tự do đăng ký tài khoản mới trực tiếp trên trang đăng ký (`/Auth/Register`).
- Sinh viên được quyền vào phòng chat, chọn môn học và thực hiện hỏi đáp trực tiếp với trợ lý AI dựa trên tài liệu môn học đó đã được giảng viên tải lên.
=======
# PRN222_GROUP_PROJECT
>>>>>>> ffd9614e5ba453d033a0c199d6ed8f5d6e04e6de
