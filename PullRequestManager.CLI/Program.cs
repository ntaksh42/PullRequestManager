using AzureDevopsTool.Core.Models;
using AzureDevopsTool.Core.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Azure DevOps Pull Request Tool - CLI");
        Console.WriteLine("=====================================");

        if (args.Length < 4)
        {
            Console.WriteLine("Usage: AzureDevopsTool.CLI <organization> <project> <repository> <personal-access-token>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  organization          Azure DevOps organization name");
            Console.WriteLine("  project              Azure DevOps project name");
            Console.WriteLine("  repository           Repository name");
            Console.WriteLine("  personal-access-token Personal Access Token for authentication");
            return;
        }

        var config = new AzureDevOpsConfig
        {
            Organization = args[0],
            Project = args[1],
            Repository = args[2],
            PersonalAccessToken = args[3]
        };

        try
        {
            Console.WriteLine($"Connecting to Azure DevOps...");
            Console.WriteLine($"Organization: {config.Organization}");
            Console.WriteLine($"Project: {config.Project}");
            Console.WriteLine($"Repository: {config.Repository}");
            Console.WriteLine();

            var service = new AzureDevOpsService(config);
            var pullRequests = await service.GetPullRequestsAsync();

            Console.WriteLine($"Found {pullRequests.Count()} pull requests:");
            Console.WriteLine();

            if (pullRequests.Any())
            {
                Console.WriteLine($"{"ID",-6} {"Status",-15} {"Title",-50} {"Created By",-20} {"Created Date",-12}");
                Console.WriteLine(new string('-', 103));

                foreach (var pr in pullRequests.OrderByDescending(p => p.CreatedDate))
                {
                    var title = pr.Title.Length > 47 ? pr.Title.Substring(0, 47) + "..." : pr.Title;
                    var createdBy = pr.CreatedBy.Length > 17 ? pr.CreatedBy.Substring(0, 17) + "..." : pr.CreatedBy;
                    
                    Console.WriteLine($"{pr.Id,-6} {pr.Status,-15} {title,-50} {createdBy,-20} {pr.CreatedDate:yyyy/MM/dd}");
                }
            }
            else
            {
                Console.WriteLine("No pull requests found.");
            }

            Console.WriteLine();
            Console.WriteLine("To get detailed information in JSON format, use --json flag (not implemented yet)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
