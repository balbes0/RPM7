using EasyData.Services;
using Microsoft.EntityFrameworkCore;
using WebApplication11.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавление сессий
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // Настройка времени жизни сессии
    options.Cookie.HttpOnly = true;                   // Ограничение доступа cookie только сервером
    options.Cookie.IsEssential = true;                // Устанавливает cookie как обязательное
});

builder.Services.AddDbContext<WebApplication11.Models.Rpm2Context>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("con")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();  // Добавление сессий

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapEasyData(options =>
{
    options.UseDbContext<Rpm2Context>(); // Подключение контекста
});


app.Run();
