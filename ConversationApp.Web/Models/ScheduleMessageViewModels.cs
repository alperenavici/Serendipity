using ConversationApp.Entity.Enums;
using ConversationApp.Web.Controllers;
using System.ComponentModel.DataAnnotations;

namespace ConversationApp.Web.Models
{
    // Ana liste için ViewModel
    public class ScheduleMessageListViewModel
    {
        public List<ScheduleMessageViewModel> ScheduleMessages { get; set; } = new List<ScheduleMessageViewModel>();
    }

    // Liste item için ViewModel
    public class ScheduleMessageViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public string? CronExpression { get; set; }
        public DateTime? RunOnceAt { get; set; }
        public DateTime NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public ScheduleStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int TargetCount { get; set; }

        public string StatusText => Status switch
        {
            ScheduleStatus.Pending => "Beklemede",
            ScheduleStatus.Active => "Çalışıyor",
            ScheduleStatus.Completed => "Tamamlandı",
            ScheduleStatus.Paused => "Duraklatıldı",
            ScheduleStatus.Failed => "Başarısız",
            _ => "Bilinmiyor"
        };

        public string StatusCssClass => Status switch
        {
            ScheduleStatus.Pending => "badge bg-warning",
            ScheduleStatus.Active => "badge bg-primary",
            ScheduleStatus.Completed => "badge bg-success",
            ScheduleStatus.Paused => "badge bg-secondary",
            ScheduleStatus.Failed => "badge bg-danger",
            _ => "badge bg-light"
        };
    }

    // Yeni mesaj oluşturma için ViewModel
    public class CreateScheduleMessageViewModel
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj içeriği gereklidir")]
        [StringLength(1000, ErrorMessage = "Mesaj içeriği en fazla 1000 karakter olabilir")]
        public string MessageContent { get; set; } = string.Empty;

        [Required(ErrorMessage = "Zamanlama türü seçmelisiniz")]
        public ScheduleType ScheduleType { get; set; } = ScheduleType.Once;

        [RequiredIf("ScheduleType", ScheduleType.Once, ErrorMessage = "Tek seferlik mesajlar için tarih seçmelisiniz")]
        public DateTime? RunOnceAt { get; set; }

        [RequiredIf("ScheduleType", ScheduleType.Recurring, ErrorMessage = "Tekrarlanan mesajlar için cron ifadesi gereklidir")]
        public string? CronExpression { get; set; }

        [Required(ErrorMessage = "En az bir hedef kullanıcı seçmelisiniz")]
        public List<Guid> SelectedUserIds { get; set; } = new List<Guid>();

        public List<UserSelectViewModel> AvailableUsers { get; set; } = new List<UserSelectViewModel>();
    }

    // Kullanıcı seçim için ViewModel
    public class UserSelectViewModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    // Detay sayfası için ViewModel
    public class ScheduleMessageDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public string? CronExpression { get; set; }
        public DateTime? RunOnceAt { get; set; }
        public DateTime NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public ScheduleStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<ScheduleMessageTargetViewModel> Targets { get; set; } = new List<ScheduleMessageTargetViewModel>();

        public string StatusText => Status switch
        {
            ScheduleStatus.Pending => "Beklemede",
            ScheduleStatus.Active => "Çalışıyor",
            ScheduleStatus.Completed => "Tamamlandı",
            ScheduleStatus.Paused => "Duraklatıldı",
            ScheduleStatus.Failed => "Başarısız",
            _ => "Bilinmiyor"
        };

        public string ScheduleTypeText => string.IsNullOrEmpty(CronExpression) ? "Tek Seferlik" : "Tekrarlanan";
    }

    // Hedef kullanıcı bilgisi için ViewModel
    public class ScheduleMessageTargetViewModel
    {
        public Guid TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        public string? TargetUserEmail { get; set; }
    }
}

// RequiredIf özel validation attribute'u
public class RequiredIfAttribute : ValidationAttribute
{
    private readonly string _propertyName;
    private readonly object _expectedValue;

    public RequiredIfAttribute(string propertyName, object expectedValue)
    {
        _propertyName = propertyName;
        _expectedValue = expectedValue;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var instance = validationContext.ObjectInstance;
        var type = instance.GetType();
        var propertyValue = type.GetProperty(_propertyName)?.GetValue(instance);

        if (Equals(propertyValue, _expectedValue))
        {
            if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
} 