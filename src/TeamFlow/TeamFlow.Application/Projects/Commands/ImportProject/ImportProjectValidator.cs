using FluentValidation;
using TeamFlow.Application.Common;
using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Application.Projects.Commands.ImportProject;

public class ImportProjectValidator : AbstractValidator<ImportProjectCommand>
{
    public ImportProjectValidator()
    {
        RuleFor(x => x.Extension)
            .NotEmpty()
            .Must(value => FileExtensionParser.TryParse(value, out _))
            .WithMessage(ErrorMessages.InvalidExtension);
    }
}
