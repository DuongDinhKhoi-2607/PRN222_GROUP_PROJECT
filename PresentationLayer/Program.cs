using BussinessLayer;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using PresentationLayer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.Filters.Add<PresentationLayer.Filters.ForcePasswordChangeFilter>();
    });
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LecturerOrAdmin", policy =>
        policy.RequireRole("lecturer", "admin"));
});

builder.Services.AddBusinessServices(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
    builder.Configuration);

var app = builder.Build();

// ── Seed required lookup data ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var seed = scope.ServiceProvider.GetRequiredService<ISeedService>();
    await seed.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<DocumentHub>("/documentHub");

app.Run();
