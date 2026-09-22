using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class FileSizeValidator :AbstractValidator<IFormFile>
    {
        private const long MaxFileSize = 1 * 1024 * 1024;
        public FileSizeValidator()
        {
            RuleFor(x => x)
               .Must(file => file.Length <= MaxFileSize)
               .WithMessage("File size must not exceed 1 MB.")
               .When(x => x != null);
        }
    }
}
