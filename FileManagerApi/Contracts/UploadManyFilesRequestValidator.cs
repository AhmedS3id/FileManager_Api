using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadManyFilesRequestValidator : AbstractValidator<UploadManyFilesRequest>
    {
        public UploadManyFilesRequestValidator()
        {
            RuleForEach(x=>x.Files)
                .SetValidator(new FileSizeValidator());

            RuleForEach(x=>x.Files)
                .SetValidator(new LockedSignatureValidator());

        }
    }
}
