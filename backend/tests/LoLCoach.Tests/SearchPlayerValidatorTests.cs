using LoLCoach.Api.Application;

namespace LoLCoach.Tests;

public sealed class SearchPlayerValidatorTests
{
    private readonly SearchPlayerValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = " Faker ",
            TagLine = "TAG",
            Region = "BR1",
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GameName_accepts_unicode_letters_digits_and_spaces()
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Café com Leite 2",
            TagLine = "TAG",
            Region = "br1",
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    [InlineData("abcdefghijklmnopq")]
    public async Task GameName_out_of_range_fails(string gameName)
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = gameName,
            TagLine = "TAG",
            Region = "br1",
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "GameName");
    }

    [Fact]
    public async Task GameName_with_symbols_fails()
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Faker#1",
            TagLine = "TAG",
            Region = "br1",
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "GameName");
    }

    [Fact]
    public async Task TagLine_accepts_legacy_two_chars()
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Example",
            TagLine = "BR",
            Region = "br1",
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("TOOLONG")]
    public async Task TagLine_out_of_range_fails(string tagLine)
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Example",
            TagLine = tagLine,
            Region = "br1",
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "TagLine");
    }

    [Theory]
    [InlineData("TA G")]
    [InlineData("TAG!")]
    public async Task TagLine_non_alphanumeric_fails(string tagLine)
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Example",
            TagLine = tagLine,
            Region = "br1",
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "TagLine");
    }

    [Theory]
    [InlineData("BR1")]
    [InlineData("br1")]
    [InlineData(" BR1 ")]
    [InlineData("kr")]
    [InlineData("vn2")]
    public async Task Region_supported_passes_case_insensitively(string region)
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Example",
            TagLine = "TAG",
            Region = region,
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("xx1")]
    [InlineData("americas")]
    [InlineData("")]
    public async Task Region_unsupported_fails(string region)
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand
        {
            GameName = "Example",
            TagLine = "TAG",
            Region = region,
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Region");
    }

    [Fact]
    public async Task Missing_fields_fail_for_each_property()
    {
        var result = await _validator.ValidateAsync(new SearchPlayerCommand());
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }
}
