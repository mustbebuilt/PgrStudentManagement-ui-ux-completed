using PgrStudentManagement.Web.Modules.ResearchActivities.Models;

namespace PgrStudentManagement.Web.Modules.ResearchActivities.Data;

public static class ResearchActivityDataSeeder
{
    public static async Task SeedAsync(
        ResearchActivityRepository repository,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetForStudentAsync("S100001", cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        await repository.AddAsync(new ResearchActivity
        {
            Id = "activity-001",
            StudentNumber = "S100001",
            ActivityType = ResearchActivityType.ResearchReflection,
            Title = "Review of initial literature search",
            ActivityDate = new DateTime(2026, 2, 12),
            CreatedOn = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            Tags = ["literature", "planning"],
            Details = new Dictionary<string, object>
            {
                ["summary"] = "Reviewed the initial search results.",
                ["nextSteps"] = new[]
                {
                    "Repeat the search using revised terms",
                    "Discuss the inclusion criteria"
                }
            }
        }, cancellationToken);

        await repository.AddAsync(new ResearchActivity
        {
            Id = "activity-002",
            StudentNumber = "S100001",
            ActivityType = ResearchActivityType.SupervisionMeeting,
            Title = "Monthly supervision meeting",
            ActivityDate = new DateTime(2026, 3, 4),
            CreatedOn = new DateTime(2026, 3, 4, 14, 0, 0, DateTimeKind.Utc),
            Tags = ["supervision", "methods"],
            Details = new Dictionary<string, object>
            {
                ["discussion"] = "Reviewed the proposed data-collection method.",
                ["actions"] = new[]
                {
                    "Revise the participant information sheet",
                    "Prepare the ethics application"
                }
            }
        }, cancellationToken);

        await repository.AddAsync(new ResearchActivity
        {
            Id = "activity-003",
            StudentNumber = "S100002",
            ActivityType = ResearchActivityType.ConferenceAttendance,
            Title = "Postgraduate research conference",
            ActivityDate = new DateTime(2026, 3, 18),
            CreatedOn = new DateTime(2026, 3, 18, 9, 0, 0, DateTimeKind.Utc),
            Tags = ["conference", "presentation"],
            Details = new Dictionary<string, object>
            {
                ["conferenceName"] = "Postgraduate Research Conference",
                ["location"] = "Sheffield",
                ["presentationTitle"] = "Initial Findings from the Research Project"
            }
        }, cancellationToken);
    }
}
