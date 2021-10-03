using DocumentParser.DataTypes;
using DocumentParser.Utilities.DataExtraction;
using DocumentParser.Utilities.DataRendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSharp_Pathfinder_Parser_3
{
	public partial class Form1 : Form
	{
		/// <summary>
		/// Currently parsed document set.
		/// </summary>
		private PdfDocumentParser DocumentSet;

		public Form1()
		{
			InitializeComponent();
		}


		bool flag = true;
		private void PrintPanel_Paint(object sender, PaintEventArgs e)
		{
			//check if current page does not have any elements with unknown style
			//{
			//	var page = DocumentObject.Pages[(int)numericUpDown1.Value];
			//	var knownStyleDict = TextStyle.knownStyleDict;
			//	bool unknownStyleFound = false;
			//	foreach (var kv in page.TextStyleOccurences)
			//		if (!knownStyleDict.ContainsKey(kv.Key.GetHashCode()))
			//			unknownStyleFound = true;
			//	if (!unknownStyleFound)
			//	{
			//		numericUpDown1.Value++;
			//		return;
			//	}
			//}

			//print document
			if (DocumentSet != null)
			{
				if (flag)
				{
					PrintPanel.Invalidate();
					flag = false;
				}
				else
				{
					flag = true;
					int margin = 10;
					PageRenderer renderer = new PageRenderer(
#warning TODO: FIX!
						DocumentSet.Documents[0].Pages[(int)numericUpDown1.Value],
						new Rectangle(
							margin,
							margin,
							PrintPanel.Size.Width - (2 * margin),
							PrintPanel.Size.Height - (2 * margin)),
						e.Graphics);
					renderer.RenderPage();
				}
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			//DEBUG!
			DocumentSet = new PdfDocumentParser(@"D:\SYNCABLES\Assets and Resources\Tabletop Stuff\RPG\Pathfinder\Handbooks\Core Books\Pathfinder - Core Rulebook Lite.pdf");
			//DocumentSet = new PdfDocumentParser(@"D:\SYNCABLES\Assets and Resources\Tabletop Stuff\RPG\Pathfinder\Handbooks\Pathfinder Campaign Setting - Inner Sea\Pathfinder Campaign Setting - Technology Guide.pdf");

			//DEBUG 2: SAVE TEXT TO FILE
			//string filepath = @"D:\SYNCABLES\Pathfinder TeX.tex";
			//File.Delete(filepath);
			//StringBuilder sb = new StringBuilder();
			//foreach (DocumentPage page in DocumentObject.Pages)
			//{
			//	foreach (TextLine line in page.LineBuffer.Lines)
			//		sb.AppendLine(line.Text.TeXValue);
			//	File.AppendAllText(filepath, sb.ToString());
			//	sb.Clear();
			//}
			//numericUpDown1.Maximum = DocumentObject.Pages.Count - 1;

			//DEBUG 3: save page renders to files
			//MOVED TO DOCUMENT
		}

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			PrintPanel.Invalidate();
		}
	}
}
