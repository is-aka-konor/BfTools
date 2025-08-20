using Bfmd.Core.Services;

namespace Bfmd.UnitTests;

public class SlugServiceTests
{
    [Theory]
    [InlineData("Ёжик в тумане", "ezhik-v-tumane")]
    [InlineData("Сила и Ловкость", "sila-i-lovkost")]
    [InlineData("Меч-кладенец!!!", "mech-kladenec")]
    [InlineData("йцукен", "ycuken")]
    public void Transliteration_ShouldProduceSlug_WhenGivenCyrillicInput(string input, string expected)
    {
        Assert.Equal(expected, SlugService.From(input));
    }
}
