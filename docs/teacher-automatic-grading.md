# Tích hợp chấm tự động với phần Student

## Phân công

Backend tự kiểm tra bài đã nộp mỗi 5 giây khi ứng dụng đang chạy. Bài còn câu tự luận chưa có điểm hiển thị “Chờ chấm tự luận”.

## Dữ liệu đầu vào

- `user_exams`: đúng `UserId`, `ExamId`, `AttemptNumber`, `SubmittedAt` khác null, `Status = "submitted"`.
- `user_exam_answers`: đúng **một dòng cho mỗi câu trong đề**, kể cả câu bỏ trống; không có câu ngoài đề hoặc dòng trùng.
- Trắc nghiệm một đáp án: `SelectedOptionId` là ID lựa chọn của chính câu đó; bỏ trống dùng null.
- Điền từ một ô nhập: `TextAnswer` là văn bản; bỏ trống dùng null hoặc chuỗi rỗng.
- Tự luận: `TextAnswer` chứa bài viết, `GradingStatus = "pending_review"`.
- Khi nộp, chưa tự đặt điểm: `ScoreEarned = null`, `TotalScore = null`. Với trắc nghiệm/điền từ, có thể dùng `GradingStatus = "auto_graded"` theo enum hiện có; điểm null vẫn được hệ thống coi là chưa chấm.
- Không nhận điểm, trạng thái chấm hoặc đáp án chuẩn từ trình duyệt. Student phải kiểm tra chủ bài, quyền làm đề, thời gian/lượt làm và khóa chỉnh sửa sau khi nộp.

## Quy tắc

- Điểm lấy từ `exam_questions.points`, không lấy `questions.default_points`.
- Trắc nghiệm cần >= 2 lựa chọn, đúng 1 lựa chọn `IsCorrect`; chọn đúng được đủ điểm, sai/bỏ trống được 0. Lựa chọn thuộc câu khác là lỗi dữ liệu, không âm thầm cho 0.
- Điền từ chấp nhận nhiều dòng `question_answers`. Cắt khoảng trắng đầu/cuối, chuẩn hóa Unicode NFC; phân biệt hoa/thường theo `IsCaseSensitive` của từng đáp án. Không bỏ dấu, sửa chính tả hoặc tự suy diễn gần đúng; khoảng trắng bên trong được giữ nguyên.
- Sai/bỏ trống được 0, đúng được đủ điểm. Bộ đáp án thiếu/rỗng làm cả lần chấm bị hủy.
- Câu tự luận đã chấm được giữ nguyên; câu chưa chấm được đặt chờ chấm, điểm null.
- Khi chấm đủ, lưu tổng điểm và trạng thái `passed`/`failed` theo `PassScore`. Nếu chưa đủ: tổng null, trạng thái bài `submitted`.
- `pending_auto` chỉ là trạng thái hiển thị API, không thêm giá trị enum database.
- Backend khóa lượt nộp khi chấm. Điểm khách quan đã chấm và điểm tự luận của giáo viên được giữ nguyên.
