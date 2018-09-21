using System;
using System.Drawing;
using System.Linq;
using System.Text;

namespace StoryFormatter
{

	/// <summary>
	/// Encapsulates the rendering logic.
	/// </summary>
	public class StoryRenderer
	{

		private static readonly PointF MeasureZeroPoint = new PointF(0, 0);
		private static readonly StringFormat MeasureFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);

		public Graphics Graphics { get; }
		public IniReader Ini { get; }

		public int WrapAfterCharacters => Ini[null].GetInt32("WrapAfterCharacters") ?? 92;
		public int ParagraphSize => Ini[null].GetInt32("ParagraphSize") ?? 112;
		public int LeadTabSize => Ini[null].GetInt32("LeadTabSize") ?? 165;
		public int LeadTabSpaces => Ini[null].GetInt32("LeadTabSpaces") ?? 3;
		public int TabSpaces => Ini[null].GetInt32("TabSpaces") ?? 3;
		public string FontFamily => Ini[null].GetString("FontFamily") ?? "Verdana";
		public string EndOn => Ini[null].GetString("EndOn");

		public string TabWidthVal { get; }
		public Font ParagraphFont { get; }
		public Font LeadTabFont { get; }
		public float LeadTabWidth { get; }
		public float WrapAfterWidth { get; }

		public StoryRenderer(Graphics graphics, IniReader ini)
		{
			Graphics = graphics;
			Ini = ini;

			TabWidthVal = new string(' ', TabSpaces);
			ParagraphFont = GetFont(ParagraphSize);
			LeadTabFont = GetFont(LeadTabSize);
			LeadTabWidth = MeasureString(new string(' ', LeadTabSpaces), LeadTabFont);

			// Character size is the size of the glyph "0" in the element's font.
			// MeasureString does a little padding though, so rather than trying
			// arithmetic, we simply measure WrapAfterCharacters x 0.
			if (0 != WrapAfterCharacters)
			{
				var wrapAfterGlyphs = new string('0', WrapAfterCharacters);
				WrapAfterWidth = Measure(wrapAfterGlyphs);
			}
			else
			{
				WrapAfterWidth = 0f;
			}
		}

		private Font GetFont(int size)
		{
			// The site's base font size is 10pt, which apparently converts to 0.8em:
			// http://websemantics.co.uk/resources/font_size_conversion_chart/
			// We have a percentage to add onto that.
			var fontSizeEm = 0.8f*(size/100f);
			var font = new Font(FontFamily, fontSizeEm);
			return font;
		}

		private float MeasureString(string text, Font font)
		{
			return Graphics.MeasureString(text, font, MeasureZeroPoint, MeasureFormat).Width;
		}
		private float Measure(string text)
		{
			return MeasureString(text.Replace("\t", TabWidthVal), ParagraphFont);
		}

		private static string ConsumeWord(string line, out string word)
		{
			var state = 0;
			for (var i = 0; i < line.Length; i++)
			{
				switch (state)
				{
					case 0: // Initial state, make sure we find a word at all.
						if (!Char.IsWhiteSpace(line, i))
							state = 1;
						break;
					case 1: // Look for the end of the word.
						if (Char.IsWhiteSpace(line, i))
							state = 2;
						break;
					case 2: // Finish once we find the next word.
						if (!Char.IsWhiteSpace(line, i))
						{
							word = line.Substring(0, i);
							return line.Substring(i);
						}
						break;
				}
			}

			// Fall through, just return everything.
			word = line;
			return String.Empty;
		}

		public string Render(string[] fileLines, string section)
		{
			var leadTabVal = Ini[section].GetString("LeadTabVal");
			var tabVal = Ini[section].GetString("TabVal");
			var nbsp = Ini[section].GetString("NonBreakableSpace");
			var tagFontOpen = Ini[section].GetString("TagFontOpen");
			var tagFontClose = Ini[section].GetString("TagFontClose");
			var tagSizeOpen = Ini[section].GetString("TagSizeOpen");
			var tagSizeClose = Ini[section].GetString("TagSizeClose");
			var tagBreak = String.Concat(Ini[section].GetString("TagBreak") ?? String.Empty, Environment.NewLine);
			var tagItalicOpen = Ini[section].GetString("TagItalicOpen");
			var tagItalicClose = Ini[section].GetString("TagItalicClose");

			var italicPrefix = Ini[null].GetString("ItalicPrefix");
			var ignoreLinePrefix = Ini[null].GetString("IgnoreLinePrefix");
			var endOnPrefix = Ini[null].GetString("EndOnPrefix");

			// Pre-define the size tags. They're the same for all lines.
			var paragraphSizeOpen = String.Format(tagSizeOpen, ParagraphSize);
			var paragraphSizeClose = tagSizeClose;
			var tabSizeOpen = String.Format(tagSizeOpen, LeadTabSize);
			var tabSizeClose = tagSizeClose;

			var result = new StringBuilder();

			// Append a header, if specified.
			if (!String.IsNullOrEmpty(Ini[section].GetString("Header")))
				result.Append(Ini[section].GetString("Header"));

			// Start with the font tag.
			result.AppendFormat(tagFontOpen, FontFamily);
			result.AppendLine();

			foreach (var original in fileLines)
			{
				var currentOpen = paragraphSizeOpen;
				var currentClose = paragraphSizeClose;

				var remaining = original;
				var leadingWidth = 0f;

				// Perform replacements.
				foreach (var pair in Ini["replace"])
					if (!String.IsNullOrEmpty(pair.Key))
						remaining = remaining.Replace(pair.Key, pair.Value);

				// Handle ignored lines.
				if (!String.IsNullOrEmpty(ignoreLinePrefix) && remaining.StartsWith(ignoreLinePrefix))
					continue;

				// Handle ending line processing.
				if (!String.IsNullOrEmpty(endOnPrefix) && remaining.StartsWith(endOnPrefix))
					break;

				// Special case for empty lines.
				if (String.IsNullOrEmpty(remaining))
				{
					result.Append(currentOpen);
					result.Append(nbsp);
					result.Append(currentClose);
					result.Append(tagBreak);
					continue;
				}

				// Handle italic lines.
				if (!String.IsNullOrEmpty(italicPrefix) && remaining.StartsWith(italicPrefix))
				{
					currentOpen = tagItalicOpen;
					currentClose = tagItalicClose;
					remaining = remaining.Substring(italicPrefix.Length);
				}

				// Handle lead tabs.
				if (remaining.StartsWith("\t"))
				{
					result.Append(tabSizeOpen);
					result.Append(leadTabVal);
					leadingWidth += LeadTabWidth;
					remaining = remaining.Substring(1);
					result.Append(tabSizeClose);
				}

				// Handle special case for no wrapping.
				if (0 == WrapAfterCharacters)
				{
					result.Append(currentOpen);
					result.Append(remaining.Replace("\t", tabVal));
					result.Append(currentClose);
					result.Append(tagBreak);
					continue;
				}

				result.Append(currentOpen);

				var firstWord = true;
				var line = new StringBuilder();
				var previous = String.Empty;
				while (!String.IsNullOrEmpty(remaining))
				{
					remaining = ConsumeWord(remaining, out var w);
					line.Append(w);
					var current = line.ToString().TrimEnd();

					// Always continue on the first word.
					if (firstWord)
					{
						previous = current;
						firstWord = false;
						continue;
					}

					// Continue if we haven't reached the wrap width yet.
					var currentWidth = leadingWidth + Measure(current);
					if (currentWidth <= WrapAfterWidth)
					{
						previous = current;
						continue;
					}

					// Write out the previous line.
					result.Append(previous.Replace("\t", tabVal));
					result.Append(tagBreak);

					// Reset the line to just this word.
					line.Clear();
					line.Append(w);
					previous = line.ToString().TrimEnd();
					leadingWidth = 0f; // Also reset the leading width.
				}

				// Write out the final line.
				result.Append(previous.Replace("\t", tabVal));
				result.Append(currentClose);
				result.Append(tagBreak);
			}

			// Close with the font tag.
			result.AppendLine(tagFontClose);

			// Append a footer, if specified.
			if (!String.IsNullOrEmpty(Ini[section].GetString("Footer")))
				result.Append(Ini[section].GetString("Footer"));

			return result.ToString();
		}

	}

}
