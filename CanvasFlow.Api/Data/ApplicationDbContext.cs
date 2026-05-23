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

        public DbSet<User> Users { get; set; }

        public DbSet<Content> Contents { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Content>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Content>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Contents)
                .UsingEntity("ContentTags");

            modelBuilder.Entity<Message>()
                .HasOne<User>() // явно вказуЇмо тип User зам≥сть m => m.Sender
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.RecipientId });
        }
    }
}