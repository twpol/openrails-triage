using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Open_Rails_Triage
{
	/// <summary>
	/// Matches configuration patterns with basic key/value[] objects
	/// </summary>
	/// <remarks>
	/// <code>
	/// Order of operations:       Required  Repeated  Default
	/// - $not/$and/$or                      Yes       $and
	/// - (property)               Yes
	/// - $not
	/// - $all/$any                                    $all
	/// - $eq/$ne/$lt/$le/$gt/$ge                      $eq
	/// - (value)                  Yes
	/// </code>
	/// </remarks>
	class Matcher
	{
		readonly IConfigurationSection Config;
		readonly bool Debug;

		public Matcher(IConfigurationSection config, bool debug = false)
		{
			Config = config;
			Debug = debug;
		}

		public bool IsMatch(Dictionary<string, string[]> data)
		{
			if (Debug) Console.WriteLine($"Matcher.IsMatch({Config.Path})");
			if (Debug) foreach (var item in data) Console.WriteLine($"- {item.Key} = {string.Join(" / ", item.Value)}");
			return IsMatch(Config, data);
		}

		bool IsMatch(IConfigurationSection config, Dictionary<string, string[]> data)
		{
			if (config.Value != null) throw new InvalidDataException($"{config.Path}: Unexpected value");
			var match = true;
			foreach (var prop in config.GetChildren())
			{
				var result = prop.Key.Trim() switch
				{
					"$not" when prop.Value == null => !IsMatch(prop, data),
					"$and" when prop.Value == null => prop.GetChildren().All(sub => IsMatch(sub, data)),
					"$or" when prop.Value == null => prop.GetChildren().Any(sub => IsMatch(sub, data)),
					var a when a.StartsWith("$") => throw new InvalidDataException($"{prop.Path}: Unknown object operator {prop.Key}"),
					_ when data.ContainsKey(prop.Key) && prop.Value == null => IsMatch(prop, data[prop.Key]),
					_ when data.ContainsKey(prop.Key) && prop.Value != null => data[prop.Key].All(value => value.Equals(prop.Value)),
					_ => throw new InvalidDataException($"{prop.Path}: Unknown object property {prop.Key}"),
				};
				if (Debug) Console.WriteLine($"{prop.Path}={prop.Value ?? "<null>"} OBJECT {result}");
				match &= result;
			}
			if (Debug) Console.WriteLine($"{config.Path}={config.Value ?? "<null>"} OBJECT {match}");
			return match;
		}

		bool IsMatch(IConfigurationSection config, string[] data)
		{
			var match = true;
			var valueOperator = 0;
			foreach (var prop in config.GetChildren())
			{
				var result = prop.Key.Trim() switch
				{
					"$not" when prop.Value == null => !IsMatch(prop, data),
					"$all" when prop.Value == null => data.All(value => IsMatch(prop, value)),
					"$any" when prop.Value == null => data.Any(value => IsMatch(prop, value)),
					"$not" when prop.Value != null => !data.All(value => value.Equals(prop.Value)),
					"$all" when prop.Value != null => data.All(value => value.Equals(prop.Value)),
					"$any" when prop.Value != null => data.Any(value => value.Equals(prop.Value)),
					var a when a.StartsWith("$") && prop.Value != null => ++valueOperator == 0,
					_ => throw new InvalidDataException($"{prop.Path}: Unknown property value {prop.Key}"),
				};
				if (valueOperator > 0) break;
				if (Debug) Console.WriteLine($"{prop.Path}={prop.Value ?? "<null>"} PROPERTY {result}");
				match &= result;
			}
			if (valueOperator > 0) match = data.All(value => IsMatch(config, value));
			if (Debug) Console.WriteLine($"{config.Path}={config.Value ?? "<null>"} PROPERTY {match}");
			return match;
		}

		bool IsMatch(IConfigurationSection config, string data)
		{
			var match = true;
			foreach (var prop in config.GetChildren())
			{
				var result = prop.Key.Trim() switch
				{
					_ when prop.Value == null => throw new InvalidDataException($"{prop.Path}: Unknown nesting of value matcher"),
					"$eq" => data.Equals(prop.Value),
					"$ne" => !data.Equals(prop.Value),
					"$startsWith" => data.StartsWith(prop.Value),
					"$contains" => data.Contains(prop.Value),
					"$endsWith" => data.EndsWith(prop.Value),
					"$lt" => double.TryParse(data, out var dataF) && double.TryParse(prop.Value, out var valueF) && dataF < valueF,
					"$le" => double.TryParse(data, out var dataF) && double.TryParse(prop.Value, out var valueF) && dataF <= valueF,
					"$gt" => double.TryParse(data, out var dataF) && double.TryParse(prop.Value, out var valueF) && dataF > valueF,
					"$ge" => double.TryParse(data, out var dataF) && double.TryParse(prop.Value, out var valueF) && dataF >= valueF,
					_ => throw new InvalidDataException($"{prop.Path}: Unknown value operator {prop.Key}"),
				};
				if (Debug) Console.WriteLine($"{prop.Path}={prop.Value ?? "<null>"} '{data}' VALUE {result}");
				match &= result;
			}
			if (Debug) Console.WriteLine($"{config.Path}={config.Value ?? "<null>"} '{data}' VALUE {match}");
			return match;
		}
	}
}
