using PersonalTimeline.API.Services;
namespace PersonalTimeline.Tests;
public class EntryValidationTests
{
    [Fact]
    public void RejectsBlankTitleInvalidDateAndExecutableLinks()
    {
        var dto = new TimelineEntryDto { Title = " ", EntryType = "Unknown", ExternalUrl = "javascript:alert(1)" };
        var errors = EntryValidation.Validate(dto);
        Assert.Contains("title", errors.Keys); Assert.Contains("eventDate", errors.Keys);
        Assert.Contains("externalUrl", errors.Keys); Assert.Contains("entryType", errors.Keys);
    }
    [Fact]
    public void AcceptsAValidMomentAndSerializesDatabaseDatesAsUtc()
    {
        var dto = new TimelineEntryDto { Title = "A milestone", EventDate = DateTime.UtcNow, EntryType = "Memory", ExternalUrl = "https://example.com" };
        Assert.Empty(EntryValidation.Validate(dto));
        var entry = new TimelineEntry { SourceApi = "Manual" }; TimelineMapping.Apply(dto, entry);
        entry.EventDate = DateTime.SpecifyKind(entry.EventDate, DateTimeKind.Unspecified);
        Assert.Equal(DateTimeKind.Utc, TimelineMapping.ToDto(entry).EventDate.Kind);
    }
}
