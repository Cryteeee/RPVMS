using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public enum ConcernCategory
    {
        [Display(Name = "Noise Complaint")]
        NoiseComplaint,

        [Display(Name = "Property Maintenance")]
        PropertyMaintenance,

        [Display(Name = "Rule Violation")]
        RuleViolation,

        [Display(Name = "Security Issue")]
        SecurityIssue,

        [Display(Name = "Parking Violation")]
        ParkingViolation,

        [Display(Name = "Neighbor Dispute")]
        NeighborDispute,

        [Display(Name = "Common Area Issue")]
        CommonAreaIssue,

        [Display(Name = "Pet Related")]
        PetRelated,

        [Display(Name = "Architectural Violation")]
        ArchitecturalViolation,

        [Display(Name = "Other")]
        Other
    }
} 