using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;

namespace PresentationLayer.Pages.Auth
{
    public class VerifyEmailModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public VerifyEmailModel(IUserService userService, ITokenService tokenService, IEmailService emailService)
        {
            _userService = userService;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                IsSuccess = false;
                Message = "Thông tin xác thực không hợp lệ hoặc bị thiếu.";
                return Page();
            }

            // Validate token
            bool isValidToken = _tokenService.ValidateVerificationToken(token, out var tokenEmail);
            if (!isValidToken || !string.Equals(email.Trim(), tokenEmail, StringComparison.OrdinalIgnoreCase))
            {
                IsSuccess = false;
                Message = "Liên kết kích hoạt không hợp lệ hoặc đã hết hạn (hiệu lực trong 24 giờ). Vui lòng liên hệ với Admin để nhận liên kết mới.";
                return Page();
            }

            // Check if user exists
            var user = await _userService.EmailExistsAsync(email);
            if (!user)
            {
                IsSuccess = false;
                Message = "Tài khoản giảng viên này không tồn tại trên hệ thống.";
                return Page();
            }

            // Find user details to check if they are already active
            var users = await _userService.GetAllUsersAsync();
            var matchedUser = System.Linq.Enumerable.FirstOrDefault(users, u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (matchedUser == null)
            {
                IsSuccess = false;
                Message = "Không thể tìm thấy thông tin chi tiết tài khoản.";
                return Page();
            }

            if (matchedUser.IsActive == true)
            {
                IsSuccess = true;
                EmailAddress = email;
                Message = "Tài khoản của bạn đã được kích hoạt. Xin vui lòng kiểm tra email để nhận tài khoản và mật khẩu đăng nhập.";
                return Page();
            }

            try
            {
                // Activate the user in the database
                var activated = await _userService.ActivateUserAsync(email);
                if (activated)
                {
                    const string tempPassword = "1234@AbcD";
                    var loginUrl = $"{Request.Scheme}://{Request.Host}/Auth/Login";
                    
                    // Send credentials email
                    await _emailService.SendLecturerCredentialsAsync(email, matchedUser.FullName, tempPassword, loginUrl);

                    IsSuccess = true;
                    EmailAddress = email;
                    Message = "Kích hoạt tài khoản thành công! Vui lòng kiểm tra email để nhận tài khoản và mật khẩu đăng nhập.";
                }
                else
                {
                    IsSuccess = false;
                    Message = "Đã xảy ra lỗi trong quá trình kích hoạt tài khoản. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                Message = "Lỗi hệ thống khi kích hoạt tài khoản: " + ex.Message;
            }

            return Page();
        }
    }
}
