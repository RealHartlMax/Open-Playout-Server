using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlayoutServer.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging folder/file init
PlayoutServer.Core.Services.FileLogger.Initialize("Logs/Log_core", "core");
PlayoutServer.Core.Services.FileLogger.Log("Program start");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<WebSocketServer>();
builder.Services.AddHostedService<PlayoutService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

