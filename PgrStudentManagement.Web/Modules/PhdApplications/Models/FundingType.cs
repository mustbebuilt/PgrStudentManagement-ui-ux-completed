using System.ComponentModel.DataAnnotations;

namespace PgrStudentManagement.Web.Modules.PhdApplications.Models;

public enum FundingType
{
    [Display(Name = "Self-funded")]
    SelfFunded,

    [Display(Name = "University studentship")]
    Studentship,

    [Display(Name = "UK Research Council (UKRI)")]
    ResearchCouncil,

    [Display(Name = "External or industry sponsor")]
    ExternalSponsor
}
