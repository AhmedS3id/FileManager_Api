using FluentValidation;
using System.Text.RegularExpressions;

namespace FileManagerApi.Contracts
{
    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        public UploadFileRequestValidator()
        {
            RuleFor(x => x.File)
                .SetValidator(new FileSizeValidator());

            RuleFor(x => x.File)
                .SetValidator(new LockedSignatureValidator());
        }
    }
}
