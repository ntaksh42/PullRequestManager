namespace AzureDevopsTool.Core.Models;

public class PullRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> ModifiedFiles { get; set; } = new List<string>();
    public string ModifiedFilesDisplay => string.Join(", ", ModifiedFiles);
    
    // Extended details for detail pane
    public int CommentCount { get; set; }
    public int UnresolvedCommentCount { get; set; }
    public List<Reviewer> Reviewers { get; set; } = new List<Reviewer>();
    public bool IsDraft { get; set; }
    public bool HasMergeConflicts { get; set; }
    public string BuildStatus { get; set; } = string.Empty;
    public List<WorkItem> RelatedWorkItems { get; set; } = new List<WorkItem>();
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public int ChangedFiles { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    
    // Display properties
    public string ReviewersDisplay => string.Join(", ", Reviewers.Select(r => r.DisplayName));
    public string ApprovalStatus => $"{Reviewers.Count(r => r.Vote > 0)}/{Reviewers.Count} approved";
    public string ApprovalStatusShort => $"{Reviewers.Count(r => r.Vote > 0)}/{Reviewers.Count}";
    public string WorkItemsDisplay => string.Join(", ", RelatedWorkItems.Select(w => $"#{w.Id}"));
    public string ChangesSummary => $"+{AddedLines} -{DeletedLines} ({ChangedFiles} files)";
    
    // Status icons
    public string StatusIcon => Status.ToLower() switch
    {
        "active" => "🔵",
        "completed" => "✅",
        "abandoned" => "❌",
        "draft" => "📝",
        _ => "⚪"
    };
    
    public string ApprovalIcon => GetApprovalIcon();
    
    private string GetApprovalIcon()
    {
        if (!Reviewers.Any()) return "👤";
        
        var approvedCount = Reviewers.Count(r => r.Vote > 0);
        var rejectedCount = Reviewers.Count(r => r.Vote < 0);
        
        if (rejectedCount > 0) return "❌";
        if (approvedCount == Reviewers.Count) return "✅";
        if (approvedCount > 0) return "🟡";
        return "⏳";
    }
    
    public string BuildStatusIcon => BuildStatus.ToLower() switch
    {
        "succeeded" => "✅",
        "failed" => "❌",
        "partiallySucceeded" => "🟡",
        "inProgress" => "🔄",
        _ => "⚪"
    };
    
    public string ConflictIcon => HasMergeConflicts ? "⚠️" : "✅";
}

public class Reviewer
{
    public string DisplayName { get; set; } = string.Empty;
    public string UniqueDisplayName { get; set; } = string.Empty;
    public int Vote { get; set; } // -10=rejected, -5=waiting, 0=no vote, 5=approved with suggestions, 10=approved
    public bool IsRequired { get; set; }
    public bool HasDeclined { get; set; }
}

public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}