using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp1.Server.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ApplicationDbContext context, ILogger<ContactService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactForm> CreateContactFormAsync(ContactForm contactForm)
        {
            try
            {
                _logger.LogInformation("Creating contact form with data: {@ContactForm}", contactForm);

                // Ensure required fields are set
                if (contactForm.CreatedAt == default)
                {
                    contactForm.CreatedAt = DateTime.UtcNow;
                }

                // Validate form type
                if (!Enum.IsDefined(typeof(FormType), contactForm.FormType))
                {
                    throw new ArgumentException($"Invalid form type: {contactForm.FormType}");
                }

                // Add the form to the context
                _logger.LogInformation("Adding contact form to database context");
                await _context.ContactForms.AddAsync(contactForm);

                // Save changes
                _logger.LogInformation("Saving changes to database");
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully created contact form with ID: {Id}", contactForm.Id);
                return contactForm;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating contact form");
                throw new Exception("A database error occurred while saving the contact form", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact form");
                throw new Exception("An error occurred while saving the contact form", ex);
            }
        }

        public async Task<IEnumerable<ContactForm>> GetAllContactFormsAsync()
        {
            try
            {
                return await _context.ContactForms
                    .Where(f => !f.IsArchived && f.Status != SubmissionStatus.Deleted)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact forms");
                throw new Exception("An error occurred while retrieving contact forms", ex);
            }
        }

        public async Task<ContactForm> GetContactFormByIdAsync(int id)
        {
            try
            {
                var form = await _context.ContactForms.FindAsync(id);
                if (form == null)
                {
                    _logger.LogWarning("Contact form not found with ID: {Id}", id);
                }
                return form;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact form with ID: {Id}", id);
                throw new Exception($"An error occurred while retrieving contact form with ID: {id}", ex);
            }
        }

        public async Task<ContactForm> UpdateContactFormAsync(ContactForm contactForm)
        {
            _context.ContactForms.Update(contactForm);
            await _context.SaveChangesAsync();
            return contactForm;
        }

        public async Task<bool> ArchiveContactFormAsync(int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                    return false;

                contactForm.IsArchived = true;
                contactForm.ArchivedDate = DateTime.UtcNow;
                contactForm.Status = contactForm.IsResolved ? SubmissionStatus.Resolved : SubmissionStatus.Archived;
                
                _context.ContactForms.Update(contactForm);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error archiving contact form with ID {id}");
                return false;
            }
        }

        public async Task<IEnumerable<ContactForm>> GetArchivedContactFormsAsync()
        {
            try
            {
                return await _context.ContactForms
                    .Where(f => f.IsArchived || f.Status == SubmissionStatus.Resolved || f.Status == SubmissionStatus.Rejected)
                    .OrderByDescending(f => f.ArchivedDate ?? f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving archived contact forms");
                throw new Exception("An error occurred while retrieving archived contact forms", ex);
            }
        }
    }
} 