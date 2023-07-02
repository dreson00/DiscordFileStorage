using FileUploadWeb.Enums;
using FileUploadWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace FileUploadWeb.Data
{
    public class FileDbContext : DbContext
    {
        public DbSet<ArchiveFile> ArchiveFiles { get; private set; }
        public DbSet<VideoFile> VideoFiles { get; private set; }
        public IEnumerable<GeneralFile> AllFiles => ArchiveFiles
            .Select(file => (GeneralFile)file)
            .ToList()
            .Concat(VideoFiles
                .Select(file => (GeneralFile)file));


        public FileDbContext(DbContextOptions<FileDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GeneralFile>()
                .HasKey(e => e.FileName);
        }
    }
}
