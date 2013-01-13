using System;
using System.Linq;
using EB.Budget.DataLoad;
using EB.Budget.Export;

namespace EB.Budget
{
	public class Program
	{
		static void Main(string[] args)
		{
			var loader = new DataLoader();
			var lines = loader.Read(2013);
			//Output(2012);
			//Console.WriteLine("Total line count: {0}", lines.Count());
			//OutputByCategory();
			Console.WriteLine("Press the any key...");
			Console.ReadKey();
			//ExportAll();
			//OutputMultipleYears(2011);
		}
	}
}
