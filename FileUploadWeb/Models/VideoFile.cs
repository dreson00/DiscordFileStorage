using FileUploadWeb.Enums;

namespace FileUploadWeb.Models
{
    public class VideoFile : GeneralFile
    {
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }
        public int VideoWidth { get; set; }
        public int VideoHeight { get; set; }
    }
}
