using Microsoft.EntityFrameworkCore;
using FinTrack_Pro.Models;

namespace FinTrack_Pro.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Investment> Investments { get; set; }
        public DbSet<Debt> Debts { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}

