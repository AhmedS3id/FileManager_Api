using FileManagerApi.Entities;
using FileManagerApi.Persistence;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace FileManagerApi.Services
{
    public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
    {
        private readonly string _filePath = $"{webHostEnvironment.WebRootPath}/Uploads";
        private readonly string _imagePath = $"{webHostEnvironment.WebRootPath}/Images";
        private readonly ApplicationDbContext _context = context;

        public async Task<Guid> UploadAsync(IFormFile file , CancellationToken cancellationToken = default)
        {
            var uploadedFile = await SaveFile(file,cancellationToken);

            await _context.AddAsync(uploadedFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFile.Id;
        }
        public async Task<ICollection<Guid>> UploadManyAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
        {
            List<UploadedFiles> uploadedFiles = [];
            foreach (var file in files)
            {
                var uploadedFile = await SaveFile(file, cancellationToken);
                uploadedFiles.Add(uploadedFile);
            }

            await _context.AddRangeAsync(uploadedFiles, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFiles.Select(x => x.Id).ToList();
        }
        public async Task UploadImageAsync(IFormFile Image, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(_imagePath, Image.FileName);
            using var stream = File.Create(path);
            await Image.CopyToAsync(stream, cancellationToken);
        }

        public async Task<(byte[] fileContent, string contentType, string fileName)> DownloadAsync(Guid Id, CancellationToken cancellationToken = default)
        {
            var file = await _context.Files.FindAsync([Id], cancellationToken: cancellationToken);
                
            if (file == null)
                return ([],string.Empty,string.Empty);

            var path = Path.Combine(_filePath, file.StoredFileName);

            MemoryStream memoryStream = new();
            using FileStream fileStream = new(path, FileMode.Open);
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return (memoryStream.ToArray(), file.ContentType, file.FileName);
        }

        private async Task<UploadedFiles> SaveFile(IFormFile file, CancellationToken cancellationToken = default)
        {
            var randomFileName = Path.GetRandomFileName();
            var uploadedFile = new UploadedFiles
            {
                FileName = file.Name,
                ContentType = file.ContentType,
                StoredFileName = randomFileName,
                FileExtension = Path.GetExtension(file.FileName)
            };
            var path = Path.Combine(_filePath, randomFileName);
            using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            return uploadedFile;
        }

    }
}
