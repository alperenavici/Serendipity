using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Interfaces
{
    public interface IScheduleMessageService
    {
        Task<ScheduleMessage> CreateScheduleMessageAsync(Guid createdByUserId, Guid? targetUserId, string messageText, DateTime scheduledTime);
        Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId);
        Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId);
        Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync();
        Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime);
        Task ProcessDueScheduledMessagesAsync();
        Task<bool> UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status);
        Task<bool> DisableScheduleMessageAsync(Guid scheduleMessageId);
        Task<bool> EnableScheduleMessageAsync(Guid scheduleMessageId);
    }
} 