using Microsoft.EntityFrameworkCore;
using CanvasFlow.Api.Models;

namespace CanvasFlow.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet for User (Module 1)
        public DbSet<User> Users { get; set; }

        // DbSet for Content (Module 2)
        public DbSet<Content> Contents { get; set; }

        // DbSet for Messages (Module 1)
        public DbSet<Message> Messages { get; set; }
        
        // DbSet for Tags (Module 2)
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- User Configuration ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            // --- Content Configuration ---
            modelBuilder.Entity<Content>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict); 
            
            // --- Tag Configuration (Many-to-Many) ---
            modelBuilder.Entity<Content>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Contents)
                .UsingEntity(joinEntityName: "ContentTags"); // Optional: Name the join table

            // --- Message Configuration ---
            modelBuilder.Entity<Message>()
                .HasOne(m => new Message { Id = m.Id }) 
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}