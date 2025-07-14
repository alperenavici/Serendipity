using Conversation.Core.DTo;
using ConversationApp.Entity.Entites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;


public class AccountController : Controller
{
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager,IPasswordHasher<User> passwordHasher,SignInManager<User> signInManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        
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
    public IActionResult Main()
    {
        return View();
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
            ViewBag.Success = "Registration succeeded!";
            ModelState.Clear(); 
            return View(); 
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


}