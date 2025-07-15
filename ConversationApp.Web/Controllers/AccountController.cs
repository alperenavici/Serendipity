using Conversation.Core.DTo;
using ConversationApp.Entity.Entites;
using ConversationApp.Web.Models;
using ConversationApp.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<User> userManager, IPasswordHasher<User> passwordHasher, SignInManager<User> signInManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _context = context;
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

            var userConversationsQuery = _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Messages.OrderByDescending(m => m.SentDate).Take(1))
                        .ThenInclude(m => m.Sender)
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Where(cp => cp.UserId == currentUser.Id && !cp.IsDeleted);

            // Arama filtresi ekle
            if (!string.IsNullOrWhiteSpace(query))
            {
                userConversationsQuery = userConversationsQuery
                    .Where(cp => cp.Conversation.Title.Contains(query) ||
                                 cp.Conversation.Participants.Any(p => p.User.UserName.Contains(query)));
                ViewBag.SearchQuery = query;
            }

            var userConversations = await userConversationsQuery.ToListAsync();

            var conversationList = new List<ConversationListItemViewModel>();
            
            foreach (var userConv in userConversations)
            {
                var conversation = userConv.Conversation;
                var lastMessage = conversation.Messages.OrderByDescending(m => m.SentDate).FirstOrDefault();
                
                var otherParticipants = conversation.Participants
                    .Where(p => p.UserId != currentUser.Id && !p.IsDeleted)
                    .Select(p => p.User)
                    .ToList();

                string conversationName;
                string avatarUrl;
                
                if (conversation.Type == 0) // Private conversation
                {
                    var otherUser = otherParticipants.FirstOrDefault();
                    conversationName = otherUser?.UserName ?? "Bilinmeyen Kullanıcı";
                    avatarUrl = $"https://i.pravatar.cc/150?u={otherUser?.UserName ?? "unknown"}";
                }
                else 
                {
                    conversationName = conversation.Title ?? "Grup Sohbeti";
                    avatarUrl = "https://i.pravatar.cc/150?u=group";
                }

                conversationList.Add(new ConversationListItemViewModel
                {
                    Id = conversation.Id,
                    Name = conversationName,
                    LastMessage = lastMessage?.Content ?? "Henüz mesaj yok",
                    LastMessageTime = lastMessage?.SentDate.ToString("HH:mm") ?? "",
                    UnreadCount = 0, 
                    AvatarUrl = avatarUrl,
                    IsActive = conversationId.HasValue ? conversation.Id == conversationId.Value : false
                });
            }

            // Eğer conversationId belirtilmemişse ilk konuşmayı aktif yap
            if (!conversationId.HasValue && conversationList.Any())
            {
                conversationList.First().IsActive = true;
                conversationId = conversationList.First().Id;
            }

            // Aktif konuşma için mesajları çek
            var activeConversation = conversationList.FirstOrDefault(c => c.IsActive);
            var messages = new List<MessageViewModel>();
            ActiveChatUserViewModel activeChatUser = null;

            if (activeConversation != null)
            {
                var conversationMessages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ConversationId == activeConversation.Id)
                    .OrderBy(m => m.SentDate)
                    .ToListAsync();

                messages = conversationMessages.Select(m => new MessageViewModel
                {
                    SenderName = m.Sender.UserName,
                    Content = m.Content,
                    SentDate = m.SentDate,
                    IsOutgoing = m.UserId == currentUser.Id
                }).ToList();

                var activeConv = userConversations.FirstOrDefault(uc => uc.ConversationId == activeConversation.Id)?.Conversation;
                if (activeConv?.Type == 0) // Private conversation
                {
                    var otherUser = activeConv.Participants
                        .Where(p => p.UserId != currentUser.Id && !p.IsDeleted)
                        .Select(p => p.User)
                        .FirstOrDefault();
                    
                    if (otherUser != null)
                    {
                        activeChatUser = new ActiveChatUserViewModel
                        {
                            Name = otherUser.UserName,
                            Role = otherUser.Role == 1 ? "Admin" : "Developer",
                            AvatarUrl = $"https://i.pravatar.cc/150?u={otherUser.UserName}"
                        };
                    }
                }
                else // Group conversation
                {
                    activeChatUser = new ActiveChatUserViewModel
                    {
                        Name = activeConv?.Title ?? "Grup Sohbeti",
                        Role = "Grup",
                        AvatarUrl = "https://i.pravatar.cc/150?u=group"
                    };
                }
            }

            ViewBag.ActiveConversationId = conversationId;

            var viewModel = new ConversationViewModel
            {
                Title = activeConversation?.Name ?? "Sohbet",
                CurrentUser = new UserProfileViewModel
                {
                    UserName = currentUser.UserName,
                    Role = currentUser.Role == 1 ? "Admin" : "Developer",
                    AvatarUrl = $"https://i.pravatar.cc/150?u={currentUser.UserName}"
                },
                ConversationList = conversationList,
                ActiveChatUser = activeChatUser,
                Messages = messages
            };
            
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

            var users = await _userManager.Users
                .Where(u => u.UserName.Contains(query) && u.Id != currentUser.Id && !u.IsDeleted)
                .Take(10)
                .Select(u => new
                {
                    username = u.UserName,
                    email = u.Email,
                    role = u.Role == 1 ? "Admin" : "Developer",
                    avatarUrl = $"https://i.pravatar.cc/150?u={u.UserName}",
                    creationDate = u.CreationDate.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            return Json(users);
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

            var users = await _userManager.Users
                .Where(u => u.Id != currentUser.Id && !u.IsDeleted)
                .OrderBy(u => u.UserName)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> StartNewConversation(string targetUsername, string message)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var targetUser = await _userManager.FindByNameAsync(targetUsername);

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

            // Mevcut konuşma var mı kontrol et
            var existingConversation = await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Participants)
                .Where(cp => cp.UserId == currentUser.Id)
                .Select(cp => cp.Conversation)
                .Where(c => c.Type == 0 && 
                            c.Participants.Count == 2 &&
                            c.Participants.Any(p => p.UserId == targetUser.Id))
                .FirstOrDefaultAsync();

            ConversationApp.Entity.Entites.Conversation conversation;

            if (existingConversation == null)
            {
                // Yeni konuşma oluştur
                conversation = new ConversationApp.Entity.Entites.Conversation
                {
                    Id = Guid.NewGuid(),
                    Title = $"{currentUser.UserName} - {targetUser.UserName}",
                    Type = 0, // Private
                    CreationDate = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);

                // Katılımcıları ekle
                _context.ConversationParticipants.Add(new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = currentUser.Id,
                    JoinedDate = DateTime.UtcNow
                });

                _context.ConversationParticipants.Add(new ConversationParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = targetUser.Id,
                    JoinedDate = DateTime.UtcNow
                });
            }
            else
            {
                conversation = existingConversation;
            }

            // Mesajı kaydet
            var newMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = currentUser.Id,
                Content = message,
                SentDate = DateTime.UtcNow
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mesaj başarıyla gönderildi.";
            return RedirectToAction("Main", new { conversationId = conversation.Id });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(Guid conversationId, string message)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return RedirectToAction("Main", new { conversationId = conversationId });
            }

            // Kullanıcının bu konuşmaya katılımcı olduğunu kontrol et
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && 
                               cp.UserId == currentUser.Id && 
                               !cp.IsDeleted);

            if (!isParticipant)
            {
                return Forbid();
            }

            // Mesajı kaydet
            var newMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = currentUser.Id,
                Content = message,
                SentDate = DateTime.UtcNow
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction("Main", new { conversationId = conversationId });
        }
    }
}