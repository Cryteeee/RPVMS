using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public enum RequestType
    {
        [Display(Name = "Maintenance Request")]
        MaintenanceRequest,

        [Display(Name = "Architectural Change")]
        ArchitecturalChange,

        [Display(Name = "Common Area Reservation")]
        CommonAreaReservation,

        [Display(Name = "Document Request")]
        DocumentRequest,

        [Display(Name = "Parking Permit")]
        ParkingPermit,

        [Display(Name = "Pool Access")]
        PoolAccess,

        [Display(Name = "Moving In/Out")]
        MovingInOut,

        [Display(Name = "Landscaping Service")]
        LandscapingService,

        [Display(Name = "General Inquiry")]
        GeneralInquiry,

        [Display(Name = "Other")]
        Other
    }
} 