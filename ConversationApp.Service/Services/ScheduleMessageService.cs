//using Azure.Messaging;
//using ConversationApp.Data.Interfaces;
//using ConversationApp.Entity.Entites;
//using ConversationApp.Service.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace ConversationApp.Service.Services
//{
//    public class ScheduleMessageService : IScheduleMessageService
//    {
//        private readonly IUnitOfWork _unitOfWork;

//        public ScheduleMessageService(IUnitOfWork unitOfWork)
//        {
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<ScheduleMessage> CreateScheduleMessageAsync(Guid createdByUserId, Guid? targetUserId, string MessageContent, DateTime NextRunTime
//            )
//        {
//            var scheduleMessage = new ScheduleMessage
//            {
//                Id = Guid.NewGuid(),
//                MessageContent = MessageContent,
//                NextRunTime = NextRunTime.ToString("yyyy-MM-dd HH:mm:ss"),
//                TargetUserId = targetUserId,
//                CreatedByUserId = createdByUserId,
//                IsEnabled = true,
//                ScheduledSentTime = scheduledTime,
//                Status = 0, // Pending
//                CreationDate = DateTime.UtcNow
//            };

//            _unitOfWork.ScheduleMessages.Add(scheduleMessage);
//            await _unitOfWork.SaveChangesAsync();

//            return scheduleMessage;
//        }

//        public async Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId)
//        {
//            return await _unitOfWork.ScheduleMessages.GetUserScheduledMessagesAsync(userId);
//        }

//        public async Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId)
//        {
//            return await _unitOfWork.ScheduleMessages.GetScheduledMessagesForUserAsync(targetUserId);
//        }

//        public async Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync()
//        {
//            return await _unitOfWork.ScheduleMessages.GetPendingScheduledMessagesAsync();
//        }

//        public async Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime)
//        {
//            return await _unitOfWork.ScheduleMessages.GetScheduledMessagesDueAsync(dateTime);
//        }

//        public async Task ProcessDueScheduledMessagesAsync()
//        {
//            var dueMessages = await GetScheduledMessagesDueAsync(DateTime.UtcNow);
            
//            foreach (var scheduleMessage in dueMessages)
//            {
//                try
//                {
                    
//                    await UpdateScheduleMessageStatusAsync(scheduleMessage.Id, 1); // 1 = Sent
//                }
//                catch (Exception)
//                {
                    
//                    await UpdateScheduleMessageStatusAsync(scheduleMessage.Id, 2); // 2 = Failed
//                }
//            }
//        }

//        public async Task<bool> UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status)
//        {
//            try
//            {
//                await _unitOfWork.ScheduleMessages.UpdateScheduleMessageStatusAsync(scheduleMessageId, status);
//                await _unitOfWork.SaveChangesAsync();
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public async Task<bool> DisableScheduleMessageAsync(Guid scheduleMessageId)
//        {
//            try
//            {
//                var scheduleMessage = await _unitOfWork.ScheduleMessages.GetScheduledMessageByIdAsync(scheduleMessageId);
//                if (scheduleMessage != null)
//                {
//                    scheduleMessage.IsEnabled = false;
//                    _unitOfWork.ScheduleMessages.Update(scheduleMessage);
//                    await _unitOfWork.SaveChangesAsync();
//                    return true;
//                }
//                return false;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public async Task<bool> EnableScheduleMessageAsync(Guid scheduleMessageId)
//        {
//            try
//            {
//                var scheduleMessage = await _unitOfWork.ScheduleMessages.GetScheduledMessageByIdAsync(scheduleMessageId);
//                if (scheduleMessage != null)
//                {
//                    scheduleMessage.IsEnabled = true;
//                    _unitOfWork.ScheduleMessages.Update(scheduleMessage);
//                    await _unitOfWork.SaveChangesAsync();
//                    return true;
//                }
//                return false;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//    }
//} 