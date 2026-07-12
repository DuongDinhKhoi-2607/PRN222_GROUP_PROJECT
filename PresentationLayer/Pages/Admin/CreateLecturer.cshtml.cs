using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class CreateLecturerModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;

        public CreateLecturerModel(IUserService userService, IEmailService emailService, ITokenService tokenService)
        {
            _userService = userService;
            _emailService = emailService;
            _tokenService = tokenService;
        }

        [BindProperty]
        public RegisterDto Input { get; set; } = new();

        public void OnGet()
        {
            Input.Role = "lecturer";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Set temporary password to 1234@AbcD and role to lecturer for validation
            const string tempPassword = "1234@AbcD";
            Input.Password = tempPassword;
            Input.ConfirmPassword = tempPassword;
            Input.Role = "lecturer";
            Input.IsActive = false; // Save as inactive until verified

            // Clear model state errors for fields that are set programmatically
            ModelState.Remove("Input.Password");
            ModelState.Remove("Input.ConfirmPassword");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userService.EmailExistsAsync(Input.Email);
            if (existingUser)
            {
                ModelState.AddModelError(string.Empty, "Địa chỉ email này đã được sử dụng.");
                return Page();
            }

            try
            {
                await _userService.RegisterUserAsync(Input);
                
                // Generate token valid for 24 hours
                var token = _tokenService.GenerateVerificationToken(Input.Email, DateTime.UtcNow.AddHours(24));
                var activationLink = $"{Request.Scheme}://{Request.Host}/Auth/VerifyEmail?email={Uri.EscapeDataString(Input.Email)}&token={Uri.EscapeDataString(token)}";

                // Send activation email
                await _emailService.SendActivationLinkAsync(Input.Email, Input.FullName, activationLink);

                TempData["SuccessMessage"] = $"Đã tạo tài khoản giảng viên thành công ở trạng thái chờ kích hoạt. Liên kết kích hoạt đã được gửi tới email {Input.Email}.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi tạo tài khoản: " + ex.Message);
                return Page();
            }
        }
    }
}
