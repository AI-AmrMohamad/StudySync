using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudySync.Models;

namespace StudySync.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<SwapBooking> SwapBookings { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<FocusRoom> FocusRooms { get; set; }
        public DbSet<FocusSession> FocusSessions { get; set; }
        public DbSet<HelpBounty> HelpBounties { get; set; }
        public DbSet<CommunityChannel> CommunityChannels { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserSkill: composite unique index on UserId + SkillId
            builder.Entity<UserSkill>()
                .HasIndex(us => new { us.UserId, us.SkillId })
                .IsUnique();

            builder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSkill>()
                .HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // SwapBooking: Restrict delete to prevent circular cascade paths
            builder.Entity<HelpBounty>()
                .HasOne(hb => hb.Requester)
                .WithMany()
                .HasForeignKey(hb => hb.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Channel)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.CommunityChannelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<ChatMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.ApplicationUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SwapBooking>()
                .HasOne(b => b.Requester)
                .WithMany(u => u.RequestedBookings)
                .HasForeignKey(b => b.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SwapBooking>()
                .HasOne(b => b.Provider)
                .WithMany(u => u.ProvidedBookings)
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SwapBooking>()
                .HasOne(b => b.Skill)
                .WithMany(s => s.SwapBookings)
                .HasForeignKey(b => b.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            // FocusSession relationships
            builder.Entity<FocusSession>()
                .HasOne(fs => fs.User)
                .WithMany(u => u.FocusSessions)
                .HasForeignKey(fs => fs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FocusSession>()
                .HasOne(fs => fs.Room)
                .WithMany(r => r.FocusSessions)
                .HasForeignKey(fs => fs.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // CreditTransaction relationships
            builder.Entity<CreditTransaction>()
                .HasOne(ct => ct.User)
                .WithMany(u => u.CreditTransactions)
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed skills for the marketplace
            builder.Entity<Skill>().HasData(
                new Skill { Id = 1, Name = "Python Programming", Category = "Programming" },
                new Skill { Id = 2, Name = "Calculus", Category = "Mathematics" },
                new Skill { Id = 3, Name = "Essay Writing", Category = "Writing" },
                new Skill { Id = 4, Name = "Data Structures", Category = "Programming" },
                new Skill { Id = 5, Name = "Graphic Design", Category = "Design" },
                new Skill { Id = 6, Name = "Statistics", Category = "Mathematics" },
                new Skill { Id = 7, Name = "Public Speaking", Category = "Communication" },
                new Skill { Id = 8, Name = "Web Development", Category = "Programming" }
            );
        }
    }
}
