using PdfDocumentData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SliceAdjuster
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//check if argument was not provided
			if (args.Count() <= 0)
				return;

			//get project data from file
			DocumentProject projectData;
			using (TextReader reader = new StreamReader(args[0]))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(DocumentProject));
				projectData = serializer.Deserialize(reader) as DocumentProject;
			}
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1(projectData));
		}
	}
}
