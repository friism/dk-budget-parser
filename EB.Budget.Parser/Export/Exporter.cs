using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using EB.Budget.Model;
using EB.Budget.Parser.DataLoad;
using EB.Budget.Persistence;
using FileHelpers;

namespace EB.Budget.Parser.Export
{
	public class Exporter
	{
		private static void OutputByCategory()
		{
			var foo = new Dictionary<string, Dictionary<int, decimal>>();

			var years = Enumerable.Range(2003, 10);

			var dataLoader = new DataLoader();

			foreach (var year in years)
			{
				var lines = dataLoader.Read(year).Where(x => x.LineLevel == 0);
				foreach (var line in lines)
				{
					if (!foo.ContainsKey(line.LineName))
					{
						foo[line.LineName] = new Dictionary<int, decimal>();
					}
					foo[line.LineName][year] = line.CurrentYearBudget;
				}
			}

			var outputlines = foo.Keys.Select(x => new CategoryBudgetLine
			{
				Name = x,
				Y2003 = foo[x].ContainsKey(2003) ? foo[x][2003] : 0,
				Y2004 = foo[x].ContainsKey(2004) ? foo[x][2004] : 0,
				Y2005 = foo[x].ContainsKey(2005) ? foo[x][2005] : 0,
				Y2006 = foo[x].ContainsKey(2006) ? foo[x][2006] : 0,
				Y2007 = foo[x].ContainsKey(2007) ? foo[x][2007] : 0,
				Y2008 = foo[x].ContainsKey(2008) ? foo[x][2008] : 0,
				Y2009 = foo[x].ContainsKey(2009) ? foo[x][2009] : 0,
				Y2010 = foo[x].ContainsKey(2010) ? foo[x][2010] : 0,
				Y2011 = foo[x].ContainsKey(2011) ? foo[x][2011] : 0,
				Y2012 = foo[x].ContainsKey(2012) ? foo[x][2012] : 0,
			});

			var eng = new FileHelperEngine<CategoryBudgetLine>();
			eng.WriteFile("cats.csv", outputlines);

			//Console.WriteLine(lines.Count());
			//foreach (var m in foo.Keys)
			//{
			//    Console.WriteLine(m);
			//}
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

			var db = new Context();
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

		private static IEnumerable<BudgetLine> GetTopLevelLines(Context db)
		{
			return GetTopLevelLines(db, _ => true);
		}

		private static IEnumerable<BudgetLine> GetTopLevelLines(IEnumerable<BudgetLine> lines)
		{
			var lastlevels = lines.Where(b =>
				b.LineLevel == 5 &&
				b.CurrentYearBudget > 0 &&
					// these two eliminate 'afdrag på statsgælden' and 'skatter og afgifter' because they involve refinancing and other crap
				b.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineCode != "38" &&
				b.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineCode != "42"
			);
			return lastlevels;
		}

		private static IEnumerable<BudgetLine> GetTopLevelLines(Context db,
			Expression<Func<BudgetLine, bool>> selector)
		{
			return GetTopLevelLines(db.BudgetLines.Where(selector));
		}

		private static void Output(int year)
		{
			var alllines = new DataLoader().Read(year);

			var lastlevels = GetTopLevelLines(alllines);

			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				ActivityArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainAccount = l.ParentBudgetLine.ParentBudgetLine.LineName,
				SubAccount = l.ParentBudgetLine.LineName,
				StandardAccount = l.LineName,
				Ammount = l.CurrentYearBudget
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
			var db = new Context();

			var lastlevels = GetTopLevelLines(db, _ => _.Year == year);

			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				ActivityArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainAccount = l.ParentBudgetLine.ParentBudgetLine.LineName,
				SubAccount = l.ParentBudgetLine.LineName,
				StandardAccount = l.LineName,
				Y2011 = l.CurrentYearBudget,
				Y2010 = l.PreviousYear1Budget,
				Y2009 = l.PreviousYear2Budget,
				Y2012 = l.Year1Budget,
				Y2013 = l.Year2Budget,
				Y2014 = l.Year3Budget,

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
			var db = new Context();
			var lastlevels = GetTopLevelLines(db);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				ActivityArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainAccount = l.ParentBudgetLine.ParentBudgetLine.LineName,
				SubAccount = l.ParentBudgetLine.LineName,
				StandardAccount = l.LineName,
				Year2010 = l.Year == 2010 ? l.CurrentYearBudget : 0,
				Year2011 = l.Year == 2011 ? l.CurrentYearBudget : 0
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
			var db = new Context();

			var lastlevels = GetTopLevelLines(db, _ => _.Year == year);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				ActivityArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				Ammount = l.CurrentYearBudget
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
			var db = new Context();

			var lastlevels = GetTopLevelLines(db);
			var lines = lastlevels.Select(l => new
			{
				Paragraph = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				MainArea = l.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.ParentBudgetLine.LineName,
				Ammount = l.CurrentYearBudget
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

	[DelimitedRecord(";")]
	public class CategoryBudgetLine
	{
		public String Name { get; set; }
		public decimal Y2003 { get; set; }
		public decimal Y2004 { get; set; }
		public decimal Y2005 { get; set; }
		public decimal Y2006 { get; set; }
		public decimal Y2007 { get; set; }
		public decimal Y2008 { get; set; }
		public decimal Y2009 { get; set; }
		public decimal Y2010 { get; set; }
		public decimal Y2011 { get; set; }
		public decimal Y2012 { get; set; }
	}

	public static class Extensions
	{
		public static string Kapow(this string s)
		{
			if (s == null)
				return null;
			//return s.Reencode(Encoding.GetEncoding("windows-1252"), Encoding.UTF8);
			if (s.Contains("Genudlån"))
			{
				s = "Genudlån";
			}
			return s.Replace("ø", "oe").Replace("å", "aa").Replace("æ", "ae").
				Replace("Ø", "Oe").Replace("Å", "Aa").Replace("Æ", "Ae");
		}
	}
}
