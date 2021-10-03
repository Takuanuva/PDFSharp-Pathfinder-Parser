using PDF_Data_Extractor.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDF_Data_Extractor
{
	class Program
	{
		/// <summary>
		/// Main function.
		/// </summary>
		/// <param name="args">Command line params.</param>
		static void Main(string[] args)
		{
			Console.WriteLine("STARTING EXTRACTOR...");

			//check if argument was not provided
			if (args.Count() <= 0)
			{
				Console.WriteLine("NO PARAM!");
				return;
			}

			Console.WriteLine($"FILE PATH: {args.First()}");
			Console.WriteLine("STARTING EXTRACTION...");

			//attempt to parse file data
			PdfExtractor extractor = new PdfExtractor(args.First());
			extractor.ExtractDataToFiles();
		}
	}
}
