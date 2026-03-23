using Microsoft.EntityFrameworkCore;
using task_manager.Models;                    
using BCrypt.Net;
using System.Threading.Tasks;

namespace task_manager.Database;

public class TaskManagerDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=taskmanager.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Author)
            .WithMany(u => u.AuthoredTasks)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public async Task InitializeAsync()  
    {
        await Database.EnsureCreatedAsync();

        if (!await Users.AnyAsync())      
        {
            var admin = new User
            {
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = Role.Admin
            };

            var manager = new User
            {
                Login = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager"),
                Role = Role.Manager
            };

            var user = new User
            {
                Login = "user",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user"),
                Role = Role.User
            };

            Users.Add(admin);
            Users.Add(manager);
            Users.Add(user);
            await SaveChangesAsync();
        }
    }
}