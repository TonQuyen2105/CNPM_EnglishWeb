using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services.Implementations;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStudentAuthService _studentAuthService;

        public AccountController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _studentAuthService = new StudentAuthService(_context);
        }

        // GET: /Account/Login hoặc /login hoặc /login.html
        [HttpGet("/Account/Login")]
        [HttpGet("/login")]
        [HttpGet("/login.html")]
        public IActionResult Login()
        {
            return View("~/login.html");
        }

        // POST: /Account/Login (Hỗ trợ cả Form Submit truyền thống và JSON body)
        [HttpPost("/Account/Login")]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost()
        {
            string? email = null;
            string? password = null;

            // 1. Nếu client submit Form chuẩn (application/x-www-form-urlencoded hoặc multipart/form-data)
            if (Request.HasFormContentType)
            {
                email = Request.Form["email"].FirstOrDefault();
                password = Request.Form["password"].FirstOrDefault();
            }
            // 2. Nếu client gửi JSON body (fetch API)
            else if (Request.ContentType?.Contains("application/json") == true)
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var json = System.Text.Json.JsonSerializer.Deserialize<StudentLoginRequestDto>(body, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        email = json?.Email;
                        password = json?.Password;
                    }
                }
                catch { }
            }

            // Fallback lấy từ Query nếu có
            if (string.IsNullOrWhiteSpace(email))
            {
                email = Request.Query["email"].FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                password = Request.Query["password"].FirstOrDefault();
            }

            var result = await _studentAuthService.LoginAsync(new StudentLoginRequestDto
            {
                Email = email ?? string.Empty,
                Password = password ?? string.Empty
            });

            // Nếu client gửi yêu cầu dưới dạng JSON / AJAX
            if (Request.Headers["Accept"].ToString().Contains("application/json") || 
                Request.ContentType?.Contains("application/json") == true)
            {
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, ApiResponse<object>.Fail(result.Message));
                }
                return Ok(ApiResponse<StudentLoginResponseDto>.Ok(result.Data!, result.Message));
            }

            // Nếu submit qua standard HTML form
            if (!result.IsSuccess)
            {
                ViewBag.Error = result.Message;
                return View("~/login.html");
            }

            return Redirect("/Areas/Student/Views/dashboard.html");
        }

        // API Endpoint tiện ích: POST /api/v1/auth/login
        [HttpPost("/api/v1/auth/login")]
        public async Task<IActionResult> ApiLogin([FromBody] StudentLoginRequestDto request)
        {
            var result = await _studentAuthService.LoginAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, ApiResponse<object>.Fail(result.Message));
            }
            return Ok(ApiResponse<StudentLoginResponseDto>.Ok(result.Data!, result.Message));
        }

        // GET: /Account/Logout
        [HttpGet("/Account/Logout")]
        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            return Redirect("/login.html");
        }

        // GET: /Account/Register hoặc /register hoặc /register.html
        [HttpGet("/Account/Register")]
        [HttpGet("/register")]
        [HttpGet("/register.html")]
        public IActionResult Register()
        {
            return View("~/register.html");
        }

        // POST: /Account/Register (Hỗ trợ cả Form Submit truyền thống và JSON body)
        [HttpPost("/Account/Register")]
        [ActionName("Register")]
        public async Task<IActionResult> RegisterPost()
        {
            StudentRegisterRequestDto dto = new();

            if (Request.HasFormContentType)
            {
                dto.FullName = Request.Form["full_name"].FirstOrDefault() ?? Request.Form["FullName"].FirstOrDefault() ?? string.Empty;
                dto.Email = Request.Form["email"].FirstOrDefault() ?? Request.Form["Email"].FirstOrDefault() ?? string.Empty;
                dto.PhoneNumber = Request.Form["phone_number"].FirstOrDefault() ?? Request.Form["PhoneNumber"].FirstOrDefault();
                dto.Password = Request.Form["password"].FirstOrDefault() ?? Request.Form["Password"].FirstOrDefault() ?? string.Empty;
                dto.ConfirmPassword = Request.Form["confirm_password"].FirstOrDefault() ?? Request.Form["ConfirmPassword"].FirstOrDefault() ?? string.Empty;
            }
            else if (Request.ContentType?.Contains("application/json") == true)
            {
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var json = System.Text.Json.JsonSerializer.Deserialize<StudentRegisterRequestDto>(body, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (json != null) dto = json;
                    }
                }
                catch { }
            }

            var result = await _studentAuthService.RegisterAsync(dto);

            if (Request.Headers["Accept"].ToString().Contains("application/json") || 
                Request.ContentType?.Contains("application/json") == true)
            {
                if (!result.IsSuccess)
                {
                    return StatusCode(result.StatusCode, ApiResponse<object>.Fail(result.Message));
                }
                return StatusCode(result.StatusCode, ApiResponse<StudentRegisterResponseDto>.Ok(result.Data!, result.Message));
            }

            if (!result.IsSuccess)
            {
                ViewBag.Error = result.Message;
                return View("~/register.html");
            }

            return Redirect("/Areas/Student/Views/dashboard.html");
        }
    }
}
