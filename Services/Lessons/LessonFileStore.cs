namespace CNPM_EnglishLearning.Services.Lessons;

public sealed class LessonFileStore(IWebHostEnvironment environment)
{
    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "lesson-media");
    public string GetPath(string key)
    {
        var extension = Path.GetExtension(key);
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(key), "N", out _) || extension is not (".mp4" or ".webm" or ".pdf") || Path.GetFileName(key) != key)
            throw new InvalidDataException("Đường dẫn tài liệu không hợp lệ.");
        return Path.Combine(Root, key);
    }
    public async Task<LessonAttachment> SaveAsync(IFormFile file, string kind, CancellationToken ct)
    {
        var limit = kind == "video" ? 50L * 1024 * 1024 : 20L * 1024 * 1024;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > limit || (kind == "video" ? ext is not (".mp4" or ".webm") : ext != ".pdf"))
            throw new InvalidDataException("Chọn video MP4/WebM tối đa 50 MB hoặc tài liệu PDF tối đa 20 MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[12]; var count = 0;
        while (count < header.Length) { var read = await input.ReadAsync(header.AsMemory(count), ct); if (read == 0) break; count += read; }
        var valid = ext switch {
            ".pdf" => count >= 5 && System.Text.Encoding.ASCII.GetString(header, 0, 5) == "%PDF-",
            ".mp4" => count >= 12 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            ".webm" => count >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            _ => false
        };
        if (!valid) throw new InvalidDataException("Nội dung file không khớp định dạng video/PDF đã chọn.");
        Directory.CreateDirectory(Root);
        var key = Guid.NewGuid().ToString("N") + ext; var path = GetPath(key);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await output.WriteAsync(header.AsMemory(0, count), ct);
            var buffer = new byte[81920]; long size = count; int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                size += read;
                if (size > limit) throw new InvalidDataException("File vượt dung lượng cho phép.");
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            name = new string(name.Where(c => !char.IsControl(c)).Take(180).ToArray());
            return new(key, name, ext == ".pdf" ? "application/pdf" : ext == ".mp4" ? "video/mp4" : "video/webm", size);
        }
        catch { File.Delete(path); throw; }
    }
    public void Delete(string key) => File.Delete(GetPath(key));
}
