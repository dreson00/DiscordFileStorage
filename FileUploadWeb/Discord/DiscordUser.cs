using Microsoft.AspNetCore.Identity;

namespace FileUploadWeb.Discord
{
    public class DiscordUser : IdentityUser
    {
        public string DiscordId { get; set; }
    }
}
