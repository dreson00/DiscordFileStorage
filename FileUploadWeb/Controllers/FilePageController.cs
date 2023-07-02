using System.Text.Json;
using FileUploadWeb.Data;
using FileUploadWeb.Discord;
using FileUploadWeb.Models;
using FileUploadWeb.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FileUploadWeb.Controllers
{
    public class FilePageController : Controller
    {
        private readonly FileDbContext _fileDbContext;
        private readonly FileManager _fileManager;
        private readonly DiscordBotManager _discordBotManager;
        private readonly UserManager<DiscordUser> _userManager;

        public FilePageController(
            FileDbContext fileDbContext,
            FileManager fileManager,
            DiscordBotManager discordBotManager,
            UserManager<DiscordUser> userManager)
        {
            _fileDbContext = fileDbContext;
            _fileManager = fileManager;
            _discordBotManager = discordBotManager;
            _userManager = userManager;
        }

        [Authorize]
        public IActionResult FileManagement()
        {
            var model = new FileManagementModel()
            {
                DiscordChannels = _discordBotManager
                    .GetChannels()
                    .Select(channel => new DiscordChannelLight()
                    {
                        Id = channel.Id.ToString(),
                        Name = channel.Name
                    }).ToList(),
                Files = _fileDbContext.AllFiles.Where(file => file.UserId == _userManager.GetUserId(User))
            };
            return View("FileManagement", model);
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteFile(string fileName)
        {
            _fileManager.DeleteFile(fileName);
            return FileManagement();
        }

        [Authorize]
        [HttpPost]
        public IActionResult ResendLink(string fileName, string channelId)
        {
            _discordBotManager.SendLink(
                channelId,
                _fileDbContext.AllFiles
                    .First(file => file.FileName == fileName)
                    .ShareLink,
                User.Identity!.Name!);
            return Ok();
        }

        public IActionResult VideoPage(string fileName)
        {
            var videoFile = _fileDbContext.VideoFiles.First(file => file.FileName == fileName);

            return View(videoFile);
        }
    }
}
