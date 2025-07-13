using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class ScheduleMessage
    {
        public Guid Id { get; set; }
        public string MessageText { get; set; }
        public string ScheduledTime { get; set; } 
        public Guid? TargetUserId { get; set; } 
        public Guid CreatedByUserId { get; set; }
        public bool IsEnabled { get; set; } 

        public DateTime ScheduledSentTime {get; set; }
        public int Status { get; set; } 
        public DateTime? SentTime { get; set; }
        public DateTime CreationDate { get; set; }

        public virtual User TargetUser { get; set; } 
        public virtual User CreatedByAdmin { get; set; } 
    }
}
