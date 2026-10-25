import fs from 'fs';
import path from 'path';
import { wrapStudentPage } from './student_views.js';

const outDir = path.resolve('areas/student/views');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

function writeView(fileName, title, content) {
  const fullHtml = wrapStudentPage({ title, activePage: fileName, content });
  fs.writeFileSync(path.join(outDir, fileName), fullHtml, 'utf-8');
  console.log(`[Student] Generated ${fileName}`);
}

// 1. Dashboard
writeView('dashboard.html', 'Trang chủ Học viên', `
  <div class="page-header" style="background: linear-gradient(135deg, #EFF6FF, #DBEAFE); padding: 1.5rem; border-radius: 12px; border: 1px solid #BFDBFE; margin-bottom: 2rem;">
    <div>
      <h1 class="page-header-title" style="color: #1E3A8A;">Chào Hoàng Anh, chúc bạn một ngày học tập hứng khởi! 🌟</h1>
      <p class="page-header-subtitle" style="color: #1E40AF;">Hôm nay bạn có ca học lúc 18:00 và 25 từ vựng cần ôn tập bằng Flashcards.</p>
    </div>
    <div class="page-header-actions">
      <a href="learn.html" class="btn btn-primary">
        <span>Tiếp tục bài học dở dang</span>
      </a>
    </div>
  </div>

  <!-- 4 Stat Cards -->
  <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Lớp đang học</span>
        <span class="stat-value">2</span>
        <span class="stat-sub text-success">COMM-01 & IELTS-B01</span>
      </div>
      <div class="stat-icon primary">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Từ vựng đã thuộc</span>
        <span class="stat-value text-success">180</span>
        <span class="stat-sub text-success">↗ +25 từ tuần này</span>
      </div>
      <div class="stat-icon accent">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="17 1 21 5 17 9"></polyline><path d="M3 11V9a4 4 0 0 1 4-4h14"></path><polyline points="7 23 3 19 7 15"></polyline><path d="M21 13v2a4 4 0 0 1-4 4H3"></path></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Điểm trung bình</span>
        <span class="stat-value">8.5</span>
        <span class="stat-sub text-success">Xếp loại: Giỏi</span>
      </div>
      <div class="stat-icon warning">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Tỷ lệ chuyên cần</span>
        <span class="stat-value text-success">100%</span>
        <span class="stat-sub text-success">Đã tham gia 8/8 buổi</span>
      </div>
      <div class="stat-icon success">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
      </div>
    </div>
  </div>

  <!-- Lớp học & Tiện ích ôn tập -->
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-8 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Khóa học đang diễn ra</h3>
      <div style="display: flex; flex-direction: column; gap: 16px;">
        <div style="border: 1px solid var(--border-color); border-radius: 8px; padding: 1.25rem;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
            <strong>Tiếng Anh Giao Tiếp Cơ Bản (COMM-01)</strong>
            <span class="badge badge-success">Đã hoàn thành 33%</span>
          </div>
          <p style="font-size: 0.8125rem; color: var(--text-muted); margin-bottom: 0.75rem;">Giáo viên: Cô Lê Thị Mai • Lịch: T2-T4-T6 18:00</p>
          <div class="progress" style="margin-bottom: 0.75rem;"><div class="progress-bar" style="width: 33%;"></div></div>
          <div style="display: flex; justify-content: space-between; align-items: center;">
            <span style="font-size: 0.8125rem;">Bài tiếp theo: <strong>Bài 9 - Ordering Food in a Restaurant</strong></span>
            <a href="learn.html" class="btn btn-sm btn-primary">Vào học ngay</a>
          </div>
        </div>
      </div>
    </div>

    <div class="col-span-4 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Lối tắt ôn tập</h3>
      <div style="display: flex; flex-direction: column; gap: 10px;">
        <a href="flashcards.html" class="btn btn-secondary" style="justify-content: flex-start; padding: 12px;">
          <span>🗂️ Ôn tập 25 Flashcards hôm nay</span>
        </a>
        <a href="exams.html" class="btn btn-secondary" style="justify-content: flex-start; padding: 12px;">
          <span>📝 Làm bài thi thử TOEIC Mini</span>
        </a>
        <a href="schedule.html" class="btn btn-secondary" style="justify-content: flex-start; padding: 12px;">
          <span>📅 Xem thời khóa biểu tuần</span>
        </a>
      </div>
    </div>
  </div>
`);

// 2. Courses View
writeView('courses.html', 'Khám phá khóa học', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Khám phá các khóa học tiếng Anh</h1>
      <p class="page-header-subtitle">Lộ trình học chuẩn quốc tế từ Giao tiếp cơ bản đến Luyện thi IELTS / TOEIC</p>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card card-hover" style="overflow: hidden;">
      <div style="height: 140px; background: linear-gradient(135deg, #1E3A8A, #3B82F6); color: white; padding: 1.5rem; display: flex; flex-direction: column; justify-content: space-between;">
        <span class="badge badge-success" style="align-self: flex-start;">Đang theo học</span>
        <h3 style="color: white; margin: 0; font-size: 1.125rem;">Tiếng Anh Giao Tiếp Cơ Bản</h3>
      </div>
      <div class="card-body">
        <span class="badge badge-info">Cơ bản (Beginner)</span>
        <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 0.5rem;">Xây dựng phát âm chuẩn IPA và 20 chủ đề giao tiếp quen thuộc.</p>
        <div style="display: flex; justify-content: space-between; margin-top: 1rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">
          <span style="font-size: 0.8125rem; color: var(--text-muted);">24 bài học</span>
          <strong style="color: var(--primary-600);">2.500.000 ₫</strong>
        </div>
      </div>
      <div class="card-footer">
        <a href="learn.html" class="btn btn-sm btn-primary" style="width: 100%;">Tiếp tục học</a>
      </div>
    </div>

    <div class="card card-hover" style="overflow: hidden;">
      <div style="height: 140px; background: linear-gradient(135deg, #065F46, #10B981); color: white; padding: 1.5rem; display: flex; flex-direction: column; justify-content: space-between;">
        <span class="badge badge-primary" style="align-self: flex-start;">Khóa đề xuất</span>
        <h3 style="color: white; margin: 0; font-size: 1.125rem;">Luyện Thi IELTS 6.5+</h3>
      </div>
      <div class="card-body">
        <span class="badge badge-warning">Nâng cao (Advanced)</span>
        <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 0.5rem;">Bứt phá điểm số Writing Task 2 & Speaking Mock Test 1-on-1.</p>
        <div style="display: flex; justify-content: space-between; margin-top: 1rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">
          <span style="font-size: 0.8125rem; color: var(--text-muted);">36 bài học</span>
          <strong style="color: var(--primary-600);">5.800.000 ₫</strong>
        </div>
      </div>
      <div class="card-footer">
        <a href="course-detail.html" class="btn btn-sm btn-secondary" style="width: 100%;">Xem chi tiết & Đăng ký</a>
      </div>
    </div>
  </div>
`);

// 3. Course Detail View
writeView('course-detail.html', 'Chi tiết khóa học', `
  <div class="page-header">
    <div>
      <a href="courses.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại danh mục</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Khóa học: Luyện Thi IELTS 6.5+</h1>
      <p class="page-header-subtitle">Cam kết đầu ra bằng văn bản • Giảng viên IELTS 8.5 trực tiếp phụ trách</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Đăng ký tư vấn khóa học</button>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-8 card" style="padding: 1.5rem;">
      <h3 class="card-title">Bạn sẽ học được những gì?</h3>
      <ul style="font-size: 0.875rem; color: var(--text-secondary); line-height: 1.7; margin: 1rem 0; padding-left: 1.25rem;">
        <li>Nắm trọn 10 dạng biểu đồ trong Writing Task 1 và dàn ý chuẩn band 7.0+</li>
        <li>Phương pháp trả lời Speaking Part 2 dài và tự nhiên, không bị ngắc ngứ</li>
        <li>Kỹ thuật Skimming & Scanning định vị từ khóa trong bài đọc 3 phần</li>
      </ul>
    </div>
    <div class="col-span-4 card" style="padding: 1.5rem;">
      <h3 class="card-title">Thông tin lớp</h3>
      <div style="display: flex; flex-direction: column; gap: 10px; margin-top: 1rem; font-size: 0.875rem;">
        <div>Học phí: <strong style="color: var(--primary-600); font-size: 1.125rem;">5.800.000 ₫</strong></div>
        <div>Thời lượng: <strong>36 buổi (72 giờ)</strong></div>
        <div>Sĩ số tối đa: <strong>15 học viên/lớp</strong></div>
      </div>
    </div>
  </div>
`);

// 4. My Classes View
writeView('my-classes.html', 'Lớp học của tôi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Lớp học tôi đang tham gia</h1>
      <p class="page-header-subtitle">Quản lý lớp học, theo dõi buổi học và trao đổi với giảng viên</p>
    </div>
  </div>

  <div class="grid grid-cols-2 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.75rem;">
        <span class="badge badge-primary">COMM-01</span>
        <span class="badge badge-success">Đang diễn ra</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Giao Tiếp Cơ Bản K01</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Giảng viên: Cô Lê Thị Mai • Phòng học: LAB-01</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted); border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 0.75rem;">
        Lịch: <strong>T2 - T4 - T6 (18:00 - 19:30)</strong>
      </div>
      <div style="display: flex; gap: 8px; margin-top: 1.25rem;">
        <a href="class-detail.html" class="btn btn-sm btn-primary">Vào lớp học</a>
        <a href="schedule.html" class="btn btn-sm btn-secondary">Xem lịch</a>
      </div>
    </div>
  </div>
`);

// 5. Class Detail View
writeView('class-detail.html', 'Trung tâm lớp học', `
  <div class="page-header">
    <div>
      <a href="my-classes.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại lớp của tôi</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Lớp COMM-01: Giao Tiếp Cơ Bản K01</h1>
      <p class="page-header-subtitle">Giáo viên: Lê Thị Mai • Bạn đã tham gia 8 / 24 buổi</p>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <h3 class="card-title" style="margin-bottom: 1rem;">Tài liệu và bài học buổi tiếp theo</h3>
    <div style="padding: 1rem; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between; align-items: center;">
      <div>
        <strong>Buổi 9: Thực hành gọi món tại nhà hàng (Dining Out)</strong>
        <div style="font-size: 0.8125rem; color: var(--text-muted);">Diễn ra lúc 18:00 Thứ 6, 24/10/2026 tại Phòng LAB-01</div>
      </div>
      <a href="learn.html" class="btn btn-primary">Mở tài liệu buổi 9</a>
    </div>
  </div>
`);

// 6. Schedule View
writeView('schedule.html', 'Thời khóa biểu học viên', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Thời khóa biểu học tập</h1>
      <p class="page-header-subtitle">Lịch các buổi học trực tiếp tại trung tâm trong tuần này</p>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <table class="table" style="text-align: center;">
      <thead>
        <tr>
          <th>Ca học</th>
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

// 7. Learn View
writeView('learn.html', 'Phòng học bài giảng', `
  <div class="page-header">
    <div>
      <a href="my-classes.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại lớp</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Bài 9: Ordering Food in a Restaurant</h1>
      <p class="page-header-subtitle">Khóa: Tiếng Anh Giao Tiếp Cơ Bản • Giáo viên: Lê Thị Mai</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-success">✓ Đánh dấu hoàn thành bài này</button>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-8">
      <div class="card" style="padding: 0; overflow: hidden; margin-bottom: 1.5rem;">
        <div style="background: #0F172A; height: 360px; display: flex; flex-direction: column; align-items: center; justify-content: center; color: white;">
          <div style="width: 64px; height: 64px; border-radius: 50%; background: var(--primary-600); display: flex; align-items: center; justify-content: center; cursor: pointer;">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="white"><polygon points="5 3 19 12 5 21 5 3"></polygon></svg>
          </div>
          <span style="margin-top: 12px; font-size: 0.875rem; color: #94A3B8;">Video bài giảng: Restaurant Dialogues & Pronunciation (22:45)</span>
        </div>
      </div>

      <div class="card" style="padding: 1.5rem;">
        <h3 class="card-title">Mẫu câu quan trọng trong bài</h3>
        <ul style="font-size: 0.875rem; line-height: 1.8; color: var(--text-primary); margin-top: 0.5rem;">
          <li><strong>"Could we have a table for two, please?"</strong> - Cho chúng tôi xin bàn 2 người.</li>
          <li><strong>"What do you recommend for the main course?"</strong> - Bạn gợi ý món chính nào?</li>
          <li><strong>"Could we get the bill, please?"</strong> - Làm ơn tính tiền giúp chúng tôi.</li>
        </ul>
      </div>
    </div>

    <div class="col-span-4 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Mục lục bài học</h3>
      <div style="display: flex; flex-direction: column; gap: 8px; font-size: 0.8125rem;">
        <div style="padding: 8px; background: #ECFDF5; border-radius: 6px; color: #047857;">✓ Bài 7: Directions in Town</div>
        <div style="padding: 8px; background: #ECFDF5; border-radius: 6px; color: #047857;">✓ Bài 8: Shopping & Prices</div>
        <div style="padding: 8px; background: #EFF6FF; border-radius: 6px; color: #1D4ED8; font-weight: 700;">▶ Bài 9: Ordering Food (Hiện tại)</div>
        <div style="padding: 8px; background: var(--neutral-50); border-radius: 6px;">Bài 10: Hotel Check-in</div>
      </div>
    </div>
  </div>
`);

// 8. Flashcards View
writeView('flashcards.html', 'Học từ vựng Flashcards', `
  <style>
    .flashcard-3d-wrapper {
      perspective: 1000px; max-width: 540px; margin: 0 auto; height: 320px;
    }
    .flashcard-inner {
      position: relative; width: 100%; height: 100%; text-align: center;
      transition: transform 0.6s cubic-bezier(0.4, 0, 0.2, 1);
      transform-style: preserve-3d; cursor: pointer;
    }
    .flashcard-inner.flipped {
      transform: rotateY(180deg);
    }
    .flashcard-front, .flashcard-back {
      position: absolute; width: 100%; height: 100%;
      backface-visibility: hidden; border-radius: 16px;
      padding: 2.5rem; display: flex; flex-direction: column;
      justify-content: space-between; align-items: center;
      box-shadow: 0 10px 25px -5px rgba(0,0,0,0.1);
      border: 1px solid var(--border-color);
    }
    .flashcard-front {
      background: white; color: var(--text-primary);
    }
    .flashcard-back {
      background: linear-gradient(145deg, #1E3A8A, #2563EB);
      color: white; transform: rotateY(180deg);
    }
  </style>

  <div class="page-header" style="text-align: center;">
    <div>
      <h1 class="page-header-title">Thẻ từ vựng thông minh (Spaced Repetition)</h1>
      <p class="page-header-subtitle">Nhấp vào thẻ để lật xem định nghĩa, phát âm và câu ví dụ</p>
    </div>
  </div>

  <div style="max-width: 540px; margin: 0 auto 1.5rem auto; display: flex; justify-content: space-between; align-items: center; font-size: 0.875rem;">
    <span>Bộ thẻ: <strong>TOEIC 600 Essential Words</strong></span>
    <span>Thẻ <strong>14 / 50</strong></span>
  </div>

  <!-- 3D Flashcard -->
  <div class="flashcard-3d-wrapper">
    <div class="flashcard-inner" id="flashcard-flipper">
      <!-- Front -->
      <div class="flashcard-front">
        <span class="badge badge-primary">Từ vựng giao tiếp</span>
        <div>
          <h2 style="font-size: 2.25rem; font-weight: 800; color: var(--primary-700); margin: 0;">Negotiation</h2>
          <div style="font-size: 1.125rem; color: var(--text-muted); margin-top: 6px;">/nɪˌɡoʊ.ʃiˈeɪ.ʃən/</div>
        </div>
        <span style="font-size: 0.8125rem; color: var(--text-muted);">👉 Bấm để lật thẻ</span>
      </div>

      <!-- Back -->
      <div class="flashcard-back">
        <span class="badge badge-neutral" style="background: rgba(255,255,255,0.2); color: white;">Danh từ (Noun)</span>
        <div>
          <div style="font-size: 1.5rem; font-weight: 700; margin-bottom: 0.5rem;">Sự đàm phán, thương lượng</div>
          <p style="font-size: 0.9375rem; font-style: italic; opacity: 0.9;">"The agreement was reached after months of tough negotiations."</p>
        </div>
        <span style="font-size: 0.8125rem; opacity: 0.8;">👉 Bấm để quay lại</span>
      </div>
    </div>
  </div>

  <!-- Action Controls -->
  <div style="max-width: 540px; margin: 2rem auto; display: flex; justify-content: center; gap: 1rem;">
    <button class="btn btn-danger" style="flex: 1;">❌ Chưa thuộc (Ôn lại sau 10p)</button>
    <button class="btn btn-secondary" id="btn-flip-card">🔄 Lật thẻ</button>
    <button class="btn btn-success" style="flex: 1;">✓ Đã thuộc (Ghi nhớ)</button>
  </div>
`);

// 9. Exams View
writeView('exams.html', 'Luyện thi trực tuyến', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Phòng luyện thi & Khảo thí</h1>
      <p class="page-header-subtitle">Hệ thống bài thi đánh giá năng lực theo chuẩn TOEIC và IELTS</p>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <span class="badge badge-success" style="margin-bottom: 0.75rem;">Sẵn sàng thi</span>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Kiểm tra định kỳ giữa khóa COMM</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">40 câu trắc nghiệm Nghe & Ngữ pháp trong 45 phút.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted); margin-bottom: 1rem;">Thời gian: <strong>45 phút</strong> • 40 câu hỏi</div>
      <a href="take-exam.html" class="btn btn-primary" style="width: 100%;">Bắt đầu làm bài thi</a>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <span class="badge badge-primary" style="margin-bottom: 0.75rem;">Luyện tập tự do</span>
      <h3 style="font-size: 1.125rem; font-weight: 700;">TOEIC Listening Part 1 & 2</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">30 câu nghe tranh và câu hỏi - đáp nhanh.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted); margin-bottom: 1rem;">Thời gian: <strong>25 phút</strong> • 30 câu hỏi</div>
      <a href="take-exam.html" class="btn btn-secondary" style="width: 100%;">Luyện tập ngay</a>
    </div>
  </div>
`);

// 10. Take Exam View
writeView('take-exam.html', 'Phòng làm bài thi', `
  <div class="page-header" style="background: white; border-bottom: 2px solid var(--primary-600); padding: 1rem 1.5rem; position: sticky; top: 64px; z-index: 10;">
    <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
      <div>
        <h2 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Kiểm tra định kỳ giữa khóa COMM</h2>
        <span style="font-size: 0.8125rem; color: var(--text-muted);">Đã làm: 12 / 40 câu</span>
      </div>
      <div style="display: flex; align-items: center; gap: 1rem;">
        <div style="font-size: 1.25rem; font-weight: 800; color: #DC2626; background: #FEF2F2; padding: 6px 14px; border-radius: 8px; border: 1px solid #FCA5A5;">
          ⏱ 34:25
        </div>
        <a href="results.html" class="btn btn-primary">Nộp bài thi</a>
      </div>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6" style="margin-top: 1.5rem;">
    <!-- Nội dung câu hỏi -->
    <div class="col-span-8 card" style="padding: 2rem;">
      <div style="font-size: 0.875rem; color: var(--text-muted); margin-bottom: 0.5rem;">Câu hỏi số 13 trong 40:</div>
      <p style="font-size: 1.125rem; font-weight: 600; line-height: 1.6; color: var(--text-primary); margin-bottom: 1.5rem;">
        Could you please tell me how _______ to the nearest subway station from here?
      </p>

      <div style="display: flex; flex-direction: column; gap: 12px;">
        <label style="padding: 12px 16px; border: 1px solid var(--border-color); border-radius: 8px; display: flex; align-items: center; gap: 10px; cursor: pointer;">
          <input type="radio" name="q13" value="A">
          <span>A. get</span>
        </label>
        <label style="padding: 12px 16px; border: 2px solid var(--primary-600); background: var(--primary-50); border-radius: 8px; display: flex; align-items: center; gap: 10px; cursor: pointer;">
          <input type="radio" name="q13" value="B" checked>
          <span><strong>B. to get (Đã chọn)</strong></span>
        </label>
        <label style="padding: 12px 16px; border: 1px solid var(--border-color); border-radius: 8px; display: flex; align-items: center; gap: 10px; cursor: pointer;">
          <input type="radio" name="q13" value="C">
          <span>C. getting</span>
        </label>
        <label style="padding: 12px 16px; border: 1px solid var(--border-color); border-radius: 8px; display: flex; align-items: center; gap: 10px; cursor: pointer;">
          <input type="radio" name="q13" value="D">
          <span>D. got</span>
        </label>
      </div>

      <div style="display: flex; justify-content: space-between; margin-top: 2rem; border-top: 1px solid var(--border-color); padding-top: 1rem;">
        <button class="btn btn-secondary">← Câu trước</button>
        <button class="btn btn-primary">Câu tiếp theo →</button>
      </div>
    </div>

    <!-- Bảng câu hỏi -->
    <div class="col-span-4 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Danh sách câu hỏi</h3>
      <div style="display: grid; grid-template-columns: repeat(5, 1fr); gap: 8px;">
        <button class="btn btn-sm btn-success">1</button>
        <button class="btn btn-sm btn-success">2</button>
        <button class="btn btn-sm btn-success">3</button>
        <button class="btn btn-sm btn-success">4</button>
        <button class="btn btn-sm btn-success">5</button>
        <button class="btn btn-sm btn-success">6</button>
        <button class="btn btn-sm btn-success">7</button>
        <button class="btn btn-sm btn-success">8</button>
        <button class="btn btn-sm btn-success">9</button>
        <button class="btn btn-sm btn-success">10</button>
        <button class="btn btn-sm btn-success">11</button>
        <button class="btn btn-sm btn-success">12</button>
        <button class="btn btn-sm btn-primary">13</button>
        <button class="btn btn-sm btn-secondary">14</button>
        <button class="btn btn-sm btn-secondary">15</button>
      </div>
    </div>
  </div>
`);

// 11. Results View
writeView('results.html', 'Lịch sử kết quả thi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Lịch sử bài thi & Điểm số</h1>
      <p class="page-header-subtitle">Xem lại kết quả các bài kiểm tra bạn đã hoàn thành</p>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <table class="table">
      <thead>
        <tr>
          <th>Tên bài thi</th>
          <th>Ngày thi</th>
          <th>Thời gian làm</th>
          <th>Số câu đúng</th>
          <th>Điểm số</th>
          <th>Xếp loại</th>
          <th class="text-right">Chi tiết</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>Kiểm tra đầu vào tổng quát</strong></td>
          <td>20/09/2026</td>
          <td>38 phút</td>
          <td>35 / 40</td>
          <td><strong style="color: var(--primary-600); font-size: 1.125rem;">8.8</strong></td>
          <td><span class="badge badge-success">Đạt</span></td>
          <td class="text-right"><a href="result-detail.html" class="btn btn-sm btn-secondary">Xem bài giải</a></td>
        </tr>
      </tbody>
    </table>
  </div>
`);

// 12. Result Detail View
writeView('result-detail.html', 'Chi tiết bài giải', `
  <div class="page-header">
    <div>
      <a href="results.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại kết quả thi</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Chi tiết bài giải: Kiểm tra đầu vào</h1>
      <p class="page-header-subtitle">Điểm số: 8.8 / 10 • Đúng 35 / 40 câu hỏi</p>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem; margin-bottom: 1.5rem;">
    <div style="padding: 1rem; border-left: 4px solid var(--success-main); background: #F0FDF4; border-radius: 0 8px 8px 0; margin-bottom: 1rem;">
      <strong>Câu 1: What _______ your brother do on Sundays?</strong>
      <div style="margin-top: 4px; font-size: 0.875rem;">Bạn đã chọn: <strong>B. does (Chính xác)</strong></div>
      <div style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 4px;">Giải thích: Chủ ngữ ngôi thứ 3 số ít "your brother" đi với trợ động từ "does" ở thì Hiện tại đơn.</div>
    </div>
  </div>
`);

// 13. Progress View
writeView('progress.html', 'Tiến độ học tập', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Thống kê tiến độ & Năng lực</h1>
      <p class="page-header-subtitle">Theo dõi sự tiến bộ qua các kỹ năng Nghe, Nói, Đọc, Viết</p>
    </div>
  </div>

  <div class="grid grid-cols-2 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Mức độ thành thạo 4 kỹ năng</h3>
      <div style="display: flex; flex-direction: column; gap: 14px;">
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.875rem; margin-bottom: 4px;">
            <span>Listening (Nghe hiểu)</span>
            <strong>8.5 / 10</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 85%;"></div></div>
        </div>
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.875rem; margin-bottom: 4px;">
            <span>Reading (Đọc hiểu)</span>
            <strong>9.0 / 10</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 90%;"></div></div>
        </div>
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.875rem; margin-bottom: 4px;">
            <span>Speaking (Phát âm & Nói)</span>
            <strong>8.0 / 10</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 80%;"></div></div>
        </div>
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.875rem; margin-bottom: 4px;">
            <span>Writing (Ngữ pháp & Viết)</span>
            <strong>7.8 / 10</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 78%;"></div></div>
        </div>
      </div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Chỉ số học tập tích lũy</h3>
      <div style="display: flex; flex-direction: column; gap: 12px; font-size: 0.875rem;">
        <div style="display: flex; justify-content: space-between; padding-bottom: 8px; border-bottom: 1px solid var(--border-color);">
          <span class="text-muted">Tổng số giờ học:</span>
          <strong>48 giờ</strong>
        </div>
        <div style="display: flex; justify-content: space-between; padding-bottom: 8px; border-bottom: 1px solid var(--border-color);">
          <span class="text-muted">Bài tập đã nộp:</span>
          <strong>16 bài tập</strong>
        </div>
        <div style="display: flex; justify-content: space-between; padding-bottom: 8px; border-bottom: 1px solid var(--border-color);">
          <span class="text-muted">Chuỗi ngày học liên tục:</span>
          <strong style="color: #D97706;">🔥 5 ngày</strong>
        </div>
      </div>
    </div>
  </div>
`);

// 14. Reviews View
writeView('reviews.html', 'Đánh giá & Phản hồi', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Đánh giá khóa học & Giảng viên</h1>
      <p class="page-header-subtitle">Ý kiến đóng góp giúp trung tâm nâng cao chất lượng đào tạo</p>
    </div>
  </div>

  <div class="card" style="max-width: 600px; padding: 1.5rem;">
    <h3 class="card-title" style="margin-bottom: 1rem;">Gửi nhận xét mới</h3>
    <div style="display: flex; flex-direction: column; gap: 14px;">
      <div>
        <label style="font-size: 0.8125rem; font-weight: 600;">Khóa học bạn muốn đánh giá</label>
        <select class="form-control">
          <option>Tiếng Anh Giao Tiếp Cơ Bản - COMM-01 (Cô Mai)</option>
        </select>
      </div>
      <div>
        <label style="font-size: 0.8125rem; font-weight: 600;">Mức độ hài lòng</label>
        <div style="font-size: 1.5rem; color: #F59E0B; cursor: pointer;">⭐⭐⭐⭐⭐</div>
      </div>
      <div>
        <label style="font-size: 0.8125rem; font-weight: 600;">Nội dung phản hồi</label>
        <textarea class="form-control" rows="4" placeholder="Chia sẻ cảm nhận về giáo viên, phòng học, giáo trình..."></textarea>
      </div>
      <button class="btn btn-primary">Gửi đánh giá</button>
    </div>
  </div>
`);

// 15. Profile View
writeView('profile.html', 'Hồ sơ học viên', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Hồ sơ cá nhân</h1>
      <p class="page-header-subtitle">Thông tin tài khoản học viên và cài đặt thông báo</p>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-4 card" style="padding: 1.5rem; text-align: center;">
      <div class="avatar avatar-lg" style="width: 90px; height: 90px; font-size: 2rem; background: #2563EB; color: white; margin: 0 auto 1rem auto;">HA</div>
      <h2 style="font-size: 1.25rem; font-weight: 700; margin: 0;">Nguyễn Hoàng Anh</h2>
      <div style="font-size: 0.875rem; color: var(--text-muted); margin-top: 4px;">student@el.com</div>
      <div style="margin-top: 1rem;"><span class="badge badge-primary">Học viên tích cực</span></div>
    </div>

    <div class="col-span-8 card" style="padding: 1.5rem;">
      <h3 class="card-title" style="margin-bottom: 1rem;">Thông tin chi tiết</h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Họ và tên</label>
          <input type="text" class="form-control" value="Nguyễn Hoàng Anh">
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Địa chỉ Email</label>
          <input type="email" class="form-control" value="student@el.com" disabled>
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Số điện thoại</label>
          <input type="text" class="form-control" value="0987 654 321">
        </div>
        <div>
          <label style="font-size: 0.8125rem; color: var(--text-muted);">Mục tiêu đầu ra</label>
          <input type="text" class="form-control" value="Giao tiếp lưu loát & IELTS 6.5+">
        </div>
      </div>
      <button class="btn btn-primary" style="margin-top: 1.5rem;">Cập nhật thông tin</button>
    </div>
  </div>
`);

console.log('Finished generating Student views.');
