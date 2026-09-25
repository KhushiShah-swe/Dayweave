using System;
using Xunit;

namespace PersonalTimeline.Tests
{
    public class TimelineEntryTests
    {
        [Fact]
        public void TimelineEntry_ShouldSetDefaultDates_OnCreation()
        {
            // Arrange & Act
            var entry = new TimelineEntry();

            // Assert
            // Check that CreatedAt is not the default DateTime.MinValue
            Assert.NotEqual(DateTime.MinValue, entry.CreatedAt);
            Assert.NotEqual(DateTime.MinValue, entry.UpdatedAt);

            // Check that defaults are close to "Now" (within 1 second)
            Assert.True((DateTime.UtcNow - entry.CreatedAt).TotalSeconds < 1);
        }

        [Fact]
        public void TimelineEntry_ShouldAllowSettingProperties()
        {
            // Arrange
            var entry = new TimelineEntry
            {
                Title = "Test Title",
                SourceApi = "GitHub"
            };

            // Assert
            Assert.Equal("Test Title", entry.Title);
            Assert.Equal("GitHub", entry.SourceApi);
        }
    }
}
