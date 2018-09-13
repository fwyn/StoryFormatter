using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace

public class IniReader : IEnumerable<KeyValuePair<string,IReadOnlyDictionary<string, string>>>
{

	internal static readonly string LineComment = ";";
	internal static readonly string SectionStart = "[";
	internal static readonly string SectionEnd = "]";
	internal static readonly char[] ValueSplit = {'='};

	protected readonly StringComparer Comparer;
	protected readonly Dictionary<string, Dictionary<string, string>> Sections;

	public IniReader(string path,
		InvalidSectionStrategy invalidSectionStrategy = 0,
		DuplicateSectionStrategy duplicateSectionStrategy = 0,
		DuplicateKeyStrategy duplicateKeyStrategy = 0,
		StringComparer comparer = null)
	{
		// Default to the OrdinalIgnoreCase comparer.
		Comparer = comparer ?? StringComparer.OrdinalIgnoreCase;

		// Initialize our section container
		Sections = new Dictionary<string, Dictionary<string, string>>(Comparer);

		// Default section.
		var section = new Dictionary<string, string>(Comparer);
		Sections[String.Empty] = section;

		var index = 0;
		foreach (var originalLine in File.ReadAllLines(path))
		{
			index++;

			// Don't trim the end of all lines.
			var line = originalLine.TrimStart();
			if (String.IsNullOrEmpty(line))
				continue;
			if (line.StartsWith(LineComment))
				continue;

			if (line.StartsWith(SectionStart))
			{
				// Do trim the end of section lines.
				var sectionLine = line.TrimEnd();
				if (!sectionLine.EndsWith(SectionEnd))
				{
					switch (invalidSectionStrategy)
					{
						case InvalidSectionStrategy.ThrowException:
							throw new ApplicationException($"[{path}:{index}] Invalid section specification.");
						case InvalidSectionStrategy.Ignore:
							// Ignore by adding keys to a section that is not stored.
							section = new Dictionary<string, string>(Comparer);
							continue;
						case InvalidSectionStrategy.Merge:
							// Just skip and leave the current section as is.
							continue;
					}
				}
				var sectionName = sectionLine.Substring(1, sectionLine.Length - 2);

				if (!Sections.ContainsKey(sectionName))
				{
					// New section.
					section = new Dictionary<string, string>(Comparer);
					Sections[sectionName] = section;
					continue;
				}

				switch (duplicateSectionStrategy)
				{
					case DuplicateSectionStrategy.ThrowException:
						throw new ApplicationException($"[{path}:{index}] Duplicate section specification.");
					case DuplicateSectionStrategy.Ignore:
						// Ignore by adding keys to a section that is not stored.
						section = new Dictionary<string, string>(Comparer);
						continue;
					case DuplicateSectionStrategy.Merge:
						// Merge by reusing the existing section.
						section = Sections[sectionName];
						continue;
					case DuplicateSectionStrategy.Replace:
						// Overwrite by replacing the existing section.
						section = new Dictionary<string, string>(Comparer);
						Sections[sectionName] = section;
						continue;
				}

				continue;
			}

			// Ok, whatever remains should be a key.
			var keyParts = line.Split(ValueSplit, 2);
			var keyName = keyParts[0];
			var keyValue = keyParts.Length > 1 ? keyParts[1] : String.Empty;

			if (!section.ContainsKey(keyName))
			{
				// New key.
				section[keyName] = keyValue;
				continue;
			}

			switch (duplicateKeyStrategy)
			{
				case DuplicateKeyStrategy.ThrowException:
					throw new ApplicationException($"[{path}:{index}] Duplicate key specification.");
				case DuplicateKeyStrategy.Ignore:
					continue;
				case DuplicateKeyStrategy.Replace:
					section[keyName] = keyValue;
					continue;
			}
		} // foreach (var originalLine
	}

	public bool Exists(string section) =>
		Sections.ContainsKey(section ?? String.Empty);
	public bool Exists(string section, string key) =>
		Sections.ContainsKey(section ?? String.Empty) && Sections[section ?? String.Empty].ContainsKey(key);

	public IReadOnlyDictionary<string, string> this[string section] =>
		!Exists(section)
			? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
			: new ReadOnlyDictionary<string, string>(Sections[section ?? String.Empty]);

	public string this[string section, string key] =>
		!Exists(section, key) ? null : Sections[section ?? String.Empty][key];

	public IEnumerator<KeyValuePair<string, IReadOnlyDictionary<string, string>>> GetEnumerator()
	{
		foreach (var section in Sections)
			yield return new KeyValuePair<string, IReadOnlyDictionary<string, string>>(
				section.Key, new ReadOnlyDictionary<string, string>(section.Value));
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public enum InvalidSectionStrategy
	{

		/// <summary>
		/// Throw an exception on invalid section. (default)
		/// </summary>
		ThrowException = 0,

		/// <summary>
		/// Ignore keys of invalid section.
		/// </summary>
		Ignore,

		/// <summary>
		/// Add keys to previous section.
		/// </summary>
		Merge,

	}

	public enum DuplicateSectionStrategy
	{

		/// <summary>
		/// Throw an exception on duplicate section. (default)
		/// </summary>
		ThrowException = 0,

		/// <summary>
		/// Ignore keys of duplicate section.
		/// </summary>
		Ignore,

		/// <summary>
		/// Add keys to existing section.
		/// </summary>
		Merge,

		/// <summary>
		/// Remove existing section.
		/// </summary>
		Replace,

	}

	public enum DuplicateKeyStrategy
	{

		/// <summary>
		/// Throw an exception on duplicate key. (default)
		/// </summary>
		ThrowException = 0,

		/// <summary>
		/// Ignore duplicate keys.
		/// </summary>
		Ignore,

		/// <summary>
		/// Overwrite existing value.
		/// </summary>
		Replace,

	}

}

public static class IniReaderExt
{

	public static string GetString(this IReadOnlyDictionary<string, string> src, string key)
	{
		return src.ContainsKey(key) ? src[key] : null;
	}

	public static int? GetInt32(this IReadOnlyDictionary<string, string> src, string key, NumberStyles styles = NumberStyles.Integer, IFormatProvider provider = null)
	{
		var v = src.GetString(key);
		if (v == null)
			return null;
		if (provider == null)
			provider = CultureInfo.InvariantCulture;
		if (Int32.TryParse(v, styles, provider, out var i))
			return i;
		return null;
	}

	public static string[] BooleanTrue = {"1", "true", "t", "yes", "y", "ja", "j"};
	public static string[] BooleanFalse = {"0", "false", "f", "no", "n", "nee"};
	public static bool? GetBoolean(this IReadOnlyDictionary<string, string> src, string key)
	{
		var v = src.GetString(key);
		if (v == null)
			return null;
		if (BooleanTrue.Contains(v, StringComparer.OrdinalIgnoreCase))
			return true;
		if (BooleanFalse.Contains(v, StringComparer.OrdinalIgnoreCase))
			return false;
		return null;
	}

}
