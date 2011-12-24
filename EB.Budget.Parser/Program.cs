using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Globalization;
using System.IO;
using EB.Budget.Model;
using FileHelpers;
using System.Data.Linq;
using System.Linq.Expressions;
using AutoMapper;

namespace EB.Budget.Parser
{
	class Program
	{
		static void Main(string[] args)
		{
			//ExportAll();
			OutputMultipleYears(2011);
		}

		private static void ParseAll()
		{
			Parse(2011);
			Parse(2010);
			Parse(2009);
			Parse(2008);
			Parse(2007);
			Parse(2006);
			Parse(2005);
			Parse(2004);
			Parse(2003);
		}

		private static void ExportAll()
		{
			var header = new FullExportLine
			{
				BudgetMinusYear1 = "budgetminusyear1",
				BudgetMinusYear2 = "budgetminusyear2",
				BudgetYear1 = "budgetyear1",
				BudgetYear2 = "budgetyear2",
				BudgetYear3 = "budgetyear3",
				CurrentBudget = "currentbudget",
				Id = "id",
				LineCode = "linecode",
				LineId = "lineid",
				LineLevel = "linelevel",
				LineName = "linename",
				ParentLineId = "parentlineid",
				Year = "year"
			};

			var db = new dbDataContext();
			var lines = db.BudgetLines;
			Mapper.CreateMap<BudgetLine, FullExportLine>();

			var exportlines = (new List<FullExportLine>() { header }).
				Concat(
					lines.Select(_ => Mapper.Map<BudgetLine, FullExportLine>(_))
				);

			foreach (var l in exportlines)
			{
				l.LineName = l.LineName.Kapow();
			}

			FileHelperEngine<FullExportLine> eng = new FileHelperEngine<FullExportLine>();
			//var headerline = new List<BudgetLine> { new BudgetLine{

			//}};
			eng.WriteFile("alllines.csv",
				exportlines
				//headerline.Concat(lines)
				);
		}

		private static IQueryable<BudgetLine> GetTopLevelLines(dbDataContext db)
		{
			return GetTopLevelLines(db, _ => true);
		}

		private static IQueryable<BudgetLine> GetTopLevelLines(dbDataContext db,
			Expression<Func<BudgetLine, bool>> selector)
		{
			var lastlevels = db.BudgetLines.Where(selector).Where(b =>
			b.LineLevel == 5 &&
			b.CurrentBudget > 0 &&

			// these two eliminate 'afdrag på statsgælden' and 'skatter og afgifter' because they involve refinancing and other crap
			b.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineCode != "38" &&
			b.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineCode != "42"
			);
			return lastlevels;
		}

		private static void Output(int year)
		{
			var db = new dbDataContext();

			var lastlevels = GetTopLevelLines(db, _ => _.Year == year);

			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainArea = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				ActivityArea = l.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainAccount = l.BudgetLine1.BudgetLine1.LineName,
				SubAccount = l.BudgetLine1.LineName,
				StandardAccount = l.LineName,
				Ammount = l.CurrentBudget
			}).OrderBy(l => l.Paragraph).
			ThenBy(l => l.MainArea).
			ThenBy(l => l.ActivityArea).
			ThenBy(l => l.MainAccount).
			ThenBy(l => l.SubAccount).
			ThenBy(l => l.StandardAccount);

			var outputlines = lines.Select(l => new OutBudgetLine
			{
				Paragraph = l.Paragraph,
				MainArea = l.MainArea,
				ActivityArea = l.ActivityArea,
				MainAccount = l.MainAccount,
				SubAccount = l.SubAccount,
				StandardAccount = l.StandardAccount,
				Ammount = l.Ammount
			});

			var headerline = new List<OutBudgetLine> { GetHeaderRow() };

			var eng = new FileHelperEngine<OutBudgetLine>();
			eng.WriteFile("budget" + year + ".csv", headerline.Concat(outputlines));
		}
		
		private static void OutputMultipleYears(int year)
		{
			var db = new dbDataContext();

			var lastlevels = GetTopLevelLines(db, _ => _.Year == year);

			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainArea = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				ActivityArea = l.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainAccount = l.BudgetLine1.BudgetLine1.LineName,
				SubAccount = l.BudgetLine1.LineName,
				StandardAccount = l.LineName,
				Y2011 = l.CurrentBudget,
				Y2010 = l.BudgetMinusYear1,
				Y2009 = l.BudgetMinusYear2,
				Y2012 = l.BudgetYear1,
				Y2013 = l.BudgetYear2,
				Y2014 = l.BudgetYear3,

			}).OrderBy(l => l.Paragraph).
			ThenBy(l => l.MainArea).
			ThenBy(l => l.ActivityArea).
			ThenBy(l => l.MainAccount).
			ThenBy(l => l.SubAccount).
			ThenBy(l => l.StandardAccount);

			var outputlines = lines.Select(l => new OutBudgetLineMultiple
			{
				Paragraph = l.Paragraph,
				MainArea = l.MainArea,
				ActivityArea = l.ActivityArea,
				MainAccount = l.MainAccount,
				SubAccount = l.SubAccount,
				StandardAccount = l.StandardAccount,
				Y2009 = l.Y2009,
				Y2010 = l.Y2010,
				Y2011 = l.Y2011,
				Y2012 = l.Y2012,
				Y2013 = l.Y2013,
				Y2014 = l.Y2014
			});

			var headerline = new List<OutBudgetLineMultiple> { GetHeaderRowMultiple() };

			var eng = new FileHelperEngine<OutBudgetLineMultiple>();
			eng.WriteFile("budgetmultiple" + year + ".csv", headerline.Concat(outputlines));
		}

		private static void OutputAll()
		{
			var db = new dbDataContext();
			var lastlevels = GetTopLevelLines(db);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainArea = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				ActivityArea = l.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainAccount = l.BudgetLine1.BudgetLine1.LineName,
				SubAccount = l.BudgetLine1.LineName,
				StandardAccount = l.LineName,
				Year2010 = l.Year == 2010 ? l.CurrentBudget : 0,
				Year2011 = l.Year == 2011 ? l.CurrentBudget : 0
			});

			var foolines = from l in lines
						   group l by new
						   {
							   l.Paragraph,
							   l.MainArea,
							   l.ActivityArea,
							   l.MainAccount,
							   l.SubAccount,
							   l.StandardAccount
						   }
							   into g
							   select new
							   {
								   g.Key.Paragraph,
								   g.Key.MainArea,
								   g.Key.ActivityArea,
								   g.Key.MainAccount,
								   g.Key.SubAccount,
								   g.Key.StandardAccount,
								   Year2010 = g.Sum(_ => _.Year2010),
								   Year2011 = g.Sum(_ => _.Year2011)
							   };

			//OrderBy(l => l.Paragraph).
			//ThenBy(l => l.MainArea).
			//ThenBy(l => l.ActivityArea).
			//ThenBy(l => l.MainAccount).
			//ThenBy(l => l.SubAccount).
			//ThenBy(l => l.StandardAccount);

			var outputlines = foolines.Select(l => new OutBudgetLineYear
			{
				Paragraph = l.Paragraph,
				MainArea = l.MainArea,
				ActivityArea = l.ActivityArea,
				MainAccount = l.MainAccount,
				SubAccount = l.SubAccount,
				StandardAccount = l.StandardAccount,
				Year2010 = l.Year2010,
				Year2011 = l.Year2011
			});

			var headerline = new List<OutBudgetLineYear> { GetHeaderRowYear() };

			var eng = new FileHelperEngine<OutBudgetLineYear>();
			eng.WriteFile("budget" + "all" + ".csv", headerline.Concat(outputlines));
		}

		private static void OutputLimitedCols(int year)
		{
			var db = new dbDataContext();

			var lastlevels = GetTopLevelLines(db, _ => _.Year == year);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainArea = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				ActivityArea = l.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				Ammount = l.CurrentBudget
			});

			var foolines = from l in lines
						   group l by new { l.Paragraph, l.MainArea, l.ActivityArea }
							   into g
							   select new
							   {
								   g.Key.Paragraph,
								   g.Key.MainArea,
								   g.Key.ActivityArea,
								   Sum = g.Sum(_ => _.Ammount)
							   };

			//GroupBy(_ => new { _.Paragraph, _.MainArea }).
			//Select(_ => new { _.Key.Paragraph, _.Key.MainArea, Sum = _. })
			//OrderBy(l => l.Paragraph).
			//ThenBy(l => l.MainArea);

			var outputlines = foolines.Select(l => new OutBudgetLineLimited
			{
				Paragraph = l.Paragraph,
				MainArea = l.MainArea,
				ActivityArea = l.ActivityArea,
				Ammount = l.Sum
			});

			var headerline = new List<OutBudgetLineLimited> { GetHeaderRowLimited() };

			var eng = new FileHelperEngine<OutBudgetLineLimited>();
			eng.WriteFile("budget" + year + ".csv", headerline.Concat(outputlines));
		}

		private static void OutputLimitedColsAll()
		{
			var db = new dbDataContext();

			var lastlevels = GetTopLevelLines(db);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				MainArea = l.BudgetLine1.BudgetLine1.BudgetLine1.BudgetLine1.LineName,
				Ammount = l.CurrentBudget
			});

			var foolines = from l in lines
						   group l by new { l.Paragraph, l.MainArea }
							   into g
							   select new
							   {
								   g.Key.Paragraph,
								   g.Key.MainArea,
								   Sum = g.Sum(_ => _.Ammount)
							   };

			//GroupBy(_ => new { _.Paragraph, _.MainArea }).
			//Select(_ => new { _.Key.Paragraph, _.Key.MainArea, Sum = _. })
			//OrderBy(l => l.Paragraph).
			//ThenBy(l => l.MainArea);

			var outputlines = foolines.Select(l => new OutBudgetLineLimited
			{
				Paragraph = l.Paragraph,
				MainArea = l.MainArea,
				Ammount = l.Sum
			});

			var headerline = new List<OutBudgetLineLimited> { GetHeaderRowLimited() };

			var eng = new FileHelperEngine<OutBudgetLineLimited>();
			eng.WriteFile("budget" + "all" + ".csv", headerline.Concat(outputlines));
		}

		private static void Parse(int year)
		{
			var lines = Read(year);
			var db = new dbDataContext();
			db.BudgetLines.InsertAllOnSubmit(lines);
			Console.WriteLine("submitting...");
			db.SubmitChanges();
		}

		private static IEnumerable<BudgetLine> Read(int year)
		{
			StreamReader streamReader = new StreamReader("..\\..\\data\\" + year + ".html", Encoding.GetEncoding("ISO-8859-1"));
			string text = streamReader.ReadToEnd();
			streamReader.Close();

			string sepstring = @"<TR ALIGN=""right"" CLASS=""tabcelle""><TD ALIGN=""left"" CLASS=""tabforsp"">";
			string[] sep = new string[] { 
						//@"<TABLE BORDER=""0"" WIDTH=""100%"" CELLSPACING=""1"" CELLPADDING=""2"" BGCOLOR=""#000000"">" 
						sepstring
					};

			var firsttr = text.IndexOf(sepstring);
			var aftertable = text.Substring(firsttr, text.Length - firsttr);
			//text.Split(sep, StringSplitOptions.RemoveEmptyEntries).Skip(1).Aggregate((a, b) => a + sepstring + b);
			var intable = aftertable.Split(new string[] { "</TABLE>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim();

			var perfect = intable.Replace("<TD>", @"</TD><TD>").Replace("\n", @"</TD></TR>");

			perfect = "<table>" + perfect + "</table>";

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(perfect);

			var trs = doc.DocumentNode.ChildNodes[0].ChildNodes;
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
					CurrentBudget = GetAmount(yF),
					BudgetYear1 = GetAmount(yBO1),
					BudgetYear2 = GetAmount(yBO2),
					BudgetYear3 = GetAmount(yBO3),
					BudgetMinusYear1 = GetAmount(yB),
					BudgetMinusYear2 = GetAmount(yR),
					LineLevel = (byte)linelevel,
				};

				if (linelevel > 0)
				{
					line.BudgetLine1 = lineatlevel[linelevel - 1];
				}
				lineatlevel[linelevel] = line;
				yield return line;
			}
		}

		static decimal GetAmount(string s)
		{
			var millions = decimal.Parse(s, CultureInfo.GetCultureInfo("da-dk"));
			return millions * 1000000;
		}

		private static OutBudgetLine GetHeaderRow()
		{
			return new OutBudgetLine
			{
				Paragraph = "Paragraf",
				MainArea = "Hovedområde",
				ActivityArea = "Aktivitetsområde",
				MainAccount = "Hovedkonto",
				SubAccount = "Underkonto",
				StandardAccount = "Standardkonto",
				Ammount = 0
			};
		}

		private static OutBudgetLineMultiple GetHeaderRowMultiple()
		{
			return new OutBudgetLineMultiple
			{
				Paragraph = "Paragraf",
				MainArea = "Hovedområde",
				ActivityArea = "Aktivitetsområde",
				MainAccount = "Hovedkonto",
				SubAccount = "Underkonto",
				StandardAccount = "Standardkonto",
				Y2009 = 2009,
				Y2010 = 2010,
				Y2011 = 2011,
				Y2012 = 2012,
				Y2013 = 2013,
			};
		}

		private static OutBudgetLineLimited GetHeaderRowLimited()
		{
			return new OutBudgetLineLimited
			{
				Paragraph = "Paragraf",
				MainArea = "Hovedområde",
				Ammount = 0
			};
		}

		private static OutBudgetLineYear GetHeaderRowYear()
		{
			return new OutBudgetLineYear
			{
				Paragraph = "Paragraf",
				MainArea = "Hovedområde",
				ActivityArea = "Aktivitetsområde",
				MainAccount = "Hovedkonto",
				SubAccount = "Underkonto",
				StandardAccount = "Standardkonto",
				Year2010 = 0,
				Year2011 = 0
			};
		}
	}

	[DelimitedRecord(";")]
	public class OutBudgetLine
	{
		public String Paragraph { get; set; }
		public String MainArea { get; set; }
		public String ActivityArea { get; set; }
		public String MainAccount { get; set; }
		public String SubAccount { get; set; }
		public String StandardAccount { get; set; }
		public decimal Ammount { get; set; }
	}

	[DelimitedRecord(";")]
	public class OutBudgetLineMultiple
	{
		public String Paragraph { get; set; }
		public String MainArea { get; set; }
		public String ActivityArea { get; set; }
		public String MainAccount { get; set; }
		public String SubAccount { get; set; }
		public String StandardAccount { get; set; }
		public decimal Y2009 { get; set; }
		public decimal Y2010 { get; set; }
		public decimal Y2011 { get; set; }
		public decimal Y2012 { get; set; }
		public decimal Y2013 { get; set; }
		public decimal Y2014 { get; set; }
	}

	[DelimitedRecord(";")]
	public class OutBudgetLineYear
	{
		public String Paragraph { get; set; }
		public String MainArea { get; set; }
		public String ActivityArea { get; set; }
		public String MainAccount { get; set; }
		public String SubAccount { get; set; }
		public String StandardAccount { get; set; }
		public decimal Year2010 { get; set; }
		public decimal Year2011 { get; set; }
	}

	[DelimitedRecord(";")]
	public class OutBudgetLineLimited
	{
		public String Paragraph { get; set; }
		public String MainArea { get; set; }
		public String ActivityArea { get; set; }
		public decimal Ammount { get; set; }
	}

	[DelimitedRecord(",")]
	public class FullExportLine
	{
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string Id;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string LineCode;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string LineId;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string ParentLineId;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string Year;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string CurrentBudget;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string BudgetYear1;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string BudgetYear2;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string BudgetYear3;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string BudgetMinusYear1;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string BudgetMinusYear2;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string LineLevel;
		[FieldQuoted('"', QuoteMode.AlwaysQuoted)]
		public string LineName;
	}

	public static class Extensions
	{
		public static string Kapow(this string s)
		{
			if (s == null)
				return null;
			//return s.Reencode(Encoding.GetEncoding("windows-1252"), Encoding.UTF8);
			return s.Replace("ø", "oe").Replace("å", "aa").Replace("æ", "ae").
				Replace("Ø", "Oe").Replace("Å", "Aa").Replace("Æ", "Ae");
			//return s;
		}

	}
}
