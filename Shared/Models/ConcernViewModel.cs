using System;
using System.ComponentModel.DataAnnotations;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using System.Reflection;
using System.Linq;

namespace BlazorApp1.Shared.Models
{
    public class ConcernViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string UserEmail { get; set; }
        public DateTime DateSubmitted { get; set; }
        public PriorityLevel? PriorityLevel { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public FormType FormType { get; set; }
        public bool IsRead { get; set; }
        public bool IsResolved { get; set; }
        public bool IsClientSubmission { get; set; }
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        // Request specific properties
        public BlazorApp1.Shared.Models.RequestType? RequestType { get; set; }
        public UrgencyLevel? UrgencyLevel { get; set; }
        public DateTime? PreferredDate { get; set; }
        
        // Concern specific properties
        public ConcernCategory? ConcernCategory { get; set; }

        public string GetPriorityLevelString()
        {
            if (!PriorityLevel.HasValue) return string.Empty;
            return PriorityLevel.Value.ToString();
        }

        public string GetUrgencyLevelString()
        {
            if (!UrgencyLevel.HasValue) return string.Empty;
            return UrgencyLevel.Value.ToString();
        }

        public string GetFormTypeString()
        {
            return FormType.ToString();
        }

        // Helper method to get display name for RequestType
        public string GetRequestTypeDisplayName()
        {
            if (!RequestType.HasValue) return string.Empty;
            var field = RequestType.Value.GetType().GetField(RequestType.Value.ToString());
            return field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .OfType<DisplayAttribute>()
                .FirstOrDefault()?.Name ?? RequestType.Value.ToString();
        }

        // Helper method to get display name for ConcernCategory
        public string GetConcernCategoryDisplayName()
        {
            if (!ConcernCategory.HasValue) return string.Empty;
            var field = ConcernCategory.Value.GetType().GetField(ConcernCategory.Value.ToString());
            return field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .OfType<DisplayAttribute>()
                .FirstOrDefault()?.Name ?? ConcernCategory.Value.ToString();
        }
    }
} 