using FileManagerApi.Entities;
using FileManagerApi.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FileManagerApi.Services
{
    public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
    {
        private readonly string _filePath = $"{webHostEnvironment.WebRootPath}/Uploads";
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
