using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using ConversationApp.Data.Context;
using ConversationApp.Entity.Entites;
using ConversationApp.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using ConversationApp.Data.Interfaces;
using ConversationApp.Data.Repositories;
using ConversationApp.Service.Interfaces;
using ConversationApp.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Repositories
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMessageReadReceiptRepository, MessageReadReceiptRepository>();
builder.Services.AddScoped<IScheduleMessageRepository, ScheduleMessageRepository>();

// Register Services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScheduleMessageService, ScheduleMessageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    // �nce iste�in normal �ekilde i�lenmesine izin ver
    await next.Invoke();

    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        var unitOfWork = context.RequestServices.GetRequiredService<ConversationApp.Data.Interfaces.IUnitOfWork>();

        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user != null)
            {
                user.LastActiveDate = DateTime.UtcNow;
                await unitOfWork.CommitAsync(); 
            }
        }
    }
});


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<ChatHub>("/chatHub");

app.Run();
