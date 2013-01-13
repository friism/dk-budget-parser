using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EB.Budget.Model;
using EB.Budget.Persistence;
using HtmlAgilityPack;

namespace EB.Budget.DataLoad
{
	public class DataLoader
	{
		public void ParseAndStoreAll()
		{
			var lines = Enumerable.Range(2003, 11).SelectMany(x => Read(x));
			var context = new Context();
			try
			{
				context.Configuration.AutoDetectChangesEnabled = false;

				foreach (var line in lines)
				{
					context.BudgetLines.Add(line);
				}
				context.SaveChanges();
			}
			finally
			{
				context.Configuration.AutoDetectChangesEnabled = true;
			}
		}

		public void ParseAndStore(int year)
		{
			using (var db = new Context())
			{
				var lines = Read(year);
				foreach (var line in lines)
				{
					db.BudgetLines.Add(line);
				}
				Console.WriteLine("Submitting {0}...", year);
				db.SaveChanges();
			}
		}
		
		public IEnumerable<BudgetLine> Read(int year)
		{
			var streamReader = new StreamReader("..\\..\\data\\" + year + ".html", Encoding.GetEncoding("ISO-8859-1"));
			string text = streamReader.ReadToEnd();
			streamReader.Close();

			string sepstring = @"<TR ALIGN=""right"" CLASS=""tabcelle""><TD ALIGN=""left"" CLASS=""tabforsp"">";
			string[] sep = new string[] { 
						sepstring
					};

			var firsttr = text.IndexOf(sepstring);
			var aftertable = text.Substring(firsttr, text.Length - firsttr);
			//text.Split(sep, StringSplitOptions.RemoveEmptyEntries).Skip(1).Aggregate((a, b) => a + sepstring + b);
			var intable = aftertable.Split(new string[] { "</TABLE>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim();

			var perfect = intable.Replace("<TD>", @"</TD><TD>").Replace("\n", @"</TD></TR>");

			perfect = "<table>" + perfect + "</table>";

			var dococument = new HtmlDocument();
			dococument.LoadHtml(perfect);

			var trs = dococument.DocumentNode.ChildNodes[0].ChildNodes;
			var lineatlevel = new Dictionary<int, BudgetLine>();
			foreach (var tr in trs)
			{
				var cells = tr.SelectNodes("td");
				var name = cells[0].InnerText;
				var yR = cells[1].InnerText;
				var yB = cells[2].InnerText;
				var yF = cells[3].InnerText;
				var yBO1 = cells[4].InnerText;
				var yBO2 = cells[5].InnerText;
				var yBO3 = cells[6].InnerText;

				var starttrimmed = name.TrimStart(' ', ' ');
				var numspace = name.Length - starttrimmed.Length;
				int linelevel = (numspace - 1) / 3;

				var trimmedname = name.Trim();
				var spendcode = trimmedname.Split(' ').First();
				var itemname = trimmedname.Split(' ').Skip(1).Aggregate((a, b) => a + " " + b).Replace("(Anm.)", "").Trim();

				var line = new BudgetLine
				{
					LineName = itemname,
					LineCode = spendcode,
					Year = year,
					CurrentYearBudget = GetAmount(yF),
					Year1Budget = GetAmount(yBO1),
					Year2Budget = GetAmount(yBO2),
					Year3Budget = GetAmount(yBO3),
					PreviousYear1Budget = GetAmount(yB),
					PreviousYear2Budget = GetAmount(yR),
					LineLevel = (byte)linelevel,
				};

				if (linelevel > 0)
				{
					line.ParentBudgetLine = lineatlevel[linelevel - 1];
				}
				lineatlevel[linelevel] = line;
				yield return line;
			}
		}

		private decimal GetAmount(string amount)
		{
			var millions = decimal.Parse(amount, CultureInfo.GetCultureInfo("da-dk"));
			return millions * 1000000;
		}
	}
}
