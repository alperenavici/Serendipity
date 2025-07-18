using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class ScheduleMessageTarget
    {
        public Guid ScheduleMessageId { get; set; }
        public Guid TargetUserId { get; set; }

        public virtual ScheduleMessage ScheduleMessage { get; set; }
        public virtual User TargetUser { get; set; }
    }
}
