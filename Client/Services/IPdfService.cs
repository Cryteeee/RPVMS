using BlazorApp1.Shared.Models;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Services
{
    public interface IPdfService
    {
        Task GenerateConcernPdfAsync(ConcernViewModel concern);
    }
} 