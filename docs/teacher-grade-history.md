# Lịch sử chấm và sửa điểm tự luận

## Cập nhật database

Chạy một lần trên database dùng chung, trước khi sử dụng chức năng lưu điểm mới:

```sh
dotnet run -- --migrate-grade-history
```

Hoặc chạy toàn bộ Database/20261122_grade_changes.sql trong Supabase SQL Editor.
Bảng grade_changes bật RLS, không cấp quyền đọc/ghi cho anon/authenticated qua
Supabase REST. Backend sử dụng kết nối PostgreSQL của ứng dụng.

## Giao diện

Mở Chấm bài → lớp → học viên → bài nộp. Bên dưới các câu có mục Lịch sử chấm
và sửa điểm tự luận, sắp xếp mới nhất trước, 50 mục mỗi lần và nút Xem thêm.
Mỗi mục tương ứng một câu thực sự thay đổi: điểm trước/sau, nhận xét trước/sau,
người chấm, thời gian Việt Nam, nội dung câu, điểm tối đa và lý do sửa.
Lần đầu được ghi là Chấm lần đầu. Lưu lại mà không thay đổi không tạo bản ghi.
Sau khi lưu, trang mở lại chính bài nộp để hiển thị lịch sử mới.

Sửa câu đã chấm cần lý do tối đa 1.000 ký tự. Chỉ sửa nhận xét cũng được ghi lại.
Hủy form, validation lỗi, xung đột phiên bản hoặc lớp đã hủy không ghi lịch sử.
Điểm và lịch sử được lưu cùng giao dịch PostgreSQL. Không có API sửa/xóa lịch sử.
Tên người chấm và nội dung câu được chụp lại tại thời điểm lưu.

Lịch sử chỉ bắt đầu từ khi triển khai; không thể suy ra những lần sửa trước đây.
Đây là lịch sử chấm tay tự luận, chưa phải nhật ký của worker chấm tự động.
Quyền xem kế thừa quyền xem bài nộp hiện có; lớp đã hủy vẫn có thể xem lịch sử.
Lỗi tải lịch sử có nút thử lại và không làm mất form đang nhập.

## Code

- Models/Entities/GradeChange.cs: model bản ghi lịch sử.
- Data/ApplicationDbContext.cs: DbSet GradeChanges.
- Areas/Teacher/Controllers/TeacherGradingController.cs: kiểm tra lý do, lưu
  snapshot trước khi đổi điểm; GET attempts/{id}/history có phân trang beforeId.
- wwwroot/js/teacher.js: nhập lý do, hiển thị lịch sử và mở lại bài sau khi lưu.
- Database/20261122_grade_changes.sql, Program.cs: tạo bảng và lệnh cập nhật.

## Lượt nộp cũ chỉ được xem

Trong cùng học viên và đề thi, chỉ lượt đã nộp có AttemptNumber lớn nhất (nếu
trùng thì Id lớn nhất) được chấm/sửa. Đây cũng là thứ tự chọn của sổ điểm.
Lượt làm lại chưa nộp không khóa lượt đã nộp trước đó. Lớp đã hủy vẫn khóa cả
lượt mới nhất. Lịch sử và nội dung lượt cũ vẫn đọc được, nhưng API lưu điểm trả
409 nếu đã có lượt nộp mới hơn. Service/worker tự động cũng bỏ qua lượt cũ.
Quy tắc dùng chung ở Services/Implementations/GradingAttemptPolicy.cs.
Không cần thay đổi schema cho quy tắc này.
