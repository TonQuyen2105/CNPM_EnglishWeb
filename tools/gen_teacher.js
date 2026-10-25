import fs from 'fs';
import path from 'path';
import { wrapTeacherPage } from './teacher_views.js';

const outDir = path.resolve('areas/teacher/views');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

function writeView(fileName, title, content) {
  const fullHtml = wrapTeacherPage({ title, activePage: fileName, content });
  fs.writeFileSync(path.join(outDir, fileName), fullHtml, 'utf-8');
  console.log(`[Teacher] Generated ${fileName}`);
}

// 1. Dashboard
writeView('dashboard.html', 'Bảng điều khiển Giảng viên', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Chào mừng trở lại, Cô Lê Thị Mai!</h1>
      <p class="page-header-subtitle">Hôm nay bạn có 2 ca giảng dạy và 4 bài tập cần chấm điểm</p>
    </div>
    <div class="page-header-actions">
      <a href="grading.html" class="btn btn-primary">
        <span>Vào chấm bài (4)</span>
      </a>
    </div>
  </div>

  <!-- 4 Stat Cards -->
  <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Lớp đang phụ trách</span>
        <span class="stat-value">3</span>
        <span class="stat-sub text-success">33 học viên đang theo học</span>
      </div>
      <div class="stat-icon primary">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Ca dạy trong tuần</span>
        <span class="stat-value">6 buổi</span>
        <span class="stat-sub text-muted">Tổng 12 giờ giảng dạy</span>
      </div>
      <div class="stat-icon accent">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Bài nộp chờ chấm</span>
        <span class="stat-value text-warning">4</span>
        <span class="stat-sub text-warning">Hạn hoàn thành: Ngày mai</span>
      </div>
      <div class="stat-icon warning">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Đánh giá trung bình</span>
        <span class="stat-value text-success">4.9 / 5.0</span>
        <span class="stat-sub text-success">Từ 48 lượt phản hồi</span>
      </div>
      <div class="stat-icon success">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
      </div>
    </div>
  </div>

  <!-- Lịch dạy hôm nay & Hàng đợi chấm bài -->
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-7 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Ca dạy hôm nay (Thứ 6, 24/10/2026)</h3>
      <div style="display: flex; flex-direction: column; gap: 12px;">
        <div style="border-left: 4px solid var(--primary-600); background: var(--neutral-50); padding: 1rem; border-radius: 0 8px 8px 0; display: flex; justify-content: space-between; align-items: center;">
          <div>
            <span class="badge badge-primary">COMM-01</span>
            <strong style="margin-left: 6px; font-size: 0.9375rem;">Tiếng Anh Giao Tiếp K01</strong>
            <div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 4px;">Thời gian: 18:00 - 19:30 • Phòng LAB-01 (20 học viên)</div>
          </div>
          <a href="class-detail.html" class="btn btn-sm btn-primary">Vào lớp</a>
        </div>
      </div>
    </div>

    <div class="col-span-5 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Bài nộp mới nhất cần chấm</h3>
      <div style="display: flex; flex-direction: column; gap: 10px;">
        <div style="padding: 10px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
          <div>
            <strong>Trần Phương Nam</strong>
            <div style="font-size: 0.75rem; color: var(--text-muted);">IELTS Writing Task 2 • Nộp 2 giờ trước</div>
          </div>
          <a href="grading.html" class="btn btn-sm btn-secondary">Chấm bài</a>
        </div>
        <div style="padding: 10px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
          <div>
            <strong>Nguyễn Hoàng Anh</strong>
            <div style="font-size: 0.75rem; color: var(--text-muted);">Audio Speaking Part 2 • Nộp 4 giờ trước</div>
          </div>
          <a href="grading.html" class="btn btn-sm btn-secondary">Chấm bài</a>
        </div>
      </div>
    </div>
  </div>
`);

// 2. Classes View
writeView('classes.html', 'Lớp của tôi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Danh sách lớp giảng dạy</h1>
      <p class="page-header-subtitle">Các lớp học bạn đang trực tiếp phụ trách chuyên môn</p>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card card-hover" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.75rem;">
        <span class="badge badge-primary">COMM-01</span>
        <span class="badge badge-success">Đang diễn ra</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Giao Tiếp Cơ Bản K01</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Lớp học thực hành phát âm và hội thoại ngữ cảnh văn phòng.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted); border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 1rem;">
        <div>Lịch học: <strong>T2 - T4 - T6 (18:00 - 19:30)</strong></div>
        <div style="margin-top: 4px;">Sĩ số: <strong>18 / 20</strong> • Phòng: <strong>LAB-01</strong></div>
      </div>
      <div style="margin-top: 1.25rem;">
        <a href="class-detail.html" class="btn btn-sm btn-primary" style="width: 100%;">Chi tiết lớp & Điểm danh</a>
      </div>
    </div>

    <div class="card card-hover" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.75rem;">
        <span class="badge badge-primary">IELTS-B01</span>
        <span class="badge badge-success">Đang diễn ra</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Luyện Thi IELTS 6.5+ Sáng</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Chuyên sâu kỹ năng Writing Task 1-2 và Speaking Mock Test.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted); border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 1rem;">
        <div>Lịch học: <strong>T3 - T5 - T7 (08:30 - 10:30)</strong></div>
        <div style="margin-top: 4px;">Sĩ số: <strong>15 / 15</strong> • Phòng: <strong>A101</strong></div>
      </div>
      <div style="margin-top: 1.25rem;">
        <a href="class-detail.html" class="btn btn-sm btn-primary" style="width: 100%;">Chi tiết lớp & Điểm danh</a>
      </div>
    </div>
  </div>
`);

// 3. Class Detail View
writeView('class-detail.html', 'Chi tiết lớp giảng dạy', `
  <div class="page-header">
    <div>
      <a href="classes.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại danh sách lớp</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Lớp COMM-01: Giao Tiếp Cơ Bản K01</h1>
      <p class="page-header-subtitle">Phòng học: LAB-01 • Lịch: Thứ 2, 4, 6 (18:00 - 19:30)</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Điểm danh buổi hôm nay</button>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem; margin-bottom: 1.5rem;">
    <h3 class="card-title" style="margin-bottom: 1rem;">Danh sách 18 học viên & Bảng theo dõi</h3>
    <table class="table">
      <thead>
        <tr>
          <th>Học viên</th>
          <th>Email</th>
          <th>Chuyên cần</th>
          <th>Điểm nói</th>
          <th>Điểm viết</th>
          <th>Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>Nguyễn Hoàng Anh</strong></td>
          <td>student@el.com</td>
          <td><span class="badge badge-success">8/8 buổi (100%)</span></td>
          <td>8.5</td>
          <td>8.0</td>
          <td><button class="btn btn-sm btn-secondary">Nhận xét</button></td>
        </tr>
        <tr>
          <td><strong>Trần Phương Nam</strong></td>
          <td>nam.tran@el.com</td>
          <td><span class="badge badge-success">7/8 buổi (87%)</span></td>
          <td>8.0</td>
          <td>8.5</td>
          <td><button class="btn btn-sm btn-secondary">Nhận xét</button></td>
        </tr>
      </tbody>
    </table>
  </div>
`);

// 4. Schedule View
writeView('schedule.html', 'Lịch dạy trong tuần', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Lịch giảng dạy cá nhân</h1>
      <p class="page-header-subtitle">Thời khóa biểu chi tiết các ca đứng lớp của bạn</p>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
      <strong>Tuần hiện tại: 20/10/2026 - 26/10/2026</strong>
      <div style="display: flex; gap: 8px;">
        <button class="btn btn-sm btn-secondary">Tuần trước</button>
        <button class="btn btn-sm btn-primary">Tuần này</button>
        <button class="btn btn-sm btn-secondary">Tuần sau</button>
      </div>
    </div>

    <table class="table" style="text-align: center;">
      <thead>
        <tr>
          <th>Ca dạy</th>
          <th>Thứ 2</th>
          <th>Thứ 3</th>
          <th>Thứ 4</th>
          <th>Thứ 5</th>
          <th>Thứ 6</th>
          <th>Thứ 7</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>Sáng<br><small class="text-muted">08:30 - 10:30</small></strong></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101</small></div></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101</small></div></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101</small></div></td>
        </tr>
        <tr>
          <td><strong>Tối<br><small class="text-muted">18:00 - 19:30</small></strong></td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01</small></div></td>
          <td>—</td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01</small></div></td>
          <td>—</td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01</small></div></td>
          <td>—</td>
        </tr>
      </tbody>
    </table>
  </div>
`);

// 5. Lessons View
writeView('lessons.html', 'Soạn bài giảng', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý bài giảng & Giáo trình</h1>
      <p class="page-header-subtitle">Soạn nội dung giáo án, tài liệu PDF, bài tập thực hành theo từng buổi</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Thêm bài học mới</button>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <div style="display: flex; gap: 1rem; margin-bottom: 1.5rem;">
      <select class="form-control" style="max-width: 300px;">
        <option>Khóa học: Tiếng Anh Giao Tiếp Cơ Bản</option>
        <option>Khóa học: Luyện Thi IELTS 6.5+</option>
      </select>
    </div>

    <div style="display: flex; flex-direction: column; gap: 12px;">
      <div style="padding: 1rem; border: 1px solid var(--border-color); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
        <div>
          <span class="badge badge-primary">Bài 1</span>
          <strong style="margin-left: 8px;">Ngữ âm IPA: Nguyên âm đơn & Nguyên âm đôi</strong>
          <div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 4px;">Tài liệu: Slide_IPA_Lesson1.pdf • Video phát âm mẫu (45 phút)</div>
        </div>
        <button class="btn btn-sm btn-secondary">Chỉnh sửa</button>
      </div>

      <div style="padding: 1rem; border: 1px solid var(--border-color); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
        <div>
          <span class="badge badge-primary">Bài 2</span>
          <strong style="margin-left: 8px;">Kỹ năng tự giới thiệu bản thân & Nghề nghiệp</strong>
          <div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 4px;">Tài liệu: Dialogue_Workplace.pdf • Bài tập Audio nộp về nhà</div>
        </div>
        <button class="btn btn-sm btn-secondary">Chỉnh sửa</button>
      </div>
    </div>
  </div>
`);

// 6. Questions View
writeView('questions.html', 'Ngân hàng câu hỏi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Ngân hàng câu hỏi trắc nghiệm & tự luận</h1>
      <p class="page-header-subtitle">Hệ thống câu hỏi theo các chủ đề Ngữ pháp, Từ vựng, Đọc hiểu và Viết</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Tạo câu hỏi mới</button>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <div style="display: flex; flex-direction: column; gap: 16px;">
      <div style="padding: 1rem; background: var(--neutral-50); border-radius: 8px;">
        <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
          <span class="badge badge-info">IELTS Writing Task 2</span>
          <span class="badge badge-warning">Độ khó: Nâng cao</span>
        </div>
        <p style="font-size: 0.9375rem; font-weight: 600;">"Some people believe that unpaid community service should be a compulsory part of high school programmes. To what extent do you agree or disagree?"</p>
        <div style="font-size: 0.8125rem; color: var(--text-muted);">Dạng: Bài luận tự luận (Tối thiểu 250 từ)</div>
      </div>

      <div style="padding: 1rem; background: var(--neutral-50); border-radius: 8px;">
        <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
          <span class="badge badge-info">TOEIC Reading</span>
          <span class="badge badge-success">Độ khó: Trung bình</span>
        </div>
        <p style="font-size: 0.9375rem; font-weight: 600;">Ms. Watson requested that all expense reports be submitted _______ Friday afternoon.</p>
        <div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 4px;">A. by (Đúng) • B. during • C. at • D. until</div>
      </div>
    </div>
  </div>
`);

// 7. Exams View
writeView('exams.html', 'Đề thi của tôi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Đề thi & Kiểm tra lớp học</h1>
      <p class="page-header-subtitle">Các bài kiểm tra định kỳ bạn đã biên soạn cho học viên</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Tạo đề thi mới</button>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Tên đề thi</th>
            <th>Lớp áp dụng</th>
            <th>Thời lượng</th>
            <th>Số câu</th>
            <th>Đã nộp bài</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Kiểm tra giữa kỳ - Pronunciation & Listening</strong></td>
            <td>COMM-01</td>
            <td>45 phút</td>
            <td>30 câu</td>
            <td>18 / 18</td>
            <td><a href="grading.html" class="btn btn-sm btn-primary">Chấm điểm</a></td>
          </tr>
          <tr>
            <td><strong>Mock Test Writing Task 2</strong></td>
            <td>IELTS-B01</td>
            <td>40 phút</td>
            <td>1 bài luận</td>
            <td>15 / 15</td>
            <td><a href="grading.html" class="btn btn-sm btn-primary">Chấm điểm</a></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 8. Grading View
writeView('grading.html', 'Chấm bài tự luận', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Chấm bài tự luận & Nói</h1>
      <p class="page-header-subtitle">Đánh giá bài nộp của học viên theo tiêu chí chuẩn hóa</p>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-8 card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
        <div>
          <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Bài nộp: Trần Phương Nam</h3>
          <span style="font-size: 0.8125rem; color: var(--text-muted);">Đề bài: Unpaid community service for high school students</span>
        </div>
        <span class="badge badge-warning">Chờ chấm điểm</span>
      </div>

      <div style="background: var(--neutral-50); padding: 1.25rem; border-radius: 8px; font-size: 0.875rem; line-height: 1.7; color: var(--text-primary); font-family: monospace;">
        Nowadays, whether voluntary service should become compulsory for high school students is a matter of considerable debate. On the one hand, proponents argue that participating in social activities equips youngsters with vital interpersonal skills and a deep sense of civic responsibility...
      </div>
    </div>

    <div class="col-span-4 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Tiêu chí chấm điểm IELTS</h3>
      <div style="display: flex; flex-direction: column; gap: 12px;">
        <div>
          <label style="font-size: 0.8125rem; font-weight: 600;">Task Achievement (0 - 9.0)</label>
          <input type="number" class="form-control" value="7.5" step="0.5" min="0" max="9">
        </div>
        <div>
          <label style="font-size: 0.8125rem; font-weight: 600;">Coherence & Cohesion (0 - 9.0)</label>
          <input type="number" class="form-control" value="7.0" step="0.5" min="0" max="9">
        </div>
        <div>
          <label style="font-size: 0.8125rem; font-weight: 600;">Lexical Resource (0 - 9.0)</label>
          <input type="number" class="form-control" value="8.0" step="0.5" min="0" max="9">
        </div>
        <div>
          <label style="font-size: 0.8125rem; font-weight: 600;">Grammar Range & Accuracy</label>
          <input type="number" class="form-control" value="7.5" step="0.5" min="0" max="9">
        </div>
        <div>
          <label style="font-size: 0.8125rem; font-weight: 600;">Nhận xét của giáo viên</label>
          <textarea class="form-control" rows="3" placeholder="Nhận xét chi tiết điểm mạnh và lỗi cần sửa...">Bài viết rất mạch lạc, từ vựng học thuật phong phú. Chú ý cấu trúc câu phức ở đoạn 2.</textarea>
        </div>
        <button class="btn btn-primary" style="width: 100%; margin-top: 0.5rem;">Lưu kết quả & Gửi học sinh</button>
      </div>
    </div>
  </div>
`);

// 9. Results View
writeView('results.html', 'Sổ điểm học viên', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Bảng điểm & Kết quả học tập</h1>
      <p class="page-header-subtitle">Tổng hợp điểm chuyên cần, giữa kỳ và bài thi cuối khóa</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-secondary">Xuất bảng điểm Excel</button>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Học viên</th>
            <th>Lớp</th>
            <th>Chuyên cần</th>
            <th>Điểm bài tập</th>
            <th>Điểm giữa kỳ</th>
            <th>Điểm trung bình</th>
            <th>Xếp loại</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Nguyễn Hoàng Anh</strong></td>
            <td>COMM-01</td>
            <td>100%</td>
            <td>9.0</td>
            <td>8.5</td>
            <td><strong style="color: var(--primary-600); font-size: 1rem;">8.7</strong></td>
            <td><span class="badge badge-success">Giỏi</span></td>
          </tr>
          <tr>
            <td><strong>Trần Phương Nam</strong></td>
            <td>IELTS-B01</td>
            <td>95%</td>
            <td>8.5</td>
            <td>8.0</td>
            <td><strong style="color: var(--primary-600); font-size: 1rem;">8.2</strong></td>
            <td><span class="badge badge-success">Giỏi</span></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 10. Profile View
writeView('profile.html', 'Hồ sơ chuyên môn', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Hồ sơ giảng viên</h1>
      <p class="page-header-subtitle">Thông tin lý lịch, học vị, chứng chỉ và lịch tiếp sinh viên</p>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-4 card" style="padding: 1.5rem; text-align: center;">
      <div class="avatar avatar-lg" style="width: 90px; height: 90px; font-size: 2rem; background: #059669; color: white; margin: 0 auto 1rem auto;">LM</div>
      <h2 style="font-size: 1.25rem; font-weight: 700; margin: 0;">Lê Thị Mai, M.Ed</h2>
      <div style="font-size: 0.875rem; color: var(--text-muted); margin-top: 4px;">Giảng viên Chuyên Ngữ Anh</div>
      <div style="margin-top: 1rem;">
        <span class="badge badge-success">IELTS 8.5</span>
        <span class="badge badge-info" style="margin-left: 4px;">TESOL Certified</span>
      </div>
    </div>

    <div class="col-span-8 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Thông tin chi tiết</h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Email công tác</label>
          <div style="font-weight: 600;">teacher@el.com</div>
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Số điện thoại</label>
          <div style="font-weight: 600;">0912 345 678</div>
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Bằng cấp cao nhất</label>
          <div style="font-weight: 600;">Thạc sĩ Lý luận & Phương pháp giảng dạy tiếng Anh</div>
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Số năm kinh nghiệm</label>
          <div style="font-weight: 600;">7 năm</div>
        </div>
      </div>
    </div>
  </div>
`);

console.log('Finished generating Teacher views.');
