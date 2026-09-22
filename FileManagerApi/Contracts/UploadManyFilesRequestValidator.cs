using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadManyFilesRequestValidator : AbstractValidator<UploadManyFilesRequest>
    {
        private const int MaxFileCount = 10;

        public UploadManyFilesRequestValidator()
        {
            RuleFor(x => x.Files)
                .NotEmpty().WithMessage("At least one file is required.")
                .Must(files => files.Count <= MaxFileCount)
                .WithMessage($"No more than {MaxFileCount} files can be uploaded at once.");

            RuleForEach(x => x.Files)
                .SetValidator(new FileSizeValidator());

            RuleForEach(x => x.Files)
                .SetValidator(new LockedSignatureValidator());

        }
    }
}