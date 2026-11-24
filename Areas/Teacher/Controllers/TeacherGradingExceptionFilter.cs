using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

// Keep database details in the server log, while giving the user a traceable error.
public sealed class TeacherGradingExceptionFilter(ILogger<TeacherGradingExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var http = context.HttpContext;
        logger.LogError(context.Exception,
            "Teacher grading failed. TraceId={TraceId}, Path={Path}, ClassId={ClassId}, ExamId={ExamId}",
            http.TraceIdentifier, http.Request.Path, http.Request.Query["classId"].ToString(), http.Request.Query["examId"].ToString());
        var unavailable = context.Exception is NpgsqlException { IsTransient: true } || context.Exception.GetBaseException() is System.Net.Sockets.SocketException;
        context.Result = new ObjectResult(new
        {
            message = unavailable ? "Chưa kết nối được database. Vui lòng thử lại."
                : "Máy chủ gặp lỗi khi xử lý dữ liệu chấm bài. Xem dòng 'Teacher grading failed' trong terminal.",
            traceId = http.TraceIdentifier
        }) { StatusCode = unavailable ? 503 : 500 };
        context.ExceptionHandled = true;
    }
}
