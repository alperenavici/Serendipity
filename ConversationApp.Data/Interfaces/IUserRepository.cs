using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUsernameAsync(string username);
        Task<List<User>> SearchUsersAsync(string searchTerm, Guid? excludeUserId = null);
        Task<List<User>> GetActiveUsersAsync();
        Task<List<User>> GetUsersExceptAsync(Guid userId);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<List<User>> GetUsersByRoleAsync(int role);
        // Admin Dashboard Ýstatistikleri
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetNewUsersCountAsync(int days);
        Task<int> GetActiveUsersCountAsync();
        Task<List<int>> GetMonthlyUserRegistrationsAsync();
    }
} 