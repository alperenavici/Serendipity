using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IScheduleMessageRepository : IGenericRepository<ScheduleMessage>
    {
        Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId);
        Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId);
        Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync();
        Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime);
        Task<List<ScheduleMessage>> GetEnabledScheduledMessagesAsync();
        Task<ScheduleMessage> GetScheduledMessageByIdAsync(Guid scheduleMessageId);
        Task UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status);
    }
} 