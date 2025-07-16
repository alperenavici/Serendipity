using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conversation.Core.DTOs
{
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public int Role { get; set; } // 0: User, 1: Admin 2:3 sUPERADMİN


    }
}
