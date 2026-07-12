using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly IHubContext<DocumentHub> _hubContext;

        public IndexModel(
            IPermissionService permissionService,
            IUserService userService,
            IHubContext<DocumentHub> hubContext)
        {
            _permissionService = permissionService;
            _userService = userService;
            _hubContext = hubContext;
        }

        public IEnumerable<LecturerPermissionDto> Permissions { get; set; } = new List<LecturerPermissionDto>();
        public IEnumerable<UserDto> Lecturers { get; set; } = new List<UserDto>();
        public IEnumerable<UserDto> Students { get; set; } = new List<UserDto>();

        public async Task OnGetAsync()
        {
            Permissions = await _permissionService.GetAllPermissionsAsync();
            var allUsers = await _userService.GetAllUsersAsync();
            
            Lecturers = allUsers.Where(u => string.Equals(u.Role, "lecturer", StringComparison.OrdinalIgnoreCase)).ToList();
            Students = allUsers.Where(u => string.Equals(u.Role, "student", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<IActionResult> OnPostRevokeAsync(long id)
        {
            await _permissionService.RevokePermissionAsync(id);
            await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "PermissionsUpdated", "Một quyền truy cập môn học đã được thu hồi bởi quản trị viên.");
            
            TempData["SuccessMessage"] = "Đã thu hồi quyền thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(long id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (success)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "UserDeleted", "Một tài khoản người dùng đã được xóa bởi quản trị viên.");
                    TempData["SuccessMessage"] = "Đã xóa tài khoản thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tìm thấy tài khoản để xóa.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tài khoản: " + ex.Message;
            }
            return RedirectToPage();
        }
    }
}
