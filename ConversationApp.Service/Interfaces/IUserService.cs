using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> SearchUsersAsync(string searchTerm, Guid? excludeUserId = null);
        Task<List<User>> GetActiveUsersExceptAsync(Guid excludeUserId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<List<User>> GetUsersByRoleAsync(int role);
    }
} 