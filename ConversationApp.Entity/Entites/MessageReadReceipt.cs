using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationApp.Entity.Entites
{
    public class MessageReadReceipt
    {
        public Guid Id { get; set; } 
        public Guid MessageId { get; set; } 
        public Guid UserId { get; set; } 
        public DateTime ReadDate { get; set; } 

        public virtual Message Message { get; set; } 
        public virtual User User { get; set; }
    }
}


