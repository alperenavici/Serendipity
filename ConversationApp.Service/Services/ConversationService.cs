using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using Conversation.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Service.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConversationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ConversationListItemViewModel>> GetUserConversationsAsync(Guid userId, string searchQuery = null)
        {
            List<ConversationApp.Entity.Entites.Conversation> conversations;

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                conversations = await _unitOfWork.Conversations.SearchConversationsAsync(userId, searchQuery);
            }
            else
            {
                conversations = await _unitOfWork.Conversations.GetUserConversationsWithMessagesAsync(userId);
            }

            var conversationList = new List<ConversationListItemViewModel>();

            foreach (var conversation in conversations)
            {
                var lastMessage = conversation.Messages?.OrderByDescending(m => m.SentDate).FirstOrDefault();
                var otherParticipants = await _unitOfWork.ConversationParticipants.GetOtherParticipantsAsync(conversation.Id, userId);

                string conversationName;
                string avatarUrl;

                if (conversation.Type == 0) 
                {
                    var otherUser = otherParticipants.FirstOrDefault();
                    conversationName = otherUser?.UserName ?? "Bilinmeyen Kullanıcı";
                    avatarUrl = $"https://i.pravatar.cc/150?u={otherUser?.UserName ?? "unknown"}";
                }
                else
                {
                    conversationName = conversation.Title ?? "Grup Sohbeti";
                    avatarUrl = "https://i.pravatar.cc/150?u=group";
                }

                var unreadCount = await _unitOfWork.Messages.GetUnreadMessageCountAsync(conversation.Id, userId);

                conversationList.Add(new ConversationListItemViewModel
                {
                    Id = conversation.Id,
                    Name = conversationName,
                    LastMessage = lastMessage?.Content ?? "Henüz mesaj yok",
                    LastMessageTime = lastMessage?.SentDate.ToString("HH:mm") ?? "",
                    UnreadCount = unreadCount,
                    AvatarUrl = avatarUrl,
                    IsActive = false
                });
            }

            return conversationList.OrderByDescending(c => c.LastMessageTime).ToList();
        }

        public async Task<ConversationViewModel> GetConversationViewModelAsync(Guid userId, Guid? conversationId, string searchQuery = null)
        {
            var conversationList = await GetUserConversationsAsync(userId, searchQuery);

            if (!conversationId.HasValue && conversationList.Any())
            {
                conversationId = conversationList.First().Id;
            }

            var activeConversation = conversationList.FirstOrDefault(c => c.Id == conversationId);
            if (activeConversation != null)
            {
                activeConversation.IsActive = true;
            }

            var currentUser = await _unitOfWork.Users.GetByIdAsync(userId);
            var currentUserProfile = new UserProfileViewModel
            {
                UserName = currentUser.UserName,
                Role = currentUser.Role == 1 ? "Admin" : "Developer",
                AvatarUrl = $"https://i.pravatar.cc/150?u={currentUser.UserName}"
            };

            var messages = new List<MessageViewModel>();
            ActiveChatUserViewModel activeChatUser = null;

            if (conversationId.HasValue)
            {
                messages = await GetConversationMessagesAsync(conversationId.Value, userId);

                var conversation = await _unitOfWork.Conversations.GetConversationWithParticipantsAsync(conversationId.Value);
                if (conversation?.Type == 0) 
                {
                    var otherParticipants = await _unitOfWork.ConversationParticipants.GetOtherParticipantsAsync(conversationId.Value, userId);
                    var otherUser = otherParticipants.FirstOrDefault();

                    if (otherUser != null)
                    {
                        activeChatUser = new ActiveChatUserViewModel
                        {
                            Name = otherUser.UserName,
                            Role = otherUser.Role == 1 ? "Admin" : "Developer",
                            AvatarUrl = $"https://i.pravatar.cc/150?u={otherUser.UserName}"
                        };
                    }
                }
                else 
                {
                    activeChatUser = new ActiveChatUserViewModel
                    {
                        Name = conversation?.Title ?? "Grup Sohbeti",
                        Role = "Grup",
                        AvatarUrl = "https://i.pravatar.cc/150?u=group"
                    };
                }
            }

            return new ConversationViewModel
            {
                Title = activeConversation?.Name ?? "Sohbet",
                CurrentUser = currentUserProfile,
                ConversationList = conversationList,
                ActiveChatUser = activeChatUser,
                Messages = messages
            };
        }

        public async Task<ConversationApp.Entity.Entites.Conversation> CreatePrivateConversationAsync(Guid currentUserId, Guid targetUserId, string initialMessage)
        {
            var existingConversation = await _unitOfWork.Conversations.GetPrivateConversationBetweenUsersAsync(currentUserId, targetUserId);
            if (existingConversation != null)
            {
                await SendMessageAsync(existingConversation.Id, currentUserId, initialMessage);
                return existingConversation;
            }

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId);

            var conversation = new ConversationApp.Entity.Entites.Conversation
            {
                Id = Guid.NewGuid(),
                Title = $"{currentUser.UserName} - {targetUser.UserName}",
                Type = 0, // Private
                CreationDate = DateTime.UtcNow
            };

            _unitOfWork.Conversations.Add(conversation);

            var participants = new[]
            {
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = currentUserId,
                    JoinedDate = DateTime.UtcNow
                },
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = targetUserId,
                    JoinedDate = DateTime.UtcNow
                }
            };

            _unitOfWork.ConversationParticipants.AddRange(participants);

            await SendMessageAsync(conversation.Id, currentUserId, initialMessage);

            await _unitOfWork.SaveChangesAsync();

            return conversation;
        }

        public async Task<ConversationApp.Entity.Entites.Conversation> GetOrCreatePrivateConversationAsync(Guid user1Id, Guid user2Id)
        {
            var existingConversation = await _unitOfWork.Conversations.GetPrivateConversationBetweenUsersAsync(user1Id, user2Id);
            if (existingConversation != null)
            {
                return existingConversation;
            }

            var user1 = await _unitOfWork.Users.GetByIdAsync(user1Id);
            var user2 = await _unitOfWork.Users.GetByIdAsync(user2Id);

            var conversation = new ConversationApp.Entity.Entites.Conversation
            {
                Id = Guid.NewGuid(),
                Title = $"{user1.UserName} - {user2.UserName}",
                Type = 0, // Private
                CreationDate = DateTime.UtcNow
            };

            _unitOfWork.Conversations.Add(conversation);

            var participants = new[]
            {
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = user1Id,
                    JoinedDate = DateTime.UtcNow
                },
                new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = user2Id,
                    JoinedDate = DateTime.UtcNow
                }
            };

            _unitOfWork.ConversationParticipants.AddRange(participants);
            await _unitOfWork.SaveChangesAsync();

            return conversation;
        }

        public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _unitOfWork.ConversationParticipants.IsUserParticipantAsync(conversationId, userId);
        }

        public async Task<List<User>> GetConversationParticipantsAsync(Guid conversationId, Guid excludeUserId)
        {
            return await _unitOfWork.ConversationParticipants.GetOtherParticipantsAsync(conversationId, excludeUserId);
        }

        public async Task<Message> SendMessageAsync(Guid conversationId, Guid senderId, string content)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = senderId,
                Content = content,
                SentDate = DateTime.UtcNow
            };

            _unitOfWork.Messages.Add(message);
            await _unitOfWork.SaveChangesAsync();

            return message;
        }

        public async Task<List<MessageViewModel>> GetConversationMessagesAsync(Guid conversationId, Guid currentUserId)
        {
            var messages = await _unitOfWork.Messages.GetConversationMessagesAsync(conversationId);

            return messages.Select(m => new MessageViewModel
            {
                SenderName = m.Sender.UserName,
                Content = m.Content,
                SentDate = m.SentDate,
                IsOutgoing = m.UserId == currentUserId
            }).ToList();
        }
    }
} 