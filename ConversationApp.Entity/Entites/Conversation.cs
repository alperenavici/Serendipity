using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Type { get; set; } // 0: Private, 1: Group
        public DateTime CreationDate { get; set; }

        public virtual ICollection<ConversationParticipant> Participants { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
        public Conversation()
        {
            Participants = new HashSet<ConversationParticipant>();
            Messages = new HashSet<Message>();
        }
    }
}
