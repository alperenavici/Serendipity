using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class ConversationParticipant
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ConversationId { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User User { get; set; }
        public virtual Conversation Conversation { get; set; }


    }
}
