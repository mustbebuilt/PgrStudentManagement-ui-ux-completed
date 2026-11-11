using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

using PgrStudentManagement.Web.Modules.AcademicSupervisors.Models;
using PgrStudentManagement.Web.Modules.PhdApplications.Models;
using PgrStudentManagement.Web.Modules.ResearchActivities.Models;
using PgrStudentManagement.Web.Modules.Students.Models;

namespace PgrStudentManagement.Web.Modules.Students.Data;

public class StudentManagementDbContext : DbContext
{
    public StudentManagementDbContext(
        DbContextOptions<StudentManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    // Tables for academic supervisor module
    public DbSet<AcademicSupervisor> AcademicSupervisors =>
        Set<AcademicSupervisor>();

    public DbSet<AcademicSupervisorEligibleRole>
        AcademicSupervisorEligibleRoles =>
            Set<AcademicSupervisorEligibleRole>();

    public DbSet<SupervisoryAssignment> SupervisoryAssignments =>
        Set<SupervisoryAssignment>();

    // Tables for PhD applications module
    public DbSet<PhdApplication> PhdApplications =>
        Set<PhdApplication>();

    // Tables for research activities module
    public DbSet<ResearchActivity> ResearchActivities =>
        Set<ResearchActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureStudents(modelBuilder);
        ConfigureAcademicSupervisors(modelBuilder);
        ConfigurePhdApplications(modelBuilder);
        ConfigureResearchActivities(modelBuilder);
    }

    private static void ConfigureStudents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(student =>
                student.StudentNumber);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(enrolment => enrolment.EnrolmentId);

            entity
            .HasOne(enrolment => enrolment.Student)
            .WithMany(student => student.Enrolments)
            .HasForeignKey(enrolment => enrolment.StudentNumber)
            .OnDelete(DeleteBehavior.Restrict);

            entity
            .Property(enrolment => enrolment.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        });
    }

    private static void ConfigureAcademicSupervisors(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicSupervisor>(
            entity =>
            {
                entity.ToTable("AcademicSupervisors");
                entity.HasKey(supervisor => supervisor.StaffNumber);
                entity.Property(supervisor => supervisor.StaffNumber)
                    .IsRequired();

                entity.Property(supervisor => supervisor.FirstName);
                entity.Property(supervisor => supervisor.LastName);

                entity.Property(supervisor => supervisor.EmailAddress)
                    .IsRequired();

                entity.HasMany(supervisor => supervisor.EligibleRoleEntries)
                    .WithOne(eligibleRole =>
                        eligibleRole.AcademicSupervisor)
                    .HasForeignKey(eligibleRole =>
                        eligibleRole.SupervisorStaffNumber)
                    .OnDelete(DeleteBehavior.Cascade);

            });

        modelBuilder.Entity<AcademicSupervisorEligibleRole>(
            entity =>
            {
                entity.ToTable("AcademicSupervisorEligibleRoles");

                entity.HasKey(eligibleRole => new
                {
                    eligibleRole.SupervisorStaffNumber,
                    eligibleRole.Role
                });

                entity.Property(eligibleRole => eligibleRole.Role)
                    .HasConversion<string>()
                    .IsRequired();
            });

        modelBuilder.Entity<SupervisoryAssignment>(
            entity =>
            {
                entity.ToTable("SupervisoryAssignments");

                entity.HasKey(assignment => new
                {
                    assignment.StudentNumber,
                    assignment.SupervisorStaffNumber,
                    assignment.Role,
                    assignment.StartDate
                });

                entity.Property(assignment => assignment.Role)
                    .HasConversion<string>();

                entity.HasOne<Student>()
                    .WithMany()
                    .HasForeignKey(assignment => assignment.StudentNumber)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<AcademicSupervisor>()
                    .WithMany()
                    .HasForeignKey(assignment =>
                        assignment.SupervisorStaffNumber)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(assignment => assignment.Current);
            });
    }

    private static void ConfigurePhdApplications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhdApplication>(
            entity =>
            {
                entity.ToTable("PhdApplications");

                entity.HasKey(application =>
                    application.ApplicationReference);

                entity.Property(application =>
                        application.ApplicationReference)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(application => application.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(application => application.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(application => application.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(application => application.ProgrammeCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(application => application.ModeOfStudy)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(application => application.ProposedThesisTitle)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(application => application.ResearchSummary)
                    .HasMaxLength(2000);

                entity.Property(application =>
                        application.ProposedSupervisorStaffNumber)
                    .HasMaxLength(20);

                entity.Property(application =>
                        application.EnrolledStudentNumber)
                    .HasMaxLength(20);

                entity.Property(application => application.FundingSource)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(application => application.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasOne<AcademicSupervisor>()
                    .WithMany()
                    .HasForeignKey(application =>
                        application.ProposedSupervisorStaffNumber)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Student>()
                    .WithMany()
                    .HasForeignKey(application =>
                        application.EnrolledStudentNumber)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });
    }

    private static void ConfigureResearchActivities(ModelBuilder modelBuilder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var tagsConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());

        var tagsComparer = new ValueComparer<List<string>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var detailsConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, object>()
                : DeserializeAndNormalizeDetails(v, jsonOptions));

        var detailsComparer = new ValueComparer<Dictionary<string, object>>(
            (d1, d2) => JsonSerializer.Serialize(d1, jsonOptions) == JsonSerializer.Serialize(d2, jsonOptions),
            d => JsonSerializer.Serialize(d, jsonOptions).GetHashCode(),
            d => DeserializeAndNormalizeDetails(JsonSerializer.Serialize(d, jsonOptions), jsonOptions));

        modelBuilder.Entity<ResearchActivity>(entity =>
        {
            entity.ToTable("ResearchActivities");

            entity.HasKey(activity => activity.Id);

            entity.Property(activity => activity.Id)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(activity => activity.StudentNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(activity => activity.ActivityType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(activity => activity.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(activity => activity.ActivityDate)
                .IsRequired();

            entity.Property(activity => activity.CreatedOn)
                .IsRequired();

            entity.Property(activity => activity.Tags)
                .HasConversion(tagsConverter)
                .Metadata.SetValueComparer(tagsComparer);

            entity.Property(activity => activity.Details)
                .HasConversion(detailsConverter)
                .Metadata.SetValueComparer(detailsComparer);

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(activity => activity.StudentNumber)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static Dictionary<string, object> DeserializeAndNormalizeDetails(
        string json,
        JsonSerializerOptions jsonOptions)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, object>>(json, jsonOptions);
        if (raw is null || raw.Count == 0)
        {
            return new Dictionary<string, object>();
        }

        var normalized = new Dictionary<string, object>(raw.Count);
        foreach (var (key, value) in raw)
        {
            normalized[key] = NormalizeValue(value);
        }

        return normalized;
    }

    private static object NormalizeValue(object value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element
                .EnumerateArray()
                .Select(item => NormalizeValue(item))
                .ToArray(),
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(prop => prop.Name, prop => NormalizeValue(prop.Value)),
            _ => string.Empty
        };
    }
}
