using SportsStore.Models;

namespace SportsStore.Services
{
    // 🎯 MẪU THIẾT KẾ STRATEGY - Xác định hợp đồng cho các chiến lược thông báo
    // Cho phép các implementation thông báo khác nhau (email, SMS, push, etc.)
    // 🔗 INTERFACE: INotificationService là interface được implement bởi NotificationService
    public interface INotificationService
    {
        Task CreateOrderNotificationAsync(string userId, int orderId, string status, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}
