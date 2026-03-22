using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

namespace SportsStore.Services
{
    // =================================================================
    // 🎯 MẦU THIẾT KẾ STRATEGY - Implementation cụ thể của voucher strategy
    // =================================================================
    // Mục đích: Xử lý logic áp dụng voucher và tính toán giảm giá
    //
    // Cách hoạt động:
    //   ApplyVoucherAsync():\n    //     1. Validate code tồn tại + active\n    //     2. Kiểm tra: daily limit, min order total, user eligibility\n    //     3. Tính discount amount\n    //     4. Tru số usage từ voucher\n    //     5. Return ApplyVoucherResult (success/failure)\n    //
    // Business Rules:\n    //   • Daily usage limit: Voucher có DailyLimit (e.g. 100 lần/ngày)\n    //   • Min order total: Phải đạt mức tối thiểu để dùng\n    //   • User usage limit: Mỗi user dùng được bao nhiêu lần\n    //   • Expiration: Voucher có ngày hết hạn\n    //
    // 🔄 LIÊN QUAN:\n    //   • STRATEGY PATTERN: Có thể có PercentageVoucherStrategy, FixedAmountVoucherStrategy\n    //   • DISCOUNT PATTERN: Tính toán giajust giả để\n    //   • VALIDATION PATTERN: Validate voucher trước áp dụng\n    //
    // 📄 LIÊN KẾT VỚI FILE KHÁC:\n    //   • Services/IVoucherService.cs: Interface definition\n    //   • Models/Voucher.cs: Entity model\n    //   • Models/VoucherUserUsage.cs: Track user usage\n    //   • Program.cs: Đăng ký: `AddScoped<IVoucherService, VoucherService>()`\n    //   • Controllers/OrderController.cs: Áp dụng voucher khi checkout\n    // ==================================================================
    public class VoucherService : IVoucherService
    {
        private readonly StoreDbContext _ctx;
        public VoucherService(StoreDbContext ctx) => _ctx = ctx;

        public async Task<ApplyVoucherResult> ApplyVoucherAsync(string userId, string code, decimal orderSubtotal)
        {
            var result = new ApplyVoucherResult { Success = false, Code = code };
            if (string.IsNullOrWhiteSpace(code))
            {
                result.Message = "Không có mã voucher.";
                return result;
            }
            code = code.Trim().ToUpperInvariant();

            var voucher = await _ctx.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null || !voucher.IsActive)
            {
                result.Message = "Mã voucher không hợp lệ hoặc đã ngừng hoạt động.";
                return result;
            }

            // Reset daily usage if day changed
            if (voucher.LastResetDate.Date != DateTime.UtcNow.Date)
            {
                voucher.DailyUsedCount = 0;
                voucher.LastResetDate = DateTime.UtcNow.Date;
            }

            if (voucher.DailyLimit > 0 && voucher.DailyUsedCount >= voucher.DailyLimit)
            {
                result.Message = "Voucher hôm nay đã dùng hết số lượt.";
                return result;
            }

            if (voucher.MinOrderTotal.HasValue && orderSubtotal < voucher.MinOrderTotal.Value)
            {
                result.Message = $"Đơn hàng chưa đạt mức tối thiểu {voucher.MinOrderTotal.Value:N0} đ để dùng mã.";
                return result;
            }

            // Track per-user usage references (avoid re-query after add)
            VoucherUserUsage? userDailyRef = null;
            VoucherUserUsage? userWeeklyRef = null;

            if (voucher.PerUserDailyLimit > 0)
            {
                var today = DateTime.UtcNow.Date;
                userDailyRef = await _ctx.VoucherUserUsages.FirstOrDefaultAsync(u => u.VoucherId == voucher.Id && u.UserId == userId && u.PeriodType == "Daily" && u.PeriodStartDate == today);
                if (userDailyRef == null)
                {
                    userDailyRef = new VoucherUserUsage
                    {
                        VoucherId = voucher.Id,
                        UserId = userId,
                        PeriodType = "Daily",
                        PeriodStartDate = today,
                        UsageCount = 0
                    };
                    _ctx.VoucherUserUsages.Add(userDailyRef);
                }
                if (userDailyRef.UsageCount >= voucher.PerUserDailyLimit)
                {
                    result.Message = $"Bạn đã đạt giới hạn {voucher.PerUserDailyLimit} lần dùng mã này trong ngày.";
                    return result; // do NOT save partial usage row
                }
            }

            if (voucher.PerUserWeeklyLimit > 0)
            {
                var today = DateTime.UtcNow.Date;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = today.AddDays(-diff);
                userWeeklyRef = await _ctx.VoucherUserUsages.FirstOrDefaultAsync(u => u.VoucherId == voucher.Id && u.UserId == userId && u.PeriodType == "Weekly" && u.PeriodStartDate == weekStart);
                if (userWeeklyRef == null)
                {
                    userWeeklyRef = new VoucherUserUsage
                    {
                        VoucherId = voucher.Id,
                        UserId = userId,
                        PeriodType = "Weekly",
                        PeriodStartDate = weekStart,
                        UsageCount = 0
                    };
                    _ctx.VoucherUserUsages.Add(userWeeklyRef);
                }
                if (userWeeklyRef.UsageCount >= voucher.PerUserWeeklyLimit)
                {
                    result.Message = $"Bạn đã đạt giới hạn {voucher.PerUserWeeklyLimit} lần dùng mã này trong tuần.";
                    return result;
                }
            }

            decimal discount = 0m;
            if (voucher.DiscountType == VoucherDiscountType.Percentage)
            {
                discount = Math.Round(orderSubtotal * voucher.Value, 2, MidpointRounding.AwayFromZero);
            }
            else // FixedAmount
            {
                discount = voucher.Value;
            }

            if (discount > orderSubtotal) discount = orderSubtotal; // safety cap
            var final = orderSubtotal - discount;

            voucher.DailyUsedCount += 1;

            // increment per-user counters (using tracked refs)
            if (userDailyRef != null) userDailyRef.UsageCount += 1;
            if (userWeeklyRef != null) userWeeklyRef.UsageCount += 1;

            await _ctx.SaveChangesAsync();

            result.Success = true;
            result.DiscountAmount = discount;
            result.FinalAmount = final;
            result.Message = "Áp dụng voucher thành công.";
            return result;
        }
    }
}
