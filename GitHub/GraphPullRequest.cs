using System;

namespace Open_Rails_Triage.GitHub
{
	public class GraphPullRequest
	{
		public const string FIELD_QUERY = @"
			url
			number
			createdAt
			author {
				login
				url
			}
			state
			title
			body
			labels(first: 100) {
				nodes {
					name
				}
			}
			additions
			deletions
			files(first: 100) {
				nodes {
					path
				}
			}
		";
		public Uri Url;
		public int Number;
		public DateTimeOffset CreatedAt;
		public GraphPullRequestActor Author;
		public GraphPullRequestState State;
		public string Title;
		public string Body;
		public GraphPullRequestLabels Labels;
		public int Additions;
		public int Deletions;
		public GraphPullRequestFiles Files;
	}

	public class GraphPullRequestActor
	{
		public string Login;
		public string Url;
	}

	public enum GraphPullRequestState
	{
		Open,
		Closed,
		Merged
	}

	public class GraphPullRequestLabels
	{
		public GraphPullRequestLabelNode[] Nodes;
	}

	public class GraphPullRequestLabelNode
	{
		public string Name;
	}

	public class GraphPullRequestFiles
	{
		public GraphPullRequestFileNode[] Nodes;
	}

	public class GraphPullRequestFileNode
	{
		public string Path;
	}
}
