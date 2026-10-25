# 📚 Hệ Thống Quản Lý Trung Tâm Tiếng Anh - EnglishLearning

Dự án được xây dựng theo kiến trúc **ASP.NET Core MVC Areas (.NET 9.0)** chuẩn công nghiệp:

---

## 🗂️ SƠ ĐỒ CẤU TRÚC THƯ MỤC CHUẨN

```text
CNPM_EnglishLearning/
📁 Areas/
│   ├── Admin/
│   │   ├── Controllers/        ← DashboardController.cs, UsersController.cs, AdminControllers.cs
│   │   ├── ViewModels/         ← AdminViewModels.cs
│   │   └── Views/              ← 14 file .html giao diện có sẵn mock data
│   ├── Teacher/
│   │   ├── Controllers/        ← TeacherControllers.cs (Dashboard, Classes, Grading...)
│   │   ├── ViewModels/         ← TeacherViewModels.cs
│   │   └── Views/              ← 10 file .html giao diện có sẵn mock data
│   └── Student/
│       ├── Controllers/        ← StudentControllers.cs (Dashboard, Courses, Flashcards...)
│       ├── ViewModels/         ← StudentViewModels.cs
│       └── Views/              ← 15 file .html giao diện có sẵn mock data
│
📁 Controllers/                 ← AccountController.cs (Login/Logout), HomeController.cs (Trang công khai)
📁 Models/
│   └── Entities/               ← 22 Entity Database dùng chung:
│       ├── User.cs
│       ├── Teacher.cs
│       ├── Subject.cs
│       ├── Course.cs
│       ├── CourseClass.cs
│       ├── Classroom.cs
│       ├── TimeSlot.cs
│       ├── ClassSchedule.cs
│       ├── ClassTeacher.cs
│       ├── ClassLesson.cs
│       ├── Enrollment.cs
│       ├── CourseReview.cs
│       ├── Exam.cs
│       ├── ExamQuestion.cs
│       ├── Lesson.cs
│       ├── Question.cs
│       ├── QuestionOption.cs
│       ├── QuestionAnswer.cs
│       ├── UserExam.cs
│       ├── UserExamAnswer.cs
│       ├── Flashcard.cs
│       └── UserProgress.cs
│
📁 Data/
│   ├── ApplicationDbContext.cs ← Quản lý truy xuất 22 bảng bằng Entity Framework
│   ├── DbInitializer.cs        ← Nạp dữ liệu seed ban đầu
│   ├── mockData.json           ← Dữ liệu JSON mô phỏng 22 bảng
│   └── seed.js                 ← Dữ liệu LocalStorage cho client-side
│
📁 Services/
│   ├── Interfaces/             ← IAuthService, ICourseService, IExamService
│   └── Implementations/        ← AuthService, CourseService, ExamService
│
📁 Migrations/                  ← Thư mục sẵn sàng cho Entity Framework Core Migrations
📁 Views/Shared/                ← _Layout.cshtml, Error.cshtml
📁 wwwroot/
│   ├── css/                    ← tokens.css, base.css, layout.css, components.css
│   ├── js/                     ← site.js, core/
│   ├── images/                 ← Thư mục hình ảnh static
│   └── lib/                    ← icons.js, charts.js, modal.js, toast.js...
│
📄 CNPM_EnglishLearning.csproj  ← Project file .NET 9.0 Web App
📄 appsettings.json             ← Chuỗi kết nối Database SQL Server & cấu hình
📄 Program.cs                   ← Khởi tạo WebApplication, DI Services & Area Routing
📄 index.html                   ← Trang Gateway đăng nhập (xem trực tiếp)
📄 styleguide.html              ← Thư viện UI Design System
```

---

## 🚀 HƯỚNG DẪN CHẠY DỰ ÁN

### 1. Chạy Backend bằng .NET:
```bash
dotnet run
```
Truy cập: `http://localhost:5000` (hoặc cổng HTTPS thông báo trên terminal).

### 2. Xem trực tiếp giao diện tĩnh (HTML Preview):
- Mở file `index.html` hoặc nhấp đúp vào bất kỳ file `.html` nào trong `Areas/{Admin, Teacher, Student}/Views/` để kiểm tra giao diện và dữ liệu mẫu bất cứ lúc nào.
