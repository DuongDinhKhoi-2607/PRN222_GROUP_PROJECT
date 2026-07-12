using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;
using BussinessLayer.Interfaces;
using System;

namespace PresentationLayer.Filters
{
    public class ForcePasswordChangeFilter : IAsyncPageFilter
    {
        private readonly IUserService _userService;

        public ForcePasswordChangeFilter(IUserService userService)
        {
            _userService = userService;
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim == "lecturer")
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                    {
                        var path = context.HttpContext.Request.Path.Value ?? "";
                        
                        // Tránh vòng lặp chuyển hướng: bỏ qua nếu đang ở trang Đổi mật khẩu hoặc Đăng xuất
                        if (!path.Contains("/Auth/ChangePassword", StringComparison.OrdinalIgnoreCase) &&
                            !path.Contains("/Auth/Logout", StringComparison.OrdinalIgnoreCase))
                        {
                            var isTemp = await _userService.IsUsingTempPasswordAsync(userId);
                            if (isTemp)
                            {
                                context.Result = new RedirectToPageResult("/Auth/ChangePassword", new { forced = true });
                                return;
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
