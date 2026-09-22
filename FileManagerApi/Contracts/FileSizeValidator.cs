using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class FileSizeValidator : AbstractValidator<IFormFile>
    {
        private const long MaxFileSize = 1 * 1024 * 1024;
        public FileSizeValidator()
        {
            RuleFor(x => x)
               .Must(file => file.Length > 0 && file.Length <= MaxFileSize)
               .WithMessage("File size must be between 1 byte and 1 MB.")
               .When(x => x != null);
        }
    }
}