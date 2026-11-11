using PgrStudentManagement.Tests.Fixtures;
using PgrStudentManagement.Web.Modules.ResearchActivities.Data;
using PgrStudentManagement.Web.Modules.ResearchActivities.Models;
using PgrStudentManagement.Web.Modules.ResearchActivities.Services;
using PgrStudentManagement.Web.Modules.Students.Data;
using PgrStudentManagement.Web.Modules.Students.Data.Daos;
using Xunit;

namespace PgrStudentManagement.Tests.Data;

public class ResearchActivityRepositoryTests
{
    private static async Task<SqliteTestDatabase> CreateDatabaseWithStudentsAsync()
    {
        var database = await SqliteTestDatabase.CreateAsync();
        var studentDao = new StudentDao(database.Context);
        var studentSeeder = new StudentDataSeeder(studentDao);
        await studentSeeder.SeedAsync();
        return database;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActivities()
    {
        await using var database = await CreateDatabaseWithStudentsAsync();
        var repository = new ResearchActivityRepository(database.Context);

        await ResearchActivityDataSeeder.SeedAsync(repository);

        var all = await repository.GetAllAsync();
        Assert.True(all.Count >= 3);
        Assert.Contains(all, a => a.Id == "activity-001" && a.StudentNumber == "S100001");
        Assert.Contains(all, a => a.Id == "activity-002" && a.StudentNumber == "S100001");
        Assert.Contains(all, a => a.Id == "activity-003" && a.StudentNumber == "S100002");
    }

    [Fact]
    public async Task SeedAsync_AddsExpectedActivities()
    {
        await using var database = await CreateDatabaseWithStudentsAsync();
        var repository = new ResearchActivityRepository(database.Context);
        var service = new ResearchActivityService(repository);

        await ResearchActivityDataSeeder.SeedAsync(repository);

        var s1Activities = await service.GetActivitiesForStudentAsync("S100001");
        Assert.Equal(2, s1Activities.Count);

        var s2Activities = await service.GetActivitiesForStudentAsync("S100002");
        Assert.Single(s2Activities);

        var s4Activities = await service.GetActivitiesForStudentAsync("S100004");
        Assert.Empty(s4Activities);
    }

    [Fact]
    public async Task SeedAsync_WhenCalledTwice_DoesNotDuplicate()
    {
        await using var database = await CreateDatabaseWithStudentsAsync();
        var repository = new ResearchActivityRepository(database.Context);
        var service = new ResearchActivityService(repository);

        await ResearchActivityDataSeeder.SeedAsync(repository);
        await ResearchActivityDataSeeder.SeedAsync(repository);

        var s1Activities = await service.GetActivitiesForStudentAsync("S100001");
        Assert.Equal(2, s1Activities.Count);
    }

    [Fact]
    public async Task GetActivitiesForStudentAsync_ReturnsChronologicalOrder()
    {
        await using var database = await CreateDatabaseWithStudentsAsync();
        var repository = new ResearchActivityRepository(database.Context);
        var service = new ResearchActivityService(repository);

        await ResearchActivityDataSeeder.SeedAsync(repository);

        var activities = await service.GetActivitiesForStudentAsync("S100001");
        Assert.Equal(2, activities.Count);
        Assert.True(activities[0].ActivityDate <= activities[1].ActivityDate);
        Assert.Equal(ResearchActivityType.ResearchReflection, activities[0].ActivityType);
        Assert.Equal(ResearchActivityType.SupervisionMeeting, activities[1].ActivityType);
    }

    [Fact]
    public async Task AddAsync_StoresCustomDetailsProperly()
    {
        await using var database = await CreateDatabaseWithStudentsAsync();
        var repository = new ResearchActivityRepository(database.Context);
        var service = new ResearchActivityService(repository);

        var activity = new ResearchActivity
        {
            Id = "activity-test-01",
            StudentNumber = "S100003",
            ActivityType = ResearchActivityType.Experiment,
            Title = "Lab Experiment 1",
            ActivityDate = new DateTime(2026, 4, 1),
            CreatedOn = DateTime.UtcNow,
            Tags = ["experiment", "data"],
            Details = new Dictionary<string, object>
            {
                ["hypothesis"] = "Testing hypothesis A",
                ["sampleSize"] = 50
            }
        };

        await repository.AddAsync(activity);

        var retrieved = await service.GetActivitiesForStudentAsync("S100003");
        var single = Assert.Single(retrieved);
        Assert.Equal("activity-test-01", single.Id);
        Assert.Equal("Lab Experiment 1", single.Title);
        Assert.Equal("Testing hypothesis A", single.Details["hypothesis"].ToString());
    }
}
