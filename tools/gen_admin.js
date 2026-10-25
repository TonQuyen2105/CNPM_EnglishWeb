import fs from 'fs';
import path from 'path';
import { wrapAdminPage } from './admin_views.js';

const outDir = path.resolve('areas/admin/views');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

function writeView(fileName, title, content) {
  const fullHtml = wrapAdminPage({ title, activePage: fileName, content });
  fs.writeFileSync(path.join(outDir, fileName), fullHtml, 'utf-8');
  console.log(`[Admin] Generated ${fileName}`);
}

// 1. Dashboard
writeView('dashboard.html', 'Bảng điều khiển', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Bảng điều khiển Quản trị</h1>
      <p class="page-header-subtitle">Tổng hợp chỉ số vận hành trung tâm và các hạng mục cần xử lý</p>
    </div>
    <div class="page-header-actions">
      <button type="button" class="btn btn-secondary">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
        <span>Làm mới dữ liệu</span>
      </button>
      <a href="classes.html" class="btn btn-primary">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        <span>Mở lớp học mới</span>
      </a>
    </div>
  </div>

  <!-- 4 Stat Cards -->
  <div class="grid grid-cols-4 gap-6" style="margin-bottom: 2rem;">
    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Tổng học sinh</span>
        <span class="stat-value">40</span>
        <span class="stat-sub text-success">↗ 40 tài khoản đang hoạt động</span>
      </div>
      <div class="stat-icon primary">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Đội ngũ giảng viên</span>
        <span class="stat-value">6</span>
        <span class="stat-sub text-warning">⚠️ 2 hồ sơ chờ duyệt</span>
      </div>
      <div class="stat-icon accent">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 10v6M2 10l10-5 10 5-10 5z"></path><path d="M6 12v5c3 3 9 3 12 0v-5"></path></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Lớp đang diễn ra</span>
        <span class="stat-value">5</span>
        <span class="stat-sub text-muted">3 lớp sắp khai giảng</span>
      </div>
      <div class="stat-icon warning">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"></path><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"></path></svg>
      </div>
    </div>

    <div class="stat-card">
      <div class="stat-info">
        <span class="stat-label">Doanh thu ước tính</span>
        <span class="stat-value" style="font-size: 1.5rem;">154.500.000 ₫</span>
        <span class="stat-sub text-success">↗ Ước tính từ 48 lượt đăng ký</span>
      </div>
      <div class="stat-icon success">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="8" r="7"></circle><polyline points="8.21 13.89 7 23 12 20 17 23 15.79 13.88"></polyline></svg>
      </div>
    </div>
  </div>

  <!-- 2 Biểu đồ thống kê -->
  <div class="grid grid-cols-12 gap-6" style="margin-bottom: 2rem;">
    <div class="col-span-8 card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
        <div>
          <h3 class="card-title">Số lượng học viên đăng ký theo tháng</h3>
          <div class="card-subtitle">Thống kê dữ liệu đăng ký lớp học trong 6 tháng gần nhất</div>
        </div>
        <span class="badge badge-primary">Năm 2026</span>
      </div>
      <div style="height: 200px; display: flex; align-items: flex-end; justify-content: space-around; padding-bottom: 20px; border-bottom: 1px solid var(--border-color);">
        <div style="text-align: center;"><div style="height: 90px; width: 36px; background: var(--primary-500); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; color: var(--text-muted); margin-top: 6px; display: block;">T5 (18)</span></div>
        <div style="text-align: center;"><div style="height: 120px; width: 36px; background: var(--primary-500); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; color: var(--text-muted); margin-top: 6px; display: block;">T6 (24)</span></div>
        <div style="text-align: center;"><div style="height: 155px; width: 36px; background: var(--primary-500); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; color: var(--text-muted); margin-top: 6px; display: block;">T7 (31)</span></div>
        <div style="text-align: center;"><div style="height: 140px; width: 36px; background: var(--primary-500); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; color: var(--text-muted); margin-top: 6px; display: block;">T8 (28)</span></div>
        <div style="text-align: center;"><div style="height: 190px; width: 36px; background: var(--primary-600); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; font-weight: 700; color: var(--primary-600); margin-top: 6px; display: block;">T9 (45)</span></div>
        <div style="text-align: center;"><div style="height: 160px; width: 36px; background: var(--primary-500); border-radius: 4px; margin: 0 auto;"></div><span style="font-size: 0.75rem; color: var(--text-muted); margin-top: 6px; display: block;">T10 (36)</span></div>
      </div>
    </div>

    <div class="col-span-4 card" style="padding: 1.5rem;">
      <div style="margin-bottom: 1.25rem;">
        <h3 class="card-title">Cấp độ đào tạo</h3>
        <div class="card-subtitle">Phân bổ 12 khóa học theo trình độ</div>
      </div>
      <div style="display: flex; flex-direction: column; gap: 12px; margin-top: 1.5rem;">
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.8125rem; margin-bottom: 4px;">
            <span>Cơ bản (Beginner)</span>
            <strong>4 khóa (33%)</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 33%; background-color: #3B82F6;"></div></div>
        </div>
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.8125rem; margin-bottom: 4px;">
            <span>Trung cấp (Intermediate)</span>
            <strong>5 khóa (42%)</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 42%; background-color: #F59E0B;"></div></div>
        </div>
        <div>
          <div style="display: flex; justify-content: space-between; font-size: 0.8125rem; margin-bottom: 4px;">
            <span>Nâng cao (Advanced)</span>
            <strong>3 khóa (25%)</strong>
          </div>
          <div class="progress"><div class="progress-bar" style="width: 25%; background-color: #EF4444;"></div></div>
        </div>
      </div>
    </div>
  </div>

  <!-- Bảng Đăng ký mới nhất -->
  <div class="card" style="padding: 1.5rem; margin-bottom: 2rem;">
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
      <h3 class="card-title">Đăng ký mới nhất</h3>
      <a href="enrollments.html" class="btn btn-sm btn-secondary">Xem tất cả</a>
    </div>
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Học viên</th>
            <th>Mã lớp</th>
            <th>Khóa học</th>
            <th>Học phí</th>
            <th>Ngày đăng ký</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Nguyễn Hoàng Anh</strong><br><small class="text-muted">student@el.com</small></td>
            <td><span class="badge badge-primary">COMM-01</span></td>
            <td>Tiếng Anh Giao Tiếp Cơ Bản</td>
            <td>2.500.000 ₫</td>
            <td>24/10/2026</td>
            <td><span class="badge badge-success">Đã hoàn thành</span></td>
          </tr>
          <tr>
            <td><strong>Trần Phương Nam</strong><br><small class="text-muted">nam.tran@el.com</small></td>
            <td><span class="badge badge-primary">IELTS-B01</span></td>
            <td>Luyện Thi IELTS 6.5+</td>
            <td>5.800.000 ₫</td>
            <td>23/10/2026</td>
            <td><span class="badge badge-success">Đã hoàn thành</span></td>
          </tr>
          <tr>
            <td><strong>Lê Thị Kim Ngân</strong><br><small class="text-muted">ngan.le@el.com</small></td>
            <td><span class="badge badge-primary">TOEIC-A01</span></td>
            <td>TOEIC 750+ Cấp Tốc</td>
            <td>3.200.000 ₫</td>
            <td>22/10/2026</td>
            <td><span class="badge badge-warning">Chờ thanh toán</span></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 2. Users View
writeView('users.html', 'Tài khoản người dùng', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý người dùng</h1>
      <p class="page-header-subtitle">Danh sách toàn bộ tài khoản Quản trị, Giảng viên và Học viên</p>
    </div>
    <div class="page-header-actions">
      <button type="button" class="btn btn-primary">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        <span>Thêm tài khoản mới</span>
      </button>
    </div>
  </div>

  <div class="card" style="margin-bottom: 1.5rem; padding: 1rem 1.25rem;">
    <div class="grid grid-cols-12 gap-4 items-center">
      <div class="col-span-5">
        <input type="text" class="form-control" placeholder="Tìm theo tên hoặc email...">
      </div>
      <div class="col-span-3">
        <select class="form-control">
          <option value="">Tất cả vai trò</option>
          <option value="admin">Quản trị viên (Admin)</option>
          <option value="teacher">Giảng viên (Teacher)</option>
          <option value="student">Học viên (Student)</option>
        </select>
      </div>
      <div class="col-span-2">
        <select class="form-control">
          <option value="">Tất cả trạng thái</option>
          <option value="1">Đang hoạt động</option>
          <option value="0">Đã khóa</option>
        </select>
      </div>
      <div class="col-span-2 text-right">
        <button type="button" class="btn btn-secondary" style="width: 100%;">Đặt lại</button>
      </div>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Người dùng</th>
            <th>Email</th>
            <th>Vai trò</th>
            <th>Trạng thái</th>
            <th>Ngày tạo</th>
            <th class="text-right">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="avatar avatar-sm">AD</div>
                <strong>Nguyễn Quản Trị</strong>
              </div>
            </td>
            <td>admin@el.com</td>
            <td><span class="badge badge-primary">Quản trị viên</span></td>
            <td><span class="badge badge-success">Hoạt động</span></td>
            <td>15/09/2026</td>
            <td class="text-right">
              <button class="btn btn-sm btn-secondary">Sửa</button>
            </td>
          </tr>
          <tr>
            <td>
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="avatar avatar-sm" style="background: #059669; color: white;">LM</div>
                <strong>Lê Thị Mai</strong>
              </div>
            </td>
            <td>teacher@el.com</td>
            <td><span class="badge badge-info">Giảng viên</span></td>
            <td><span class="badge badge-success">Hoạt động</span></td>
            <td>15/09/2026</td>
            <td class="text-right">
              <button class="btn btn-sm btn-secondary">Sửa</button>
              <button class="btn btn-sm btn-danger">Khóa</button>
            </td>
          </tr>
          <tr>
            <td>
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="avatar avatar-sm" style="background: #2563EB; color: white;">HA</div>
                <strong>Nguyễn Hoàng Anh</strong>
              </div>
            </td>
            <td>student@el.com</td>
            <td><span class="badge badge-neutral">Học viên</span></td>
            <td><span class="badge badge-success">Hoạt động</span></td>
            <td>20/09/2026</td>
            <td class="text-right">
              <button class="btn btn-sm btn-secondary">Sửa</button>
              <button class="btn btn-sm btn-danger">Khóa</button>
            </td>
          </tr>
          <tr>
            <td>
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="avatar avatar-sm">PN</div>
                <strong>Trần Phương Nam</strong>
              </div>
            </td>
            <td>nam.tran@el.com</td>
            <td><span class="badge badge-neutral">Học viên</span></td>
            <td><span class="badge badge-success">Hoạt động</span></td>
            <td>22/09/2026</td>
            <td class="text-right">
              <button class="btn btn-sm btn-secondary">Sửa</button>
              <button class="btn btn-sm btn-danger">Khóa</button>
            </td>
          </tr>
          <tr>
            <td>
              <div style="display: flex; align-items: center; gap: 10px;">
                <div class="avatar avatar-sm">KN</div>
                <strong>Lê Thị Kim Ngân</strong>
              </div>
            </td>
            <td>ngan.le@el.com</td>
            <td><span class="badge badge-neutral">Học viên</span></td>
            <td><span class="badge badge-danger">Đã khóa</span></td>
            <td>25/09/2026</td>
            <td class="text-right">
              <button class="btn btn-sm btn-secondary">Mở khóa</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div style="display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.5rem; border-top: 1px solid var(--border-color);">
      <span style="font-size: 0.8125rem; color: var(--text-muted);">Hiển thị 1 - 5 trong 47 tài khoản</span>
      <div style="display: flex; gap: 4px;">
        <button class="btn btn-sm btn-secondary" disabled>«</button>
        <button class="btn btn-sm btn-primary">1</button>
        <button class="btn btn-sm btn-secondary">2</button>
        <button class="btn btn-sm btn-secondary">3</button>
        <button class="btn btn-sm btn-secondary">»</button>
      </div>
    </div>
  </div>
`);

// 3. Teachers View
writeView('teachers.html', 'Đội ngũ Giảng viên', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý đội ngũ giảng viên</h1>
      <p class="page-header-subtitle">Hồ sơ chuyên môn, chứng chỉ quốc tế và các lớp học đang phụ trách</p>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 1rem;">
        <div class="avatar avatar-lg" style="background: #2563EB; color: white;">VH</div>
        <div>
          <h3 style="font-size: 1rem; font-weight: 700; margin: 0;">Nguyễn Văn Hùng</h3>
          <span style="font-size: 0.8125rem; color: var(--text-muted);">hung.nguyen@el.com</span>
        </div>
      </div>
      <div style="margin-bottom: 1rem;">
        <span class="badge badge-success">Đã xác minh</span>
        <span class="badge badge-info" style="margin-left: 4px;">Thạc sĩ TESOL</span>
      </div>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5;">Hơn 8 năm kinh nghiệm giảng dạy tiếng Anh giao tiếp và luyện thi chứng chỉ quốc tế tại các trường đại học.</p>
      <div style="border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 1rem; display: flex; justify-content: space-between; font-size: 0.8125rem;">
        <span>Lớp phụ trách: <strong>2 lớp</strong></span>
        <a href="classes.html" class="btn btn-sm btn-secondary">Xem lớp</a>
      </div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 1rem;">
        <div class="avatar avatar-lg" style="background: #059669; color: white;">LM</div>
        <div>
          <h3 style="font-size: 1rem; font-weight: 700; margin: 0;">Lê Thị Mai</h3>
          <span style="font-size: 0.8125rem; color: var(--text-muted);">teacher@el.com</span>
        </div>
      </div>
      <div style="margin-bottom: 1rem;">
        <span class="badge badge-success">Đã xác minh</span>
        <span class="badge badge-info" style="margin-left: 4px;">IELTS 8.5</span>
      </div>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5;">Chuyên gia đào tạo kỹ năng Speaking và Writing học thuật với hàng trăm học viên đạt band 7.0+.</p>
      <div style="border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 1rem; display: flex; justify-content: space-between; font-size: 0.8125rem;">
        <span>Lớp phụ trách: <strong>3 lớp</strong></span>
        <a href="classes.html" class="btn btn-sm btn-secondary">Xem lớp</a>
      </div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 1rem;">
        <div class="avatar avatar-lg" style="background: #D97706; color: white;">QH</div>
        <div>
          <h3 style="font-size: 1rem; font-weight: 700; margin: 0;">Phạm Quang Huy</h3>
          <span style="font-size: 0.8125rem; color: var(--text-muted);">huy.pham@el.com</span>
        </div>
      </div>
      <div style="margin-bottom: 1rem;">
        <span class="badge badge-warning">Chờ duyệt hồ sơ</span>
        <span class="badge badge-info" style="margin-left: 4px;">TOEIC 980</span>
      </div>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5;">Hồ sơ vừa đăng ký ứng tuyển bộ môn TOEIC Cấp Tốc, đang gửi tài liệu chứng chỉ bổ sung.</p>
      <div style="border-top: 1px dashed var(--border-color); padding-top: 0.75rem; margin-top: 1rem; display: flex; justify-content: space-between; font-size: 0.8125rem;">
        <span>Lớp phụ trách: <strong>0 lớp</strong></span>
        <button class="btn btn-sm btn-primary">Duyệt hồ sơ</button>
      </div>
    </div>
  </div>
`);

// 4. Subjects View
writeView('subjects.html', 'Môn học đào tạo', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý môn học</h1>
      <p class="page-header-subtitle">Danh mục các chương trình trọng điểm trung tâm đang giảng dạy</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Thêm môn học mới</button>
    </div>
  </div>

  <div class="grid grid-cols-4 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <div class="badge badge-primary" style="margin-bottom: 0.75rem;">Mã: COMM</div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Tiếng Anh Giao Tiếp</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0 1rem 0;">Luyện phản xạ, phát âm chuẩn IPA, đàm thoại tự tin theo ngữ cảnh công việc và đời sống.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Số khóa học: <strong>4 khóa</strong></div>
    </div>
    <div class="card" style="padding: 1.5rem;">
      <div class="badge badge-primary" style="margin-bottom: 0.75rem;">Mã: TOEIC</div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Luyện Thi TOEIC</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0 1rem 0;">Luyện đề 2 kỹ năng và 4 kỹ năng cam kết đầu ra 550 - 850+.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Số khóa học: <strong>3 khóa</strong></div>
    </div>
    <div class="card" style="padding: 1.5rem;">
      <div class="badge badge-primary" style="margin-bottom: 0.75rem;">Mã: IELTS</div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Luyện Thi IELTS</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0 1rem 0;">Lộ trình từ Foundation 4.5 đến Master 7.5+ toàn diện 4 kỹ năng.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Số khóa học: <strong>3 khóa</strong></div>
    </div>
    <div class="card" style="padding: 1.5rem;">
      <div class="badge badge-primary" style="margin-bottom: 0.75rem;">Mã: KIDS</div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Tiếng Anh Thiếu Nhi</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0 1rem 0;">Phương pháp học qua trò chơi, tương tác trực quan cho trẻ 6-12 tuổi.</p>
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Số khóa học: <strong>2 khóa</strong></div>
    </div>
  </div>
`);

// 5. Courses View
writeView('courses.html', 'Khóa học', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý chương trình khóa học</h1>
      <p class="page-header-subtitle">Hệ thống 12 khóa học tiếng Anh chuẩn hóa theo các cấp độ</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Tạo khóa học mới</button>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card card-hover" style="overflow: hidden;">
      <div style="height: 140px; background: linear-gradient(135deg, #1E3A8A, #3B82F6); color: white; padding: 1.5rem; display: flex; flex-direction: column; justify-content: space-between;">
        <span class="badge badge-success" style="align-self: flex-start;">Đang mở</span>
        <h3 style="color: white; margin: 0; font-size: 1.125rem;">Tiếng Anh Giao Tiếp Cơ Bản</h3>
      </div>
      <div class="card-body">
        <span class="badge badge-info">Cơ bản (Beginner)</span>
        <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 0.5rem;">Khóa học dành cho người mất gốc, làm quen với 44 âm IPA và các mẫu câu hàng ngày.</p>
        <div style="display: flex; justify-content: space-between; margin-top: 1rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">
          <span style="font-size: 0.8125rem; color: var(--text-muted);">24 bài học</span>
          <strong style="color: var(--primary-600);">2.500.000 ₫</strong>
        </div>
      </div>
      <div class="card-footer" style="display: flex; justify-content: space-between;">
        <a href="course-detail.html" class="btn btn-sm btn-secondary">Chi tiết</a>
        <a href="classes.html" class="btn btn-sm btn-primary">Xem lớp mở</a>
      </div>
    </div>

    <div class="card card-hover" style="overflow: hidden;">
      <div style="height: 140px; background: linear-gradient(135deg, #065F46, #10B981); color: white; padding: 1.5rem; display: flex; flex-direction: column; justify-content: space-between;">
        <span class="badge badge-success" style="align-self: flex-start;">Đang mở</span>
        <h3 style="color: white; margin: 0; font-size: 1.125rem;">Luyện Thi IELTS 6.5+</h3>
      </div>
      <div class="card-body">
        <span class="badge badge-warning">Nâng cao (Advanced)</span>
        <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 0.5rem;">Chiến thuật giải đề Writing Task 2 và Speaking Part 2-3 bứt phá band điểm.</p>
        <div style="display: flex; justify-content: space-between; margin-top: 1rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">
          <span style="font-size: 0.8125rem; color: var(--text-muted);">36 bài học</span>
          <strong style="color: var(--primary-600);">5.800.000 ₫</strong>
        </div>
      </div>
      <div class="card-footer" style="display: flex; justify-content: space-between;">
        <a href="course-detail.html" class="btn btn-sm btn-secondary">Chi tiết</a>
        <a href="classes.html" class="btn btn-sm btn-primary">Xem lớp mở</a>
      </div>
    </div>

    <div class="card card-hover" style="overflow: hidden;">
      <div style="height: 140px; background: linear-gradient(135deg, #7C2D12, #F97316); color: white; padding: 1.5rem; display: flex; flex-direction: column; justify-content: space-between;">
        <span class="badge badge-success" style="align-self: flex-start;">Đang mở</span>
        <h3 style="color: white; margin: 0; font-size: 1.125rem;">TOEIC 750+ Cấp Tốc</h3>
      </div>
      <div class="card-body">
        <span class="badge badge-primary">Trung cấp (Intermediate)</span>
        <p style="font-size: 0.8125rem; color: var(--text-secondary); margin-top: 0.5rem;">Bộ bẫy đề thi Part 5, 6, 7 và kỹ năng nghe hiểu bắt từ khóa Part 3, 4.</p>
        <div style="display: flex; justify-content: space-between; margin-top: 1rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">
          <span style="font-size: 0.8125rem; color: var(--text-muted);">30 bài học</span>
          <strong style="color: var(--primary-600);">3.200.000 ₫</strong>
        </div>
      </div>
      <div class="card-footer" style="display: flex; justify-content: space-between;">
        <a href="course-detail.html" class="btn btn-sm btn-secondary">Chi tiết</a>
        <a href="classes.html" class="btn btn-sm btn-primary">Xem lớp mở</a>
      </div>
    </div>
  </div>
`);

// 6. Course Detail View
writeView('course-detail.html', 'Chi tiết khóa học', `
  <div class="page-header">
    <div>
      <a href="courses.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại danh sách khóa học</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Tiếng Anh Giao Tiếp Cơ Bản</h1>
      <p class="page-header-subtitle">Mã: COMM-BASIC • Môn: Tiếng Anh Giao Tiếp • Cấp độ: Cơ bản</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-secondary">Chỉnh sửa</button>
      <a href="classes.html" class="btn btn-primary">Mở lớp cho khóa này</a>
    </div>
  </div>

  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-8">
      <div class="card" style="padding: 1.5rem; margin-bottom: 1.5rem;">
        <h3 class="card-title">Mô tả và mục tiêu đầu ra</h3>
        <p style="font-size: 0.875rem; color: var(--text-secondary); line-height: 1.6; margin-top: 0.5rem;">Khóa học Tiếng Anh Giao Tiếp Cơ Bản được thiết kế đặc biệt cho học viên bắt đầu lại từ đầu. Chương trình tập trung xây dựng nền tảng ngữ âm vững chắc qua bảng phiên âm quốc tế IPA, kèm theo 20 tình huống giao tiếp đời sống thực tế hàng ngày.</p>
      </div>

      <div class="card" style="padding: 1.5rem;">
        <h3 class="card-title" style="margin-bottom: 1rem;">Đề cương bài học (24 bài)</h3>
        <div style="display: flex; flex-direction: column; gap: 8px;">
          <div style="padding: 10px 12px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between;">
            <span><strong>Bài 1:</strong> Bảng ngữ âm IPA & Các nguyên âm đơn</span>
            <span class="text-muted">45 phút</span>
          </div>
          <div style="padding: 10px 12px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between;">
            <span><strong>Bài 2:</strong> Các nguyên âm đôi và phụ âm bật hơi</span>
            <span class="text-muted">45 phút</span>
          </div>
          <div style="padding: 10px 12px; background: var(--neutral-50); border-radius: 8px; display: flex; justify-content: space-between;">
            <span><strong>Bài 3:</strong> Chào hỏi & Tự giới thiệu bản thân</span>
            <span class="text-muted">60 phút</span>
          </div>
        </div>
      </div>
    </div>

    <div class="col-span-4">
      <div class="card" style="padding: 1.5rem;">
        <h3 class="card-title">Thông số khóa học</h3>
        <div style="display: flex; flex-direction: column; gap: 12px; margin-top: 1rem; font-size: 0.875rem;">
          <div style="display: flex; justify-content: space-between;">
            <span class="text-muted">Học phí:</span>
            <strong style="color: var(--primary-600); font-size: 1.125rem;">2.500.000 ₫</strong>
          </div>
          <div style="display: flex; justify-content: space-between;">
            <span class="text-muted">Thời lượng:</span>
            <span>24 buổi (36 giờ)</span>
          </div>
          <div style="display: flex; justify-content: space-between;">
            <span class="text-muted">Trạng thái:</span>
            <span class="badge badge-success">Đang mở tuyển sinh</span>
          </div>
        </div>
      </div>
    </div>
  </div>
`);

// 7. Classes View
writeView('classes.html', 'Quản lý Lớp học', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý lớp học</h1>
      <p class="page-header-subtitle">Danh sách tất cả các lớp đang mở, đang học và sắp khai giảng</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Tạo lớp học mới</button>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Mã lớp</th>
            <th>Tên lớp</th>
            <th>Giảng viên</th>
            <th>Phòng</th>
            <th>Lịch học</th>
            <th>Sĩ số</th>
            <th>Trạng thái</th>
            <th class="text-right">Chi tiết</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><span class="badge badge-primary">COMM-01</span></td>
            <td><strong>Giao Tiếp Cơ Bản K01</strong></td>
            <td>Lê Thị Mai</td>
            <td>Phòng LAB-01</td>
            <td>T2-T4-T6 (18:00 - 19:30)</td>
            <td><strong>18</strong> / 20</td>
            <td><span class="badge badge-success">Đang diễn ra</span></td>
            <td class="text-right"><a href="class-detail.html" class="btn btn-sm btn-secondary">Xem</a></td>
          </tr>
          <tr>
            <td><span class="badge badge-primary">IELTS-B01</span></td>
            <td><strong>Luyện Thi IELTS 6.5+ Sáng</strong></td>
            <td>Lê Thị Mai</td>
            <td>Phòng A101</td>
            <td>T3-T5-T7 (08:30 - 10:30)</td>
            <td><strong>15</strong> / 15</td>
            <td><span class="badge badge-success">Đang diễn ra</span></td>
            <td class="text-right"><a href="class-detail.html" class="btn btn-sm btn-secondary">Xem</a></td>
          </tr>
          <tr>
            <td><span class="badge badge-primary">TOEIC-A01</span></td>
            <td><strong>TOEIC Cấp Tốc Tối</strong></td>
            <td>Nguyễn Văn Hùng</td>
            <td>Phòng B202</td>
            <td>T2-T4-T6 (19:30 - 21:00)</td>
            <td><strong>12</strong> / 25</td>
            <td><span class="badge badge-warning">Sắp khai giảng</span></td>
            <td class="text-right"><a href="class-detail.html" class="btn btn-sm btn-secondary">Xem</a></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 8. Class Detail View
writeView('class-detail.html', 'Chi tiết lớp học', `
  <div class="page-header">
    <div>
      <a href="classes.html" style="font-size: 0.8125rem; color: var(--primary-600); text-decoration: none;">← Quay lại danh sách lớp</a>
      <h1 class="page-header-title" style="margin-top: 4px;">Lớp COMM-01: Giao Tiếp Cơ Bản K01</h1>
      <p class="page-header-subtitle">Giảng viên: Lê Thị Mai • Phòng học: LAB-01 • Khai giảng: 01/10/2026</p>
    </div>
  </div>

  <div class="grid grid-cols-4 gap-6" style="margin-bottom: 1.5rem;">
    <div class="stat-card">
      <div class="stat-info"><span class="stat-label">Sĩ số học viên</span><span class="stat-value">18 / 20</span></div>
    </div>
    <div class="stat-card">
      <div class="stat-info"><span class="stat-label">Buổi đã học</span><span class="stat-value">8 / 24</span></div>
    </div>
    <div class="stat-card">
      <div class="stat-info"><span class="stat-label">Tỷ lệ chuyên cần</span><span class="stat-value text-success">96%</span></div>
    </div>
    <div class="stat-card">
      <div class="stat-info"><span class="stat-label">Điểm TB giữa kỳ</span><span class="stat-value">8.4</span></div>
    </div>
  </div>

  <div class="card" style="padding: 1.5rem;">
    <h3 class="card-title" style="margin-bottom: 1rem;">Danh sách học viên trong lớp (18 học viên)</h3>
    <table class="table">
      <thead>
        <tr>
          <th>STT</th>
          <th>Họ và tên</th>
          <th>Email</th>
          <th>Chuyên cần</th>
          <th>Điểm giữa kỳ</th>
          <th>Trạng thái</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>1</td>
          <td><strong>Nguyễn Hoàng Anh</strong></td>
          <td>student@el.com</td>
          <td>8/8 buổi (100%)</td>
          <td><strong>8.5</strong></td>
          <td><span class="badge badge-success">Bình thường</span></td>
        </tr>
        <tr>
          <td>2</td>
          <td><strong>Trần Phương Nam</strong></td>
          <td>nam.tran@el.com</td>
          <td>7/8 buổi (87%)</td>
          <td><strong>8.0</strong></td>
          <td><span class="badge badge-success">Bình thường</span></td>
        </tr>
      </tbody>
    </table>
  </div>
`);

// 9. Schedules View
writeView('schedules.html', 'Thời khóa biểu', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Thời khóa biểu trung tâm</h1>
      <p class="page-header-subtitle">Lịch phân bổ phòng học và ca giảng dạy trong tuần</p>
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
          <td><strong>Sáng<br><small class="text-muted">08:00 - 11:30</small></strong></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101 (GV: Mai)</small></div></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101 (GV: Mai)</small></div></td>
          <td>—</td>
          <td><div style="background: #EFF6FF; border: 1px solid #BFDBFE; padding: 8px; border-radius: 6px;"><strong style="color: #1D4ED8;">IELTS-B01</strong><br><small>Phòng A101 (GV: Mai)</small></div></td>
        </tr>
        <tr>
          <td><strong>Tối<br><small class="text-muted">18:00 - 21:00</small></strong></td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01 (GV: Mai)</small></div></td>
          <td>—</td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01 (GV: Mai)</small></div></td>
          <td>—</td>
          <td><div style="background: #ECFDF5; border: 1px solid #A7F3D0; padding: 8px; border-radius: 6px;"><strong style="color: #047857;">COMM-01</strong><br><small>Phòng LAB-01 (GV: Mai)</small></div></td>
          <td>—</td>
        </tr>
      </tbody>
    </table>
  </div>
`);

// 10. Facilities View
writeView('facilities.html', 'Phòng học & Cơ sở', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý phòng học & cơ sở vật chất</h1>
      <p class="page-header-subtitle">Trạng thái phòng học, trang thiết bị máy chiếu, micro, tai nghe</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-primary">Thêm phòng mới</button>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
        <span class="badge badge-success">Đang sử dụng</span>
        <span class="text-muted" style="font-size: 0.75rem;">Tầng 1</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Phòng LAB-01</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Phòng máy tính 25 máy dành cho luyện thi TOEIC/IELTS Listening và Speaking.</p>
      <div style="font-size: 0.8125rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">Sức chứa: <strong>25 máy</strong> • Thiết bị: Máy lạnh, Tai nghe Sony</div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
        <span class="badge badge-success">Đang sử dụng</span>
        <span class="text-muted" style="font-size: 0.75rem;">Tầng 2</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Phòng A101 (Lý thuyết)</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Phòng học tiêu chuẩn bàn ghế nhóm linh hoạt cho lớp Giao Tiếp.</p>
      <div style="font-size: 0.8125rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">Sức chứa: <strong>30 chỗ</strong> • Thiết bị: Máy chiếu 4K, Loa trợ giảng</div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
        <span class="badge badge-info">Phòng trống</span>
        <span class="text-muted" style="font-size: 0.75rem;">Tầng 2</span>
      </div>
      <h3 style="font-size: 1.125rem; font-weight: 700;">Phòng B202 (Hội thảo)</h3>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0.5rem 0;">Hội trường tổ chức thi thử IELTS Speaking mock test và Workshop.</p>
      <div style="font-size: 0.8125rem; border-top: 1px dashed var(--border-color); padding-top: 0.75rem;">Sức chứa: <strong>60 chỗ</strong> • Thiết bị: Màn chiếu lớn, 4 mic không dây</div>
    </div>
  </div>
`);

// 11. Enrollments View
writeView('enrollments.html', 'Học viên & Đăng ký', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý ghi danh & học phí</h1>
      <p class="page-header-subtitle">Toàn bộ hồ sơ đăng ký khóa học và trạng thái thanh toán học phí</p>
    </div>
  </div>

  <div class="card" style="padding: 0; overflow: hidden;">
    <div class="table-container">
      <table class="table">
        <thead>
          <tr>
            <th>Mã ĐK</th>
            <th>Học viên</th>
            <th>Lớp học</th>
            <th>Học phí</th>
            <th>Hình thức</th>
            <th>Ngày đóng</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>#ENR-101</strong></td>
            <td>Nguyễn Hoàng Anh</td>
            <td><span class="badge badge-primary">COMM-01</span> Giao Tiếp K01</td>
            <td><strong>2.500.000 ₫</strong></td>
            <td>Chuyển khoản (Vietcombank)</td>
            <td>24/10/2026</td>
            <td><span class="badge badge-success">Đã thu học phí</span></td>
          </tr>
          <tr>
            <td><strong>#ENR-102</strong></td>
            <td>Trần Phương Nam</td>
            <td><span class="badge badge-primary">IELTS-B01</span> IELTS 6.5+</td>
            <td><strong>5.800.000 ₫</strong></td>
            <td>Chuyển khoản (Techcombank)</td>
            <td>23/10/2026</td>
            <td><span class="badge badge-success">Đã thu học phí</span></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 12. Reviews View
writeView('reviews.html', 'Đánh giá khóa học', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Phản hồi & Đánh giá từ học viên</h1>
      <p class="page-header-subtitle">Theo dõi chất lượng giảng dạy của giáo viên và sự hài lòng của học sinh</p>
    </div>
  </div>

  <div class="grid grid-cols-2 gap-6">
    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.75rem;">
        <div>
          <strong>Nguyễn Hoàng Anh</strong>
          <div style="color: #F59E0B; font-size: 0.875rem;">⭐⭐⭐⭐⭐ 5/5 sao</div>
        </div>
        <span class="text-muted" style="font-size: 0.75rem;">2 ngày trước</span>
      </div>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5;">Cô Mai dạy rất nhiệt tình, sửa phát âm từng từ chi tiết. Sau khóa học em tự tin nói tiếng Anh hơn rất nhiều!</p>
      <div style="font-size: 0.75rem; color: var(--primary-600); font-weight: 600; margin-top: 0.75rem;">Khóa học: Tiếng Anh Giao Tiếp Cơ Bản</div>
    </div>

    <div class="card" style="padding: 1.5rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.75rem;">
        <div>
          <strong>Trần Phương Nam</strong>
          <div style="color: #F59E0B; font-size: 0.875rem;">⭐⭐⭐⭐⭐ 5/5 sao</div>
        </div>
        <span class="text-muted" style="font-size: 0.75rem;">4 ngày trước</span>
      </div>
      <p style="font-size: 0.8125rem; color: var(--text-secondary); line-height: 1.5;">Tài liệu và ngân hàng đề thi bám sát đề thi thật tại IDP. Phòng LAB nghe tai nghe rất rõ.</p>
      <div style="font-size: 0.75rem; color: var(--primary-600); font-weight: 600; margin-top: 0.75rem;">Khóa học: Luyện Thi IELTS 6.5+</div>
    </div>
  </div>
`);

// 13. Exams View
writeView('exams.html', 'Đề thi & Khảo thí', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Quản lý kỳ thi & đề thi</h1>
      <p class="page-header-subtitle">Hệ thống bài kiểm tra đầu vào, kiểm tra giữa kỳ và thi thử trực tuyến</p>
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
            <th>Tên bài thi</th>
            <th>Môn học</th>
            <th>Thời gian</th>
            <th>Số câu</th>
            <th>Điểm qua</th>
            <th>Lượt thi</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Kiểm tra đầu vào tổng quát</strong></td>
            <td>Chung</td>
            <td>45 phút</td>
            <td>40 câu</td>
            <td>20 / 40</td>
            <td>128 lượt</td>
            <td><span class="badge badge-success">Đang mở</span></td>
          </tr>
          <tr>
            <td><strong>IELTS Reading Mock Test 1</strong></td>
            <td>Luyện Thi IELTS</td>
            <td>60 phút</td>
            <td>40 câu</td>
            <td>24 / 40</td>
            <td>85 lượt</td>
            <td><span class="badge badge-success">Đang mở</span></td>
          </tr>
          <tr>
            <td><strong>TOEIC Mini Test 50 Câu</strong></td>
            <td>Luyện Thi TOEIC</td>
            <td>35 phút</td>
            <td>50 câu</td>
            <td>30 / 50</td>
            <td>94 lượt</td>
            <td><span class="badge badge-success">Đang mở</span></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
`);

// 14. Reports View
writeView('reports.html', 'Báo cáo & Thống kê', `
  <div class="page-header">
    <div>
      <h1 class="page-header-title">Báo cáo & Thống kê vận hành</h1>
      <p class="page-header-subtitle">Chỉ số tăng trưởng học viên, doanh thu học phí và tỷ lệ hoàn thành khóa học</p>
    </div>
    <div class="page-header-actions">
      <button class="btn btn-secondary">Xuất file Excel</button>
    </div>
  </div>

  <div class="grid grid-cols-3 gap-6" style="margin-bottom: 2rem;">
    <div class="card" style="padding: 1.5rem;">
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Tổng doanh thu Quý 4</div>
      <div style="font-size: 1.75rem; font-weight: 800; color: var(--primary-600); margin: 0.25rem 0;">154.500.000 ₫</div>
      <span class="text-success" style="font-size: 0.75rem;">↗ Tăng 18% so với Quý trước</span>
    </div>
    <div class="card" style="padding: 1.5rem;">
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Tỷ lệ học viên hoàn thành khóa</div>
      <div style="font-size: 1.75rem; font-weight: 800; color: #059669; margin: 0.25rem 0;">92.5%</div>
      <span class="text-success" style="font-size: 0.75rem;">Đạt chỉ tiêu xuất sắc</span>
    </div>
    <div class="card" style="padding: 1.5rem;">
      <div style="font-size: 0.8125rem; color: var(--text-muted);">Học viên tái đăng ký khóa mới</div>
      <div style="font-size: 1.75rem; font-weight: 800; color: #7C3AED; margin: 0.25rem 0;">64.8%</div>
      <span class="text-success" style="font-size: 0.75rem;">Tỷ lệ giữ chân cao</span>
    </div>
  </div>
`);

console.log('Finished generating Admin views.');
