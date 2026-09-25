namespace PersonalTimeline.API.Services;

public static class EntryValidation
{
    public static Dictionary<string, string[]> Validate(TimelineEntryDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length > 160) errors["title"] = new[] { "A title of 1–160 characters is required." };
        if (dto.Description?.Length > 5000) errors["description"] = new[] { "Use at most 5,000 characters." };
        if (dto.Category?.Length > 60) errors["category"] = new[] { "Use at most 60 characters." };
        if (dto.EventDate == default || dto.EventDate.Kind != DateTimeKind.Utc) errors["eventDate"] = new[] { "A valid UTC date and time with a Z suffix is required." };
        if (!new[] { "Activity", "Achievement", "Milestone", "Memory" }.Contains(dto.EntryType)) errors["entryType"] = new[] { "Choose Activity, Achievement, Milestone, or Memory." };
        foreach (var (name, value) in new[] { ("externalUrl", dto.ExternalUrl), ("imageUrl", dto.ImageUrl) })
            if (!string.IsNullOrWhiteSpace(value) && (value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")))
                errors[name] = new[] { "Use an absolute HTTP(S) URL of at most 2,048 characters." };
        return errors;
    }
}
public static class TimelineMapping
{
    public static TimelineEntryDto ToDto(TimelineEntry entry) => new()
    {
        Id = entry.Id, Title = entry.Title, Description = entry.Description ?? "", Category = entry.Category ?? "",
        EventDate = DateTime.SpecifyKind(entry.EventDate, DateTimeKind.Utc), EntryType = entry.EntryType,
        ImageUrl = entry.ImageUrl, ExternalUrl = entry.ExternalUrl, SourceApi = entry.SourceApi ?? "Manual"
    };
    public static void Apply(TimelineEntryDto dto, TimelineEntry entry)
    {
        entry.Title = dto.Title.Trim(); entry.Description = dto.Description?.Trim(); entry.Category = dto.Category?.Trim();
        entry.EventDate = dto.EventDate; entry.EntryType = dto.EntryType;
        entry.ImageUrl = dto.ImageUrl; entry.ExternalUrl = dto.ExternalUrl; entry.UpdatedAt = DateTime.UtcNow;
    }
}
