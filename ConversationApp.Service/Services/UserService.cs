using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public UserService(IUnitOfWork unitOfWork,IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, Guid? excludeUserId = null)
        {
            return await _unitOfWork.Users.SearchUsersAsync(searchTerm, excludeUserId);
        }

        public async Task<List<User>> GetActiveUsersExceptAsync(Guid excludeUserId)
        {
            return await _unitOfWork.Users.GetUsersExceptAsync(excludeUserId);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _unitOfWork.Users.GetByUsernameAsync(username);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _unitOfWork.Users.GetByEmailAsync(email);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _unitOfWork.Users.IsEmailExistsAsync(email);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _unitOfWork.Users.IsUsernameExistsAsync(username);
        }

        public async Task<List<User>> GetUsersByRoleAsync(int role)
        {
            return await _unitOfWork.Users.GetUsersByRoleAsync(role);
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _unitOfWork.Users.GetTotalUsersCountAsync();
        }

        public async Task<int> GetNewUsersCountAsync(int days = 30)
        {
            return await _unitOfWork.Users.GetNewUsersCountAsync(days);
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            const string cacheKey = "OnlineUsersCount"; 

            if (_cache.TryGetValue(cacheKey, out int count))
            {
                return count;
            }

            var userCountFromDb = await _unitOfWork.Users.GetActiveUsersCountAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(20));

            _cache.Set(cacheKey, userCountFromDb, cacheEntryOptions);

            return userCountFromDb;


        }

        public async Task<List<int>> GetMonthlyUserRegistrationsAsync()
        {
            return await _unitOfWork.Users.GetMonthlyUserRegistrationsAsync();
        }

        public async Task<double> GetUserGrowthPercentageAsync(int days = 30)
        {
            var currentPeriodCount = await _unitOfWork.Users.GetNewUsersCountAsync(days);
            var previousPeriodCount = await _unitOfWork.Users.GetNewUsersCountAsync(days * 2) - currentPeriodCount;

            if (previousPeriodCount == 0)
                return currentPeriodCount > 0 ? 100 : 0;

            return ((double)(currentPeriodCount - previousPeriodCount) / previousPeriodCount) * 100;
        }
    }
} 