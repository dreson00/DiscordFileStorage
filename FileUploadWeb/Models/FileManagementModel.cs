using DSharpPlus.Entities;
using FileUploadWeb.Discord;

namespace FileUploadWeb.Models
{
    public class FileManagementModel
    {
        public IEnumerable<GeneralFile> Files { get; set; }
        public IEnumerable<DiscordChannelLight> DiscordChannels { get; set; }
    }
}
