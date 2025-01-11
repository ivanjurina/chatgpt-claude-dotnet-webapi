using Microsoft.EntityFrameworkCore;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using BCrypt.Net;

namespace chatgpt_claude_dotnet_webapi.DataModel
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Document> Documents { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            // Configure Chat
            modelBuilder.Entity<Chat>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId);

                modelBuilder.Entity<Document>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Chat)
            .WithMany()
            .HasForeignKey(d => d.ChatId)
            .OnDelete(DeleteBehavior.SetNull);

            // Configure Message
            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);
          
            // Seed admin user
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "admin",
                IsAdmin = true,
                IsActive = true
            });
        }
    }
}