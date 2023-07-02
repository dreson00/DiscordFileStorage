using FileUploadWeb.Discord;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileUploadWeb.Data
{
    public class DiscordDbContext : IdentityDbContext<DiscordUser>
    {
        public DiscordDbContext(DbContextOptions<DiscordDbContext> options)
            : base(options)
        {
        }
    }
}
