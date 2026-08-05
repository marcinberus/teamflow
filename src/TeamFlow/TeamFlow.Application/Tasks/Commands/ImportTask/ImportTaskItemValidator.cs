using FluentValidation;
using TeamFlow.Application.Common;
using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Application.Tasks.Commands.ImportTask;

public class ImportTaskItemValidator : AbstractValidator<ImportTaskItemCommand>
{
    public ImportTaskItemValidator()
    {
        RuleFor(x => x.Extension)
            .NotEmpty()
            .Must(value => FileExtensionParser.TryParse(value, out _))
            .WithMessage(ErrorMessages.InvalidExtension);
    }
}
