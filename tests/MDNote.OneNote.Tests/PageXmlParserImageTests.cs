using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class PageXmlParserImageTests
    {
        private const string Ns = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [Fact]
        public void GetImages_NoImages_ReturnsEmptyList()
        {
            var xml =
                $"<Page xmlns='{Ns}' ID='p1'>" +
                "<Outline objectID='o1'><OEChildren>" +
                "<OE><T><![CDATA[No images here]]></T></OE>" +
                "</OEChildren></Outline></Page>";

            var parser = new PageXmlParser(xml);
            parser.GetImages().Should().BeEmpty();
        }

        [Fact]
        public void GetImages_SingleImage_ReturnsCorrectData()
        {
            var testBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic
            var base64 = Convert.ToBase64String(testBytes);

            var xml =
                $"<Page xmlns='{Ns}' ID='p1'>" +
                $"<Outline objectID='o1'><OEChildren><OE>" +
                $"<Image objectID='img-abc' format='png'>" +
                $"<Data>{base64}</Data>" +
                "</Image>" +
                "</OE></OEChildren></Outline></Page>";

            var parser = new PageXmlParser(xml);
            var images = parser.GetImages();

            images.Should().HaveCount(1);
            images[0].Id.Should().Be("img-abc");
            images[0].FileName.Should().Be("image-0.png");
            images[0].Data.Should().BeEquivalentTo(testBytes);
        }

        [Fact]
        public void GetImages_MultipleImages_ReturnsAll()
        {
            var data1 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            var data2 = Convert.ToBase64String(new byte[] { 4, 5, 6 });

            var xml =
                $"<Page xmlns='{Ns}' ID='p1'>" +
                $"<Outline objectID='o1'><OEChildren><OE>" +
                $"<Image objectID='img-1' format='png'><Data>{data1}</Data></Image>" +
                $"<Image objectID='img-2' format='jpg'><Data>{data2}</Data></Image>" +
                "</OE></OEChildren></Outline></Page>";

            var parser = new PageXmlParser(xml);
            var images = parser.GetImages();

            images.Should().HaveCount(2);
            images[0].Id.Should().Be("img-1");
            images[0].FileName.Should().Be("image-0.png");
            images[1].Id.Should().Be("img-2");
            images[1].FileName.Should().Be("image-1.jpg");
        }

        [Fact]
        public void GetImages_ImageWithCallbackId_UsesAsFileName()
        {
            var data = Convert.ToBase64String(new byte[] { 1 });

            var xml =
                $"<Page xmlns='{Ns}' ID='p1'>" +
                $"<Outline objectID='o1'><OEChildren><OE>" +
                $"<Image objectID='img-1' format='png'>" +
                $"<CallbackID callbackID='C:\\Users\\test\\photo.png'/>" +
                $"<Data>{data}</Data>" +
                "</Image>" +
                "</OE></OEChildren></Outline></Page>";

            var parser = new PageXmlParser(xml);
            var images = parser.GetImages();

            images.Should().HaveCount(1);
            images[0].FileName.Should().Be("photo.png");
            images[0].OriginalReference.Should().Be("C:\\Users\\test\\photo.png");
        }

        [Fact]
        public void GetImages_ImageWithEmptyData_Skipped()
        {
            var xml =
                $"<Page xmlns='{Ns}' ID='p1'>" +
                $"<Outline objectID='o1'><OEChildren><OE>" +
                $"<Image objectID='img-1' format='png'>" +
                "<Data></Data>" +
                "</Image>" +
                "</OE></OEChildren></Outline></Page>";

            var parser = new PageXmlParser(xml);
            parser.GetImages().Should().BeEmpty();
        }
    }
}
