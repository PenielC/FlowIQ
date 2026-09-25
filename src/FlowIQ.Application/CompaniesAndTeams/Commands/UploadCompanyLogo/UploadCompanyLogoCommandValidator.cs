using FluentValidation;

namespace FlowIQ.Application.CompaniesAndTeams.Commands.UploadCompanyLogo;

public class UploadCompanyLogoCommandValidator : AbstractValidator<UploadCompanyLogoCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
    };

    private const int MaxSizeBytes = 2 * 1024 * 1024;

    public UploadCompanyLogoCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Logo must be a PNG, JPEG, or WebP image.");
        RuleFor(x => x.Data)
            .Must(d => d.Length > 0 && d.Length <= MaxSizeBytes)
            .WithMessage("Logo must be 2MB or smaller.");
    }
}
