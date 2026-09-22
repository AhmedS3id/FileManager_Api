using FileManagerApi.Contracts;
using FileManagerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileManagerApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FilesController(IFileService fileService) : ControllerBase
    {
        private readonly IFileService _fileService = fileService;

        [HttpPost("")]
        public async Task<IActionResult> Upload([FromForm]UploadFileRequest request, CancellationToken cancellationToken)
        {
            var result = await _fileService.UploadAsync(request.File, cancellationToken);
            return CreatedAtAction(nameof(DownloadFile), new { id = result }, null);
        }
        [HttpPost("upload-files")]
        public async Task<IActionResult> UploadMany([FromForm]UploadManyFilesRequest request, CancellationToken cancellationToken)
        {
            var result = await _fileService.UploadManyAsync(request.Files, cancellationToken);
            return CreatedAtAction(nameof(DownloadFile), new { id = result }, null);
        }
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm]UploadImageRequest request, CancellationToken cancellationToken)
        {
            await _fileService.UploadImageAsync(request.Image, cancellationToken);
            return Created();
        }
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var (fileContent, contentType, fileName) = await _fileService.DownloadAsync(id, cancellationToken);

            return fileContent != null ? File(fileContent, contentType, fileName) : NotFound();
        }
        [HttpGet("stream/{id}")]
        public async Task<IActionResult> StreamFile([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var (fileContent, contentType, fileName) = await _fileService.StreamAsync(id, cancellationToken);

            return fileContent != null ? File(fileContent, contentType, fileName,enableRangeProcessing:true) : NotFound();
        }
    }
}
