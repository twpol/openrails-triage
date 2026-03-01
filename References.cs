using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open_Rails_Triage
{
	class References : Dictionary<string, Reference>
	{
		static readonly Regex ReferencesPattern = new("(?:https://github.com/[^/]+/[^/]+/pull/[0-9]+|https://bugs\\.launchpad\\.net/[^/]+/\\+bug/[0-9]+)");
		// TODO: https://trello\\.com/c/[0-9a-zA-Z]+
		// TODO: https://blueprints\\.launchpad\\.net/[^/]+/\\+spec/[0-9a-z-]+

		static string GetReferenceType(string reference) => reference switch
		{
			var a when a.StartsWith("https://github.com/") && a.Contains("/pull/") => "github-pr",
			var a when a.StartsWith("https://bugs.launchpad.net/") => "launchpad-bug",
			// TODO: var a when a.StartsWith("https://blueprints.launchpad.net/") => "launchpad-blueprint",
			// TODO: var a when a.StartsWith("https://trello.com/c/") => "trello-card",
			_ => throw new InvalidDataException($"Unknown reference: {reference}"),
		};

		public void Add(Git.Commit commit, out HashSet<string> types)
		{
			types = new();
			foreach (var match in commit.Commits.Select(commit => ReferencesPattern.Matches(commit.Message)).Append(ReferencesPattern.Matches(commit.Message)).SelectMany(match => match).Select(match => match.Value))
			{
				types.Add(GetReferenceType(match));
				GetReference(match).GitCommits.Add(commit);
			}
		}

		public void Add(GitHub.GraphPullRequest pr, out HashSet<string> types)
		{
			types = new();
			foreach (var match in ReferencesPattern.Matches(pr.Body).Select(match => match.Value))
			{
				types.Add(GetReferenceType(match));
				GetReference(match).GitHubPullRequests.Add(pr);
			}
		}

		public void Add(Launchpad.Bug bug, out HashSet<string> types)
		{
			types = new();
			foreach (var match in ReferencesPattern.Matches(bug.Description).Select(match => match.Value))
			{
				types.Add(GetReferenceType(match));
				GetReference(match).LaunchpadBugs.Add(bug);
			}
		}

		Reference GetReference(string key)
		{
			if (!ContainsKey(key)) this[key] = new();
			return this[key];
		}
	}

	class Reference
	{
		public List<Git.Commit> GitCommits { get; } = new();
		public List<GitHub.GraphPullRequest> GitHubPullRequests { get; } = new();
		public List<Launchpad.Bug> LaunchpadBugs { get; } = new();
	}
}
