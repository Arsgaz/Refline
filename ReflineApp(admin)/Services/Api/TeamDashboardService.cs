using Refline.Admin.Business.Identity;
using Refline.Admin.Data.Infrastructure;
using Refline.Admin.Models;

namespace Refline.Admin.Services.Api;

public sealed class TeamDashboardService : ITeamDashboardService
{
    private readonly IAdminUsersService _adminUsersService;
    private readonly IAdminUserAnalyticsService _analyticsService;
    private readonly CurrentSessionContext _currentSessionContext;

    public TeamDashboardService(
        IAdminUsersService adminUsersService,
        IAdminUserAnalyticsService analyticsService,
        CurrentSessionContext currentSessionContext)
    {
        _adminUsersService = adminUsersService;
        _analyticsService = analyticsService;
        _currentSessionContext = currentSessionContext;
    }

    public async Task<OperationResult<TeamDashboardSnapshot>> GetDashboardAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (_currentSessionContext.CurrentUser is null)
        {
            return OperationResult<TeamDashboardSnapshot>.Failure("Сессия администратора не найдена.");
        }

        var usersResult = await _adminUsersService.GetCompanyUsersAsync(_currentSessionContext.CompanyId, cancellationToken);
        if (!usersResult.IsSuccess || usersResult.Value is null)
        {
            return OperationResult<TeamDashboardSnapshot>.Failure(usersResult.Message, usersResult.ErrorCode);
        }

        var users = usersResult.Value.ToList();
        if (_currentSessionContext.Role == UserRole.Manager)
        {
            var currentManager = new CompanyUserListItem
            {
                Id = _currentSessionContext.CurrentUser.Id,
                CompanyId = _currentSessionContext.CurrentUser.CompanyId,
                FullName = _currentSessionContext.CurrentUser.FullName,
                Login = _currentSessionContext.CurrentUser.Login,
                Role = _currentSessionContext.CurrentUser.Role,
                ManagerId = null,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            if (users.All(user => user.Id != currentManager.Id))
            {
                users.Insert(0, currentManager);
            }
        }

        if (users.Count == 0)
        {
            return OperationResult<TeamDashboardSnapshot>.Success(new TeamDashboardSnapshot
            {
                Users = [],
                Members = [],
                Days = [],
                Applications = [],
                Categories = [],
                TotalTrackedSeconds = 0,
                TotalProductiveSeconds = 0,
                TotalIdleSeconds = 0,
                AverageProductiveSecondsPerEmployee = 0,
                TopEmployeeByProductiveTime = null
            });
        }

        var analyticsTasks = users
            .Select(async user =>
            {
                var analyticsResult = await _analyticsService.GetEmployeeAnalyticsAsync(user.Id, from, to, cancellationToken);
                return new { User = user, Result = analyticsResult };
            })
            .ToList();

        var analyticsResults = await Task.WhenAll(analyticsTasks);

        var failedResult = analyticsResults.FirstOrDefault(item => !item.Result.IsSuccess || item.Result.Value is null);
        if (failedResult is not null)
        {
            return OperationResult<TeamDashboardSnapshot>.Failure(
                failedResult.Result.Message,
                failedResult.Result.ErrorCode);
        }

        var members = analyticsResults
            .Select(item => new TeamMemberAnalyticsItem
            {
                User = item.User,
                TotalTrackedSeconds = item.Result.Value!.Summary.TotalTrackedSeconds,
                ProductiveSeconds = item.Result.Value.Summary.ProductiveSeconds,
                IdleSeconds = item.Result.Value.Summary.IdleSeconds,
                ActiveSeconds = item.Result.Value.Summary.ActiveSeconds
            })
            .OrderByDescending(item => item.ProductiveSeconds)
            .ThenBy(item => item.User.FullName)
            .ToList();

        var dayMap = new Dictionary<DateOnly, TeamDailyAggregate>();
        var applicationMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var categoryMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in analyticsResults.Select(item => item.Result.Value!))
        {
            foreach (var day in result.Breakdown.Days)
            {
                if (!dayMap.TryGetValue(day.Date, out var existing))
                {
                    existing = new TeamDailyAggregate { Date = day.Date };
                }

                dayMap[day.Date] = new TeamDailyAggregate
                {
                    Date = day.Date,
                    TotalSeconds = existing.TotalSeconds + day.TotalSeconds,
                    ProductiveSeconds = existing.ProductiveSeconds + day.ProductiveSeconds,
                    IdleSeconds = existing.IdleSeconds + day.IdleSeconds
                };
            }

            foreach (var app in result.Breakdown.Applications)
            {
                applicationMap[app.ApplicationName] = applicationMap.GetValueOrDefault(app.ApplicationName) + app.TotalSeconds;
            }

            foreach (var category in result.Breakdown.Categories)
            {
                categoryMap[category.Category] = categoryMap.GetValueOrDefault(category.Category) + category.TotalSeconds;
            }
        }

        var teamSnapshot = new TeamDashboardSnapshot
        {
            Users = users,
            Members = members,
            Days = dayMap.Values.OrderBy(item => item.Date).ToList(),
            Applications = applicationMap
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(8)
                .Select(item => new TeamAggregateListItem { Name = item.Key, TotalSeconds = item.Value })
                .ToList(),
            Categories = categoryMap
                .OrderByDescending(item => item.Value)
                .ThenBy(item => item.Key)
                .Take(8)
                .Select(item => new TeamAggregateListItem { Name = item.Key, TotalSeconds = item.Value })
                .ToList(),
            TotalTrackedSeconds = members.Sum(item => item.TotalTrackedSeconds),
            TotalProductiveSeconds = members.Sum(item => item.ProductiveSeconds),
            TotalIdleSeconds = members.Sum(item => item.IdleSeconds),
            AverageProductiveSecondsPerEmployee = members.Count == 0 ? 0 : members.Sum(item => item.ProductiveSeconds) / members.Count,
            TopEmployeeByProductiveTime = members
                .OrderByDescending(item => item.ProductiveSeconds)
                .ThenBy(item => item.User.FullName)
                .FirstOrDefault()
        };

        return OperationResult<TeamDashboardSnapshot>.Success(teamSnapshot);
    }
}
