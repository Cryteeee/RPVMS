using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Client.Services
{
    public interface ISubmissionService
    {
        Task<List<SubmissionDto>> GetSubmissionsAsync();
        Task<SubmissionDto> GetSubmissionAsync(int id);
        Task<SubmissionDto> CreateSubmissionAsync(SubmissionDto submission);
        Task<List<SubmissionDto>> GetArchivedSubmissionsAsync();
        Task<bool> RestoreSubmissionAsync(int id);
        Task<bool> DeleteSubmissionAsync(int id);
        Task<bool> ArchiveSubmissionAsync(int id);
        Task<bool> UpdateStatusAsync(int id, SubmissionStatus newStatus);
    }
} 