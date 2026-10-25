# 📚 Hệ Thống Quản Lý Trung Tâm Tiếng Anh - EnglishLearning

Dự án được xây dựng theo kiến trúc **ASP.NET Core MVC Areas (.NET 9.0)** chuẩn hóa:
- **Backend:** 100% bằng **C# (.cs)** với đầy đủ Models (Entities, Validation Attributes), Controllers (kế thừa `Microsoft.AspNetCore.Mvc.Controller`, định tuyến `[Area]`), và Data Context (`ApplicationDbContext.cs`, `DbInitializer.cs`).
- **Frontend / Views:** Toàn bộ 39 trang trong `views/` là các file **`.html` thuần** đã tích hợp sẵn mã giao diện và **dữ liệu mẫu (dummy/mock data)** trực quan, có thể xem trực tiếp hoặc render qua C# Controller.
- **Tài nguyên tĩnh:** Nằm trong `wwwroot/` gồm CSS Design System, icons SVG, biểu đồ và kịch bản tương tác UI.

---

## 🗂️ SƠ ĐỒ CẤU TRÚC THƯ MỤC CHUẨN KIẾN TRÚC

```text
CNPM_EnglishLearning/
├── 📄 index.html                       # Trang đăng nhập tập trung, chọn cổng và điều hướng
├── 📄 styleguide.html                  # Thư viện Design System hiển thị trực quan toàn bộ UI components
├── 📄 .gitignore                       # Danh sách loại trừ khi đẩy code lên Git
├── 📄 README.md                        # Tài liệu hướng dẫn chi tiết dự án
│
├── 📂 areas/                           # PHÂN VÙNG HỆ THỐNG (AREAS)
│   ├── 📂 admin/                       # 1. CỔNG QUẢN TRỊ VIÊN (ADMIN AREA)
│   │   ├── 📂 models/                  # Các lớp thực thể quản trị: UserModel, CourseModel, v.v.
│   │   ├── 📂 controllers/             # Bộ điều phối nghiệp vụ: DashboardController, UsersController, v.v.
│   │   └── 📂 views/                   # 14 FILE HTML GIAO DIỆN CHỨA DỮ LIỆU ẢO MẪU:
│   │       ├── 📄 dashboard.html       # Bảng điều khiển: 4 stat card, biểu đồ cột & tròn, danh sách duyệt
│   │       ├── 📄 users.html           # Quản lý người dùng, lọc vai trò, bảng tài khoản, modal thêm/sửa
│   │       ├── 📄 teachers.html        # Quản lý đội ngũ giảng viên, bằng cấp, chứng chỉ, duyệt hồ sơ
│   │       ├── 📄 subjects.html        # Quản lý môn học đào tạo (Giao tiếp, TOEIC, IELTS, Thiếu nhi)
│   │       ├── 📄 courses.html         # Danh mục khóa học dạng lưới thẻ (Grid cards), học phí, cấp độ
│   │       ├── 📄 course-detail.html   # Chi tiết khóa học: mô tả, chuẩn đầu ra, đề cương bài học
│   │       ├── 📄 classes.html         # Danh sách lớp học toàn hệ thống, mã lớp, sĩ số, trạng thái
│   │       ├── 📄 class-detail.html    # Chi tiết lớp học: giảng viên, phòng học, danh sách học viên
│   │       ├── 📄 schedules.html       # Thời khóa biểu trung tâm dạng lưới ca học x Thứ 2-CN
│   │       ├── 📄 facilities.html      # Cơ sở vật chất & phòng học (LAB, lý thuyết, hội thảo)
│   │       ├── 📄 enrollments.html     # Quản lý ghi danh học viên, trạng thái thu học phí
│   │       ├── 📄 reviews.html         # Đánh giá & phản hồi từ học viên, kiểm duyệt đánh giá
│   │       ├── 📄 exams.html           # Ngân hàng đề thi và khảo thí toàn trường
│   │       └── 📄 reports.html         # Báo cáo doanh thu, tăng trưởng học viên và phân tích
│   │
│   ├── 📂 teacher/                     # 2. CỔNG GIẢNG VIÊN (TEACHER AREA)
│   │   ├── 📂 models/                  # Thực thể giảng viên: LessonModel, QuestionModel, v.v.
│   │   ├── 📂 controllers/             # Bộ điều phối giảng dạy: TeacherClassesController, v.v.
│   │   └── 📂 views/                   # 10 FILE HTML GIAO DIỆN CHỨA DỮ LIỆU ẢO MẪU:
│   │       ├── 📄 dashboard.html       # Bảng làm việc: ca dạy hôm nay, bài nộp cần chấm, thống kê
│   │       ├── 📄 classes.html         # Danh sách các lớp học phụ trách, sĩ số và lịch dạy
│   │       ├── 📄 class-detail.html    # Chi tiết lớp học: danh sách học viên, điểm danh buổi học
│   │       ├── 📄 schedule.html        # Thời khóa biểu giảng dạy cá nhân trong tuần
│   │       ├── 📄 lessons.html         # Soạn bài giảng, giáo án, tài liệu đính kèm từng buổi
│   │       ├── 📄 questions.html       # Ngân hàng câu hỏi trắc nghiệm & tự luận theo chủ đề
│   │       ├── 📄 exams.html           # Quản lý đề thi của lớp & giao bài kiểm tra
│   │       ├── 📄 grading.html         # Màn hình chấm bài tự luận: rubric tiêu chí, nhận xét giáo viên
│   │       ├── 📄 results.html         # Sổ điểm học viên, điểm giữa kỳ, cuối khóa và xếp loại
│   │       └── 📄 profile.html         # Hồ sơ chuyên môn giảng viên, học vị, chứng chỉ quốc tế
│   │
│   └── 📂 student/                     # 3. CỔNG HỌC VIÊN (STUDENT AREA)
│       ├── 📂 models/                  # Thực thể học viên: FlashcardModel, UserProgressModel, v.v.
│       ├── 📂 controllers/             # Bộ điều phối học tập: StudentCoursesController, v.v.
│       └── 📂 views/                   # 15 FILE HTML GIAO DIỆN CHỨA DỮ LIỆU ẢO MẪU:
│           ├── 📄 dashboard.html       # Trang chủ học tập: chuỗi streak 🔥, bài học dở dang, lối tắt
│           ├── 📄 courses.html         # Khám phá các khóa học tiếng Anh, lọc theo trình độ
│           ├── 📄 course-detail.html   # Chi tiết khóa học: giáo trình, học phí, đăng ký tư vấn
│           ├── 📄 my-classes.html      # Danh sách lớp học đang tham gia và tiến độ hoàn thành
│           ├── 📄 class-detail.html    # Trung tâm lớp học: lịch học tiếp theo, tài liệu buổi học
│           ├── 📄 schedule.html        # Thời khóa biểu cá nhân của học sinh
│           ├── 📄 learn.html           # Phòng học tương tác: video bài giảng, mẫu câu quan trọng
│           ├── 📄 flashcards.html      # Thẻ từ vựng Spaced Repetition (SRS) hiệu ứng lật thẻ 3D
│           ├── 📄 exams.html           # Danh sách bài thi trực tuyến & luyện tập tự do
│           ├── 📄 take-exam.html       # Phòng thi thời gian thực: đồng hồ đếm ngược, bảng câu hỏi
│           ├── 📄 results.html         # Lịch sử bài thi và điểm số đã đạt
│           ├── 📄 result-detail.html   # Xem chi tiết bài giải và lời giải thích từng câu
│           ├── 📄 progress.html        # Biểu đồ năng lực 4 kỹ năng Nghe - Nói - Đọc - Viết
│           ├── 📄 reviews.html         # Gửi đánh giá và phản hồi về lớp học/giáo viên
│           └── 📄 profile.html         # Hồ sơ cá nhân học viên, cập nhật thông tin
│
├── 📂 data/                            # CƠ SỞ DỮ LIỆU MẪU (DATABASE SEED & MOCK)
│   ├── 📄 seed.js                      # Mã nguồn khởi tạo cơ sở dữ liệu mô phỏng LocalStorage
│   └── 📄 mockData.json                # Toàn bộ dữ liệu 22 bảng ở định dạng JSON chuẩn
│
└── 📂 wwwroot/                         # TÀI NGUYÊN TĨNH DÙNG CHUNG (STATIC ASSETS)
    ├── 📂 css/                         # HỆ THỐNG DESIGN SYSTEM CSS THUẦN
    │   ├── 📄 tokens.css               # Biến Design Tokens: màu sắc, typography, spacing, radius
    │   ├── 📄 base.css                 # CSS Reset hiện đại, normalize, typography chuẩn
    │   ├── 📄 layout.css               # CSS Layout: app-layout, sidebar, topbar, grid 12 cột
    │   └── 📄 components.css           # 40+ UI components: Button, Card, Table, Badge, Modal, Form...
    ├── 📂 js/                          # JAVASCRIPT GIAO DIỆN & TIỆN ÍCH DÙNG CHUNG
    │   ├── 📄 site.js                  # Xử lý tương tác UI: mở/đóng sidebar mobile, dropdown, modal, tabs
    │   └── 📂 core/                    # Module lõi: storage.js, auth.js, eventBus.js, utils.js
    └── 📂 lib/                         # THƯ VIỆN UI ĐỘC LẬP
        ├── 📄 icons.js                 # Bộ thư viện 40+ icon SVG thuần
        ├── 📄 charts.js                # Bộ vẽ biểu đồ cột & donut bằng SVG thuần
        ├── 📄 modal.js                 # Hộp thoại Modal tương tác
        ├── 📄 toast.js                 # Thông báo Toast góc màn hình
        └── 📄 dataTable.js             # Bảng dữ liệu tương tác
```

---

## 🚀 HƯỚNG DẪN MỞ VÀ TRẢI NGHIỆM GIAO DIỆN

Bạn có thể xem trước giao diện theo 2 cách cực kỳ nhanh chóng:

### Cách 1: Mở trực tiếp bằng trình duyệt (Không cần server)
- Nhấp đúp chuột vào bất kỳ file `.html` nào trong thư mục `areas/{admin, teacher, student}/views/` (ví dụ: `areas/admin/views/dashboard.html` hoặc `areas/student/views/flashcards.html`).
- Giao diện sẽ hiển thị đầy đủ bảng dữ liệu, nút bấm, thẻ từ vựng 3D và bố cục chuyên nghiệp.
- Mọi liên kết thanh điều hướng (Sidebar / Navbar) đã được trỏ trực tiếp đến các file `.html` cùng cấp.

### Cách 2: Mở từ trang chủ `index.html`
- Mở file `index.html` ở thư mục gốc.
- Bạn có thể đăng nhập bằng các nút **1-Click Demo** (Admin / Teacher / Student) hoặc bấm trực tiếp vào các nút xem trước:
  - 👑 **Cổng Quản Trị:** `areas/admin/views/dashboard.html`
  - 👨‍🏫 **Cổng Giảng Viên:** `areas/teacher/views/dashboard.html`
  - 🎓 **Cổng Học Viên:** `areas/student/views/dashboard.html`

---

## 💡 ĐẶC ĐIỂM KỸ THUẬT NỔI BẬT

1. **Chuẩn kiến trúc ASP.NET Core MVC Areas:** Thư mục được tổ chức theo chuẩn công nghiệp, sẵn sàng để sau này gắn backend C# .NET, Node.js, Spring Boot hoặc PHP mà không cần thay đổi cấu trúc file.
2. **HTML Thuần & Dữ Liệu Ảo Trực Quan:** Mỗi file trong `views/` chứa toàn bộ thẻ HTML và dữ liệu mẫu phong phú (tên học viên, điểm số, ca học, tiền tệ VND...), dễ dàng xem, chỉnh sửa mã HTML và tham khảo giao diện tổng quan.
3. **Hiệu năng cao:** Không phụ thuộc thư viện bên ngoài (không dùng Bootstrap, jQuery, Tailwind hay CDN), tải trang nhanh tức thì và hoạt động hoàn hảo offline.
