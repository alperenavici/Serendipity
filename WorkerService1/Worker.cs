using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Enums;
using ConversationApp.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace WorkerService1
{
    public class ScheduleMessageWorker : BackgroundService
    {
        private readonly ILogger<ScheduleMessageWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly int _checkIntervalInSeconds;

        public ScheduleMessageWorker(ILogger<ScheduleMessageWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _checkIntervalInSeconds = _configuration.GetValue<int>("WorkerSettings:CheckIntervalInSeconds", 30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Schedule Message Worker başlatıldı. Kontrol aralığı: {Interval} saniye", _checkIntervalInSeconds);

            await PerformHealthCheckAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledMessagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Zamanlanmış mesajlar işlenirken genel hata oluştu");
                }

                await Task.Delay(TimeSpan.FromSeconds(_checkIntervalInSeconds), stoppingToken);
            }
            
            _logger.LogInformation("Schedule Message Worker durduruldu");
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduleMessageRepository = scope.ServiceProvider.GetRequiredService<IScheduleMessageRepository>();
                
                var activeMessages = await scheduleMessageRepository.GetEnabledScheduledMessagesAsync();
                _logger.LogInformation("✅ Sağlık kontrolü başarılı - Database'de {Count} aktif mesaj bulundu", activeMessages.Count);
                
                Console.WriteLine("=====================================");
                Console.WriteLine("🚀SCHEDULE MESSAGE WORKER BAŞLATILDI");
                Console.WriteLine($"⏱Kontrol Aralığı: {_checkIntervalInSeconds} saniye");
                Console.WriteLine($"📊 Aktif Mesaj Sayısı: {activeMessages.Count}");
                Console.WriteLine($"🕐Başlatılma Zamanı: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Sağlık kontrolü başarısız - Database bağlantısı sorunu olabilir");
                Console.WriteLine("❌ WORKER BAŞLATMA HATASI: Database bağlantısı kurulamadı");
                throw;
            }
        }

        private async Task ProcessScheduledMessagesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduleMessageService = scope.ServiceProvider.GetRequiredService<IScheduleMessageService>();
            var scheduleMessageRepository = scope.ServiceProvider.GetRequiredService<IScheduleMessageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                var now = DateTime.UtcNow;
                TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                DateTime turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(now, turkeyTimeZone);
                _logger.LogDebug("⏰ Zamanlanmış mesajlar kontrol ediliyor UTC: {TimeUtc} | Türkiye Saati: {TimeTurkey}", now, turkeyTime);

                var allActiveMessages = await scheduleMessageRepository.GetEnabledScheduledMessagesAsync();
                _logger.LogInformation("Database'de toplam {Count} aktif zamanlanmış mesaj bulundu", allActiveMessages.Count);

                if (allActiveMessages.Any())
                {
                    foreach (var msg in allActiveMessages)
                    {
                        var nextRunTimeTurkey = TimeZoneInfo.ConvertTimeFromUtc(msg.NextRunTime, turkeyTimeZone);
                        _logger.LogDebug("Mesaj ID: {MessageId}, Başlık: {Title}, NextRunTime UTC: {NextRunTime} | TR: {NextRunTimeTurkey}, Status: {Status}, IsActive: {IsActive}", 
                            msg.Id, msg.Title, msg.NextRunTime, nextRunTimeTurkey, msg.Status, msg.IsActive);
                    }
                }

                var dueMessages = await scheduleMessageService.GetScheduledMessagesDueAsync(now);
                _logger.LogInformation("Şu anda çalışması gereken mesaj sayısı: {Count} (Zaman: {CurrentTime})", dueMessages.Count, now);

                if (dueMessages.Any())
                {
                    _logger.LogInformation("🎯 {Count} adet zamanlanmış mesaj işlenecek", dueMessages.Count);

                    foreach (var message in dueMessages)
                    {
                        try
                        {
                            _logger.LogInformation("Mesaj işleniyor: {MessageId} - {Title}", message.Id, message.Title);

                            await scheduleMessageService.UpdateScheduleMessageStatusAsync(message.Id, (int)ScheduleStatus.Active);

                            bool allMessagesSent = true;
                            foreach (var target in message.Targets)
                            {
                                try
                                {
                                    _logger.LogDebug("Mesaj gönderiliyor - Kullanıcı: {UserId}, İçerik: {Content}", 
                                        target.TargetUserId, message.MessageContent);

                                    await SendMessageToUserAsync(target.TargetUserId, message.MessageContent, message.Title, message.CreatedByUserId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Kullanıcıya mesaj gönderilirken hata: {UserId}", target.TargetUserId);
                                    allMessagesSent = false;
                                }
                            }

                            if (!string.IsNullOrEmpty(message.CronExpression))
                            {
                                var nextRunTime = CalculateNextRunTime(message.CronExpression, now);
                                if (nextRunTime.HasValue)
                                {
                                    await scheduleMessageRepository.UpdateNextRunTimeAsync(message.Id, nextRunTime.Value);
                                    await unitOfWork.CommitAsync();
                                    
                                    var newStatus = allMessagesSent ? (int)ScheduleStatus.Pending : (int)ScheduleStatus.Failed;
                                    await scheduleMessageService.UpdateScheduleMessageStatusAsync(message.Id, newStatus);
                                    await unitOfWork.CommitAsync();
                                    
                                    _logger.LogInformation("Tekrarlanan mesaj için sonraki çalışma zamanı: {NextTime}, Durum: {Status}", 
                                        nextRunTime.Value, newStatus == (int)ScheduleStatus.Pending ? "Pending" : "Failed");
                                }
                                else
                                {
                                    var finalStatus = allMessagesSent ? (int)ScheduleStatus.Completed : (int)ScheduleStatus.Failed;
                                    await scheduleMessageService.UpdateScheduleMessageStatusAsync(message.Id, finalStatus);
                                    await unitOfWork.CommitAsync();
                                    
                                    _logger.LogInformation("Mesaj tamamlandı (son çalışma): {MessageId}, Durum: {Status}", 
                                        message.Id, finalStatus == (int)ScheduleStatus.Completed ? "Completed" : "Failed");
                                }
                            }
                            else
                            {
                                var finalStatus = allMessagesSent ? (int)ScheduleStatus.Completed : (int)ScheduleStatus.Failed;
                                await scheduleMessageService.UpdateScheduleMessageStatusAsync(message.Id, finalStatus);
                                await unitOfWork.CommitAsync();
                                
                                _logger.LogInformation("Tek seferlik mesaj tamamlandı: {MessageId}, Durum: {Status}", 
                                    message.Id, finalStatus == (int)ScheduleStatus.Completed ? "Completed" : "Failed");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Mesaj işlenirken hata oluştu: {MessageId}", message.Id);
                            await scheduleMessageService.UpdateScheduleMessageStatusAsync(message.Id, (int)ScheduleStatus.Failed);
                            await unitOfWork.CommitAsync();
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("İşlenecek zamanlanmış mesaj bulunamadı. Kontrol zamanı: {Time}", now);
                    
                    var pendingMessages = await scheduleMessageRepository.GetPendingScheduledMessagesAsync();
                    _logger.LogDebug("Pending durumunda {Count} mesaj var", pendingMessages.Count);
                    
                    if (pendingMessages.Any())
                    {
                        foreach (var pendingMsg in pendingMessages)
                        {
                                            var pendingNextRunTimeTurkey = TimeZoneInfo.ConvertTimeFromUtc(pendingMsg.NextRunTime, turkeyTimeZone);
                _logger.LogDebug("Pending Mesaj: {MessageId}, NextRunTime UTC: {NextRunTime} | TR: {NextRunTimeTurkey}, Şimdiki Zaman UTC: {Now} | TR: {NowTurkey}, Fark: {Diff} dakika",
                    pendingMsg.Id, pendingMsg.NextRunTime, pendingNextRunTimeTurkey, now, turkeyTime, (pendingMsg.NextRunTime - now).TotalMinutes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessScheduledMessagesAsync metodunda genel hata");
            }
        }

        private async Task SendMessageToUserAsync(Guid userId, string messageContent, string title, Guid createdByUserId)
        {
            _logger.LogInformation("Mesaj gönderiliyor - UserId: {UserId}, Title: {Title}, Content: {Content}", 
                userId, title, messageContent);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var targetUser = await userService.GetUserByIdAsync(userId);
                if (targetUser == null)
                {
                    _logger.LogError("Hedef kullanıcı bulunamadı: {UserId}", userId);
                    throw new Exception($"Hedef kullanıcı bulunamadı: {userId}");
                }

                _logger.LogDebug("Hedef kullanıcı bulundu: {UserName} ({Email})", targetUser.UserName, targetUser.Email);

                try
                {
                    var sentMessage = await messageService.SendUserMessageAsync(createdByUserId, userId, title, messageContent);
                    
                    _logger.LogInformation("✅ Mesaj database'e kaydedildi - Message ID: {MessageId}", sentMessage.Id);

                    Console.WriteLine("=====================================");
                    Console.WriteLine($"REAL MESSAGE SENT To: {targetUser.UserName} ({targetUser.Email})");
                    Console.WriteLine($"{sentMessage.Id}");
                    Console.WriteLine($"{title}");
                    Console.WriteLine($"{messageContent}");
                    Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    Console.WriteLine($"{userId}");
                    Console.WriteLine($"{targetUser.UserName}");
                    Console.WriteLine("=====================================");

                    _logger.LogInformation("✅ Zamanlanmış mesaj başarıyla gönderildi ve kaydedildi - Kullanıcı: {UserName} ({Email}), Başlık: {Title}, MessageId: {MessageId}", 
                        targetUser.UserName, targetUser.Email, title, sentMessage.Id);
                        
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database'e mesaj kaydedilirken hata oluştu - UserId: {UserId}", userId);
                    
                    // Fallback: En azından console'a yazdır
                    Console.WriteLine("❌ Database hatası - Fallback mesaj:");
                    Console.WriteLine($"To: {targetUser.UserName}, Title: {title}, Content: {messageContent}");
                    
                    throw;
                }

                // Başarılı gönderim simülasyonu
                await Task.Delay(200);
                
                _logger.LogInformation("✅ Mesaj gönderme işlemi tamamlandı - UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Mesaj gönderilirken kritik hata oluştu - UserId: {UserId}", userId);
                throw; // Hatayı yukarı fırlat ki Failed olarak işaretlensin
            }
        }

        private DateTime? CalculateNextRunTime(string cronExpression, DateTime currentTime)
        {
            try
            {
                var crontab = CrontabSchedule.Parse(cronExpression);
                var nextOccurrence = crontab.GetNextOccurrence(currentTime);
                return nextOccurrence;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cron ifadesi parse edilemedi: {CronExpression}", cronExpression);
                return null;
            }
        }
    }
}
