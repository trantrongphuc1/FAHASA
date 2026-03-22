using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SportsStore.Models
{
    // 🎭 MẪU THIẾT KẾ MEMENTO - State Manager class
    // Quản lý việc thay đổi trạng thái Order với khả năng undo/redo
    // Kết hợp Originator, Caretaker và Memento để tạo state management hoàn chỉnh
    // 🔗 COMBINES: OrderOriginator + OrderCaretaker + OrderMemento
    public class OrderStateManager
    {
        private readonly StoreDbContext _context;
        private readonly ILogger<OrderStateManager> _logger;
        private readonly OrderOriginator _originator;
        private readonly OrderCaretaker _caretaker;

        public OrderStateManager(StoreDbContext context, ILogger<OrderStateManager> logger)
        {
            _context = context;
            _logger = logger;
            _originator = new OrderOriginator(context);
            _caretaker = new OrderCaretaker();
        }

        // Thay đổi trạng thái Order với khả năng undo
        public async Task<bool> ChangeOrderStatus(int orderId, OrderStatus newStatus, string? notes = null)
        {
            try
            {
                // Lấy Order từ database
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy Order với ID: {OrderId}", orderId);
                    return false;
                }

                // Thiết lập Order cho Originator
                _originator.SetOrder(order);

                // Tạo memento lưu trạng thái hiện tại trước khi thay đổi
                var memento = _originator.CreateMemento($"Trước khi thay đổi sang {newStatus}");
                _caretaker.SaveMemento(memento);

                _logger.LogInformation("Đã lưu memento cho Order {OrderId}, trạng thái cũ: {OldStatus}",
                    orderId, order.Status);

                // Thay đổi trạng thái
                order.Status = newStatus;
                // order.UpdatedAt = DateTime.Now; // Comment out vì Order model không có UpdatedAt property

                // Lưu thay đổi
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã thay đổi trạng thái Order {OrderId} sang {NewStatus}",
                    orderId, newStatus);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi trạng thái Order {OrderId}", orderId);
                return false;
            }
        }

        // Undo - quay về trạng thái trước đó
        public async Task<bool> UndoOrderStatus(int orderId)
        {
            try
            {
                // Lấy memento để undo
                var memento = _caretaker.Undo();
                if (memento == null)
                {
                    _logger.LogWarning("Không có trạng thái nào để undo cho Order {OrderId}", orderId);
                    return false;
                }

                // Lấy Order từ database
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy Order với ID: {OrderId}", orderId);
                    return false;
                }

                // Thiết lập Order cho Originator
                _originator.SetOrder(order);

                // Khôi phục từ memento
                await _originator.RestoreFromMemento(memento);

                _logger.LogInformation("Đã undo Order {OrderId} về trạng thái {Status}",
                    orderId, memento.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi undo trạng thái Order {OrderId}", orderId);
                return false;
            }
        }

        // Redo - làm lại thao tác đã undo
        public async Task<bool> RedoOrderStatus(int orderId)
        {
            try
            {
                // Lấy memento để redo
                var memento = _caretaker.Redo();
                if (memento == null)
                {
                    _logger.LogWarning("Không có trạng thái nào để redo cho Order {OrderId}", orderId);
                    return false;
                }

                // Lấy Order từ database
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy Order với ID: {OrderId}", orderId);
                    return false;
                }

                // Thiết lập Order cho Originator
                _originator.SetOrder(order);

                // Khôi phục từ memento
                await _originator.RestoreFromMemento(memento);

                _logger.LogInformation("Đã redo Order {OrderId} về trạng thái {Status}",
                    orderId, memento.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi redo trạng thái Order {OrderId}", orderId);
                return false;
            }
        }

        // Lấy lịch sử các thay đổi trạng thái
        public List<OrderMemento> GetOrderHistory(int orderId)
        {
            // Trong implementation đơn giản này, chúng ta trả về tất cả mementos
            // Trong thực tế, nên filter theo orderId
            var allMementos = new List<OrderMemento>();

            var current = _caretaker.GetCurrentMemento();
            if (current != null && current.OrderId == orderId)
            {
                allMementos.Add(current);
            }

            return allMementos;
        }
    }
}