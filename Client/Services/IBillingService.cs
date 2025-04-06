using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Services
{
    public interface IBillingService
    {
        Task<Response<List<BillingDto>>> GetUserBillsAsync(string userId);
        Task<Response> AddBillAsync(BillingDto bill);
        Task<Response> MarkBillAsPaidAsync(int billId, string orNumber);
        Task<Response> CompressBillsAsync(string userId);
        Task<Response> DeleteAllBillsAsync(string userId);
        Task<Response> SetCubicMeterPriceAsync(string userId, decimal pricePerCubicMeter);
        Task<Response> SetCubicNumberAsync(string userId, decimal cubicNumber, DateTime dueDate);
        Task<byte[]> DownloadBillsAsync(string userId);
        Task<byte[]> DownloadAllBillsAsync();
        Task<Response> SetMonthlyPriceAsync(string userId, decimal monthlyPrice);
        Task<Response> DeleteBillAsync(int billId);
    }
} 