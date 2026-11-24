using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CNPM_EnglishLearning.Services.Lessons;

// All privileged Storage requests stay on the server. Never return the API key to clients.
public sealed class SupabaseLessonStorage(LessonFileStore local, IConfiguration config,
    IHttpClientFactory clients, ILogger<SupabaseLessonStorage> logger)
{
    private (Uri Url, string Key, string Bucket) Settings()
    {
        var url = config["Supabase:Url"];
        var key = config["Supabase:SecretKey"];
        var bucket = config["Supabase:Bucket"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(bucket))
            throw new InvalidDataException("Chưa cấu hình Supabase Storage. Chạy scripts/configure-lesson-storage.py để nhập khóa phía server rồi khởi động lại ứng dụng.");
        return (new Uri(uri.GetLeftPart(UriPartial.Authority) + "/storage/v1/"), key, bucket);
    }
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var settings = Settings();
        var request = new HttpRequestMessage(method, new Uri(settings.Url, path));
        request.Headers.Add("apikey", settings.Key);
        // Legacy service_role JWT uses Bearer. New sb_secret keys use the apikey header only.
        if (!settings.Key.StartsWith("sb_secret_", StringComparison.Ordinal))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Key);
        return request;
    }
    private string ObjectPath(LessonAttachment asset)
    {
        _ = local.GetPath(asset.Key); // Validate generated object key, including legacy metadata.
        if (asset.Bucket != Settings().Bucket) throw new InvalidDataException("Bucket tài liệu không khớp cấu hình máy chủ.");
        return Uri.EscapeDataString(asset.Bucket!) + "/" + Uri.EscapeDataString(asset.Key);
    }
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            var response = await clients.CreateClient("LessonStorage").SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return response;
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new InvalidDataException(status switch {
                401 or 403 => "Supabase từ chối truy cập. Kiểm tra khóa Secret/service_role phía server và quyền Storage.",
                413 => "File vượt giới hạn bucket Supabase. Video tối đa 50 MB, PDF tối đa 20 MB.",
                404 => "Không tìm thấy bucket hoặc file trên Supabase. Kiểm tra tên Lesson_media (phân biệt hoa thường).",
                _ => $"Supabase Storage trả lỗi HTTP {status}. Kiểm tra bucket, giới hạn dung lượng và định dạng được phép rồi thử lại."
            });
        }
        catch (HttpRequestException) { throw new InvalidDataException("Không kết nối được Supabase Storage. Kiểm tra mạng rồi thử lại."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new InvalidDataException("Upload hoặc đọc Supabase quá thời gian. Vui lòng thử lại."); }
    }
    public async Task<LessonAttachment> SaveAsync(IFormFile file, string kind, CancellationToken ct)
    {
        var settings = Settings(); // Fail before staging when the server key is missing.
        var staged = await local.SaveAsync(file, kind, ct);
        var asset = staged with { Bucket = settings.Bucket };
        try
        {
            using var request = Request(HttpMethod.Post, "object/" + ObjectPath(asset));
            request.Content = new StreamContent(File.OpenRead(local.GetPath(staged.Key)));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(asset.ContentType);
            request.Headers.Add("x-upsert", "false");
            using var response = await SendAsync(request, ct);
            return asset;
        }
        catch { await DeleteAsync(asset); throw; }
        finally { local.Delete(staged.Key); }
    }
    public async Task<string> SignedUrlAsync(LessonAttachment asset, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Post, "object/sign/" + ObjectPath(asset));
        request.Content = JsonContent.Create(new { expiresIn = 3600 });
        using var response = await SendAsync(request, ct);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!json.RootElement.TryGetProperty("signedURL", out var property) || string.IsNullOrEmpty(property.GetString()))
            throw new InvalidDataException("Supabase không trả đường dẫn xem tài liệu hợp lệ.");
        var baseUri = Settings().Url;
        var value = property.GetString()!;
        var signed = new Uri(baseUri, value.StartsWith("/object/", StringComparison.Ordinal) ? value.TrimStart('/') : value);
        if (signed.Scheme != "https" || signed.Authority != baseUri.Authority || !signed.AbsolutePath.StartsWith("/storage/v1/object/sign/", StringComparison.Ordinal))
            throw new InvalidDataException("Đường dẫn xem tài liệu Supabase không hợp lệ.");
        return signed.AbsoluteUri;
    }
    // Cleanup failures must not hide the original save failure; retained objects can be cleaned later.
    public async Task DeleteAsync(LessonAttachment asset)
    {
        try
        {
            if (asset.Bucket == null) { local.Delete(asset.Key); return; }
            using var request = Request(HttpMethod.Delete, "object/" + ObjectPath(asset));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await SendAsync(request, timeout.Token);
        }
        catch { logger.LogWarning("Could not clean up unreferenced lesson object {ObjectKey}.", asset.Key); }
    }
}
