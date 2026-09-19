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
            return Ok();
        }
        [HttpPost("upload-files")]
        public async Task<IActionResult> UploadMany([FromForm]UploadManyFilesRequest request, CancellationToken cancellationToken)
        {
            var result = await _fileService.UploadManyAsync(request.Files, cancellationToken);
            return Ok(result);
        }
    }
}
