using ConversationApp.Data.Context;
using ConversationApp.Data.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace ConversationApp.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;

        // Repository fields
        private IConversationRepository _conversations;
        private IMessageRepository _messages;
        private IConversationParticipantRepository _conversationParticipants;
        private IUserRepository _users;
        private IMessageReadReceiptRepository _messageReadReceipts;
        private IScheduleMessageRepository _scheduleMessages;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Repository properties with lazy initialization
        public IConversationRepository Conversations => 
            _conversations ??= new ConversationRepository(_context);

        public IMessageRepository Messages => 
            _messages ??= new MessageRepository(_context);

        public IConversationParticipantRepository ConversationParticipants => 
            _conversationParticipants ??= new ConversationParticipantRepository(_context);

        public IUserRepository Users => 
            _users ??= new UserRepository(_context);

        public IMessageReadReceiptRepository MessageReadReceipts => 
            _messageReadReceipts ??= new MessageReadReceiptRepository(_context);

        public IScheduleMessageRepository ScheduleMessages => 
            _scheduleMessages ??= new ScheduleMessageRepository(_context);

        // Transaction methods
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task CommitAsync()
        {
          await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
} 