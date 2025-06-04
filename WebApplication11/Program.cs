using EasyData.Services;
using Microsoft.EntityFrameworkCore;
using WebApplication11.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  
    options.Cookie.HttpOnly = true;                  
    options.Cookie.IsEssential = true;               
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

app.UseSession(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapEasyData(options =>
{
    options.UseDbContext<Rpm2Context>();
});


app.Run();
