using System.Text.Json.Serialization;

namespace CNPM_EnglishLearning.Areas.Student.DTOs.Common
{
    /// <summary>
    /// Định dạng мета phân trang chuẩn cho các API danh sách.
    /// </summary>
    public class PaginationMeta
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages => Limit > 0 ? (int)Math.Ceiling((double)Total / Limit) : 0;

        public PaginationMeta() { }

        public PaginationMeta(int page, int limit, int total)
        {
            Page = page;
            Limit = limit;
            Total = total;
        }
    }

    /// <summary>
    /// Định dạng Response JSON chuẩn cho toàn bộ API Phân hệ Học sinh (/api/v1/student).
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu trả về trong trường data</typeparam>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Thành công";

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public T? Data { get; set; }

        [JsonPropertyName("meta")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMeta? Meta { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Thành công", PaginationMeta? meta = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = meta
            };
        }

        public static ApiResponse<T> Fail(string message, T? data = default)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data,
                Meta = null
            };
        }
    }

    /// <summary>
    /// Kết quả xử lý tầng Nghiệp vụ (Service Layer) theo chuẩn OOP, tách biệt logic với HTTP.
    /// </summary>
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; private init; }
        public int StatusCode { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public T? Data { get; private init; }
        public PaginationMeta? Meta { get; private init; }

        private ServiceResult() { }

        public static ServiceResult<T> Ok(T data, string message = "Thành công", PaginationMeta? meta = null) =>
            new()
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = message,
                Data = data,
                Meta = meta
            };

        public static ServiceResult<T> Created(T data, string message = "Khởi tạo thành công") =>
            new()
            {
                IsSuccess = true,
                StatusCode = 201,
                Message = message,
                Data = data
            };

        public static ServiceResult<T> BadRequest(string message) =>
            new()
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = message
            };

        public static ServiceResult<T> Unauthorized(string message = "Chưa xác thực hoặc phiên đăng nhập không hợp lệ.") =>
            new()
            {
                IsSuccess = false,
                StatusCode = 401,
                Message = message
            };

        public static ServiceResult<T> Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.") =>
            new()
            {
                IsSuccess = false,
                StatusCode = 403,
                Message = message
            };

        public static ServiceResult<T> NotFound(string message = "Không tìm thấy dữ liệu yêu cầu.") =>
            new()
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = message
            };

        public static ServiceResult<T> Conflict(string message) =>
            new()
            {
                IsSuccess = false,
                StatusCode = 409,
                Message = message
            };
    }
}
