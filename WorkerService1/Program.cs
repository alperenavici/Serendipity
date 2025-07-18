using ConversationApp.Data.Context;
using ConversationApp.Data.Interfaces;
using ConversationApp.Data.Repositories;
using ConversationApp.Service.Interfaces;
using ConversationApp.Service.Services;
using Microsoft.EntityFrameworkCore;
using WorkerService1;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Pattern - Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IScheduleMessageRepository, ScheduleMessageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
builder.Services.AddScoped<IMessageReadReceiptRepository, MessageReadReceiptRepository>();

// Services
builder.Services.AddScoped<IScheduleMessageService, ScheduleMessageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddMemoryCache();

// Worker Service
builder.Services.AddHostedService<ScheduleMessageWorker>();

var host = builder.Build();
host.Run();
