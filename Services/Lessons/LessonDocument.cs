using System.Text.Json;

namespace CNPM_EnglishLearning.Services.Lessons;

public record LessonAttachment(string Key, string Name, string ContentType, long Size, string? Bucket = null);
public record LessonDocument(string? Text, LessonAttachment? Video = null, LessonAttachment? Pdf = null)
{
    private const string Prefix = "EL_LESSON_MEDIA_V1\n";
    public static LessonDocument Read(string? content) => content?.StartsWith(Prefix, StringComparison.Ordinal) == true
        ? JsonSerializer.Deserialize<LessonDocument>(content[Prefix.Length..]) ?? new(Text: null) : new(content);
    public string Serialize() => Prefix + JsonSerializer.Serialize(this);
    public static object? View(int id, LessonAttachment? asset, string kind) => asset == null
        ? null : new { asset.Name, asset.Size, asset.ContentType, Url = $"/api/lessons/{id}/media/{kind}?v={asset.Key}" };
}
