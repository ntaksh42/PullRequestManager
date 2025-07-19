using AzureDevopsTool.Core.Models;

namespace AzureDevopsTool.Core.Services;

public interface IAzureDevOpsService
{
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync();
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository);
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate);
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate, string? authorFilter);
    Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate, string? authorFilter, string? targetBranchFilter);
}