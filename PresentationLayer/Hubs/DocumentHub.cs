using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace PresentationLayer.Hubs
{
    [Authorize(Roles = "lecturer,admin")]
    public class DocumentHub : Hub
    {
        // Hub này chủ yếu dùng để phát tin nhắn từ phía server xuống client.
        // Các phương thức Hub có thể được định nghĩa thêm tại đây nếu cần giao tiếp hai chiều.
    }
}
