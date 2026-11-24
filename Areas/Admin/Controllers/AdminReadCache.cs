using System.Threading;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    public sealed record CachedJsonResponse(byte[] Body, string ContentType);

    /// <summary>
    /// Bộ nhớ đệm ngắn hạn (60 giây) tự động vô hiệu hóa tức thì qua bộ đếm phiên bản (_version)
    /// ngay khi có bất kỳ thao tác ghi dữ liệu thành công nào trên hệ thống.
    /// </summary>
    public static class AdminReadCache
    {
        private static long _version = 1;
        public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(60);

        public static long CurrentVersion => Interlocked.Read(ref _version);

        public static void BumpVersion()
        {
            Interlocked.Increment(ref _version);
        }

        public static string BuildKey(string scope, params object?[] parts)
        {
            var v = CurrentVersion;
            if (parts == null || parts.Length == 0)
            {
                return $"admin:v{v}:{scope}";
            }

            return $"admin:v{v}:{scope}:{string.Join(":", parts.Select(p => p?.ToString() ?? "null"))}";
        }
    }
}
