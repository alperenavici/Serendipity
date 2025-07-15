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
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MessageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        public async Task<List<MessageViewModel>> GetConversationMessagesPagedAsync(Guid conversationId, Guid currentUserId, int page, int pageSize)
        {
            var messages = await _unitOfWork.Messages.GetConversationMessagesPagedAsync(conversationId, page, pageSize);

            return messages.Select(m => new MessageViewModel
            {
                SenderName = m.Sender.UserName,
                Content = m.Content,
                SentDate = m.SentDate,
                IsOutgoing = m.UserId == currentUserId
            }).ToList();
        }

        public async Task<Message> GetLastMessageInConversationAsync(Guid conversationId)
        {
            return await _unitOfWork.Messages.GetLastMessageInConversationAsync(conversationId);
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId)
        {
            return await _unitOfWork.Messages.GetUnreadMessageCountAsync(conversationId, userId);
        }

        public async Task MarkConversationAsReadAsync(Guid conversationId, Guid userId)
        {
            await _unitOfWork.MessageReadReceipts.MarkConversationMessagesAsReadAsync(conversationId, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<Message>> SearchMessagesInConversationAsync(Guid conversationId, string searchTerm)
        {
            return await _unitOfWork.Messages.SearchMessagesInConversationAsync(conversationId, searchTerm);
        }
    }
} 