namespace EB.Budget.Model
{
	public class BudgetLine : Entity
	{
		public string LineCode { get; set; }
		public BudgetLine ParentBudgetLine { get; set; }
		public int Year { get; set; }
		public decimal CurrentYearBudget { get; set; }
		public decimal Year1Budget { get; set; }
		public decimal Year2Budget { get; set; }
		public decimal Year3Budget { get; set; }
		public decimal PreviousYear1Budget { get; set; }
		public decimal PreviousYear2Budget { get; set; }
		public byte LineLevel { get; set; }
		public string LineName { get; set; }
	}
}
