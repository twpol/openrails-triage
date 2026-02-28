using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Open_Rails_Triage
{
	class FormMatcher
	{
		readonly IConfigurationSection Config;
		readonly bool Debug;
		readonly Dictionary<string, Matcher> Required = new();
		readonly Dictionary<string, Matcher> Optional = new();

		public FormMatcher(IConfigurationSection config, bool debug = false)
		{
			Config = config;
			Debug = debug;
			foreach (var form in Config.GetChildren())
			{
				var hasRequired = form.GetChildren().Any(entry => entry.Key == "$required");
				Required[form.Key] = new Matcher(hasRequired ? form.GetSection("$required") : form, Debug);
				Optional[form.Key] = new Matcher(form.GetSection("$optional"), Debug);
			}
		}

		public FormMatchResult Match(Dictionary<string, string[]> data)
		{
			var successes = new List<string>();
			var failures = new List<string>();
			var issues = new HashSet<string>();
			foreach (var form in Config.GetChildren())
			{
				if (!Required[form.Key].IsMatch(data)) continue;
				if (Optional[form.Key].IsMatch(data))
				{
					successes.Add(form.Key);
				}
				else
				{
					failures.Add(form.Key);
					issues.Add($"Form '{form.Key}' matched '{string.Join("','", Required[form.Key].GetFields())}' but not '{string.Join("','", Optional[form.Key].GetFields())}'");
				}
			}
			return new(successes, failures, successes.Count == 0 ? issues : new HashSet<string>());
		}

		public record FormMatchResult(IReadOnlyList<string> Successes, IReadOnlyList<string> Failures, HashSet<string> Issues);
	}
}
