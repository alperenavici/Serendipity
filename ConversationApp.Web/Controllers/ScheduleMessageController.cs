using ConversationApp.Entity.Entites;
using ConversationApp.Entity.Enums;
using ConversationApp.Service.Interfaces;
using ConversationApp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ConversationApp.Web.Controllers
{
    [Authorize]
    public class ScheduleMessageController : Controller
    {
        private readonly IScheduleMessageService _scheduleMessageService;
        private readonly IUserService _userService;
        private readonly ILogger<ScheduleMessageController> _logger;

        public ScheduleMessageController(IScheduleMessageService scheduleMessageService, IUserService userService, ILogger<ScheduleMessageController> logger)
        {
            _scheduleMessageService = scheduleMessageService;
            _userService = userService;
            _logger = logger;
        }

        // Ana sayfa - kullanıcının zamanlanmış mesajları
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var scheduleMessages = await _scheduleMessageService.GetUserScheduledMessagesAsync(userId);

                var viewModel = new ScheduleMessageListViewModel
                {
                    ScheduleMessages = scheduleMessages.Select(sm => new ScheduleMessageViewModel
                    {
                        Id = sm.Id,
                        Title = sm.Title,
                        MessageContent = sm.MessageContent,
                        CronExpression = sm.CronExpression,
                        RunOnceAt = sm.RunOnceAt,
                        NextRunTime = sm.NextRunTime,
                        LastRunTime = sm.LastRunTime,
                        Status = sm.Status,
                        IsActive = sm.IsActive,
                        CreatedOn = sm.CreatedOn,
                        TargetCount = sm.Targets.Count
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesajlar listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Mesajlar yüklenirken hata oluştu";
                return View(new ScheduleMessageListViewModel { ScheduleMessages = new List<ScheduleMessageViewModel>() });
            }
        }

        // Yeni zamanlanmış mesaj oluşturma formu
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                // Türkiye saatinde 2 dakika sonrasını default olarak ayarla
                TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                DateTime turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
                DateTime defaultRunTime = turkeyNow.AddMinutes(2);

                var viewModel = new CreateScheduleMessageViewModel
                {
                    AvailableUsers = users.Select(u => new UserSelectViewModel
                    {
                        Id = u.Id,
                        DisplayName = $"{u.UserName} ({u.Email})"
                    }).ToList(),
                    SelectedUserIds = new List<Guid>(),
                    ScheduleType = ScheduleType.Once,
                    RunOnceAt = defaultRunTime
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj oluşturma sayfası yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "Sayfa yüklenirken hata oluştu";
                return RedirectToAction(nameof(Index));
            }
        }

        // Yeni zamanlanmış mesaj oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateScheduleMessageViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Hata durumunda kullanıcı listesini tekrar yükle
                    var users = await _userService.GetAllUsersAsync();
                    model.AvailableUsers = users.Select(u => new UserSelectViewModel
                    {
                        Id = u.Id,
                        DisplayName = $"{u.UserName} ({u.Email})"
                    }).ToList();
                    return View(model);
                }

                var userId = GetCurrentUserId();

                DateTime scheduledTime;
                string? cronExpression = null;

                if (model.ScheduleType == ScheduleType.Once)
                {
                    if (model.RunOnceAt.HasValue)
                    {
                        // DateTime-local input'tan gelen değer local time'dır, UTC'ye convert edelim
                        scheduledTime = DateTime.SpecifyKind(model.RunOnceAt.Value, DateTimeKind.Local).ToUniversalTime();
                    }
                    else
                    {
                        scheduledTime = DateTime.UtcNow.AddMinutes(2);
                    }
                }
                else if (model.ScheduleType == ScheduleType.Recurring)
                {
                    cronExpression = model.CronExpression;
                    scheduledTime = CalculateNextRunTime(model.CronExpression) ?? DateTime.UtcNow.AddMinutes(2);
                }
                else
                {
                    scheduledTime = DateTime.UtcNow.AddMinutes(2);
                }

                // Zamanlanmış mesajı birden fazla hedefle oluştur
                await _scheduleMessageService.CreateScheduleMessageWithMultipleTargetsAsync(
                    userId, 
                    model.SelectedUserIds, 
                    model.Title, 
                    model.MessageContent, 
                    scheduledTime, 
                    cronExpression);

                TempData["SuccessMessage"] = "Zamanlanmış mesaj başarıyla oluşturuldu";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Mesaj oluşturulurken hata oluştu";

                // Hata durumunda kullanıcı listesini tekrar yükle
                var users = await _userService.GetAllUsersAsync();
                model.AvailableUsers = users.Select(u => new UserSelectViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.UserName} ({u.Email})"
                }).ToList();
                return View(model);
            }
        }

        // Zamanlanmış mesaj detayları
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var scheduleMessages = await _scheduleMessageService.GetUserScheduledMessagesAsync(userId);
                var scheduleMessage = scheduleMessages.FirstOrDefault(sm => sm.Id == id);

                if (scheduleMessage == null)
                {
                    TempData["ErrorMessage"] = "Zamanlanmış mesaj bulunamadı";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new ScheduleMessageDetailsViewModel
                {
                    Id = scheduleMessage.Id,
                    Title = scheduleMessage.Title,
                    MessageContent = scheduleMessage.MessageContent,
                    CronExpression = scheduleMessage.CronExpression,
                    RunOnceAt = scheduleMessage.RunOnceAt,
                    NextRunTime = scheduleMessage.NextRunTime,
                    LastRunTime = scheduleMessage.LastRunTime,
                    Status = scheduleMessage.Status,
                    IsActive = scheduleMessage.IsActive,
                    CreatedOn = scheduleMessage.CreatedOn,
                    Targets = scheduleMessage.Targets.Select(t => new ScheduleMessageTargetViewModel
                    {
                        TargetUserId = t.TargetUserId,
                        TargetUserName = t.TargetUser?.UserName,
                        TargetUserEmail = t.TargetUser?.Email
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj detayı yüklenirken hata oluştu. ID: {Id}", id);
                TempData["ErrorMessage"] = "Mesaj detayı yüklenirken hata oluştu";
                return RedirectToAction(nameof(Index));
            }
        }

        // Zamanlanmış mesajı durdur/aktifleştir
        [HttpPost]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var scheduleMessages = await _scheduleMessageService.GetUserScheduledMessagesAsync(userId);
                var scheduleMessage = scheduleMessages.FirstOrDefault(sm => sm.Id == id);

                if (scheduleMessage == null)
                {
                    return Json(new { success = false, message = "Mesaj bulunamadı" });
                }

                bool newStatus = !scheduleMessage.IsActive;
                var result = newStatus
                    ? await _scheduleMessageService.EnableScheduleMessageAsync(id)
                    : await _scheduleMessageService.DisableScheduleMessageAsync(id);

                if (result)
                {
                    return Json(new { success = true, message = newStatus ? "Mesaj aktifleştirildi" : "Mesaj durduruldu" });
                }
                else
                {
                    return Json(new { success = false, message = "İşlem başarısız" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj durumu değiştirilirken hata oluştu. ID: {Id}", id);
                return Json(new { success = false, message = "Hata oluştu" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı");
        }

        private DateTime? CalculateNextRunTime(string cronExpression)
        {
            try
            {
                // NCronTab kullanarak sonraki çalışma zamanını hesapla
                var crontab = NCrontab.CrontabSchedule.Parse(cronExpression);
                return crontab.GetNextOccurrence(DateTime.UtcNow);
            }
            catch
            {
                return null;
            }
        }
    }

    // Enum for schedule types
    public enum ScheduleType
    {
        Once = 1,
        Recurring = 2
    }
} 