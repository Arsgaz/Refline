using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.Admin;
using Refline.Api.Data;

namespace Refline.Api.Services.Admin;

public sealed class AdminAnalyticsService(ReflineDbContext dbContext)
{
    public async Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken)
    {
        return await dbContext.Companies
            .AsNoTracking()
            .AnyAsync(company => company.Id == companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyUserListItemDto>> GetCompanyUsersAsync(
        long companyId,
        long? managerId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Where(user => user.CompanyId == companyId);

        if (managerId.HasValue)
        {
            query = query.Where(user => user.ManagerId == managerId.Value);
        }

        return await query
            .OrderBy(user => user.Role)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Select(user => new CompanyUserListItemDto(
                user.Id,
                user.CompanyId,
                user.FullName,
                user.Login,
                user.Role,
                user.ManagerId,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<UserSummaryDto> GetUserSummaryAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var summary = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(_ => 1)
            .Select(group => new UserSummaryAggregate(
                group.Sum(record => record.DurationSeconds),
                group.Where(record => record.IsProductive).Sum(record => record.DurationSeconds),
                group.Where(record => record.IsIdle).Sum(record => record.DurationSeconds),
                group.Count()))
            .SingleOrDefaultAsync(cancellationToken);

        var topApplicationName = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(record => record.AppName)
            .Select(group => new
            {
                Key = group.Key,
                TotalSeconds = group.Sum(record => record.DurationSeconds)
            })
            .OrderByDescending(item => item.TotalSeconds)
            .ThenBy(item => item.Key)
            .Select(item => item.Key)
            .FirstOrDefaultAsync(cancellationToken);

        var topCategory = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(record => record.Category)
            .Select(group => new
            {
                Key = group.Key.ToString(),
                TotalSeconds = group.Sum(record => record.DurationSeconds)
            })
            .OrderByDescending(item => item.TotalSeconds)
            .ThenBy(item => item.Key)
            .Select(item => item.Key)
            .FirstOrDefaultAsync(cancellationToken);

        var totalTrackedSeconds = summary?.TotalTrackedSeconds ?? 0;
        var idleSeconds = summary?.IdleSeconds ?? 0;

        return new UserSummaryDto(
            userId,
            from,
            to,
            totalTrackedSeconds,
            summary?.ProductiveSeconds ?? 0,
            idleSeconds,
            totalTrackedSeconds - idleSeconds,
            topApplicationName,
            topCategory,
            summary?.TotalRecordsCount ?? 0);
    }

    public async Task<UserActivityBreakdownDto> GetUserActivityBreakdownAsync(
        long userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var applications = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(record => record.AppName)
            .Select(group => new
            {
                ApplicationName = group.Key,
                TotalSeconds = group.Sum(record => record.DurationSeconds)
            })
            .OrderByDescending(item => item.TotalSeconds)
            .ThenBy(item => item.ApplicationName)
            .ToListAsync(cancellationToken);

        var categories = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(record => record.Category)
            .Select(group => new
            {
                Category = group.Key.ToString(),
                TotalSeconds = group.Sum(record => record.DurationSeconds)
            })
            .OrderByDescending(item => item.TotalSeconds)
            .ThenBy(item => item.Category)
            .ToListAsync(cancellationToken);

        var days = await dbContext.ActivityRecords
            .AsNoTracking()
            .Where(record =>
                record.UserId == userId &&
                record.ActivityDate >= from &&
                record.ActivityDate <= to)
            .GroupBy(record => record.ActivityDate)
            .Select(group => new
            {
                Date = group.Key,
                TotalSeconds = group.Sum(record => record.DurationSeconds),
                ProductiveSeconds = group.Where(record => record.IsProductive).Sum(record => record.DurationSeconds),
                IdleSeconds = group.Where(record => record.IsIdle).Sum(record => record.DurationSeconds)
            })
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);

        return new UserActivityBreakdownDto(
            userId,
            from,
            to,
            applications.Select(item => new UserActivityApplicationDto(item.ApplicationName, item.TotalSeconds)).ToList(),
            categories.Select(item => new UserActivityCategoryDto(item.Category, item.TotalSeconds)).ToList(),
            days.Select(item => new UserActivityDayDto(item.Date, item.TotalSeconds, item.ProductiveSeconds, item.IdleSeconds)).ToList());
    }

    private sealed record UserSummaryAggregate(
        int TotalTrackedSeconds,
        int ProductiveSeconds,
        int IdleSeconds,
        int TotalRecordsCount);
}
