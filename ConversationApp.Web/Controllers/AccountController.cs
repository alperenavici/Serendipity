using Conversation.Core.DTo;
using Conversation.Core.DTOs;
using ConversationApp.Entity.Entites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ConversationApp.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConversationService _conversationService;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;

        public AccountController(
            UserManager<User> userManager, 
            SignInManager<User> signInManager,
            IConversationService conversationService,
            IUserService userService,
            IMessageService messageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _conversationService = conversationService;
            _userService = userService;
            _messageService = messageService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Main(Guid? conversationId, string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await _conversationService.GetConversationViewModelAsync(currentUser.Id, conversationId, query);
            ViewBag.SearchQuery = query;
            ViewBag.ActiveConversationId = conversationId ?? viewModel.ConversationList.FirstOrDefault(c => c.IsActive)?.Id;

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Admin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                CreationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Kayıt başarılı! {dto.Username} kullanıcı adıyla giriş yapabilirsiniz.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName ?? string.Empty, dto.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Main", "Account");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new List<object>());
            }

            var users = await _userService.SearchUsersAsync(query, currentUser.Id);
            var result = users.Take(10).Select(u => new
            {
                username = u.UserName,
                email = u.Email,
                role = u.Role == 1 ? "Admin" : "Developer",
                avatarUrl = $"https://i.pravatar.cc/150?u={u.UserName}",
                creationDate = u.CreationDate.ToString("dd.MM.yyyy")
            });

            return Json(result);
        }

        [HttpGet]
        public IActionResult SearchConversations(string query)
        {
            return RedirectToAction("Main", new { query = query });
        }

        [HttpGet]
        public async Task<IActionResult> StartNewConversation()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var users = await _userService.GetActiveUsersExceptAsync(currentUser.Id);
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> StartNewConversation(string targetUsername, string message)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var targetUser = await _userService.GetUserByUsernameAsync(targetUsername);

            if (currentUser == null || targetUser == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("StartNewConversation");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Mesaj boş olamaz.";
                return RedirectToAction("StartNewConversation");
            }

            var conversation = await _conversationService.CreatePrivateConversationAsync(currentUser.Id, targetUser.Id, message);

            TempData["Success"] = "Mesaj başarıyla gönderildi.";
            return RedirectToAction("Main", new { conversationId = conversation.Id });
        }

        
    }
}