using ConversationApp.Entity.Enums;
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
        public string Title { get; set; }
        public string MessageContent { get; set; }
        public string? CronExpression { get; set; } 
        public DateTime? RunOnceAt { get; set; }
        public DateTime NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public ScheduleStatus Status { get; set; } // 0: Pending, 1: Running, 2: Completed, 3: Failed
        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }
        public Guid CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; }
        public virtual ICollection<ScheduleMessageTarget>Targets { get; set; }
        public ScheduleMessage()
        {
            Targets = new HashSet<ScheduleMessageTarget>();
            CreatedOn = DateTime.UtcNow;
            Status = ScheduleStatus.Pending;
            IsActive = true;
        }
    }
}
