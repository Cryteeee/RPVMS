using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Services
{
    public interface ICubicMeterReadingService
    {
        Task<Response<List<CubicMeterReadingDto>>> GetUserReadingsAsync(string userId);
        Task<Response<CubicMeterReadingDto>> AddReadingAsync(CubicMeterReadingDto reading);
        Task<Response<CubicMeterReadingDto>> GetLatestReadingAsync(string userId);
    }
} 