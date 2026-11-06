using System;
using System.ComponentModel.DataAnnotations;

namespace PgrStudentManagement.Web.Modules.PhdApplications.Models;

public class CreatePhdApplicationViewModel
{
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(100, ErrorMessage = "First name must be 100 characters or fewer")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(100, ErrorMessage = "Last name must be 100 characters or fewer")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [StringLength(255, ErrorMessage = "Email address must be 255 characters or fewer")]
    public string EmailAddress { get; set; } = string.Empty;

    [Display(Name = "Programme")]
    [Required(ErrorMessage = "Select the programme you are applying for")]
    public string ProgrammeCode { get; set; } = string.Empty;

    [Display(Name = "Mode of study")]
    [Required(ErrorMessage = "Select whether you will study full-time or part-time")]
    public string ModeOfStudy { get; set; } = string.Empty;

    [Display(Name = "Proposed start date")]
    [Required(ErrorMessage = "Enter your proposed start date")]
    [DataType(DataType.Date)]
    public DateTime ProposedStartDate { get; set; } =
        new DateTime(DateTime.UtcNow.Year + 1, 10, 1);

    [Display(Name = "Proposed thesis title")]
    [Required(ErrorMessage = "Enter a working title for your proposed thesis")]
    [StringLength(300, ErrorMessage = "Proposed thesis title must be 300 characters or fewer")]
    public string ProposedThesisTitle { get; set; } = string.Empty;

    [Display(Name = "Research summary (optional)")]
    [StringLength(2000, ErrorMessage = "Research summary must be 2,000 characters or fewer")]
    public string? ResearchSummary { get; set; }

    [Display(Name = "Funding source")]
    [Required(ErrorMessage = "Select how your PhD studies will be funded")]
    public FundingType? FundingSource { get; set; }

    [Display(Name = "Proposed main supervisor (optional)")]
    public string? ProposedSupervisorStaffNumber { get; set; }
}

