using Microsoft.AspNetCore.Mvc;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password, bool rememberMe)
    {
        if (username == "admin" && password == "123")
            return RedirectToAction("Index", "Home");

        ViewBag.Error = "Wrong username or password";
        return View();
    }
}