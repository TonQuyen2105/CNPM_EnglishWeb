# Bài giảng: video và PDF trên Supabase Storage

## Cấu hình một lần trên mỗi máy chạy backend

Đã cấu hình URL dự án và bucket `Lesson_media` (phân biệt hoa thường) trong
appsettings.json. Cần chạy chuyển liên kết lớp – bài giảng một lần theo mục bên dưới.

1. Giữ bucket Private. Backend dùng khóa phía server để upload và tạo URL xem
   sau khi kiểm tra quyền. Không cần mở policy upload/download cho tất cả mọi người.
2. Trong Supabase Project Settings → API Keys, lấy Secret key (`sb_secret_...`)
   hoặc legacy `service_role`. Không dùng publishable key hay anon key.
3. Tại thư mục project chạy:

   ```sh
   python3 scripts/configure-lesson-storage.py
   ```

   Dán khóa vào lời nhắc (không hiện ký tự). Script gửi JSON qua stdin tới
   dotnet user-secrets; khóa không nằm trong code, shell history hoặc tham số tiến trình.
4. Khởi động lại bằng `ASPNETCORE_ENVIRONMENT=Development dotnet run` để
   ASP.NET Core đọc User Secrets. Với Production, cấu hình biến môi trường
   `Supabase__SecretKey` bằng trình quản lý secrets của máy chủ.

Mỗi máy backend trong nhóm cần cấu hình khóa riêng và cùng database/Supabase.
Không gửi khóa vào frontend, chat, Git hoặc cho người dùng cuối. Nếu nhóm dùng
chung một backend được deploy, chỉ backend đó cần khóa.

## Sử dụng

Bài giảng → chọn lớp → Thêm bài giảng hoặc Sửa bài giảng. Chọn video MP4/WebM
(tối đa 50 MB) và PDF (tối đa 20 MB) ngay trong form. Nhấn Tạo/Lưu để gửi.
Không chọn file mới khi sửa sẽ giữ file cũ. Trang chi tiết chỉ cho xem video/PDF;
muốn thay phải bấm Sửa bài giảng. Giới hạn bucket/MIME trên Supabase cũng cần
cho phép `video/mp4`, `video/webm`, `application/pdf`.

Học viên mở `/Student/learn.html?lessonId=ID&classId=CLASS_ID`. Bạn phụ trách Student cần truyền
ID thật từ danh sách bài. Người học phải ghi danh active/completed trong lớp
không bị hủy; giáo viên phải được phân công đúng lớp của bài giảng.

## File và database

File mới được kiểm tra định dạng trong thư mục tạm App_Data/lesson-media, gửi
lên bucket, rồi xóa bản tạm. Thông tin tên file, MIME, kích thước, khóa và Bucket
được lưu trong `lessons.content` cùng nội dung chữ bằng LessonDocument.
Vẫn đọc được văn bản cũ và metadata cũ không có Bucket.

File cũ không có Bucket vẫn đọc từ máy đã upload. Muốn chia sẻ file cũ: mở Sửa
bài giảng trên máy đang có file, chọn lại video/PDF và lưu để đưa lên Supabase.
Không tự động di chuyển hay xóa các file cũ. File cloud được thay thế cũng được
giữ lại; chưa có tác vụ dọn các phiên bản không còn dùng.

POST `/api/teacher/lessons-workspace/with-media` tạo bài; PUT
`/api/teacher/lessons-workspace/{id}/with-media` sửa bài. Multipart gồm LessonInput,
video/pdf tùy chọn và header RequestVerificationToken. Metadata/nội dung lưu trong
cùng giao dịch PostgreSQL; object mới được cố gắng xóa nếu lưu thất bại. Storage và
PostgreSQL không có giao dịch phân tán, do đó sự cố mạng có thể để lại object thừa.

GET `/api/lessons/learn?lessonId=ID` trả dữ liệu bài và URL media của ứng dụng.
GET `/api/lessons/{id}/media/video` hoặc `/pdf` kiểm tra quyền, rồi chuyển tới
signed URL Supabase có hạn 1 giờ. URL có thể được dùng bởi bất kỳ ai có nó trong
thời gian hiệu lực; không chia sẻ URL ngoài lớp. Mở lại trang để lấy URL mới khi
hết hạn. Supabase phục vụ video/PDF trực tiếp cho trình duyệt, không phụ thuộc máy
đã upload. Khóa API không bao giờ được trả ra frontend.

## File code

- Services/Lessons/SupabaseLessonStorage.cs: upload, ký URL và dọn file lỗi.
- Services/Lessons/LessonFileStore.cs: kiểm tra định dạng, lưu tạm; đọc file cũ.
- Services/Lessons/LessonDocument.cs: metadata tương thích cũ, thêm Bucket.
- Areas/Teacher/Controllers/TeacherLessonsController.cs: lưu bài và file.
- Controllers/LessonLearningController.cs: kiểm tra quyền rồi cấp link xem.
- Program.cs, appsettings.json, CNPM_EnglishLearning.csproj: DI và cấu hình.
- scripts/configure-lesson-storage.py: nhập khóa vào User Secrets.
- wwwroot/js/teacher-lessons.js: form và giới hạn file.

Tài liệu API đối chiếu: https://supabase.com/docs/reference/self-hosting-storage
và https://supabase.com/docs/guides/getting-started/api-keys .

## Gán bài giảng cho lớp

Khi triển khai phiên bản này, dừng backend cũ và chạy migration trong Supabase SQL Editor
bằng file `Database/20261122_lesson_class_scope.sql`, hoặc dùng lệnh:

```sh
dotnet run -- --migrate-lesson-classes
```

Migration chuyển liên kết cũ từ lessons.class_id sang class_lessons trước khi bỏ cột.
Các buổi dạy hiện có giữ nguyên ngày, giáo viên và trạng thái. Một bài có nhiều
buổi trong cùng lớp xuất hiện một lần trong danh sách. Khi chỉ gán bài, ngày và lịch dạy để trống.

Tạo bài trong lớp sẽ lưu bài và liên kết trong cùng giao dịch. “Gán cho lớp”
dùng lại một bài cùng khóa học chưa gán lớp hoặc thuộc một lớp giáo viên phụ trách.
Các lớp dùng chung nội dung và tài liệu, còn thứ tự được lưu theo lớp ở class_lessons.order_index.
API học và API media kiểm tra quyền qua lớp được gán. Mục lục chỉ liệt kê bài của lớp đang mở.
