using FFMpegCore;
using FFMpegCore.Enums;
using FileUploadWeb.Data;
using FileUploadWeb.Enums;
using FileUploadWeb.Models;
using System.Security.Policy;

namespace FileUploadWeb.Utilities
{
    public class FileManager
    {
        private readonly FileDbContext _fileDbContext;
        private readonly ILogger<FileManager> _logger;

        private readonly List<string> _videoFormats = new() { "mkv", "mp4", "mov", "webm", "avi" };
        private readonly List<string> _archiveFormats = new() { "zip", "rar", "7z" };

        private readonly List<string> _unsupportedVideoFormats = new() { "mkv" };
        private readonly List<string> _unsupportedVideoCodecs = new() { "hevc" };
        private readonly List<string> _unsupportedAudioCodecs = new(); // TO DO

        private readonly string _targetFilePath;

        public FileManager(FileDbContext fileDbContext, IConfiguration config, ILogger<FileManager> logger)
        {
            _fileDbContext = fileDbContext;
            _logger = logger;
            _targetFilePath = config.GetSection("AppSettings").GetValue<string>("StoredFilesPath");
        }

        public string GetNewFileName(string oldFileName)
        {
            var newFileName = "kanela_" + Path.GetRandomFileName() + Path.GetExtension(oldFileName);

            while (_fileDbContext.AllFiles.Any(file => file.FileName == newFileName))
            {
                newFileName = "kanela_" + Path.GetRandomFileName() + Path.GetExtension(oldFileName);
            }
            return newFileName;
        }

        public string GetActionName(GeneralFile file)
        {
            return file.Type switch
            {
                Types.Video => "VideoPage",
                Types.Archive => "NoVisualPage",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public string GetActionName(string fileName)
        {
            return GetActionName(_fileDbContext.AllFiles.First(file => file.FileName == fileName));
        }

        public async Task<GeneralFile> CreateAndSaveSpecificFile(string fileName, string fileLink, string shareLink, string userId)
        {
            var file = new GeneralFile()
            {
                UserId = userId,
                UploadTime = DateTime.Now,
                FileName = fileName,
                FileLink = fileLink,
                ShareLink = shareLink,
                FileFormat = Path.GetExtension(fileName).Remove(0, 1)
            };

            if (_videoFormats.Any(format => format == file.FileFormat))
            {
                var videoFile = await CreateVideoFile(file);
                _fileDbContext.VideoFiles.Add((VideoFile)videoFile);
                file = (GeneralFile)videoFile;
            }
            else if (_archiveFormats.Any(format => format == file.FileFormat))
            {
                _fileDbContext.ArchiveFiles.Add(CreateArchiveFile(file));
            }

            await _fileDbContext.SaveChangesAsync();


            return file;
        }

        private async Task<IGeneralFile> CreateVideoFile(GeneralFile generalFile)
        {
            var mediaInfo = await FFProbe.AnalyseAsync(_targetFilePath + $"/{generalFile.FileName}");
            var rotation = mediaInfo.VideoStreams.First().Rotation;
            var height = mediaInfo.VideoStreams.First().Height;
            var width = mediaInfo.VideoStreams.First().Width;
            var videoFile = new VideoFile
            {
                UserId = generalFile.UserId,
                UploadTime = generalFile.UploadTime,
                FileName = generalFile.FileName,
                FileFormat = generalFile.FileFormat,
                FileLink = generalFile.FileLink,
                ShareLink = generalFile.ShareLink,
                VideoWidth = rotation is 90 or -90 ? height : width,
                VideoHeight = rotation is 90 or -90 ? width : height,
                AudioCodec = mediaInfo.AudioStreams.First().CodecName,
                VideoCodec = mediaInfo.VideoStreams.First().CodecName,
                Type = Types.Video
            };
            return ConvertVideoFile(videoFile);
        }

        private ArchiveFile CreateArchiveFile(GeneralFile generalFile)
        {
            return new ArchiveFile()
            {
                UserId = generalFile.UserId,
                UploadTime = generalFile.UploadTime,
                FileName = generalFile.FileName,
                FileFormat = generalFile.FileFormat,
                FileLink = generalFile.FileLink,
                ShareLink = generalFile.ShareLink,
                Type = Types.Archive
            };
        }

        private IGeneralFile ConvertVideoFile(VideoFile videoFile)
        {
            var conversionsNeeded = new List<bool>() { false, false, false };

            if (_unsupportedVideoFormats.Any(format => format == videoFile.FileFormat))
            {
                videoFile.FileFormat = "mp4";
                conversionsNeeded[0] = true;
                _logger.LogInformation("Conversion to mp4 needed.");
            }
            if (_unsupportedAudioCodecs.Any(codec => codec == videoFile.AudioCodec))
            {
                videoFile.AudioCodec = "aac";
                conversionsNeeded[1] = true;
                _logger.LogInformation("Conversion to aac needed.");
            }
            if (_unsupportedVideoCodecs.Any(codec => codec == videoFile.VideoCodec))
            {
                videoFile.VideoCodec = "h264";
                conversionsNeeded[2] = true;
                _logger.LogInformation("Conversion to h264 needed.");
            }
            if (conversionsNeeded.All(b => b is false))
            {
                return videoFile;
            }

            var filePath = _targetFilePath + $"/{videoFile.FileName}";
            var newFileName = GetNewFileName(videoFile.FileName + $".{videoFile.FileFormat}");
            var newFilePath = filePath.Replace(videoFile.FileName, newFileName);

            videoFile.FileLink = videoFile.FileLink.Replace(videoFile.FileName, newFileName);
            videoFile.ShareLink = videoFile.ShareLink.Replace(videoFile.FileName, newFileName);
            videoFile.FileName = newFileName;

            _logger.LogInformation("Video conversion starts now.");

            try
            {
                FFMpegArguments
                    .FromFileInput(filePath)
                    .OutputToFile(newFilePath, false, options =>
                    {
                        if (conversionsNeeded[0])
                        {
                            options = options.ForceFormat("mp4");
                        }
                        if (conversionsNeeded[1])
                        {
                            options = options.WithAudioCodec(AudioCodec.Aac);
                        }
                        if (conversionsNeeded[2])
                        {
                            options = options.WithVideoCodec(VideoCodec.LibX264);
                        }
                    })
                    .ProcessSynchronously();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }


            _logger.LogInformation("Video conversion successful.");

            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Unsupported file successfully deleted.");
            }
            catch (IOException e)
            {
                _logger.LogError($"Deleting unsupported file was NOT successful: {e}");
            }

            return videoFile;
        }

        public void DeleteFile(string fileName)
        {
            var filePath = _targetFilePath + $"/{fileName}";
            try
            {
                File.Delete(filePath);
                _logger.LogInformation($"File successfully deleted from folder {_targetFilePath}.");
                DeleteFileFromDb(fileName);
            }
            catch (IOException e)
            {
                _logger.LogError($"Deleting file was NOT successful: {e}");
            }
        }

        private void DeleteFileFromDb(string fileName)
        {
            var file = _fileDbContext.AllFiles.First(file => file.FileName == fileName);
            try
            {
                switch (file.Type)
                {
                    case Types.Video:
                        _fileDbContext.VideoFiles.Remove((VideoFile)file);
                        break;
                    case Types.Archive:
                        _fileDbContext.ArchiveFiles.Remove((ArchiveFile)file);
                        break;
                }
                _fileDbContext.SaveChanges();
            }
            catch (IOException e)
            {
                _logger.LogError($"Deleting unsupported file was NOT successful: {e}");
            }
        }
    }
}
