using SportsStore.Models;

namespace SportsStore.Services
{
    // =================================================================
    // 🎯 MẦU THIẾT KẾ STRATEGY - Xác định hợp đồng cho các chiến lược thông báo
    // =================================================================
    // Mục đích: Định nghĩa interface cho các cách gửi thông báo khác nhau
    // 
    // Strategy variants có thể:
    //   1. DatabaseNotificationService: Lưu vào DB, UI polling
    //   2. EmailNotificationService: Gửi email real-time
    //   3. SMSNotificationService: Gửi SMS
    //   4. PushNotificationService: Web push notifications
    //   5. SignalRNotificationService: Real-time WebSocket (hiện tại dùng)\n    //
    // Cách hoạt động:
    //   - Interface định nghĩa Contract: CreateOrderNotificationAsync(), GetUserNotificationsAsync() ...\n    //   - Mỗi implementation cụ thể xử lý logic khác nhau
    //   - Program.cs Dependency Injection cho phép runtime switching\n    //
    // Lợi ích:\n    //   • Unittests dễ dàng (mock INotificationService)\n    //   • Thay đổi notification strategy mà không touch OrderController\n    //   • Mở rộng: thêm notification type mới mà không sửa existing code\n    //
    // 🔄 LIÊN QUAN:\n    //   • ADAPTER PATTERN: Adapts SignalR to INotificationService interface\n    //   • FACTORY PATTERN: DI container creates right implementation\n    //   • OBSERVER PATTERN: Notification là form của Observer pattern\n    //
    // 📄 LIÊN KẾT VỚI FILE KHÁC:\n    //   • Services/NotificationService.cs: Concrete implementation\n    //   • Program.cs: Đăng ký: `AddScoped<INotificationService, NotificationService>()`\n    //   • Controllers/OrderController.cs: Dùng INotificationService để notify\n    //   • Hubs/NotificationHub.cs: SignalR hub cho real-time delivery\n    // ==================================================================
    public interface INotificationService
    {
        Task CreateOrderNotificationAsync(string userId, int orderId, string status, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}
