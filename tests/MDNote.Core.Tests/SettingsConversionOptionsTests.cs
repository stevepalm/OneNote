using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests
{
    public class SettingsConversionOptionsTests
    {
        [Fact]
        public void ToConversionOptions_DefaultSettings_MapsCorrectly()
        {
            var settings = new Settings();
            var options = settings.ToConversionOptions();

            options.EnableSyntaxHighlighting.Should().BeTrue();
            options.EnableTableOfContents.Should().BeFalse();
            options.Theme.Should().Be("dark");
        }

        [Fact]
        public void ToConversionOptions_LightTheme_MapsCorrectly()
        {
            var settings = new Settings { Theme = "light" };
            var options = settings.ToConversionOptions();
            options.Theme.Should().Be("light");
        }

        [Fact]
        public void ToConversionOptions_TocEnabled_MapsCorrectly()
        {
            var settings = new Settings { EnableTableOfContents = true };
            var options = settings.ToConversionOptions();
            options.EnableTableOfContents.Should().BeTrue();
        }

        [Fact]
        public void ToConversionOptions_SyntaxHighlightingDisabled_MapsCorrectly()
        {
            var settings = new Settings { EnableSyntaxHighlighting = false };
            var options = settings.ToConversionOptions();
            options.EnableSyntaxHighlighting.Should().BeFalse();
        }
    }
}
