using NekoDex.Core.Text;

namespace NekoDex.Core.Tests;

public class DisplayNameTests
{
    [Theory]
    [InlineData("company chocobo", "Company Chocobo")]
    [InlineData("magitek armor", "Magitek Armor")]
    [InlineData("kamuy of the nine tails", "Kamuy of the Nine Tails")]
    [InlineData("the dreadwyrm", "The Dreadwyrm")]
    [InlineData("wind-up sun", "Wind-Up Sun")]
    [InlineData("Fat Chocobo", "Fat Chocobo")]
    [InlineData("", "")]
    public void Title_cases_english_names(string input, string expected)
    {
        Assert.Equal(expected, DisplayName.TitleCase(input));
    }
}
