using Microsoft.EntityFrameworkCore;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Constants;

namespace BlazorApp1.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserDetails> UserDetails { get; set; }
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<BoardMessage> BoardMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasColumnType("nvarchar(50)")
                    .HasDefaultValue(UserRoles.Client);
            });

            modelBuilder.Entity<UserDetails>(entity =>
            {
                entity.ToTable("UserDetails");
                entity.HasKey(e => e.UserId);
                
                entity.Property(e => e.Gender).IsRequired(false);
                entity.Property(e => e.Nationality).IsRequired(false);
                entity.Property(e => e.DateOfBirth).IsRequired(false);
                entity.Property(e => e.PhotoFileName).IsRequired(false);
                entity.Property(e => e.PhotoUrl).IsRequired(false);
                entity.Property(e => e.PhotoContentType).IsRequired(false);

                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserDetails)
                    .HasForeignKey<UserDetails>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BoardMessage>(entity =>
            {
                entity.ToTable("BoardMessages");
                entity.HasKey(e => e.MessageId);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnType("nvarchar(1000)");

                entity.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnType("datetime2");

                entity.Property(e => e.Priority)
                    .IsRequired()
                    .HasColumnType("int");

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 