using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

namespace SportsStore.Services
{
    // 🎯 MẪU THIẾT KẾ STRATEGY - Implementation cụ thể của voucher strategy
    // Xử lý logic áp dụng voucher và tính toán giảm giá
    // 🔗 IMPLEMENT: VoucherService implement interface IVoucherService
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
