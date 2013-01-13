using System.Data.Entity;
using EB.Budget.Model;

namespace EB.Budget.Persistence
{
	public class Context : DbContext
	{
		public DbSet<BudgetLine> BudgetLines { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context, Configuration>());
		}
	}
}
