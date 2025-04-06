using Microsoft.EntityFrameworkCore;
using BlazorApp1.Shared.Models;
using Microsoft.Data.SqlClient;
using BlazorApp1.Shared.Constants;
using BlazorApp1.Server.Utilities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BlazorApp1.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Collections.Generic;
using BlazorApp1.Server.Models;
using BlazorApp1.Client.Models;

namespace BlazorApp1.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserDetails> UserDetails { get; set; }
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<SubmissionTracking> SubmissionTrackings { get; set; }
        public DbSet<BoardMessage> BoardMessages { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }
        public DbSet<SubmissionEntity> Submissions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<CubicMeterReading> CubicMeterReadings { get; set; }
        public DbSet<EventPlan> EventPlans { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<CubicMeterPriceSetting> CubicMeterPriceSettings { get; set; }
        public DbSet<WaterMeterReading> WaterMeterReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Users table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Client");

                entity.Property(e => e.IsEmailVerified)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.EmailVerificationToken)
                    .HasMaxLength(100);

                // Configure relationships
                entity.HasOne(u => u.UserDetails)
                    .WithOne(ud => ud.User)
                    .HasForeignKey<UserDetails>(ud => ud.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.MessageReads)
                    .WithOne(mr => mr.User)
                    .HasForeignKey(mr => mr.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure UserDetails
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
                entity.Property(e => e.FullName).IsRequired(false);
                entity.Property(e => e.Address).IsRequired(false);
                entity.Property(e => e.MaritalStatus).IsRequired(false);
                entity.Property(e => e.ContactNumber)
                    .HasColumnType("nvarchar(11)")
                    .HasMaxLength(11)
                    .IsRequired(false);
            });

            // Configure BoardMessage
            modelBuilder.Entity<BoardMessage>(entity =>
            {
                entity.ToTable("BoardMessages");
                entity.HasKey(e => e.MessageId);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                entity.Property(e => e.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.Priority)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                // Configure relationships
                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(m => m.MessageReads)
                    .WithOne(mr => mr.Message)
                    .HasForeignKey(mr => mr.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure MessageRead
            modelBuilder.Entity<MessageRead>(entity =>
            {
                entity.ToTable("MessageReads");
                entity.HasKey(mr => new { mr.MessageId, mr.UserId });

                entity.Property(e => e.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.HasOne(mr => mr.User)
                    .WithMany(u => u.MessageReads)
                    .HasForeignKey(mr => mr.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(mr => mr.Message)
                    .WithMany(m => m.MessageReads)
                    .HasForeignKey(mr => mr.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.ReferenceId)
                    .HasMaxLength(100);
            });

            // Configure Bills table
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.ToTable("Bills");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200).IsRequired();
                
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure EventPlan
            modelBuilder.Entity<EventPlan>(entity =>
            {
                entity.ToTable("EventPlans");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .IsRequired();

                entity.Property(e => e.ImageUrl)
                    .IsRequired();

                entity.Property(e => e.EventDate)
                    .IsRequired();

                entity.Property(e => e.ExpiryDate)
                    .IsRequired();

                entity.Property(e => e.CreatedDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.CreatedBy)
                    .IsRequired();

                entity.Property(e => e.UserRole)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Location)
                    .HasMaxLength(200);

                entity.Property(e => e.EventType)
                    .HasMaxLength(50);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.ImageFileName)
                    .HasMaxLength(255);

                entity.Property(e => e.ImageContentType)
                    .HasMaxLength(100);

                // Add index for faster querying
                entity.HasIndex(e => e.EventDate);
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Configure CubicMeterPriceSetting
            modelBuilder.Entity<CubicMeterPriceSetting>(entity =>
            {
                entity.ToTable("CubicMeterPriceSettings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PricePerCubicMeter).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LastUpdated).IsRequired();
                
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure WaterMeterReading
            modelBuilder.Entity<WaterMeterReading>(entity =>
            {
                entity.ToTable("WaterMeterReadings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reading).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ReadingDate).IsRequired();
                
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                {
                    throw new Exception("A record with this information already exists.");
                }
                throw;
            }
        }
    }
}
