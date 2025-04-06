using BlazorApp1.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Server.Services
{
    public interface IContactService
    {
        Task<ContactForm> CreateContactFormAsync(ContactForm contactForm);
        Task<IEnumerable<ContactForm>> GetAllContactFormsAsync();
        Task<IEnumerable<ContactForm>> GetArchivedContactFormsAsync();
        Task<ContactForm> GetContactFormByIdAsync(int id);
        Task<ContactForm> UpdateContactFormAsync(ContactForm contactForm);
        Task<bool> ArchiveContactFormAsync(int id);
    }
} 