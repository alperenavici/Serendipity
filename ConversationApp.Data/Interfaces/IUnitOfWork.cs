using System;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }
        IConversationParticipantRepository ConversationParticipants { get; }
        IUserRepository Users { get; }
        IMessageReadReceiptRepository MessageReadReceipts { get; }
        IScheduleMessageRepository ScheduleMessages { get; }

        // Transaction methods
        Task<int> SaveChangesAsync();
        int SaveChanges();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
} 