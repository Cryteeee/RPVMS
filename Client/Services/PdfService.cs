using BlazorApp1.Shared.Models;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Services
{
    public class PdfService : IPdfService
    {
        private readonly IJSRuntime _jsRuntime;

        public PdfService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task GenerateConcernPdfAsync(ConcernViewModel concern)
        {
            try
            {
                // Call JavaScript function to generate and download PDF
                await _jsRuntime.InvokeVoidAsync("generatePdf", new
                {
                    id = concern.Id,
                    title = concern.Title ?? "No Title",
                    description = concern.Description ?? "No Description",
                    email = concern.UserEmail ?? "No Email",
                    userEmail = concern.UserEmail ?? "No Email",
                    date = concern.DateSubmitted.ToString("MM/dd/yyyy HH:mm"),
                    generatedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm"),
                    location = concern.Location ?? "N/A",
                    formType = concern.GetFormTypeString() ?? "N/A",
                    priorityLevel = concern.PriorityLevel?.ToString() ?? "N/A",
                    urgencyLevel = concern.UrgencyLevel?.ToString() ?? "N/A",
                    status = concern.IsResolved ? "Resolved" : "Pending",
                    category = concern.Category ?? concern.GetConcernCategoryDisplayName() ?? "N/A",
                    requestType = concern.RequestType?.ToString() ?? "N/A",
                    preferredDate = concern.PreferredDate?.ToString("MM/dd/yyyy") ?? "N/A"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PDF Generation Error: {ex.Message}");
                throw new Exception("Failed to generate PDF. Please try again.", ex);
            }
        }
    }
} 