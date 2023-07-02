using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FileUploadWeb.Data;
using FileUploadWeb.Filters;
using Microsoft.AspNetCore.Http.Features;
using FileUploadWeb.Discord;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using System;
using FileUploadWeb.Utilities;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDbContext<DiscordDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DiscordDbConnection"));
});


builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FilesDbConnection")));


builder.Services.AddIdentity<DiscordUser, IdentityRole>(options =>
{

})
    .AddUserManager<UserManager<DiscordUser>>()
    .AddSignInManager<SignInManager<DiscordUser>>()
    .AddEntityFrameworkStores<DiscordDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = builder.Configuration.GetSection("AppSettings").GetValue<long>("FileSizeLimit");
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions
        .AddPageApplicationModelConvention("/FileUpload",
            model =>
            {
                model.Filters.Add(
                    new GenerateAntiforgeryTokenCookieAttribute());
                model.Filters.Add(
                    new DisableFormValueModelBindingAttribute());
            });
});

builder.Services.AddSingleton<DiscordBot.DiscordBot>();
builder.Services.AddTransient<FileManager>();
builder.Services.AddTransient<DiscordBotManager>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<DiscordDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<FileDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

var videoFolderPath = app.Configuration.GetSection("AppSettings").GetValue<string>("StoredFilesPath");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(videoFolderPath),
    RequestPath = "/files"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
});

var discordBot = app.Services.GetRequiredService<DiscordBot.DiscordBot>();


app.Run();

discordBot.StartBot().GetAwaiter().GetResult();