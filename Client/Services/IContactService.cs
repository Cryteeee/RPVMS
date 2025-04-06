using BlazorApp1.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Services
{
    public interface IContactService
    {
        Task<IEnumerable<ConcernViewModel>> GetConcernsAsync();
        Task<ContactForm?> GetContactFormByIdAsync(int? id);
        Task<ContactForm> CreateContactFormAsync(ContactForm contactForm);
        Task UpdateContactFormAsync(ContactForm contactForm);
        Task<IEnumerable<ContactForm>> GetAllContactFormsAsync();
        Task<bool> DeleteContactFormAsync(int? id);
        Task<List<SubmissionDto>> GetArchivedFormsAsync();
        Task<bool> RestoreFormAsync(int id);
        Task<bool> DeleteFormAsync(int id);
        Task<bool> ArchiveFormAsync(int id);
    }
} 