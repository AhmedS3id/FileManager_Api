using FileManagerApi.Entities;
using FileManagerApi.Persistence;

namespace FileManagerApi.Services
{
    public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
    {
        private const int BufferSize = 4096;

        private readonly string _filePath = Path.Combine(webHostEnvironment.WebRootPath, "Uploads");
        private readonly ApplicationDbContext _context = context;

        public async Task<Guid> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var uploadedFile = await SaveFile(file, cancellationToken);

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

        public async Task<Guid> UploadImageAsync(IFormFile image, CancellationToken cancellationToken = default)
        {
            var uploadedImage = await SaveFile(image, cancellationToken);

            await _context.AddAsync(uploadedImage, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedImage.Id;
        }

        public Task<(Stream? fileContent, string contentType, string fileName)> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
            => OpenFileAsync(id, cancellationToken);

        public Task<(Stream? stream, string contentType, string fileName)> StreamAsync(Guid id, CancellationToken cancellationToken = default)
            => OpenFileAsync(id, cancellationToken);

        private async Task<(Stream? stream, string contentType, string fileName)> OpenFileAsync(Guid id, CancellationToken cancellationToken)
        {
            var file = await _context.Files.FindAsync([id], cancellationToken: cancellationToken);

            if (file is null)
                return (null, string.Empty, string.Empty);

            var path = Path.Combine(_filePath, file.StoredFileName);

            if (!File.Exists(path))
                return (null, string.Empty, string.Empty);

            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return (stream, file.ContentType, file.FileName);
            }
            catch (IOException)
            {
                return (null, string.Empty, string.Empty);
            }
        }

        private async Task<UploadedFiles> SaveFile(IFormFile file, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_filePath);

            var storedFileName = Path.GetRandomFileName();
            var uploadedFile = new UploadedFiles
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                StoredFileName = storedFileName,
                FileExtension = Path.GetExtension(file.FileName)
            };

            var path = Path.Combine(_filePath, storedFileName);
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
            await file.CopyToAsync(stream, cancellationToken);

            return uploadedFile;
        }
    }
}