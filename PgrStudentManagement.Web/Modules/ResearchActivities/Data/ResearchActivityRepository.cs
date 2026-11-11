using Microsoft.EntityFrameworkCore;
using PgrStudentManagement.Web.Modules.ResearchActivities.Models;
using PgrStudentManagement.Web.Modules.Students.Data;

namespace PgrStudentManagement.Web.Modules.ResearchActivities.Data;

public class ResearchActivityRepository
{
    private readonly StudentManagementDbContext _context;

    public ResearchActivityRepository(StudentManagementDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ResearchActivity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ResearchActivities
            .AsNoTracking()
            .OrderBy(activity => activity.ActivityDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResearchActivity>> GetForStudentAsync(
        string studentNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResearchActivities
            .AsNoTracking()
            .Where(activity => activity.StudentNumber == studentNumber)
            .OrderBy(activity => activity.ActivityDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ResearchActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _context.ResearchActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResearchActivities
            .AnyAsync(activity => activity.Id == id, cancellationToken);
    }
}
