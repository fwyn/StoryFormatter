using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StoryFormatter
{

	public partial class MainWindow : Form
	{

		public MainWindow()
		{
			InitializeComponent();
		}

		private void MainWindow_Load(object sender, EventArgs e)
		{
			// Prep our renderer.
			var renderer = new StoryRenderer(CreateGraphics(), Program.Ini);

			// Render each ini section.
			foreach (var sec in Program.Ini)
			{
				if (!(sec.Value.GetBoolean("Render") ?? false))
					continue;

				var result = renderer.Render(Program.FileLines, sec.Key);
				File.WriteAllText(String.Concat(Program.FileBasePath, ".", sec.Key), result);
			}

			Application.Exit();
		}

	}

}
