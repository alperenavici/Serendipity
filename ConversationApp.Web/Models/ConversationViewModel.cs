// Models/ConversationViewModel.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace ConversationApp.Web.Models
{
    public class MessageViewModel
    {
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentDate { get; set; }
        public bool IsOutgoing { get; set; }
    }

    public class ConversationListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LastMessage { get; set; }
        public string LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserProfileViewModel
    {
        public string UserName { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class ActiveChatUserViewModel
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class ConversationViewModel
    {
        public string Title { get; set; }
        public List<MessageViewModel> Messages { get; set; }
        public UserProfileViewModel CurrentUser { get; set; }
        public List<ConversationListItemViewModel> ConversationList { get; set; }
        public ActiveChatUserViewModel ActiveChatUser { get; set; }

        public ConversationViewModel()
        {
            Messages = new List<MessageViewModel>();
            ConversationList = new List<ConversationListItemViewModel>();
        }
    }
}
