using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests
{
    public class SettingsManagerTests
    {
        [Fact]
        public void ToJson_DefaultSettings_ContainsAllProperties()
        {
            var json = SettingsManager.ToJson(new Settings());

            json.Should().Contain("\"PasteMode\":1");
            json.Should().Contain("\"Theme\":\"dark\"");
            json.Should().Contain("\"EnableSyntaxHighlighting\":true");
            json.Should().Contain("\"EnableTableOfContents\":false");
            json.Should().Contain("\"FontFamily\":\"Calibri\"");
            json.Should().Contain("\"FontSize\":11");
            json.Should().Contain("\"LiveModeEnabled\":false");
            json.Should().Contain("\"LiveModeDelayMs\":1500");
            json.Should().Contain("\"DefaultExportPath\":\"\"");
            json.Should().Contain("\"IncludeImages\":true");
        }

        [Fact]
        public void FromJson_ValidJson_ParsesAllProperties()
        {
            var json = "{\"PasteMode\":2,\"Theme\":\"light\",\"EnableSyntaxHighlighting\":false,"
                     + "\"EnableTableOfContents\":true,\"FontFamily\":\"Consolas\","
                     + "\"FontSize\":14,\"LiveModeEnabled\":true,\"LiveModeDelayMs\":2000,"
                     + "\"DefaultExportPath\":\"C:\\\\docs\",\"IncludeImages\":false}";

            var s = SettingsManager.FromJson(json);

            s.PasteMode.Should().Be(PasteMode.Auto);
            s.Theme.Should().Be("light");
            s.EnableSyntaxHighlighting.Should().BeFalse();
            s.EnableTableOfContents.Should().BeTrue();
            s.FontFamily.Should().Be("Consolas");
            s.FontSize.Should().Be(14);
            s.LiveModeEnabled.Should().BeTrue();
            s.LiveModeDelayMs.Should().Be(2000);
            s.DefaultExportPath.Should().Be(@"C:\docs");
            s.IncludeImages.Should().BeFalse();
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsDefaults()
        {
            var s = SettingsManager.FromJson("{}");

            s.PasteMode.Should().Be(PasteMode.Prompt);
            s.Theme.Should().Be("dark");
            s.EnableSyntaxHighlighting.Should().BeTrue();
            s.EnableTableOfContents.Should().BeFalse();
            s.FontFamily.Should().Be("Calibri");
            s.FontSize.Should().Be(11);
            s.LiveModeEnabled.Should().BeFalse();
            s.LiveModeDelayMs.Should().Be(1500);
            s.DefaultExportPath.Should().Be("");
            s.IncludeImages.Should().BeTrue();
        }

        [Fact]
        public void FromJson_NullJson_ReturnsDefaults()
        {
            var s = SettingsManager.FromJson(null);
            s.PasteMode.Should().Be(PasteMode.Prompt);
            s.Theme.Should().Be("dark");
        }

        [Fact]
        public void FromJson_PartialJson_FillsMissingWithDefaults()
        {
            var json = "{\"Theme\":\"light\",\"FontSize\":16}";

            var s = SettingsManager.FromJson(json);

            s.Theme.Should().Be("light");
            s.FontSize.Should().Be(16);
            s.PasteMode.Should().Be(PasteMode.Prompt);
            s.EnableSyntaxHighlighting.Should().BeTrue();
            s.LiveModeDelayMs.Should().Be(1500);
            s.IncludeImages.Should().BeTrue();
        }

        [Fact]
        public void FromJson_InvalidPasteModeValue_ReturnsDefault()
        {
            var json = "{\"PasteMode\":99}";
            var s = SettingsManager.FromJson(json);
            s.PasteMode.Should().Be(PasteMode.Prompt);
        }

        [Fact]
        public void FromJson_BackslashInPath_ParsesCorrectly()
        {
            var json = "{\"DefaultExportPath\":\"C:\\\\Users\\\\test\\\\docs\"}";
            var s = SettingsManager.FromJson(json);
            s.DefaultExportPath.Should().Be(@"C:\Users\test\docs");
        }

        [Fact]
        public void FromJson_QuoteInFontFamily_ParsesCorrectly()
        {
            var json = "{\"FontFamily\":\"Segoe UI\"}";
            var s = SettingsManager.FromJson(json);
            s.FontFamily.Should().Be("Segoe UI");
        }

        [Fact]
        public void RoundTrip_AllProperties_PreservedCorrectly()
        {
            var original = new Settings
            {
                PasteMode = PasteMode.Auto,
                Theme = "light",
                EnableSyntaxHighlighting = false,
                EnableTableOfContents = true,
                FontFamily = "Consolas",
                FontSize = 14,
                LiveModeEnabled = true,
                LiveModeDelayMs = 2000,
                DefaultExportPath = @"C:\Users\test\docs",
                IncludeImages = false
            };

            var json = SettingsManager.ToJson(original);
            var parsed = SettingsManager.FromJson(json);

            parsed.PasteMode.Should().Be(PasteMode.Auto);
            parsed.Theme.Should().Be("light");
            parsed.EnableSyntaxHighlighting.Should().BeFalse();
            parsed.EnableTableOfContents.Should().BeTrue();
            parsed.FontFamily.Should().Be("Consolas");
            parsed.FontSize.Should().Be(14);
            parsed.LiveModeEnabled.Should().BeTrue();
            parsed.LiveModeDelayMs.Should().Be(2000);
            parsed.DefaultExportPath.Should().Be(@"C:\Users\test\docs");
            parsed.IncludeImages.Should().BeFalse();
        }

        [Fact]
        public void RoundTrip_DefaultSettings_IdenticalAfterRoundTrip()
        {
            var defaults = new Settings();
            var json = SettingsManager.ToJson(defaults);
            var parsed = SettingsManager.FromJson(json);

            parsed.PasteMode.Should().Be(defaults.PasteMode);
            parsed.Theme.Should().Be(defaults.Theme);
            parsed.EnableSyntaxHighlighting.Should().Be(defaults.EnableSyntaxHighlighting);
            parsed.EnableTableOfContents.Should().Be(defaults.EnableTableOfContents);
            parsed.FontFamily.Should().Be(defaults.FontFamily);
            parsed.FontSize.Should().Be(defaults.FontSize);
            parsed.LiveModeEnabled.Should().Be(defaults.LiveModeEnabled);
            parsed.LiveModeDelayMs.Should().Be(defaults.LiveModeDelayMs);
            parsed.DefaultExportPath.Should().Be(defaults.DefaultExportPath);
            parsed.IncludeImages.Should().Be(defaults.IncludeImages);
        }

        [Fact]
        public void FromJson_PasteModeOff_Parsed()
        {
            var json = "{\"PasteMode\":0}";
            var s = SettingsManager.FromJson(json);
            s.PasteMode.Should().Be(PasteMode.Off);
        }

        [Fact]
        public void ToJson_NullSettings_UsesDefaults()
        {
            var json = SettingsManager.ToJson(null);
            json.Should().Contain("\"PasteMode\":1");
            json.Should().Contain("\"Theme\":\"dark\"");
        }
    }
}
