using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Dapplo.Jira.Query;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatBots.Plugins;

class JiraPlugin
{
    IJiraClient jira;

    public JiraPlugin()
    {
        var url = Environment.GetEnvironmentVariable("JIRA_URL");
        var user = Environment.GetEnvironmentVariable("JIRA_USER");
        var token = Environment.GetEnvironmentVariable("JIRA_TOKEN");

        this.jira = JiraClient.Create(new Uri(url));
        this.jira.SetBasicAuthentication(user, token);
    }

    //[KernelFunction("get_project_versions")]
    //[Description("Get all fix versions for a project.")]
    //public Task<string> GetProjectVersions(
    //    [Description("Project key, e.g., 'PROJ'")] string projectKey)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_all_projects")]
    //[Description("Get all projects accessible to the current user.")]
    //public Task<string> GetAllProjects(
    //    [Description("Include archived projects (default: false)")] bool includeArchived = false)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_project_issues")]
    //[Description("Get issues for a specific Jira project.")]
    //public Task<string> GetProjectIssues(
    //    [Description("Project key, e.g., 'PROJ'")] string projectKey,
    //    [Description("Max results (1–50). Default: 10")] int limit = 10,
    //    [Description("Start index for pagination (0-based). Default: 0")] int startAt = 0)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_agile_boards")]
    //[Description("Get agile boards by name, project, or type.")]
    //public Task<string> GetAgileBoards(
    //    [Description("Optional: board name (fuzzy search)")] string? boardName = null,
    //    [Description("Optional: project key, e.g., 'PROJ'")] string? projectKey = null,
    //    [Description("Optional: board type ('scrum' or 'kanban')")] string? boardType = null,
    //    [Description("Start index (0-based). Default: 0")] int startAt = 0,
    //    [Description("Max results (1–50). Default: 10")] int limit = 10)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_board_issues")]
    //[Description("Get all issues linked to a board filtered by JQL.")]
    //public Task<string> GetBoardIssues(
    //    [Description("Board ID, e.g., '1001'")] string boardId,
    //    [Description("JQL query string, e.g., 'status = \"In Progress\"'")] string jql,
    //    [Description("Fields to return (comma-separated). Default: essentials")] string fields = "",
    //    [Description("Start index (0-based). Default: 0")] int startAt = 0,
    //    [Description("Max results (1–50). Default: 10")] int limit = 10,
    //    [Description("Optional: expand fields, e.g., 'changelog'. Default: 'version'")] string expand = "version")
    //    => throw new NotImplementedException();

    //[KernelFunction("get_sprints_from_board")]
    //[Description("Get sprints from a board by state.")]
    //public Task<string> GetSprintsFromBoard(
    //    [Description("Board ID, e.g., '1000'")] string boardId,
    //    [Description("Optional: sprint state ('active','future','closed')")] string? state = null,
    //    [Description("Start index (0-based). Default: 0")] int startAt = 0,
    //    [Description("Max results (1–50). Default: 10")] int limit = 10)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_sprint_issues")]
    //[Description("Get issues from a sprint.")]
    //public Task<string> GetSprintIssues(
    //    [Description("Sprint ID, e.g., '10001'")] string sprintId,
    //    [Description("Fields to return (comma-separated). Default: essentials")] string fields = "",
    //    [Description("Start index (0-based). Default: 0")] int startAt = 0,
    //    [Description("Max results (1–50). Default: 10")] int limit = 10)
    //    => throw new NotImplementedException();

    [KernelFunction("jira_get_issue")]
    //[Description("Get details of a Jira issue including Epic links and relationships.")]
    [Description("Get details of a Jira issue")]
    public async Task<string> GetIssue(
        [Description("Issue key, e.g., 'PROJ-123'")] string issueKey)
        //[Description("Fields to return (comma-separated, '*all' for all). Example: 'summary,status'")] string fields = "",
        //[Description("Optional: expand fields (e.g., 'renderedFields', 'changelog')")] string? expand = null,
        //[Description("Max number of comments (0 = none). Default: 10")] int commentLimit = 10,
        //[Description("Optional: issue properties to return (comma-separated)")] string? properties = null,
        //[Description("Optional: update issue view history (default: true)")] bool updateHistory = true)
    {
        var result = await this.jira.Issue.SearchAsync(Where.IssueKey.Is(issueKey));
        foreach (var issue in result)
            return issue.ToString();
            //return JsonSerializer.Serialize(issue, new JsonSerializerOptions { WriteIndented = true });

        return $"{issueKey} not found";
    }

    //[KernelFunction("get_transitions")]
    //[Description("Get available status transitions for an issue.")]
    //public Task<string> GetTransitions(
    //    [Description("Issue key, e.g., 'PROJ-123'")] string issueKey)
    //    => throw new NotImplementedException();

    //[KernelFunction("get_worklog")]
    //[Description("Get worklog entries for an issue.")]
    //public Task<string> GetWorklog(
    //    [Description("Issue key, e.g., 'PROJ-123'")] string issueKey)
    //    => throw new NotImplementedException();

    //[KernelFunction("update_issue")]
    //[Description("Update an existing issue (status, fields, attachments, etc.).")]
    //public Task<string> UpdateIssue(
    //    [Description("Issue key, e.g., 'PROJ-123'")] string issueKey,
    //    [Description("Fields to update (JSON, e.g., {\"summary\": \"New Title\"})")] string fields,
    //    [Description("Optional: additional fields (JSON string)")] string? additionalFields = null,
    //    [Description("Optional: file paths as JSON array or comma-separated string")] string? attachments = null)
    //    => throw new NotImplementedException();


    [KernelFunction("jira_add_comment")]
    [Description("Add a comment to an issue.")]
    public async Task<string> AddComment(
        [Description("Issue key, e.g., 'PROJ-123'")] string issueKey,
        [Description("Comment text in Markdown")] string comment)
    {
        Issue foundIssue = null;
        var result = await this.jira.Issue.SearchAsync(Where.IssueKey.Is(issueKey));
        foreach (var issue in result)
        {
            foundIssue = issue;
            break;
        }

        if (foundIssue == null) return $"{issueKey} not found";

        await foundIssue.AddCommentAsync(comment);
        return $"Comment added to {issueKey}";
    }

    [KernelFunction("jira_add_worklog")]
    [Description("Add a worklog entry to an issue.")]
    public async Task<string> AddWorklog(
        [Description("Issue key, e.g., 'PROJ-123'")] string issueKey,
        [Description("Time spent, e.g., '1h 30m', '1d', '30m'")] string timeSpent,
        [Description("Optional: comment in Markdown")] string? comment = null)
        //[Description("Optional: start time ISO format, e.g., '2023-08-01T12:00:00Z'")] string? started = null,
        //[Description("Optional: new original estimate")] string? originalEstimate = null,
        //[Description("Optional: new remaining estimate")] string? remainingEstimate = null)
    {
        var worklog = new Worklog { TimeSpent = timeSpent };
        if (comment != null) worklog.Comment = comment;

        await jira.WorkLog.CreateAsync(issueKey, worklog);

        return $"Worklog added to {issueKey}";
    }
}
