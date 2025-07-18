using ConversationApp.Data.Interfaces; 
using ConversationApp.Entity.Entites;
using ConversationApp.Entity.Enums; 
using ConversationApp.Service.Interfaces; 
using Microsoft.Extensions.Logging;

namespace ConversationApp.Service.Services
{
    public class ScheduleMessageService : IScheduleMessageService
    {

        private readonly ILogger _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IScheduleMessageRepository _scheduleMessageRepository;
        
        public ScheduleMessageService(ILogger<ScheduleMessageService> logger, IUnitOfWork unitOfWork, IScheduleMessageRepository scheduleMessageRepository)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _scheduleMessageRepository = scheduleMessageRepository;
        }

        public async Task<ScheduleMessage> CreateScheduleMessageAsync(Guid createdByUserId, Guid? targetUserId, string messageText, DateTime scheduledTime)
        {
            var scheduleMessage = new ScheduleMessage
            {
                CreatedByUserId = createdByUserId,
                Title = "Scheduled Message",
                MessageContent = messageText,
                RunOnceAt = scheduledTime,
                NextRunTime = scheduledTime,
                CreatedOn = DateTime.UtcNow,
                Status = ScheduleStatus.Pending,
                IsActive = true
            };
            if (string.IsNullOrWhiteSpace(messageText))
                throw new ArgumentException("Mesaj içeri boş olamaz.",
                    nameof(messageText));

            if (targetUserId==null)
                throw new ArgumentException("Hedef kullanıcı ID'si boş olamaz.",
                    nameof(targetUserId));
            if(scheduledTime< DateTime.UtcNow)
                throw new ArgumentException("Planlanan zaman geçmişte olamaz.",
                    nameof(scheduledTime));

            if (targetUserId.HasValue)
            {
                scheduleMessage.Targets.Add(new ScheduleMessageTarget
                {
                    TargetUserId = targetUserId.Value
                });
            }

            await _scheduleMessageRepository.AddAsync(scheduleMessage);
            await _unitOfWork.CommitAsync();
            return scheduleMessage;
        }

        public async Task<ScheduleMessage> CreateScheduleMessageWithMultipleTargetsAsync(Guid createdByUserId, List<Guid> targetUserIds, string title, string messageContent, DateTime scheduledTime, string? cronExpression = null)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                throw new ArgumentException("Mesaj içeriği boş olamaz.", nameof(messageContent));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Başlık boş olamaz.", nameof(title));

            if (targetUserIds == null || !targetUserIds.Any())
                throw new ArgumentException("En az bir hedef kullanıcı seçmelisiniz.", nameof(targetUserIds));

            if (scheduledTime < DateTime.UtcNow)
                throw new ArgumentException("Planlanan zaman geçmişte olamaz.", nameof(scheduledTime));

            var scheduleMessage = new ScheduleMessage
            {
                CreatedByUserId = createdByUserId,
                Title = title,
                MessageContent = messageContent,
                NextRunTime = scheduledTime,
                CreatedOn = DateTime.UtcNow,
                Status = ScheduleStatus.Pending,
                IsActive = true
            };

            // Tek seferlik veya tekrarlanan ayarları
            if (string.IsNullOrEmpty(cronExpression))
            {
                scheduleMessage.RunOnceAt = scheduledTime;
            }
            else
            {
                scheduleMessage.CronExpression = cronExpression;
            }

            // Tüm hedef kullanıcıları ekle
            foreach (var targetUserId in targetUserIds)
            {
                scheduleMessage.Targets.Add(new ScheduleMessageTarget
                {
                    TargetUserId = targetUserId
                });
            }

            await _scheduleMessageRepository.AddAsync(scheduleMessage);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Zamanlanmış mesaj oluşturuldu: {MessageId}, Hedef sayısı: {TargetCount}, Başlık: {Title}", 
                scheduleMessage.Id, targetUserIds.Count, title);

            return scheduleMessage;
        }

        public async Task<bool> DisableScheduleMessageAsync(Guid scheduleMessageId)
        {
            try
            {
                var scheduleMessage = await _scheduleMessageRepository.GetScheduledMessageByIdAsync(scheduleMessageId);
                if (scheduleMessage != null)
                {
                    scheduleMessage.IsActive = false;
                    await _unitOfWork.CommitAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj durduruluken hata oluştu. ID: {ScheduleMessageId}", scheduleMessageId);
                return false;
            }
        }

        public async Task<bool> EnableScheduleMessageAsync(Guid scheduleMessageId)
        {
            try
            {
                var scheduleMessage = await _scheduleMessageRepository.GetScheduledMessageByIdAsync(scheduleMessageId);
                if (scheduleMessage != null)
                {
                    scheduleMessage.IsActive = true;
                    await _unitOfWork.CommitAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj aktifleştirilirken hata oluştu. ID: {ScheduleMessageId}", scheduleMessageId);
                return false;
            }
        }

        public async Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync()
        {
            return await _scheduleMessageRepository.GetPendingScheduledMessagesAsync();
        }

        public Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime)
        {
            return _scheduleMessageRepository.GetScheduledMessagesDueAsync(dateTime);
        }

        public Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId)
        {
            return _scheduleMessageRepository.GetScheduledMessagesForUserAsync(targetUserId);
        }

        public Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId)
        {
            return _scheduleMessageRepository.GetUserScheduledMessagesAsync(userId);
        }

        public async Task ProcessDueScheduledMessagesAsync()
        {
            var now = DateTime.UtcNow;

            var dueMessages = await _scheduleMessageRepository.GetScheduledMessagesDueAsync(now);

            foreach (var message in dueMessages)
            {
                try
                {
                    await _scheduleMessageRepository.UpdateScheduleMessageStatusAsync(message.Id, (int)ScheduleStatus.Active);
                    await _unitOfWork.CommitAsync();
                    
                    _logger.LogInformation("Mesaj işlenmek üzere işaretlendi: {MessageId}", message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mesaj durumu güncellenirken hata oluştu: {MessageId}", message.Id);
                }
            }
        }

        public async Task<bool> UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status)
        {
            try
            {
                await _scheduleMessageRepository.UpdateScheduleMessageStatusAsync(scheduleMessageId, status);
                await _unitOfWork.CommitAsync();
                return true;
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj durumu güncellenirken hata oluştu. Mesaj ID: {ScheduleMessageId}, Durum: {Status}", scheduleMessageId, status);
                return false;
            }
        }
    }
}