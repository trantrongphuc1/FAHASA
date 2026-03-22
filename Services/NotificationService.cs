using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportsStore.Hubs;
using SportsStore.Models;

namespace SportsStore.Services
{
    // =================================================================
    // 🎯 MẦU THIẾT KẾ STRATEGY - Implementation cụ thể của notification strategy
    // =================================================================
    // Mục đích: Xử lý logic gửi thông báo qua:
    //   1. DATABASE: Persistent storage (Notification table)
    //   2. SIGNALR: Real-time WebSocket broadcast
    //
    // Cách hoạt động:
    //   CreateOrderNotificationAsync():
    //     → Tạo Notification record trong DB
    //     → Gửi real-time event qua SignalR webhook
    //     → User nhìn thấy notification tức thì (không cần reload)
    //
    // Kết nối Signal R:
    //   - Inject IHubContext<NotificationHub>
    //   - Gọi hubContext.Clients.User(userId).SendAsync(...)
    //   - Broadcast đến user's channel (SignalR group)
    //
    // 📚 PATTERN:
    //   • STRATEGY PATTERN: Có thể thay NotificationService bằng EmailNotificationService
    //   • OBSERVER PATTERN: User clients đăng ký lắng nghe notification events
    //   • HUB PATTERN: Dùng SignalR Hub cho real-time delivery
    //   • ADAPTER PATTERN: Adapts DbContext + HubContext to INotificationService
    //
    // 📄 LIÊN KẾT VỚI FILE KHÁC:
    //   • Services/INotificationService.cs: Interface definition
    //   • Hubs/NotificationHub.cs: SignalR hub cho broadcast
    //   • Models/Notification.cs: Entity model
    //   • Program.cs: Đăng ký: `AddScoped<INotificationService, NotificationService>()`
    //   • Controllers/OrderController.cs: Gọi CreateOrderNotificationAsync()
    // ==================================================================
    public class NotificationService : INotificationService
    {
        private readonly StoreDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(StoreDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateOrderNotificationAsync(string userId, int orderId, string status, string message)
        {
            // 🎯 STRATEGY IMPLEMENTATION: Gửi thông báo qua 2 kênh
            // CHANNEL 1: DATABASE - Persistent storage
            var notification = new Notification
            {
                UserId = userId,
                Title = GetStatusTitle(status),
                Message = message,
                Link = $"/Order/Details/{orderId}",
                NotificationType = "Order",
                OrderId = orderId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // CHANNEL 2: SIGNALR - Real-time WebSocket delivery
            // 👁️ OBSERVER PATTERN: Push notification đến user đã subscribe
            // User không cần reload page để thấy notification
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notificationId = notification.NotificationId,
                title = notification.Title,
                message = notification.Message,
                link = notification.Link,
                createdAt = notification.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                isRead = notification.IsRead
            });
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        private string GetStatusTitle(string status)
        {
            return status switch
            {
                "Approved" => "Đơn hàng đã được duyệt",
                "Shipping" => "Đơn hàng đang vận chuyển",
                "Delivered" => "Đơn hàng đã được giao",
                "Cancelled" => "Đơn hàng đã bị hủy",
                _ => "Cập nhật đơn hàng"
            };
        }
    }
}
