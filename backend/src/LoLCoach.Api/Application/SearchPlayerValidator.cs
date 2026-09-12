using System.Text.RegularExpressions;
using FluentValidation;

namespace LoLCoach.Api.Application;

public sealed class SearchPlayerValidator : AbstractValidator<SearchPlayerCommand>
{
    public const int GameNameMinLength = 3;
    public const int GameNameMaxLength = 16;
    public const int TagLineMinLength = 2;
    public const int TagLineMaxLength = 5;
    public const string TagLinePattern = "^[A-Za-z0-9]+$";
    public const string TagLineSchemaPattern = "^ *[A-Za-z0-9]+ *$";

    public SearchPlayerValidator()
    {
        RuleFor(command => command.GameName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("'gameName' is required.")
            .Must(name => name!.Trim().Length is >= GameNameMinLength and <= GameNameMaxLength)
                .WithMessage("'gameName' must be between 3 and 16 characters.")
            .Must(name => Regex.IsMatch(name!.Trim(), @"^[\p{L}\p{N} ]+$"))
                .WithMessage("'gameName' may contain only letters, digits, and spaces.");

        RuleFor(command => command.TagLine)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("'tagLine' is required.")
            .Must(tag => tag!.Trim().Length is >= TagLineMinLength and <= TagLineMaxLength)
                .WithMessage("'tagLine' must be between 2 and 5 characters.")
            .Must(tag => Regex.IsMatch(tag!.Trim(), TagLinePattern))
                .WithMessage("'tagLine' must be alphanumeric.");

        RuleFor(command => command.Region)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("'region' is required.")
            .Must(region => RiotRegions.TryNormalizePlatform(region, out _))
                .WithMessage("'region' must be a supported LoL platform.");
    }
}
