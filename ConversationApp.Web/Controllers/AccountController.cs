using Conversation.Core.DTo;
using Conversation.Core.DTOs;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        public IActionResult AdminLogin()
        {
            return View();
        }
      
        [HttpPost]
        public async Task<IActionResult> AdminLogin(AdminUserDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                if (user.Role == 1 || user.Role == 2) 
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Admin", "Account");
                }
                ModelState.AddModelError(string.Empty, "Yetkiniz yok.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
            }

            return View(dto);
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
        public async Task<IActionResult> Admin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("AdminLogin", "Account");

            if (user.Role != 1 && user.Role != 2)
                return Forbid(); 

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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveUsersCount()
        {
            try
            {
                var count = await _userService.GetActiveUsersCountAsync();
                return Ok(count); 
            }
            catch (Exception)
            {
                return Ok(0); 
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || (user.Role != 1 && user.Role != 2))
                {
                    return Json(new { error = "Yetkisiz erişim" });
                }

                var totalUsers = await _userService.GetTotalUsersCountAsync();
                var newUsers = await _userService.GetNewUsersCountAsync(30);
                
                var userGrowth = await _userService.GetUserGrowthPercentageAsync(30);

                return Json(new
                {
                    totalUsers = totalUsers,
                    newUsers = newUsers,
                    userGrowth = Math.Round(userGrowth, 2)
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "İstatistikler yüklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChartData()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || (user.Role != 1 && user.Role != 2))
                {
                    return Json(new { error = "Yetkisiz erişim" });
                }

                var monthlyData = await _userService.GetMonthlyUserRegistrationsAsync();
                var labels = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

                return Json(new
                {
                    labels = labels,
                    data = monthlyData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Kullanıcı grafiği yüklenirken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageTrafficData()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || (user.Role != 1 && user.Role != 2))
                {
                    return Json(new { error = "Yetkisiz erişim" });
                }

                var monthlyMessageData = await _messageService.GetMonthlyMessageCountsAsync();
                var labels = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };

                return Json(new
                {
                    labels = labels,
                    data = monthlyMessageData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Mesaj trafiği yüklenirken hata oluştu: " + ex.Message });
            }
        }

        
    }
}