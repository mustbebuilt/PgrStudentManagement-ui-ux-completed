using Microsoft.AspNetCore.Mvc;
using PgrStudentManagement.Web.Modules.PhdApplications.Models;
using PgrStudentManagement.Web.Modules.PhdApplications.Services;

namespace PgrStudentManagement.Web.Modules.PhdApplications.Controllers;

public sealed class PhdApplicationsController : Controller
{
    private readonly PhdApplicationService _phdApplicationService;

    public PhdApplicationsController(
        PhdApplicationService phdApplicationService)
    {
        _phdApplicationService = phdApplicationService;
    }

    public async Task<IActionResult> Index(
        ApplicationStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var applications = await _phdApplicationService.GetApplicationsAsync(
            statusFilter,
            cancellationToken);

        ViewData["SelectedStatus"] = statusFilter;

        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        string applicationReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationReference))
        {
            return View("NotFound");
        }

        var application =
            await _phdApplicationService.GetApplicationByReferenceAsync(
                applicationReference,
                cancellationToken);

        if (application is null)
        {
            return View("NotFound");
        }

        return View(application);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken = default)
    {
        ViewBag.Supervisors =
            await _phdApplicationService.GetEligibleMainSupervisorsAsync(
                cancellationToken);

        return View(new CreatePhdApplicationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreatePhdApplicationViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Supervisors =
                await _phdApplicationService.GetEligibleMainSupervisorsAsync(
                    cancellationToken);
            return View(model);
        }

        var reference =
            await _phdApplicationService.GenerateNextApplicationReferenceAsync(
                cancellationToken);

        var application = new PhdApplication
        {
            ApplicationReference = reference,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            EmailAddress = model.EmailAddress.Trim(),
            ProgrammeCode = model.ProgrammeCode.Trim(),
            ModeOfStudy = model.ModeOfStudy.Trim(),
            ProposedStartDate = model.ProposedStartDate,
            ProposedThesisTitle = model.ProposedThesisTitle.Trim(),
            ResearchSummary = model.ResearchSummary?.Trim(),
            FundingSource = model.FundingSource!.Value,
            ProposedSupervisorStaffNumber =
                string.IsNullOrWhiteSpace(model.ProposedSupervisorStaffNumber)
                    ? null
                    : model.ProposedSupervisorStaffNumber.Trim(),
            Status = ApplicationStatus.Submitted,
            SubmittedDate = DateTime.UtcNow.Date
        };

        var outcome = await _phdApplicationService.CreateApplicationAsync(
            application,
            cancellationToken);

        switch (outcome)
        {
            case ApplicationOutcome.Created:
                TempData["SuccessMessage"] =
                    $"Application {application.ApplicationReference} submitted successfully.";
                return RedirectToAction(
                    nameof(Details),
                    new { applicationReference = application.ApplicationReference });

            case ApplicationOutcome.SupervisorNotFound:
                ModelState.AddModelError(
                    nameof(model.ProposedSupervisorStaffNumber),
                    "The selected academic supervisor was not found.");
                break;

            case ApplicationOutcome.SupervisorNotEligible:
                ModelState.AddModelError(
                    nameof(model.ProposedSupervisorStaffNumber),
                    "The selected supervisor is not eligible to act as a main supervisor.");
                break;

            default:
                ModelState.AddModelError(
                    string.Empty,
                    "The PhD application could not be submitted.");
                break;
        }

        ViewBag.Supervisors =
            await _phdApplicationService.GetEligibleMainSupervisorsAsync(
                cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditStatus(
        string applicationReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationReference))
        {
            return View("NotFound");
        }

        var application =
            await _phdApplicationService.GetApplicationByReferenceAsync(
                applicationReference,
                cancellationToken);

        if (application is null)
        {
            return View("NotFound");
        }

        ViewBag.AllowedTransitions =
            _phdApplicationService.GetAllowedTransitions(application.Status);

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStatus(
        string applicationReference,
        ApplicationStatus status,
        DateTime? decisionDate = null,
        string? enrolledStudentNumber = null,
        CancellationToken cancellationToken = default)
    {
        // Server-side safety net: if the free-text status could
        // not be bound (e.g. "Offer made" instead of "OfferMade"), the
        // parameter falls back to its default. Do not persist that.
        // No error message is shown yet - see lab-instructions.md.
        if (!ModelState.IsValid)
        {
            var unchanged =
                await _phdApplicationService.GetApplicationByReferenceAsync(
                    applicationReference,
                    cancellationToken);

            if (unchanged is null)
            {
                return View("NotFound");
            }

            ViewBag.AllowedTransitions =
                _phdApplicationService.GetAllowedTransitions(unchanged.Status);

            return View(unchanged);
        }

        var outcome = await _phdApplicationService.UpdateApplicationStatusAsync(
            applicationReference,
            status,
            decisionDate,
            enrolledStudentNumber,
            cancellationToken);

        if (outcome == ApplicationOutcome.ApplicationNotFound)
        {
            return View("NotFound");
        }

        if (outcome == ApplicationOutcome.Updated)
        {
            TempData["SuccessMessage"] =
                $"Application {applicationReference} status updated to {status}.";

            return RedirectToAction(
                nameof(Details),
                new { applicationReference });
        }

        var application =
            await _phdApplicationService.GetApplicationByReferenceAsync(
                applicationReference,
                cancellationToken);

        if (application is null)
        {
            return View("NotFound");
        }

        var errorMessage = outcome switch
        {
            ApplicationOutcome.InvalidStatusTransition =>
                "This application status cannot be changed once finalized.",
            ApplicationOutcome.StudentNotFound =>
                "A valid enrolled student number is required when marking an application as Enrolled.",
            _ => "The application status could not be updated."
        };

        ModelState.AddModelError(string.Empty, errorMessage);

        ViewBag.AllowedTransitions =
            _phdApplicationService.GetAllowedTransitions(application.Status);

        return View(application);
    }
}
