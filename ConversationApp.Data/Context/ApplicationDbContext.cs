using Microsoft.EntityFrameworkCore;
using ConversationApp.Entity.Entites;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
        { }


        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }
        public DbSet<ScheduleMessage> ScheduleMessages { get; set; }
        public DbSet<ScheduleMessageTarget> ScheduleMessageTargets { get; set; }
        public DbSet<User> User { get; set; } // IdentityDbContext already includes a Users DbSet, but you can define it explicitly if needed.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.CreationDate).IsRequired();
            });

           
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Title).HasMaxLength(200);
                entity.Property(c => c.CreationDate).IsRequired();
            });

 
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

           
            modelBuilder.Entity<ScheduleMessage>(entity =>
            {
                entity.HasKey(sm => sm.Id);
                entity.Property(sm => sm.Title).IsRequired().HasMaxLength(200);
                entity.Property(sm => sm.MessageContent).IsRequired();
                entity.Property(sm => sm.CreatedOn).IsRequired();
                entity.Property(sm => sm.NextRunTime).IsRequired();
                entity.Property(sm => sm.Status).IsRequired();

                

                entity.HasOne(sm => sm.CreatedByUser)
                    .WithMany(u => u.CreatedScheduledMessages)
                    .HasForeignKey(sm => sm.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScheduleMessageTarget>(entity =>
            {
                entity.HasKey(st => new {st.ScheduleMessageId, st.TargetUserId});
                entity.HasOne(st=>st.ScheduleMessage)
                    .WithMany(sm => sm.Targets)
                    .HasForeignKey(st => st.ScheduleMessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(st => st.TargetUser)
                    .WithMany(u => u.TargetedScheduledMessages)
                    .HasForeignKey(st => st.TargetUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
