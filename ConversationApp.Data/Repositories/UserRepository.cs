using ConversationApp.Data.Context;
using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, Guid? excludeUserId = null)
        {
            var query = _context.Users
                .Where(u => !u.IsDeleted && 
                           (u.UserName.Contains(searchTerm) || 
                            u.Email.Contains(searchTerm)));

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query
                .OrderBy(u => u.UserName)
                .Take(10)
                .ToListAsync();
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsDeleted && !u.IsBanned)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersExceptAsync(Guid userId)
        {
            return await _context.Users
                .Where(u => u.Id != userId && !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _context.Users
                .AnyAsync(u => u.UserName == username);
        }

        public async Task<List<User>> GetUsersByRoleAsync(int role)
        {
            return await _context.Users
                .Where(u => u.Role == role && !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        // Admin Dashboard Ýstatistikleri
        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users
                .CountAsync(u => !u.IsDeleted);
        }

        public async Task<int> GetNewUsersCountAsync(int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Users
                .CountAsync(u => !u.IsDeleted && u.CreationDate >= startDate);
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            var timeThreshold = DateTime.UtcNow.AddMinutes(-5);

            return await _context.Users
                .CountAsync(u => !u.IsDeleted &&
                                 u.LastActiveDate.HasValue &&
                                 u.LastActiveDate.Value > timeThreshold);
        }

        public async Task<List<int>> GetMonthlyUserRegistrationsAsync()
        {
            var currentYear = DateTime.UtcNow.Year;
            var monthlyRegistrations = new List<int>();

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(currentYear, month, 1);
                var endDate = startDate.AddMonths(1);

                var count = await _context.Users
                    .CountAsync(u => !u.IsDeleted &&
                               u.CreationDate >= startDate &&
                               u.CreationDate < endDate);

                monthlyRegistrations.Add(count);
            }

            return monthlyRegistrations;
        }
    }
} 