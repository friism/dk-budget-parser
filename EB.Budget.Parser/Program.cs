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
			new Exporter().OutputForTreeMapDisplay(2013);
			Console.WriteLine("Press the any key...");
			Console.ReadKey();
		}
	}
}
