using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SportsStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository orderRepository;
        private readonly IRentalRepository rentalRepository;
        private readonly Cart cart;
        private readonly UserManager<ApplicationUser> userManager;
        // 💾 MẪU THIẾT KẾ MEMENTO - Inject OrderStateManager
        private readonly OrderStateManager orderStateManager;

        public OrderController(IOrderRepository orderRepo,
                               IRentalRepository rentalRepo,
                               Cart cartService,
                               UserManager<ApplicationUser> userMgr,
                               OrderStateManager stateManager)
        {
            orderRepository = orderRepo;
            rentalRepository = rentalRepo;
            cart = cartService;
            userManager = userMgr;
            orderStateManager = stateManager;
        }

        public ViewResult Checkout() => View(new Order());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Checkout" });
            }

            if (!cart.Lines.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống.");
                return View(order);
            }

            var purchaseLines = cart.Lines.Where(l => !l.IsRental).ToList();
            var rentalLines = cart.Lines.Where(l => l.IsRental).ToList();

            if (purchaseLines.Any())
            {
                order.UserId = user.Id;
                order.Lines = purchaseLines;

                // ✅ Tính tổng tiền và gán vào cột TotalAmount
                order.TotalAmount = purchaseLines.Sum(line => line.LineTotal);

                // Áp dụng voucher nếu có
                var voucherCode = (Request.Form["VoucherCode"].FirstOrDefault() ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(voucherCode))
                {
                    // Resolve voucher service manually (avoid constructor change)
                    var voucherService = HttpContext.RequestServices.GetService(typeof(SportsStore.Services.IVoucherService)) as SportsStore.Services.IVoucherService;
                    if (voucherService != null)
                    {
                        var applyResult = await voucherService.ApplyVoucherAsync(user.Id, voucherCode, order.TotalAmount);
                        if (applyResult.Success)
                        {
                            order.VoucherCode = applyResult.Code;
                            order.DiscountAmount = applyResult.DiscountAmount;
                            order.FinalAmount = applyResult.FinalAmount;
                            // Ghi TotalAmount thành giá cuối để thống kê đơn hàng đơn giản
                            order.TotalAmount = applyResult.FinalAmount;
                        }
                        else
                        {
                            TempData["VoucherError"] = applyResult.Message;
                        }
                    }
                }

                if (order.FinalAmount == 0 && order.TotalAmount > 0)
                {
                    order.FinalAmount = order.TotalAmount; // fallback nếu không có voucher
                }

                // ✅ Gán trạng thái mặc định ban đầu
                order.Status = OrderStatus.ChoXacNhan;

                // Lưu đơn hàng mua
                orderRepository.SaveOrder(order);
            }

            if (rentalLines.Any())
            {
                foreach (var line in rentalLines)
                {
                    var rental = new Rental
                    {
                        UserId = user.Id,
                        BookTitle = line.Product.Name,
                        StartDate = System.DateTime.Today,
                        EndDate = System.DateTime.Today.AddDays(line.RentalDays),
                        IsReturned = false
                    };
                    rentalRepository.SaveRental(rental);
                }
            }

            cart.Clear();
            
            // Truyền order ID sang trang Completed
            return RedirectToPage("/Completed", new { orderId = order.OrderID });
        }

        public async Task<IActionResult> MyOrders(OrderStatus? status = null)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Order/MyOrders" });
            }

            var orders = orderRepository.Orders
                .Where(o => o.UserId == user.Id);

            if (status != null)
            {
                orders = orders.Where(o => o.Status == status);
            }

            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Order/Details/{id}" });
            }

            var order = orderRepository.Orders
                .FirstOrDefault(o => o.OrderID == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = orderRepository.Orders
                .FirstOrDefault(o => o.OrderID == id && o.UserId == user.Id);

            if (order == null || order.Status != OrderStatus.ChoXacNhan)
            {
                TempData["Error"] = "Không thể hủy đơn hàng này.";
                return RedirectToAction("MyOrders");
            }

            order.Status = OrderStatus.DaHuy;
            orderRepository.SaveOrder(order);

            TempData["Message"] = "Đơn hàng đã được chuyển sang trạng thái 'Đã hủy'.";
            return RedirectToAction("MyOrders");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = orderRepository.Orders
                .FirstOrDefault(o => o.OrderID == id && o.UserId == user.Id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrders");
            }

            try
            {
                // 💾 MẪU THIẾT KẾ MEMENTO - Sử dụng OrderStateManager để thay đổi trạng thái
                // Tự động lưu trạng thái cũ và cho phép undo nếu cần
                await orderStateManager.ChangeOrderStatus(id, newStatus,
                    $"User {user.UserName} cập nhật trạng thái từ {order.Status} sang {newStatus}");

                if (newStatus == OrderStatus.DaNhanHang)
                {
                    order.Shipped = true; // đồng bộ flag cũ
                    orderRepository.SaveOrder(order);
                }

                TempData["Message"] = $"Đơn hàng #{id} đã được cập nhật trạng thái thành '{GetStatusDisplayName(newStatus)}'.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật trạng thái: {ex.Message}";
            }

            return RedirectToAction("MyOrders");
        }

        // Helper method để hiển thị tên trạng thái tiếng Việt
        private string GetStatusDisplayName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.ChoXacNhan => "Chờ xác nhận",
                OrderStatus.ChoGiaoHang => "Chờ giao hàng",
                OrderStatus.DangVanChuyen => "Đang vận chuyển",
                OrderStatus.DaNhanHang => "Đã nhận hàng",
                OrderStatus.DaHuy => "Đã hủy",
                OrderStatus.YeuCauHoanTien => "Yêu cầu hoàn tiền",
                OrderStatus.HoanTienThanhCong => "Hoàn tiền thành công",
                _ => status.ToString()
            };
        }
    }
}
