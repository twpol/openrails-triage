using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Open_Rails_Triage.GitHub
{
	public class Query
	{
		const string Endpoint = "https://api.github.com/graphql";

		readonly string Token;

		HttpClient Client = new HttpClient();

		public Query(string token)
		{
			Token = token;
		}

		internal async Task<JObject> Get(string query)
		{
			for (var attempt = 0; attempt < 3; attempt++)
			{
				var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
				request.Headers.UserAgent.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Open-Rails-Code-Bot", "1.0"));
				request.Headers.Authorization = new AuthenticationHeaderValue("bearer", Token);
				var graphQuery = new GraphQuery { Query = $"query {{ {query} }}" };
				var graphQueryJson = JsonConvert.SerializeObject(graphQuery);
				request.Content = new StringContent(graphQueryJson, Encoding.UTF8, "application/json");
				var response = await Client.SendAsync(request);
				var text = await response.Content.ReadAsStringAsync();
				var json = JObject.Parse(text);
				if (json["data"] != null) return json;
				Console.Error.WriteLine($"WARNING: Error from GitHub: {text}");
			}
			throw new DataException("Failed to get data from GitHub");
		}

		public async IAsyncEnumerable<GraphPullRequest> GetPullRequests(string organization, string repository)
		{
			var cursor = new string(' ', 12);
			while (cursor.Length > 11)
			{
				var query = @"
					organization(login: """ + organization + @""") {
						repository(name: """ + repository + @""") {
							pullRequests(first: 100" + cursor + @") {
								nodes {" + GraphPullRequest.FIELD_QUERY + @"}
								pageInfo {
									endCursor
								}
							}
						}
					}
				";
				var response = await Get(query);
				foreach (var item in response["data"]["organization"]["repository"]["pullRequests"]["nodes"].ToObject<List<GraphPullRequest>>()) yield return item;
				cursor = $", after: \"{response["data"]["organization"]["repository"]["pullRequests"]["pageInfo"]["endCursor"]}\"";
				// cursor = ""; // FIXME: Remove this line
			}
		}

		public async Task<GraphPullRequest> GetPullRequest(string organization, string repository, int number)
		{
			var query = @"
				organization(login: """ + organization + @""") {
					repository(name: """ + repository + @""") {
						pullRequest(number: " + number + @") {" + GraphPullRequest.FIELD_QUERY + @"}
					}
				}
			";
			var response = await Get(query);
			return response["data"]["organization"]["repository"]["pullRequest"].ToObject<GraphPullRequest>();
		}
	}
}
