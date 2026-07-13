using Microsoft.EntityFrameworkCore;
using RenoApi.Domain;

namespace RenoApi.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();
    public DbSet<TaskState> TaskStates => Set<TaskState>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeLine> RecipeLines => Set<RecipeLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Username).IsUnique();
        // 1 nhân viên chỉ có 1 bản ghi mỗi ca (sáng/chiều/tối) mỗi ngày
        b.Entity<WorkSession>().HasIndex(s => new { s.UserId, s.Date, s.Shift }).IsUnique();
        b.Entity<TaskState>().HasIndex(t => new { t.WorkSessionId, t.TaskTemplateId }).IsUnique();
        b.Entity<WorkSession>().HasMany(s => s.States).WithOne(t => t.Session)
            .HasForeignKey(t => t.WorkSessionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Recipe>().HasMany(r => r.Lines).WithOne(l => l.Recipe)
            .HasForeignKey(l => l.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}
