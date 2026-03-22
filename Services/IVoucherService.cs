using System.Threading.Tasks;

namespace SportsStore.Services
{
    // 🎯 MẪU THIẾT KẾ STRATEGY - Xác định hợp đồng cho các chiến lược voucher
    // Cho phép các implementation khác nhau (loại giảm giá, quy tắc, etc.)
    // 🔗 INTERFACE: IVoucherService là interface được implement bởi VoucherService
    public interface IVoucherService
    {
        Task<ApplyVoucherResult> ApplyVoucherAsync(string userId, string code, decimal orderSubtotal);
    }

    public class ApplyVoucherResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}
