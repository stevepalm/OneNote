using System;
using FluentAssertions;
using MDNote.OneNote;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class PageStateBackupTests : IDisposable
    {
        public PageStateBackupTests()
        {
            PageStateBackup.Reset();
        }

        public void Dispose()
        {
            PageStateBackup.Reset();
        }

        [Fact]
        public void Save_StoresSnapshot()
        {
            PageStateBackup.Save("page-1", "<xml>state1</xml>");

            PageStateBackup.GetSnapshotCount("page-1").Should().Be(1);
        }

        [Fact]
        public void PopLatest_ReturnsAndRemovesMostRecent()
        {
            PageStateBackup.Save("page-1", "<xml>state1</xml>");
            PageStateBackup.Save("page-1", "<xml>state2</xml>");

            var latest = PageStateBackup.PopLatest("page-1");

            latest.Should().Be("<xml>state2</xml>");
            PageStateBackup.GetSnapshotCount("page-1").Should().Be(1);
        }

        [Fact]
        public void MaxSnapshots_EvictsOldest()
        {
            PageStateBackup.Save("page-1", "<xml>state1</xml>");
            PageStateBackup.Save("page-1", "<xml>state2</xml>");
            PageStateBackup.Save("page-1", "<xml>state3</xml>");
            PageStateBackup.Save("page-1", "<xml>state4</xml>");

            PageStateBackup.GetSnapshotCount("page-1").Should().Be(3);

            var latest = PageStateBackup.PopLatest("page-1");
            latest.Should().Be("<xml>state4</xml>");

            var second = PageStateBackup.PopLatest("page-1");
            second.Should().Be("<xml>state3</xml>");

            var third = PageStateBackup.PopLatest("page-1");
            third.Should().Be("<xml>state2</xml>");

            // state1 was evicted
            PageStateBackup.PopLatest("page-1").Should().BeNull();
        }

        [Fact]
        public void PopLatest_EmptyReturnsNull()
        {
            PageStateBackup.PopLatest("nonexistent").Should().BeNull();
        }

        [Fact]
        public void Reset_ClearsAll()
        {
            PageStateBackup.Save("page-1", "<xml>state1</xml>");
            PageStateBackup.Save("page-2", "<xml>state2</xml>");

            PageStateBackup.Reset();

            PageStateBackup.GetSnapshotCount("page-1").Should().Be(0);
            PageStateBackup.GetSnapshotCount("page-2").Should().Be(0);
        }

        [Fact]
        public void MultiplePages_IndependentBuffers()
        {
            PageStateBackup.Save("page-1", "<xml>a</xml>");
            PageStateBackup.Save("page-2", "<xml>b</xml>");

            PageStateBackup.GetSnapshotCount("page-1").Should().Be(1);
            PageStateBackup.GetSnapshotCount("page-2").Should().Be(1);

            PageStateBackup.PopLatest("page-1").Should().Be("<xml>a</xml>");
            PageStateBackup.GetSnapshotCount("page-2").Should().Be(1);
        }

        [Fact]
        public void Save_NullPageId_NoOp()
        {
            PageStateBackup.Save(null, "<xml>state</xml>");
            PageStateBackup.Save("", "<xml>state</xml>");
            // Should not throw
        }

        [Fact]
        public void Save_NullXml_NoOp()
        {
            PageStateBackup.Save("page-1", null);
            PageStateBackup.Save("page-1", "");
            PageStateBackup.GetSnapshotCount("page-1").Should().Be(0);
        }
    }
}
