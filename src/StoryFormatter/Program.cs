using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StoryFormatter
{

	internal static class Program
	{

		/// <summary>
		///     The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
			args = new[]
			{
				@"..\..\..\..\test\FormattingTest.story"
			};
#endif

			if (!ParseFilePath(args))
				return;

			Application.Run(new MainWindow());
		}

		private static bool ParseFilePath(string[] args)
		{
			// Check if we received anything.
			if (args == null || args.Length == 0)
			{
				MessageBox.Show(
					"Please specify a text file to format on the command line.",
					"StoryFormatter - Invalid command line", MessageBoxButtons.OK);
				return false;
			}

			// Combine the whole argument list to single path.
			var path = String.Join(" ", args);

			// Then check if that path exists.
			if (!File.Exists(path))
			{
				MessageBox.Show(
					$@"Cannot find: {path}
Please specify a text file to format on the command line.",
					"StoryFormatter - Invalid command line", MessageBoxButtons.OK);
				return false;
			}

			FileStory = new FileInfo(path);
			FileDirectory = FileStory.Directory;
			FileBaseName = FileStory.NameWithoutExtension();
			FileBasePath = Path.Combine(FileDirectory.FullName, FileBaseName);
			FileIni = FileDirectory.Combine("StoryFormatter.ini");

			// Check that we have an ini file.
			if (!FileIni.Exists)
			{
				MessageBox.Show(
					$@"StoryFormatter.ini file missing from path: {FileDirectory.FullName}
Please provide a StoryFormatter.ini file in the same directory as your story.",
					"StoryFormatter - Invalid command line", MessageBoxButtons.OK);
				return false;
			}

			// Some more checks.
			if (FileStory.Length > 250*1024)
			{
				MessageBox.Show(
					$@"File too large: {path}
Please specify a text file no larger than 250Kb.",
					"StoryFormatter - Invalid command line", MessageBoxButtons.OK);
				return false;
			}

			// Read the file and count the lines.
			FileLines = File.ReadAllLines(FileStory.FullName);

			// Some more checks.
			if (FileLines.Length > 10000)
			{
				MessageBox.Show(
					$@"File has too many lines: {path}
Please specify a text file with no more than 10000 lines.",
					"StoryFormatter - Invalid command line", MessageBoxButtons.OK);
				return false;
			}

			// Read the Ini file.
			Ini = new IniReader(FileIni.FullName,
				IniReader.InvalidSectionStrategy.Merge,
				IniReader.DuplicateSectionStrategy.Merge,
				IniReader.DuplicateKeyStrategy.Replace,
				StringComparer.OrdinalIgnoreCase);

			return true;
		}

		public static FileInfo FileStory { get; set; }
		public static DirectoryInfo FileDirectory { get; set; }
		public static string FileBaseName { get; set; }
		public static string FileBasePath { get; set; }
		public static FileInfo FileIni { get; set; }
		public static string[] FileLines { get; set; }
		public static IniReader Ini { get; set; }

	}

}
