using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace SportsStore.Hubs
{
    // 👁️ MẪU THIẾT KẾ OBSERVER (qua SignalR)
    // Clients đăng ký cập nhật thông báo thông qua SignalR groups
    // Hub hoạt động như mediator giữa server và các clients đã đăng ký
    // Thông báo đẩy real-time sử dụng cơ chế publish-subscribe
    // 🔗 KẾ THỪA: NotificationHub kế thừa từ class Hub (SignalR base class)
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
