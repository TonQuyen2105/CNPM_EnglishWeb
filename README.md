# 📚 Hệ Thống Quản Lý Trung Tâm Tiếng Anh - EnglishLearning

Dự án Frontend được xây dựng hoàn toàn bằng **HTML5, CSS3 và JavaScript thuần (ES6 Modules)** theo kiến trúc **Single Page Application (SPA)** kết hợp mô hình **MVC (Model - View - Controller)** và hệ thống **Design System** hoàn chỉnh. Không sử dụng bất kỳ framework hay build tool nào, chạy trực tiếp trên trình duyệt hoặc VS Code Live Server.

Hệ thống bao gồm 3 phân hệ (cổng) độc lập:
1. 🛡️ **Cổng Quản Trị Viên (Admin Portal):** Quản lý đào tạo, tài khoản, lớp học, xếp thời khóa biểu, tài chính & báo cáo.
2. 👨‍🏫 **Cổng Giảng Viên (Teacher Portal):** Xem lịch dạy, soạn bài, quản lý lớp, ngân hàng câu hỏi, tạo đề và chấm thi tự luận.
3. 🎓 **Cổng Học Viên (Student Portal):** Khám phá khóa học, xem lịch học, học bài đa phương tiện, luyện thi trực tuyến, ôn từ vựng Spaced Repetition (SRS) lật thẻ 3D, theo dõi tiến độ và huy hiệu thành tích.

---

## 🗂️ CẤU TRÚC THƯ MỤC & CHÚ THÍCH TỪNG FILE

```text
CNPM_EnglishLearning/
├── 📄 index.html                       # Trang đăng nhập tập trung & điều hướng phân quyền (Auth Gateway)
├── 📄 styleguide.html                  # Thư viện Design System hiển thị trực quan toàn bộ UI components
├── 📄 .gitignore                       # Danh sách loại trừ các file rác khi đẩy code lên Git
├── 📄 README.md                        # Tài liệu hướng dẫn & chú thích chi tiết toàn bộ file trong dự án
│
├── 📂 admin/
│   └── 📄 index.html                   # Khung chứa SPA cho Cổng Quản Trị Viên (Admin Shell)
│
├── 📂 teacher/
│   └── 📄 index.html                   # Khung chứa SPA cho Cổng Giảng Viên (Teacher Shell)
│
├── 📂 student/
│   └── 📄 index.html                   # Khung chứa SPA cho Cổng Học Viên (Student Shell)
│
├── 📂 assets/
│   └── 📂 css/
│       ├── 📄 tokens.css               # Biến thiết kế (Design Tokens): màu sắc, font, spacing, shadow, bo góc
│       ├── 📄 base.css                 # Reset CSS và chuẩn hóa typography font Plus Jakarta Sans
│       ├── 📄 layout.css               # Khung bố cục: Sidebar, Topbar, Student Navbar, Grid 12 cột, Responsive
│       └── 📄 components.css           # Toàn bộ CSS linh kiện: Button, Card, Table, Modal, Badge, Thẻ 3D, Timer...
│
└── 📂 js/
    ├── 📂 apps/                        # Điểm khởi chạy (Bootstrap Entry Points) cho từng phân hệ
    │   ├── 📄 admin.app.js             # Khởi tạo Cổng Admin: Route Guard, Layout Shell, nạp 12 routes
    │   ├── 📄 teacher.app.js           # Khởi tạo Cổng Giảng viên: Route Guard, Layout Shell, nạp 9 routes
    │   └── 📄 student.app.js           # Khởi tạo Cổng Học viên: Route Guard, Top Navbar, nạp 11 routes
    │
    ├── 📂 core/                        # Nhân lõi của ứng dụng (Core Utilities & Services)
    │   ├── 📄 auth.js                  # Dịch vụ xác thực: quản lý session_user, đăng nhập, đăng xuất, Route Guards
    │   ├── 📄 router.js                # Bộ điều hướng SPA Hash-based (#/courses, #/classes/:id), dynamic params
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
    │   ├── 📂 shared/
    │   │   └── 📄 LayoutView.js        # Khung Shell: Sidebar/Topbar Admin, Sidebar/Topbar Teacher, Top Navbar Student
    │   │
    │   ├── 📂 admin/                   # 13 View của Cổng Admin
    │   │   ├── 📄 DashboardView.js     # Giao diện tổng quan: 4 stat card, biểu đồ cột & donut, danh sách duyệt
    │   │   ├── 📄 UsersView.js         # Bảng người dùng, bộ lọc role, switch khóa tài khoản
    │   │   ├── 📄 TeachersView.js      # Danh sách giảng viên, nút xác minh hồ sơ, lớp đang dạy
    │   │   ├── 📄 SubjectsView.js      # Danh mục môn học, số lượng khóa học trực thuộc
    │   │   ├── 📄 CoursesView.js       # Lưới khóa học dạng card, bộ lọc môn/level, học phí
    │   │   ├── 📄 CourseDetailView.js  # Chi tiết khóa học: Tab Thông tin | Bài học kéo thả | Lớp mở | Review
    │   │   ├── 📄 ClassesView.js       # Bảng lớp học toàn hệ thống, mã lớp, sĩ số, trạng thái 4 màu
    │   │   ├── 📄 ClassDetailView.js   # Chi tiết lớp học: Tab Giảng viên | Lịch học | Học viên | Buổi học
    │   │   ├── 📄 SchedulesView.js     # Thời khóa biểu dạng lưới ma trận Ca học x Thứ 2-CN
    │   │   ├── 📄 FacilitiesView.js    # Quản lý cơ sở vật chất (2 tab: Phòng học & Khung giờ)
    │   │   ├── 📄 EnrollmentsView.js   # Danh sách ghi danh học viên toàn trường, lọc lớp, doanh thu ước tính
    │   │   ├── 📄 ReviewsView.js       # Danh sách đánh giá khóa học, xếp hạng sao, xóa phản hồi xấu
    │   │   ├── 📄 ExamsView.js         # Quản lý ngân hàng đề thi chung toàn trung tâm
    │   │   └── 📄 ReportsView.js       # Báo cáo học vụ chuyên sâu & phân tích thống kê
    │   │
    │   ├── 📂 teacher/                 # 9 View của Cổng Giảng Viên
    │   │   ├── 📄 TeacherDashboardView.js # Bảng việc cần làm hôm nay, lịch dạy trong ngày, bài thi chờ chấm
    │   │   ├── 📄 TeacherClassesView.js   # Lớp đang giảng dạy, chi tiết lớp (tab học viên, buổi học, lịch)
    │   │   ├── 📄 TeacherScheduleView.js  # Lịch dạy tuần dạng thời khóa biểu lưới & xem tháng
    │   │   ├── 📄 TeacherLessonsView.js   # Soạn thảo nội dung bài giảng & xem trước (Preview)
    │   │   ├── 📄 TeacherQuestionsView.js # Ngân hàng câu hỏi động theo 4 loại định dạng câu hỏi
    │   │   ├── 📄 TeacherExamsView.js     # Đề thi của tôi & Trình dựng đề 2 cột thêm/bớt/đổi điểm câu hỏi
    │   │   ├── 📄 TeacherGradingView.js   # Màn hình chấm bài thi tự luận: câu trả lời, cho điểm, nhận xét mẫu
    │   │   ├── 📄 TeacherResultsView.js   # Bảng tổng hợp điểm số học viên theo từng lớp và đề thi
    │   │   └── 📄 TeacherProfileView.js   # Hồ sơ giảng viên, học vị, chứng chỉ và tiểu sử
    │   │
    │   └── 📂 student/                 # 11 View của Cổng Học Viên
    │       ├── 📄 StudentDashboardView.js   # Bảng học tập: chuỗi streak 🔥, buổi học sắp tới, bài học dở dang
    │       ├── 📄 StudentCoursesView.js     # Khám phá khóa học, tìm kiếm, lọc level, đăng ký lớp học mở
    │       ├── 📄 StudentMyClassesView.js   # Lớp của tôi (3 tab: Đang học, Hoàn thành, Đã hủy) & tiến độ
    │       ├── 📄 StudentScheduleView.js    # Thời khóa biểu cá nhân dạng lưới tuần và dạng danh sách (Agenda)
    │       ├── 📄 StudentLessonPlayerView.js# Trình phát bài học: từ vựng phát âm, ngữ pháp, audio nghe, làm quiz
    │       ├── 📄 StudentFlashcardsView.js  # Bộ thẻ từ vựng cá nhân & Màn hình ôn tập lật thẻ 3D (Spaced Repetition)
    │       ├── 📄 StudentExamsView.js       # Màn làm bài thi: đồng hồ đếm ngược, palette câu hỏi, tự lưu nháp
    │       ├── 📄 StudentResultsView.js     # Kết quả bài thi: điểm tổng, phân tích đúng/sai, nhận xét của giáo viên
    │       ├── 📄 StudentProgressView.js    # Tiến độ học: biểu đồ SVG điểm số, tỷ lệ hoàn thành, bộ 4 huy hiệu
    │       ├── 📄 StudentReviewsView.js     # Viết đánh giá khóa học: bộ chọn 1-5 sao tương tác & nhận xét
    │       └── 📄 StudentProfileView.js     # Hồ sơ học viên: sửa họ tên, xem trước avatar trực tiếp, đổi mật khẩu
    │
    └── 📂 controllers/                 # Lớp điều khiển logic nghiệp vụ (Tầng Controller trong MVC)
        ├── 📂 shared/
        │   └── 📄 BaseController.js    # Controller cơ sở cung cấp các hàm hỗ trợ chung
        │
        ├── 📂 admin/                   # 12 Controller của Cổng Admin
        │   ├── 📄 DashboardController.js    # Tổng hợp số liệu thống kê Dashboard, vẽ biểu đồ SVG
        │   ├── 📄 UsersController.js        # Thêm/sửa người dùng, khóa tài khoản, tạo profile giáo viên
        │   ├── 📄 TeachersController.js     # Xác minh hồ sơ giảng viên, xem lớp phụ trách
        │   ├── 📄 SubjectsController.js     # Thêm/sửa/xóa môn học
        │   ├── 📄 CoursesController.js      # Lọc khóa học, quản lý danh sách bài học kéo-thả thứ tự
        │   ├── 📄 ClassesController.js      # Tạo lớp học mới, phân công giảng viên, quản lý buổi học
        │   ├── 📄 SchedulesController.js    # Xếp thời khóa biểu, kiểm tra thuật toán chống trùng phòng/giờ GV
        │   ├── 📄 FacilitiesController.js   # Quản lý danh mục phòng học và khung giờ học
        │   ├── 📄 EnrollmentsController.js  # Quản lý danh sách đăng ký học, đổi trạng thái ghi danh
        │   ├── 📄 ReviewsController.js      # Duyệt và quản lý nhận xét của học viên
        │   ├── 📄 ExamsController.js        # Quản lý đề thi toàn hệ thống
        │   └── 📄 ReportsController.js      # Tính toán dữ liệu báo cáo và vẽ đồ thị học vụ
        │
        ├── 📂 teacher/                 # 9 Controller của Cổng Giảng Viên
        │   ├── 📄 TeacherDashboardController.js # Lọc bài thi chờ chấm, thống kê lớp phụ trách hôm nay
        │   ├── 📄 TeacherClassesController.js   # Quản lý danh sách học viên trong lớp, cập nhật buổi học
        │   ├── 📄 TeacherScheduleController.js  # Lọc lịch dạy tuần theo giáo viên đăng nhập
        │   ├── 📄 TeacherLessonsController.js   # Lưu trữ và cập nhật nội dung bài giảng
        │   ├── 📄 TeacherQuestionsController.js # Thao tác CRUD ngân hàng câu hỏi 4 định dạng
        │   ├── 📄 TeacherExamsController.js     # Điều phối trình dựng đề thi: thêm câu, tính tổng điểm
        │   ├── 📄 TeacherGradingController.js   # Chấm điểm bài thi tự luận, lưu nhận xét và tính lại tổng điểm
        │   ├── 📄 TeacherResultsController.js   # Xem bảng điểm và xuất kết quả học viên
        │   └── 📄 TeacherProfileController.js   # Cập nhật hồ sơ, chuyên môn của giáo viên
        │
        └── 📂 student/                 # 11 Controller của Cổng Học Viên
            ├── 📄 StudentDashboardController.js   # Tính chuỗi streak, tìm bài dở dang, đếm thẻ SRS đến hạn
            ├── 📄 StudentCoursesController.js     # Xử lý tìm kiếm, lọc khóa học, xử lý đăng ký lớp có kiểm tra sĩ số
            ├── 📄 StudentMyClassesController.js   # Điều hướng danh sách lớp đã ghi danh, xử lý hủy lớp
            ├── 📄 StudentScheduleController.js    # Lọc thời khóa biểu theo các lớp học viên đang học
            ├── 📄 StudentLessonPlayerController.js# Xử lý phát âm Web Speech API, tính điểm quiz, lưu tiến độ
            ├── 📄 StudentFlashcardsController.js  # CRUD thẻ từ vựng, tính toán chu kỳ ôn tập SRS (Quên/Khó/Được/Dễ)
            ├── 📄 StudentExamsController.js       # Đồng hồ đếm ngược, lưu nháp localStorage, tự động nộp bài
            ├── 📄 StudentResultsController.js     # Hiển thị phân tích bài làm chi tiết từng câu hỏi
            ├── 📄 StudentProgressController.js    # Tính toán tiến độ toàn khóa, đánh giá mở khóa huy hiệu
            ├── 📄 StudentReviewsController.js     # Gửi đánh giá sao và bình luận khóa học
            └── 📄 StudentProfileController.js     # Cập nhật họ tên, avatar URL và đổi mật khẩu học viên
```

---

## 🔑 TÀI KHOẢN MẪU DÙNG THỬ (DEMO ACCOUNTS)

Mật khẩu mặc định cho toàn bộ tài khoản mẫu là: **`123456`**

| Cổng phân hệ | Email đăng nhập | Họ và tên | Vai trò (Role) |
| :--- | :--- | :--- | :--- |
| 🛡️ **Quản Trị Viên (Admin)** | `admin@el.com` | Quản trị viên hệ thống | `admin` |
| 👨‍🏫 **Giảng Viên (Teacher)** | `teacher@el.com` | Trần Thị Mai | `teacher` |
| 🎓 **Học Viên (Student)** | `student@el.com` | Lê Quốc Bảo | `student` |

---

## 🚀 HƯỚNG DẪN CHẠY DỰ ÁN

1. Mở thư mục dự án bằng **Visual Studio Code**.
2. Nhấp chuột phải vào file [`index.html`](file:///Users/apple/Downloads/CNPM_EnglishLearning/index.html) -> Chọn **Open with Live Server** (hoặc mở trực tiếp trên trình duyệt hiện đại hỗ trợ ES6 Modules).
3. Tại trang đăng nhập:
   - Có thể chọn nhanh một trong 3 nút **Đăng nhập nhanh (Demo)** tương ứng với vai trò Admin, Giáo viên hoặc Học sinh.
   - Hệ thống sẽ tự động xác thực và chuyển hướng chính xác vào cổng phân hệ tương ứng.
