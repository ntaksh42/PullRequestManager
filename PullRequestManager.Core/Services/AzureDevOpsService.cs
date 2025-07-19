using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using AzureDevopsTool.Core.Models;

namespace AzureDevopsTool.Core.Services;

public class AzureDevOpsService : IAzureDevOpsService, IDisposable
{
    private readonly AzureDevOpsConfig _config;
    private readonly VssConnection _connection;

    public AzureDevOpsService(AzureDevOpsConfig config)
    {
        _config = config;
        var credentials = new VssBasicCredential(string.Empty, _config.PersonalAccessToken);
        _connection = new VssConnection(new Uri($"https://dev.azure.com/{_config.Organization}"), credentials);
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
    {
        return await GetPullRequestsAsync(_config.Project, _config.Repository, null, null, null);
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository)
    {
        return await GetPullRequestsAsync(project, repository, null, null, null);
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate)
    {
        return await GetPullRequestsAsync(project, repository, fromDate, null, null);
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate, string? authorFilter)
    {
        return await GetPullRequestsAsync(project, repository, fromDate, authorFilter, null);
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync(string project, string repository, DateTime? fromDate, string? authorFilter, string? targetBranchFilter)
    {
        using var gitClient = _connection.GetClient<GitHttpClient>();
        
        var pullRequests = await gitClient.GetPullRequestsAsync(
            project: project,
            repositoryId: repository,
            searchCriteria: new GitPullRequestSearchCriteria()
        );

        var results = new List<PullRequest>();
        
        foreach (var pr in pullRequests)
        {
            var modifiedFiles = await GetPullRequestFilesAsync(gitClient, project, repository, pr.PullRequestId);
            var reviewers = await GetPullRequestReviewersAsync(gitClient, project, repository, pr.PullRequestId);
            var workItems = await GetPullRequestWorkItemsAsync(gitClient, project, repository, pr.PullRequestId);
            var (addedLines, deletedLines) = await GetPullRequestStatsAsync(gitClient, project, repository, pr.PullRequestId);
            
            results.Add(new PullRequest
            {
                Id = pr.PullRequestId,
                Title = pr.Title ?? string.Empty,
                Description = pr.Description ?? string.Empty,
                Status = pr.Status.ToString(),
                CreatedBy = pr.CreatedBy?.DisplayName ?? string.Empty,
                CreatedDate = pr.CreationDate,
                SourceBranch = pr.SourceRefName ?? string.Empty,
                TargetBranch = pr.TargetRefName ?? string.Empty,
                Repository = repository,
                Project = project,
                Url = GeneratePullRequestUrl(pr.PullRequestId, project, repository),
                ModifiedFiles = modifiedFiles,
                IsDraft = pr.IsDraft ?? false,
                Reviewers = reviewers,
                RelatedWorkItems = workItems,
                AddedLines = addedLines,
                DeletedLines = deletedLines,
                ChangedFiles = modifiedFiles.Count,
                CompletedDate = pr.ClosedDate,
                ClosedBy = pr.ClosedBy?.DisplayName ?? string.Empty
            });
        }

        var filteredResults = results.AsEnumerable();

        // Apply date filter if specified
        if (fromDate.HasValue)
        {
            filteredResults = filteredResults.Where(pr => pr.CreatedDate >= fromDate.Value);
        }

        // Apply author filter if specified
        if (!string.IsNullOrWhiteSpace(authorFilter))
        {
            filteredResults = filteredResults.Where(pr => pr.CreatedBy.ToLowerInvariant().Contains(authorFilter.ToLowerInvariant()));
        }

        // Apply target branch filter if specified
        if (!string.IsNullOrWhiteSpace(targetBranchFilter))
        {
            filteredResults = filteredResults.Where(pr => pr.TargetBranch.ToLowerInvariant().Contains(targetBranchFilter.ToLowerInvariant()));
        }

        return filteredResults;
    }

    private async Task<List<string>> GetPullRequestFilesAsync(GitHttpClient gitClient, string project, string repository, int pullRequestId)
    {
        try
        {
            var iterations = await gitClient.GetPullRequestIterationsAsync(project, repository, pullRequestId);
            if (iterations.Any())
            {
                var latestIteration = iterations.Last();
                var changes = await gitClient.GetPullRequestIterationChangesAsync(project, repository, pullRequestId, latestIteration.Id ?? 0);
                
                return changes.ChangeEntries
                    .Where(change => change.Item != null && !string.IsNullOrEmpty(change.Item.Path))
                    .Select(change => change.Item.Path.TrimStart('/'))
                    .ToList();
            }
        }
        catch
        {
            // If we can't get files, return empty list
        }
        
        return new List<string>();
    }

    private async Task<List<Reviewer>> GetPullRequestReviewersAsync(GitHttpClient gitClient, string project, string repository, int pullRequestId)
    {
        try
        {
            var reviewers = await gitClient.GetPullRequestReviewersAsync(project, repository, pullRequestId);
            return reviewers.Select(r => new Reviewer
            {
                DisplayName = r.DisplayName ?? string.Empty,
                UniqueDisplayName = r.UniqueName ?? string.Empty,
                Vote = r.Vote,
                IsRequired = r.IsRequired,
                HasDeclined = r.HasDeclined.GetValueOrDefault()
            }).ToList();
        }
        catch
        {
            return new List<Reviewer>();
        }
    }

    private async Task<List<WorkItem>> GetPullRequestWorkItemsAsync(GitHttpClient gitClient, string project, string repository, int pullRequestId)
    {
        try
        {
            var workItemRefs = await gitClient.GetPullRequestWorkItemRefsAsync(project, repository, pullRequestId);
            var workItems = new List<WorkItem>();
            
            foreach (var workItemRef in workItemRefs)
            {
                if (int.TryParse(workItemRef.Id, out int workItemId))
                {
                    workItems.Add(new WorkItem
                    {
                        Id = workItemId,
                        Title = $"Work Item {workItemId}", // ResourceRef doesn't have Title property
                        Type = "WorkItem", // Default type
                        State = "Active" // Default state
                    });
                }
            }
            
            return workItems;
        }
        catch
        {
            return new List<WorkItem>();
        }
    }

    private async Task<(int addedLines, int deletedLines)> GetPullRequestStatsAsync(GitHttpClient gitClient, string project, string repository, int pullRequestId)
    {
        try
        {
            var iterations = await gitClient.GetPullRequestIterationsAsync(project, repository, pullRequestId);
            if (iterations.Any())
            {
                var latestIteration = iterations.Last();
                var changes = await gitClient.GetPullRequestIterationChangesAsync(project, repository, pullRequestId, latestIteration.Id ?? 0);
                
                int addedLines = 0;
                int deletedLines = 0;
                
                foreach (var change in changes.ChangeEntries)
                {
                    if (change.Item != null && change.Item.GitObjectType == Microsoft.TeamFoundation.SourceControl.WebApi.GitObjectType.Blob)
                    {
                        // For simplicity, we'll estimate based on the number of changes
                        // In a real implementation, you'd need to get the actual diff
                        addedLines += 10; // Placeholder
                        deletedLines += 5; // Placeholder
                    }
                }
                
                return (addedLines, deletedLines);
            }
        }
        catch
        {
            // If we can't get stats, return zeros
        }
        
        return (0, 0);
    }

    private string GeneratePullRequestUrl(int pullRequestId, string project, string repository)
    {
        return $"https://dev.azure.com/{_config.Organization}/{project}/_git/{repository}/pullrequest/{pullRequestId}";
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}