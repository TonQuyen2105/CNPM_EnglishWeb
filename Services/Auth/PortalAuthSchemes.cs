namespace CNPM_EnglishLearning.Services.Auth
{
    /// <summary>
    /// Hằng số Authentication Schemes và tên Cookie độc lập cho 3 phân hệ Admin / Teacher / Student,
    /// cho phép đăng nhập và sử dụng đồng thời cả 3 vai trò trên 3 tab cùng một trình duyệt.
    /// </summary>
    public static class PortalAuthSchemes
    {
        public const string PortalSelector = "PortalSelector";

        public const string AdminCookie = "AdminCookie";
        public const string TeacherCookie = "TeacherCookie";
        public const string StudentCookie = "StudentCookie";

        public const string AdminCookieName = "EL.Admin.Auth";
        public const string TeacherCookieName = "EL.Teacher.Auth";
        public const string StudentCookieName = "EL.Student.Auth";

        public static string? GetSchemeForRole(string? role) => (role ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "admin" => AdminCookie,
            "teacher" => TeacherCookie,
            "student" => StudentCookie,
            _ => null
        };

        public static string? ResolveSchemeFromReferer(string? referer)
        {
            if (string.IsNullOrWhiteSpace(referer)) return null;
            if (referer.Contains("/Admin", StringComparison.OrdinalIgnoreCase)) return AdminCookie;
            if (referer.Contains("/Student", StringComparison.OrdinalIgnoreCase)) return StudentCookie;
            if (referer.Contains("/Teacher", StringComparison.OrdinalIgnoreCase)) return TeacherCookie;
            return null;
        }
    }
}
