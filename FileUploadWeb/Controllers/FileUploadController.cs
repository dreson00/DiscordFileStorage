using FileUploadWeb.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using FileUploadWeb.Discord;
using FileUploadWeb.Models;
using FileUploadWeb.Utilities;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;

namespace FileUploadWeb.Controllers;

[Authorize]
public class FileUploadController : Controller
{
    private readonly long _fileSizeLimit;
    private readonly UserManager<Discord.DiscordUser> _userManager;
    private readonly ILogger<FileUploadController> _logger;
    private readonly FileManager _fileManager;
    private readonly DiscordBotManager _discordBotManager;
    private readonly string[] _permittedExtensions = { ".mp4", ".mkv", ".avi", ".webm", ".mov" };
    private readonly string _targetFilePath;
    private static readonly FormOptions _defaultFormOptions = new();

    public FileUploadController(
        UserManager<Discord.DiscordUser> userManager,
        ILogger<FileUploadController> logger,
        IConfiguration config,
        FileManager fileManager,
        DiscordBotManager discordBotManager)
    {
        _userManager = userManager;
        _logger = logger;
        _fileManager = fileManager;
        _discordBotManager = discordBotManager;
        _fileSizeLimit = config.GetSection("AppSettings").GetValue<long>("FileSizeLimit");
        _targetFilePath = config.GetSection("AppSettings").GetValue<string>("StoredFilesPath");
    }

    public IActionResult FileUpload()
    {
        ViewBag.DiscordChannels = _discordBotManager.GetChannels();
        return View();
    }

    [HttpPost]
    [DisableFormValueModelBinding]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3221225472)]
    public async Task<IActionResult> Upload()
    {
        var selectedValue = HttpContext.Request.Headers["SelectedValue"];

        if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError("File",
                $"The request couldn't be processed (Error 1).");
            _logger.LogError("Error while processing file.");

            return BadRequest(ModelState);
        }
        else if (string.IsNullOrEmpty(selectedValue))
        {
            ModelState.AddModelError("SelectedValue",
                $"The request couldn't be processed (Error 2).");
            _logger.LogError("Missing selected channel id.");

            return BadRequest(ModelState);
        }

        var boundary = MultipartRequestHelper.GetBoundary(
            MediaTypeHeaderValue.Parse(Request.ContentType),
            _defaultFormOptions.MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync();

        var trustedFileNameForFileStorage = String.Empty;
        GeneralFile file = new();

        while (section is { ContentType: not null })
        {
            var hasContentDispositionHeader =
                ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader)
            {
                if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 3).");

                    return BadRequest(ModelState);
                }
                else
                {
                    var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                        section, contentDisposition, ModelState,
                        _permittedExtensions, _fileSizeLimit);

                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    var fileName = contentDisposition.FileName.Value;
                    var trustedFileNameForDisplay = WebUtility.HtmlEncode(fileName);
                    trustedFileNameForFileStorage = _fileManager.GetNewFileName(fileName);

                    await using var targetStream = System.IO.File.Create(
                        Path.Combine(_targetFilePath, trustedFileNameForFileStorage));
                    await targetStream.WriteAsync(streamedFileContent);

                    _logger.LogInformation(
                        "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                        "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                        trustedFileNameForDisplay, _targetFilePath,
                        trustedFileNameForFileStorage);
                }
            }

            section = await reader.ReadNextSectionAsync();
        }

        _logger.LogInformation("UPLOAD COMPLETE");

        file = await _fileManager.CreateAndSaveSpecificFile(
            trustedFileNameForFileStorage,
            $"https://{HttpContext.Request.Host}" + "/files/" + trustedFileNameForFileStorage,
            $"https://{HttpContext.Request.Host}" + Url.Action(
                _fileManager.GetActionName(file), "FilePage",
                new { fileName = trustedFileNameForFileStorage }),
            _userManager.GetUserId(User));

        _discordBotManager.SendLink(
            selectedValue!,
            file.ShareLink,
            User.Identity!.Name!);

        _logger.LogInformation("Request to send message sent to discord bot.");


        return Created(nameof(FileUploadController), new { Status = "Created", FileName = trustedFileNameForFileStorage });
    }
}