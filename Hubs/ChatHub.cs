using Microsoft.AspNetCore.SignalR;
using SportsStore.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace SportsStore.Hubs
{
    // =================================================================
    // 🏗️ MẦU THIẾT KẾ: HUB PATTERN (SignalR Real-time Communication)
    // =================================================================
    // Mục đích: Quản lý kết nối WebSocket hai chiều (bidirectional communication)
    // giữa server và clients để gửi/nhận tin nhắn real-time
    //
    // Cách hoạt động:
    //   1. Server lắng nghe kết nối từ clients (OnConnectedAsync)
    //   2. Client gọi method SendMessage trên server
    //   3. Server xử lý và gọi SendAsync để broadcast tin nhắn
    //   4. Clients nhận sự kiện thông qua .on('ReceiveMessage', ...)
    //
    // Các group:
    //   - "Admins": Admin group, chứa tất cả admin connections
    //   - userId: Individual group, chứa kết nối của người dùng cụ thể
    //
    // 🔄 PATTERN LIÊN QUAN:
    //   • OBSERVER PATTERN: Clients đăng ký (subscribe) nhận thông báo
    //   • COMMAND PATTERN: SendMessage là một command được execute
    //   • GROUP PATTERN: Sử dụng SignalR groups thay vì broadcast tới tất cả
    //
    // 📁 LIÊN KẾT VỚI FILE KHÁC:
    //   • wwwroot/js/admin-chat.js: Client-side khởi tạo SignalR connection
    //   • wwwroot/js/app-core.js: User-side gửi/nhận message
    //   • Pages/Admin/Chat.razor: Admin UI receive message từ hub
    //   • Views/Shared/_ChatBox.cshtml: User chat widget
    // ==================================================================
    public class ChatHub : Hub
    {
        private readonly StoreDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public ChatHub(StoreDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        // 🎯 COMMAND PATTERN: SendMessage là một command được execute trên server
        // Nhận yêu cầu từ client, validate, persist, rồi broadcast kết quả
        // 
        // Parameter:
        //   - userId: ID của người nhận (cho admin) hoặc người gửi (cho user)
        //   - userName: Tên user để hiển thị trong chat
        //   - message: Nội dung tin nhắn
        //   - isAdmin: Flag xác định ai là người gửi
        public async Task SendMessage(string userId, string userName, string message, bool isAdmin)
        {
            // STEP 1: Persist message chỉ khi sender được xác thực hoặc là admin
            if (isAdmin || !string.IsNullOrEmpty(userId))
            {
                var chatMessage = new ChatMessage
                {
                    UserId = userId ?? string.Empty,
                    UserName = userName ?? string.Empty,
                    Message = message ?? string.Empty,
                    IsFromAdmin = isAdmin,
                    SentAt = DateTime.Now,
                    IsRead = false // Add this field to track read status
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
            }

            var time = DateTime.Now.ToString("HH:mm");

            // STEP 2: BROADCAST using OBSERVER PATTERN
            // Dùng SignalR Groups để broadcast tin nhắn tới các clients được đăng ký
            if (isAdmin)
            {
                // 👤 Admin gửi tin nhắn tới user cụ thể
                if (!string.IsNullOrEmpty(userId))
                {
                    // Gửi tới user được chỉ định (group userId)
                    await Clients.User(userId).SendAsync("ReceiveMessage", userId, userName, message, isAdmin, time);
                    
                    // Gửi lại cho admin (Caller = người gửi)
                    // Pattern: Echo back để confirm tin nhắn đã gửi
                    await Clients.Caller.SendAsync("ReceiveMessage", userId, userName, message, isAdmin, time);
                    
                    // Cập nhật UI admin dashboard - notify admin interface
                    // Gửi tới tất cả admin trong group "Admins"
                    await Clients.Group("Admins").SendAsync("AdminMessageSent", userId, userName, message, time);
                }
            }
            else
            {
                // 👥 User gửi tin nhắn tới tất cả admin
                // Broadcast tới "Admins" group (tất cả admin connections)
                await Clients.Group("Admins").SendAsync("ReceiveMessage", userId ?? string.Empty, userName ?? string.Empty, message, isAdmin, time);
                
                // Gửi lại cho user (confirmation)
                await Clients.Caller.SendAsync("ReceiveMessage", userId ?? string.Empty, userName ?? string.Empty, message, isAdmin, time);
                
                // Notify tất cả admin về tin nhắn mới từ user
                // Dùng event riêng để admin js có thể xử lý special logic (cập nhật unread count)
                await Clients.Group("Admins").SendAsync("UserMessageReceived", userId ?? string.Empty, userName ?? string.Empty, message, time);
            }
        }

        // 🎯 OBSERVER PATTERN: OnConnectedAsync - Subscribe client vào groups
        // Khi client kết nối, server tự động đăng ký (subscribe) vào các groups
        // để nhận thông báo trong tương lai
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Kiểm tra xem user có phải Admin không
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var appUser = await userManager.FindByIdAsync(userId);
                        if (appUser != null && await userManager.IsInRoleAsync(appUser, "Admin"))
                        {
                            // Admin: Thêm vào group "Admins" để nhận tin nhắn từ user
                            // Điều này làm cho admin tự động lắng nghe tất cả tin nhắn user
                            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                        }
                    }
                    // Mỗi user: Thêm vào group có tên là userId của họ
                    // Admin có thể gửi tin nhắn riêng cho user này dùng group này
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                    // Also remove from Admins group if admin
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var appUser = await userManager.FindByIdAsync(userId);
                        if (appUser != null && await userManager.IsInRoleAsync(appUser, "Admin"))
                        {
                            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
                        }
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

