using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;

namespace PresentationLayer.Pages.Auth
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly IUserService _userService;

        public ChangePasswordModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty(SupportsGet = true)]
        public bool Forced { get; set; }

        [BindProperty]
        public ChangePasswordInput Input { get; set; } = new();

        public class ChangePasswordInput
        {
            [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu hiện tại")]
            public string CurrentPassword { get; set; } = null!;

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
            [MinLength(6, ErrorMessage = "Mật khẩu mới phải dài ít nhất 6 ký tự.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; } = null!;

            [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc.")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            public string ConfirmNewPassword { get; set; } = null!;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            // Chống trường hợp cố tình đặt lại mật khẩu tạm mặc định
            if (Input.NewPassword == "1234@AbcD")
            {
                ModelState.AddModelError(string.Empty, "Bạn không được sử dụng lại mật khẩu tạm thời mặc định.");
                return Page();
            }

            var success = await _userService.ChangePasswordAsync(userId, Input.CurrentPassword, Input.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không chính xác.");
                return Page();
            }

            TempData["SuccessMessage"] = "Thay đổi mật khẩu thành công!";
            
            if (Forced)
            {
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim == "lecturer")
                {
                    return RedirectToPage("/Document/Index");
                }
                else if (roleClaim == "admin")
                {
                    return RedirectToPage("/Admin/Index");
                }
                return RedirectToPage("/Index");
            }

            // Clear input fields after successful change
            Input = new ChangePasswordInput();
            ModelState.Clear();

            return Page();
        }
    }
}
