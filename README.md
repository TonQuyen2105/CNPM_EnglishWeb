# 📚 Hệ Thống Quản Lý Trung Tâm Tiếng Anh - EnglishLearning

Dự án Frontend được xây dựng hoàn toàn bằng **HTML5, CSS3 và JavaScript thuần (ES6 Modules)** theo kiến trúc **Multi-Page Application (MPA)** kết hợp mô hình **MVC (Model - View - Controller)** và hệ thống **Design System** hoàn chỉnh. Không sử dụng bất kỳ framework hay build tool nào, chạy trực tiếp trên trình duyệt hoặc VS Code Live Server.

Mỗi trang trong hệ thống là một **tệp `.html` độc lập**, có thanh điều hướng riêng, liên kết trực tiếp giữa các file `.html` mà không phụ thuộc vào hash URL.

Hệ thống bao gồm 3 phân hệ (cổng) độc lập:
1. 🛡️ **Cổng Quản Trị Viên (Admin Portal - 15 file HTML):** Quản lý đào tạo, tài khoản, lớp học, xếp thời khóa biểu, tài chính & báo cáo.
2. 👨‍🏫 **Cổng Giảng Viên (Teacher Portal - 11 file HTML):** Xem lịch dạy, soạn bài, quản lý lớp, ngân hàng câu hỏi, tạo đề và chấm thi tự luận.
3. 🎓 **Cổng Học Viên (Student Portal - 16 file HTML):** Khám phá khóa học, xem lịch học, học bài đa phương tiện, luyện thi trực tuyến, ôn từ vựng Spaced Repetition (SRS) lật thẻ 3D, theo dõi tiến độ và huy hiệu thành tích.

---

## 🗂️ CẤU TRÚC THƯ MỤC & CHÚ THÍCH TỪNG FILE HTML

```text
CNPM_EnglishLearning/
├── 📄 index.html                       # Trang đăng nhập tập trung & điều hướng phân quyền (Auth Gateway)
├── 📄 styleguide.html                  # Thư viện Design System hiển thị trực quan toàn bộ UI components
├── 📄 .gitignore                       # Danh sách loại trừ các file rác khi đẩy code lên Git
├── 📄 README.md                        # Tài liệu hướng dẫn & chú thích chi tiết toàn bộ file trong dự án
│
├── 📂 admin/                           # 15 FILE HTML CỦA CỔNG QUẢN TRỊ VIÊN
│   ├── 📄 index.html                   # Chuyển hướng tự động vào dashboard.html
│   ├── 📄 dashboard.html               # Bảng điều khiển tổng quan: 4 stat card, biểu đồ cột & donut, danh sách duyệt
│   ├── 📄 users.html                   # Quản lý người dùng, lọc role, switch khóa tài khoản
│   ├── 📄 teachers.html                # Quản lý giảng viên, nút xác minh hồ sơ, lớp đang dạy
│   ├── 📄 subjects.html                # Quản lý môn học đào tạo
│   ├── 📄 courses.html                 # Danh sách khóa học dạng lưới card, lọc môn/level, học phí
│   ├── 📄 course-detail.html           # Chi tiết khóa học: Tab Thông tin | Bài học kéo thả | Lớp mở | Review
│   ├── 📄 classes.html                 # Danh sách lớp học toàn hệ thống, mã lớp, sĩ số, trạng thái 4 màu
│   ├── 📄 class-detail.html            # Chi tiết lớp học: Tab Giảng viên | Lịch học | Học viên | Buổi học
│   ├── 📄 schedules.html               # Thời khóa biểu dạng lưới ma trận Ca học x Thứ 2-CN
│   ├── 📄 facilities.html              # Quản lý cơ sở vật chất (2 tab: Phòng học & Khung giờ)
│   ├── 📄 enrollments.html             # Danh sách ghi danh học viên toàn trường, doanh thu ước tính
│   ├── 📄 reviews.html                 # Quản lý đánh giá khóa học, xếp hạng sao, duyệt nhận xét
│   ├── 📄 exams.html                   # Quản lý ngân hàng đề thi chung toàn trung tâm
│   └── 📄 reports.html                 # Báo cáo học vụ chuyên sâu & phân tích thống kê
│
├── 📂 teacher/                         # 11 FILE HTML CỦA CỔNG GIẢNG VIÊN
│   ├── 📄 index.html                   # Chuyển hướng tự động vào dashboard.html
│   ├── 📄 dashboard.html               # Bảng việc cần làm hôm nay, lịch dạy trong ngày, bài thi chờ chấm
│   ├── 📄 classes.html                 # Danh sách các lớp đang giảng dạy
│   ├── 📄 class-detail.html            # Chi tiết lớp học: danh sách học viên, tiến độ buổi học, lịch học
│   ├── 📄 schedule.html                # Lịch dạy tuần dạng thời khóa biểu lưới & xem tháng
│   ├── 📄 lessons.html                 # Soạn thảo nội dung bài giảng & xem trước (Preview)
│   ├── 📄 questions.html               # Ngân hàng câu hỏi động theo 4 loại định dạng câu hỏi
│   ├── 📄 exams.html                   # Đề thi của tôi & Trình dựng đề 2 cột thêm/bớt/đổi điểm câu hỏi
│   ├── 📄 grading.html                 # Màn hình chấm bài thi tự luận: câu trả lời, cho điểm, nhận xét mẫu
│   ├── 📄 results.html                 # Bảng tổng hợp điểm số học viên theo từng lớp và đề thi
│   └── 📄 profile.html                 # Hồ sơ giảng viên, học vị, chứng chỉ và tiểu sử
│
├── 📂 student/                         # 16 FILE HTML CỦA CỔNG HỌC VIÊN
│   ├── 📄 index.html                   # Chuyển hướng tự động vào dashboard.html
│   ├── 📄 dashboard.html               # Bảng học tập: chuỗi streak 🔥, buổi học sắp tới, bài học dở dang
│   ├── 📄 courses.html                 # Khám phá khóa học, tìm kiếm, lọc level, học phí
│   ├── 📄 course-detail.html           # Chi tiết khóa học: giáo trình, lớp mở, đăng ký lớp
│   ├── 📄 my-classes.html              # Lớp của tôi (3 tab: Đang học, Hoàn thành, Đã hủy) & tiến độ
│   ├── 📄 class-detail.html            # Chi tiết lớp học & trạng thái hoàn thành từng bài học
│   ├── 📄 schedule.html                # Thời khóa biểu cá nhân dạng lưới tuần và dạng danh sách (Agenda)
│   ├── 📄 learn.html                   # Trình phát bài học: từ vựng phát âm, ngữ pháp, audio nghe, làm quiz
│   ├── 📄 flashcards.html              # Thẻ từ vựng cá nhân & Màn hình ôn tập lật thẻ 3D (Spaced Repetition)
│   ├── 📄 exams.html                   # Danh sách đề thi thuộc các khóa đang học
│   ├── 📄 take-exam.html               # Màn làm bài thi: đồng hồ đếm ngược, palette câu hỏi, tự lưu nháp
│   ├── 📄 results.html                 # Lịch sử kết quả thi toàn bộ các lần làm bài
│   ├── 📄 result-detail.html           # Chi tiết bài thi: điểm tổng, phân tích đúng/sai, nhận xét của giáo viên
│   ├── 📄 progress.html                # Tiến độ học: biểu đồ SVG điểm số, tỷ lệ hoàn thành, bộ 4 huy hiệu
│   ├── 📄 reviews.html                 # Viết đánh giá khóa học: bộ chọn 1-5 sao tương tác & nhận xét
│   └── 📄 profile.html                 # Hồ sơ học viên: sửa họ tên, xem trước avatar trực tiếp, đổi mật khẩu
│
├── 📂 assets/
│   └── 📂 css/
│       ├── 📄 tokens.css               # Biến thiết kế (Design Tokens): màu sắc, font, spacing, shadow, bo góc
│       ├── 📄 base.css                 # Reset CSS và chuẩn hóa typography font Plus Jakarta Sans
│       ├── 📄 layout.css               # Khung bố cục: Sidebar, Topbar, Student Navbar, Grid 12 cột, Responsive
│       └── 📄 components.css           # Toàn bộ CSS linh kiện: Button, Card, Table, Modal, Badge, Thẻ 3D, Timer...
│
└── 📂 js/
    ├── 📂 core/                        # Nhân lõi của ứng dụng (Core Utilities & Services)
    │   ├── 📄 auth.js                  # Dịch vụ xác thực: quản lý session_user, đăng nhập, đăng xuất, Guards
    │   ├── 📄 mpa.js                   # Điều hướng MPA độc lập, tự động chuyển đổi link và kiểm tra quyền
    │   ├── 📄 router.js                # Bộ điều hướng SPA Hash-based (hỗ trợ chế độ SPA song song)
    │   ├── 📄 storage.js               # Lớp bọc LocalStorage an toàn (hỗ trợ get, set, getTable, setTable)
    │   ├── 📄 eventBus.js              # Cơ chế Event Emitter trung gian giúp các module giao tiếp phi tập trung
    │   └── 📄 utils.js                 # Thư viện hàm tiện ích: format tiền tệ VND, format ngày tháng, getInitials...
    │
    ├── 📂 data/                        # Dữ liệu khởi tạo & cấu trúc cơ sở dữ liệu mẫu
    │   └── 📄 seed.js                  # Khởi tạo mock data cho 18 bảng dữ liệu, tự động nâng cấp phiên bản DB (v2)
    │
    ├── 📂 ui/                          # Bộ linh kiện giao diện dùng chung (Reusable UI Components)
    │   ├── 📄 icons.js                 # Thư viện inline SVG chuẩn hóa (home, book, calendar, play, volume...)
    │   ├── 📄 modal.js                 # Popup Modal động đóng/mở linh hoạt kèm phím Esc và backdrop
    │   ├── 📄 toast.js                 # Thông báo nổi góc màn hình (Toast Notifications: success, error, info)
    │   ├── 📄 confirmDialog.js         # Hộp thoại xác nhận trước các hành động quan trọng (Xóa, Hủy, Nộp bài)
    │   ├── 📄 dataTable.js             # Bảng dữ liệu tự động tích hợp phân trang, sắp xếp và tìm kiếm
    │   ├── 📄 pagination.js            # Thanh phân trang chuẩn UI
    │   ├── 📄 emptyState.js            # Giao diện hiển thị khi danh sách trống dữ liệu
    │   ├── 📄 skeleton.js              # Hiệu ứng khung xương tải dữ liệu (Loading skeleton animation)
    │   └── 📄 charts.js                # Bộ vẽ biểu đồ bằng SVG thuần (Bar chart, Donut chart, Line chart)
    │
    ├── 📂 models/                      # Lớp thực thể & Truy xuất dữ liệu (Tầng Model trong MVC)
    │   ├── 📄 index.js                 # Tệp xuất tập trung (Export index) toàn bộ các Model
    │   ├── 📄 BaseModel.js             # Lớp cha CRUD trừu tượng (Điểm kết nối để thay API Backend sau này)
    │   ├── 📄 UserModel.js             # Quản lý tài khoản người dùng, đổi mật khẩu, phân quyền role
    │   ├── 📄 TeacherModel.js          # Quản lý hồ sơ giảng viên, học vị, kinh nghiệm, kiểm định is_verified
    │   ├── 📄 SubjectModel.js          # Quản lý môn học đào tạo (IELTS, TOEIC, Giao tiếp...)
    │   ├── 📄 CourseModel.js           # Quản lý khóa học, join môn học, giáo trình bài học, tính điểm sao review
    │   ├── 📄 LessonModel.js           # Quản lý bài giảng theo thứ tự order_index và content_type
    │   ├── 📄 CourseClassModel.js      # Quản lý lớp học mở, sĩ số tối đa, trạng thái lớp, join giảng viên
    │   ├── 📄 ClassModel.js            # Alias tương thích ngược cho CourseClassModel
    │   ├── 📄 ClassroomModel.js        # Quản lý phòng học cơ sở vật chất, sức chứa
    │   ├── 📄 TimeSlotModel.js         # Quản lý ca học / khung giờ học trong ngày
    │   ├── 📄 ClassScheduleModel.js    # Quản lý xếp thời khóa biểu, thuật toán cảnh báo trùng phòng / trùng giờ GV
    │   ├── 📄 ClassTeacherModel.js     # Quản lý phân công vai trò giảng viên chính (main) / trợ giảng (assistant)
    │   ├── 📄 ClassLessonModel.js      # Quản lý tiến trình từng buổi học theo lịch thực tế của lớp
    │   ├── 📄 EnrollmentModel.js       # Quản lý học viên đăng ký lớp, kiểm tra hết chỗ, hủy ghi danh, tính doanh thu
    │   ├── 📄 UserProgressModel.js     # Quản lý tiến độ học bài, bài học dở dang gần nhất, ghi nhận điểm số
    │   ├── 📄 CourseReviewModel.js     # Quản lý đánh giá và nhận xét 1-5 sao của học viên cho khóa học
    │   ├── 📄 FlashcardModel.js        # Quản lý bộ thẻ từ vựng cá nhân, thuật toán lặp lại ngắt quãng (SRS)
    │   ├── 📄 QuestionModel.js         # Quản lý ngân hàng câu hỏi (trắc nghiệm, điền từ, phát âm, tự luận)
    │   ├── 📄 ExamModel.js             # Quản lý đề thi, câu hỏi trong đề, điểm đạt pass_score, thời lượng thi
    │   └── 📄 UserExamModel.js         # Quản lý làm bài thi trực tuyến, chấm tự động, chấm tự luận & phản hồi GV
    │
    ├── 📂 views/                       # Lớp giao diện hiển thị HTML Template (Tầng View trong MVC)
    │   ├── 📂 shared/LayoutView.js     # Layout Shell dùng chung
    │   ├── 📂 admin/                   # 13 View của Cổng Admin
    │   ├── 📂 teacher/                 # 9 View của Cổng Giảng Viên
    │   └── 📂 student/                 # 11 View của Cổng Học Viên
    │
    └── 📂 controllers/                 # Lớp điều khiển logic nghiệp vụ (Tầng Controller trong MVC)
        ├── 📂 shared/BaseController.js
        ├── 📂 admin/                   # 12 Controller của Cổng Admin
        ├── 📂 teacher/                 # 9 Controller của Cổng Giảng Viên
        └── 📂 student/                 # 11 Controller của Cổng Học Viên
```

---

## 🔑 TÀI KHOẢN MẪU DÙNG THỬ (DEMO ACCOUNTS)

Mật khẩu mặc định cho toàn bộ tài khoản mẫu là: **`123456`**

| Cổng phân hệ | Email đăng nhập | Họ và tên | File trang chủ |
| :--- | :--- | :--- | :--- |
| 🛡️ **Quản Trị Viên (Admin)** | `admin@el.com` | Quản trị viên hệ thống | [`admin/dashboard.html`](admin/dashboard.html) |
| 👨‍🏫 **Giảng Viên (Teacher)** | `teacher@el.com` | Trần Thị Mai | [`teacher/dashboard.html`](teacher/dashboard.html) |
| 🎓 **Học Viên (Student)** | `student@el.com` | Lê Quốc Bảo | [`student/dashboard.html`](student/dashboard.html) |

---

## 🚀 HƯỚNG DẪN CHẠY DỰ ÁN

1. Mở thư mục dự án bằng **Visual Studio Code**.
2. Nhấp chuột phải vào file [`index.html`](index.html) -> Chọn **Open with Live Server**.
3. Bạn có thể bấm trực tiếp vào bất kỳ file `.html` nào (ví dụ: `admin/users.html`, `teacher/schedule.html`, `student/flashcards.html`) để xem và thao tác trực tiếp trên trang đó!
