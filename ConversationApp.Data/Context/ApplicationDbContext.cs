using Microsoft.EntityFrameworkCore;
using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Data.Context
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }
        public DbSet<ScheduleMessage> ScheduleMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.CreationDate).IsRequired();
            });

            // Conversation entity configuration
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Title).HasMaxLength(200);
                entity.Property(c => c.CreationDate).IsRequired();
            });

            // Message entity configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Content).IsRequired();
                entity.Property(m => m.SentDate).IsRequired();

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ConversationParticipant entity configuration
            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.HasKey(cp => cp.Id);
                entity.Property(cp => cp.JoinedDate).IsRequired();

                entity.HasOne(cp => cp.User)
                    .WithMany(u => u.ConversationParticipants)
                    .HasForeignKey(cp => cp.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cp => cp.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(cp => cp.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MessageReadReceipt entity configuration
            modelBuilder.Entity<MessageReadReceipt>(entity =>
            {
                entity.HasKey(mrr => mrr.Id);
                entity.Property(mrr => mrr.ReadDate).IsRequired();

                entity.HasOne(mrr => mrr.Message)
                    .WithMany(m => m.ReadReceipts)
                    .HasForeignKey(mrr => mrr.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mrr => mrr.User)
                    .WithMany(u => u.ReadReceipts)
                    .HasForeignKey(mrr => mrr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ScheduleMessage entity configuration
            modelBuilder.Entity<ScheduleMessage>(entity =>
            {
                entity.HasKey(sm => sm.Id);
                entity.Property(sm => sm.MessageText).IsRequired();
                entity.Property(sm => sm.ScheduledTime).IsRequired();
                entity.Property(sm => sm.ScheduledSentTime).IsRequired();
                entity.Property(sm => sm.CreationDate).IsRequired();

                entity.HasOne(sm => sm.TargetUser)
                    .WithMany(u => u.TargetedScheduledMessages)
                    .HasForeignKey(sm => sm.TargetUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(sm => sm.CreatedByAdmin)
                    .WithMany(u => u.CreatedScheduledMessages)
                    .HasForeignKey(sm => sm.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
