using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class Message
    {
        public Guid Id { get; set; }
        public int ConversationId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; }
        public DateTime SentDate{ get; set; }

        public virtual Conversation Conversation { get; set; }
        public virtual User Sender { get; set; }
        public virtual ICollection<MessageReadReceipt> ReadReceipts { get; set; }
        public Message()
        {
            ReadReceipts = new HashSet<MessageReadReceipt>();
        }


    }
}
