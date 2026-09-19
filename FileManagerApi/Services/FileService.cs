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
            var randomFileName =Path.GetRandomFileName();

            var uploadedFile = new UploadedFilesRequest
            {
                FileName = file.Name,
                ContentTybe = file.ContentType,
                StoredFileName = randomFileName,
                FileExtension = Path.GetExtension(file.FileName)
            };
            
            var path = Path.Combine(_filePath, randomFileName); 
            using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            await _context.AddAsync(uploadedFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFile.Id;
        }
    }
}
