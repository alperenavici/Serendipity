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
        Task<User> GetUserByIdAsync(Guid userId);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<List<User>> GetUsersByRoleAsync(int role);

       
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetNewUsersCountAsync(int days = 30);
        Task<int> GetActiveUsersCountAsync();
        Task<List<int>> GetMonthlyUserRegistrationsAsync();
        Task<double> GetUserGrowthPercentageAsync(int days = 30);
    }
} 