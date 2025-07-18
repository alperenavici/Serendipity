using ConversationApp.Data.Context;
using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using ConversationApp.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConversationApp.Data.Repositories
{
    public class ScheduleMessageRepository : GenericRepository<ScheduleMessage>, IScheduleMessageRepository
    {
        public ScheduleMessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Kullanıcının oluşturduğu tüm zamanlanmış mesajları getirir
        public async Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId)
        {
            return await _context.ScheduleMessages
                                 .Where(sm => sm.CreatedByUserId == userId)
                                 .Include(sm => sm.Targets) // Hedefleri de dahil et
                                 .ToListAsync();
        }

        // Belirli bir kullanıcının hedef alındığı tüm zamanlanmış mesajları getirir
        public async Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId)
        {
            return await _context.ScheduleMessages
                                 .Include(sm => sm.Targets)
                                 .Where(sm => sm.Targets.Any(t => t.TargetUserId == targetUserId))
                                 .ToListAsync();
        }

        // Durumu beklemede olan (Pending) mesajları getirir
        // Bu metot, worker tarafından gönderilmeye hazır mesajları almak için kullanılabilir.
        public async Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync()
        {
            return await _context.ScheduleMessages
                                 .Where(sm => sm.Status == ScheduleStatus.Pending && sm.IsActive)
                                 .Include(sm => sm.Targets)
                                    .ThenInclude(t => t.TargetUser) // Hedef kullanıcı detaylarını da isterseniz
                                 .ToListAsync();
        }

        // Belirli bir zamana kadar gönderilmesi gereken aktif mesajları getirir
        // Worker'ın temel sorgu metodu budur.
        public async Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime)
        {
            // NextRunTime'ı belirtilen zamandan küçük veya eşit olan, aktif ve henız tamamlanmamış/başarısız olmamış mesajları getir.
            // Failed olanları tekrar denemek için dahil ediyoruz.
            return await _context.ScheduleMessages
                                 .Include(sm => sm.Targets)
                                    .ThenInclude(t => t.TargetUser) // Hedef kullanıcı detaylarını da isterseniz
                                 .Where(sm => sm.IsActive &&
                                             (sm.Status == ScheduleStatus.Pending || sm.Status == ScheduleStatus.Failed) &&
                                             sm.NextRunTime <= dateTime)
                                 .ToListAsync();
        }

        // Aktif olan tüm zamanlanmış mesajları getirir
        public async Task<List<ScheduleMessage>> GetEnabledScheduledMessagesAsync()
        {
            return await _context.ScheduleMessages
                                 .Where(sm => sm.IsActive)
                                 .Include(sm => sm.Targets)
                                 .ToListAsync();
        }

        // Belirli bir Id'ye sahip zamanlanmış mesajı getirir
        public async Task<ScheduleMessage> GetScheduledMessageByIdAsync(Guid scheduleMessageId)
        {
            return await _context.ScheduleMessages
                                 .Include(sm => sm.Targets)
                                    .ThenInclude(t => t.TargetUser)
                                 .FirstOrDefaultAsync(sm => sm.Id == scheduleMessageId);
        }

        // Zamanlanış mesajın durumunu günceller
        public async Task UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status)
        {
            var message = await _context.ScheduleMessages.FindAsync(scheduleMessageId);
            if (message != null)
            {
                // Enum'u int'e çevirmek yerine doğrudan atama yapıyoruz
                // Çünkü Status property'niz ScheduleStatus enum türünde.
                message.Status = (ScheduleStatus)status;
                // Update metodunu çağırıp save yapmamız gerekecek.
                // Eğer UnitOfWork kullanıyorsanız, buradaki SaveChanges yerine CommitAsync'i bekleyebilir.
                // Şimdilik sadece property'i güncelledik, kaydedilmesi gerekecek.
                _context.ScheduleMessages.Update(message);
            }
        }

        // Zamanlanış mesajın sonraki çalışma zamanını günceller
        public async Task UpdateNextRunTimeAsync(Guid scheduleMessageId, DateTime nextRunTime)
        {
            var message = await _context.ScheduleMessages.FindAsync(scheduleMessageId);
            if (message != null)
            {
                message.NextRunTime = nextRunTime;
                message.LastRunTime = DateTime.UtcNow;
                _context.ScheduleMessages.Update(message);
            }
        }
    }
}