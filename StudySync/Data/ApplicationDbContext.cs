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
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionEnrollment> SessionEnrollments { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<CommunityChannel> CommunityChannels { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<TutorSession> TutorSessions { get; set; }
        public DbSet<SessionEnrollment> SessionEnrollments { get; set; }

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

            // Chat message relationships
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

            // Session: host relationship
            builder.Entity<Session>()
                .HasOne(s => s.Host)
                .WithMany(u => u.HostedSessions)
                .HasForeignKey(s => s.HostId)
                .OnDelete(DeleteBehavior.Cascade);

            // SessionEnrollment: attendee relationship
            builder.Entity<SessionEnrollment>()
                .HasOne(e => e.Session)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SessionEnrollment>()
                .HasOne(e => e.Attendee)
                .WithMany(u => u.SessionEnrollments)
                .HasForeignKey(e => e.AttendeeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Prevent duplicate enrollments
            builder.Entity<SessionEnrollment>()
                .HasIndex(e => new { e.SessionId, e.AttendeeId })
                .IsUnique();

            // CreditTransaction relationships
            builder.Entity<CreditTransaction>()
                .HasOne(ct => ct.User)
                .WithMany(u => u.CreditTransactions)
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Cascade);

<<<<<<< HEAD
            // TutorSession relationships
            builder.Entity<TutorSession>()
                .HasOne(ts => ts.Tutor)
                .WithMany(u => u.TutorSessions)
                .HasForeignKey(ts => ts.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TutorSession>()
                .HasOne(ts => ts.Skill)
                .WithMany()
                .HasForeignKey(ts => ts.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            // SessionEnrollment relationships
            builder.Entity<SessionEnrollment>()
                .HasOne(se => se.TutorSession)
                .WithMany(ts => ts.Enrollments)
                .HasForeignKey(se => se.TutorSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SessionEnrollment>()
                .HasOne(se => se.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent double enrollment
            builder.Entity<SessionEnrollment>()
                .HasIndex(se => new { se.TutorSessionId, se.StudentId })
                .IsUnique();

            // Seed skills for the marketplace
=======
            // Seed skills
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5
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
