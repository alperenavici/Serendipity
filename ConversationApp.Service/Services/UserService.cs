using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
} 