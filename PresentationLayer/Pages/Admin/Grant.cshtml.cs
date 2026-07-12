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
    public class GrantModel : PageModel
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly ISubjectService _subjectService;
        private readonly IHubContext<DocumentHub> _hubContext;

        public GrantModel(
            IPermissionService permissionService,
            IUserService userService,
            ISubjectService subjectService,
            IHubContext<DocumentHub> hubContext)
        {
            _permissionService = permissionService;
            _userService = userService;
            _subjectService = subjectService;
            _hubContext = hubContext;
        }

        [BindProperty]
        public GrantPermissionDto Input { get; set; } = new();

        public IEnumerable<UserDto> Lecturers { get; set; } = new List<UserDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        private async Task LoadDropdownsAsync()
        {
            Lecturers = await _userService.GetAllLecturersAsync();
            Subjects = await _subjectService.GetAllAsync();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            // Check if permission already exists for this exact pair
            var exists = await _permissionService.PermissionExistsAsync(Input.LecturerId, Input.SubjectId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Quyền này đã được cấp trước đó cho giảng viên này ở môn học này.";
                await LoadDropdownsAsync();
                return Page();
            }

            // Check if the subject is already assigned to someone else
            if (Input.CanUpload)
            {
                var isAssignedToOther = await _permissionService.IsSubjectAssignedToAnotherLecturerAsync(Input.SubjectId, Input.LecturerId);
                if (isAssignedToOther)
                {
                    var assignedName = await _permissionService.GetAssignedLecturerNameAsync(Input.SubjectId);
                    TempData["ErrorMessage"] = $"Môn học này đã được cấp quyền upload cho giảng viên {assignedName ?? "khác"}. Mỗi môn học chỉ được 1 người upload.";
                    await LoadDropdownsAsync();
                    return Page();
                }
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var adminId))
                return Unauthorized();

            await _permissionService.GrantPermissionAsync(Input, adminId);
            await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "PermissionsUpdated", "Quyền truy cập môn học đã được cập nhật bởi quản trị viên.");
            
            TempData["SuccessMessage"] = "Cấp quyền thành công!";
            return RedirectToPage("Index");
        }
    }
}
