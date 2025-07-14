using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ConversationApp.Entity.Entites
{
    public class User : IdentityUser<Guid>
    {
        public int Role { get; set; } // 0: User, 1: Admin
        public DateTime CreationDate { get; set; }
        public bool IsBanned { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }

        public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<MessageReadReceipt> ReadReceipts { get; set; }
        public virtual ICollection<ScheduleMessage> CreatedScheduledMessages { get; set; }
        public virtual ICollection<ScheduleMessage> TargetedScheduledMessages { get; set; }

        public User()
        {
            ConversationParticipants = new HashSet<ConversationParticipant>();
            SentMessages = new HashSet<Message>();
            ReadReceipts = new HashSet<MessageReadReceipt>();
            CreatedScheduledMessages = new HashSet<ScheduleMessage>();
            TargetedScheduledMessages = new HashSet<ScheduleMessage>();
        }
    }
}