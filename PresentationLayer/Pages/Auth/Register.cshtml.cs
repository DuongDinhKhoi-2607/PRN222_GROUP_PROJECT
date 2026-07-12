using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public RegisterDto Input { get; set; } = new();

        public IActionResult OnGet(string? returnUrl)
        {
            Input.ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.Role = "student";

            if (!ModelState.IsValid)
                return Page();

            var existingUser = await _userService.EmailExistsAsync(Input.Email);
            if (existingUser)
            {
                ModelState.AddModelError(string.Empty, "Địa chỉ email này đã được sử dụng.");
                return Page();
            }

            try
            {
                await _userService.RegisterUserAsync(Input);
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Hãy đăng nhập.";
                return RedirectToPage("Login", new { returnUrl = Input.ReturnUrl });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi đăng ký: " + ex.Message);
                return Page();
            }
        }
    }
}
