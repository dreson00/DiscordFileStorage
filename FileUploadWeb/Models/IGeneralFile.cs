using FileUploadWeb.Enums;

namespace FileUploadWeb.Models
{
    public interface IGeneralFile
    {
        public string UserId { get; set; }
        public DateTime UploadTime { get; set; }
        public string FileName { get; set; }
        public string FileFormat { get; set; }
        public string FileLink { get; set; }
        public string ShareLink { get; set; }
        public Types Type { get; set; }
    }
}
