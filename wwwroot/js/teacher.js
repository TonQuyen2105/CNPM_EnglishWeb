const teacherPage = document.querySelector('[data-teacher-page]');
const teacherContent = document.getElementById('teacher-data');

function element(tag, value, className) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (value != null) node.textContent = value;
  return node;
}

function link(label, href) {
  const node = element('a', label);
  node.href = href;
  return node;
}

function table(headers, rows, emptyMessage = 'Chưa có dữ liệu.') {
  if (!rows.length) return element('p', emptyMessage, 'card teacher-empty');
  const container = element('div', null, 'card teacher-table-scroll');
  container.tabIndex = 0;
  container.setAttribute('role', 'region');
  container.setAttribute('aria-label', 'Bảng dữ liệu có thể cuộn');
  container.style.overflowX = 'auto';
  const result = element('table', null, 'table');
  const head = result.createTHead().insertRow();
  headers.forEach(label => {
    const cell = element('th', label);
    cell.scope = 'col';
    head.append(cell);
  });
  const body = result.createTBody();
  for (const row of rows) {
    const tr = body.insertRow();
    for (const value of row) {
      const td = tr.insertCell();
      if (value instanceof Node) td.append(value);
      else td.textContent = value ?? '—';
    }
  }
  container.append(result);
  return container;
}

const statuses = {
  upcoming: 'Sắp khai giảng',
  ongoing: 'Đang diễn ra',
  active: 'Đang học',
  completed: 'Đã hoàn thành',
  cancelled: 'Đã hủy',
  submitted: 'Đã nộp',
  passed: 'Đạt',
  failed: 'Chưa đạt'
};

function statusLabel(value) {
  return statuses[value] ?? value;
}

function date(value) {
  return new Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date(value));
}

function schedule(rows) {
  const days = ['', 'Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  return table(['Thứ', 'Giờ dạy', 'Lớp', 'Phòng'], rows.map(item => [
    days[item.dayOfWeek], item.startTime.slice(0, 5) + ' – ' + item.endTime.slice(0, 5),
    item.classCode + ' · ' + item.className, item.room
  ]), 'Chưa có lịch dạy.');
}

function weeklySchedule(rows) {
  const current = new Date(new Date().toLocaleDateString('sv-SE', { timeZone: 'Asia/Ho_Chi_Minh' }) + 'T00:00:00Z');
  current.setUTCDate(current.getUTCDate() - (current.getUTCDay() + 6) % 7);
  const navigation = element('div', null, 'teacher-toolbar');
  const label = element('p');
  const list = element('div', null, 'teacher-calendar');
  for (const [title, step] of [['Tuần trước', -7], ['Tuần này', 0], ['Tuần sau', 7]]) {
    const button = element('button', title, 'btn btn-secondary');
    button.addEventListener('click', () => {
      if (step === 0) current.setTime(weekStart);
      else current.setUTCDate(current.getUTCDate() + step);
      show();
    });
    navigation.append(button);
  }
  const weekStart = current.getTime();
  function show() {
    const end = new Date(current);
    end.setUTCDate(end.getUTCDate() + 6);
    label.textContent = date(current) + ' – ' + date(end);
    const ordered = [];
    for (let offset = 0; offset < 7; offset++) {
      const day = new Date(current);
      day.setUTCDate(day.getUTCDate() + offset);
      const dayKey = day.toISOString().slice(0, 10);
      ordered.push(...rows.filter(item => item.dayOfWeek === day.getUTCDay() + 1
        && item.status !== 'cancelled' && item.startDate.slice(0, 10) <= dayKey
        && item.endDate.slice(0, 10) >= dayKey));
    }
    list.replaceChildren(calendar(ordered));
  }
  show();
  const card = element('div', null, 'card');
  card.append(navigation, label, list);
  teacherContent.append(card);
}

function renderTablePage(page, data, profile) {
  switch (page) {
    case 'dashboard': {
      const grid = element('div', null, 'grid grid-cols-3 gap-6');
      for (const [label, value] of [
        ['Lớp đang dạy', data.activeClasses], ['Học viên đang học', data.students],
        ['Câu trả lời cần chấm', data.pending]
      ]) {
        const card = element('div', null, 'card');
        card.append(element('p', label), element('strong', value));
        grid.append(card);
      }
      teacherContent.append(grid, element('h2', 'Ca dạy hôm nay'), schedule(data.today));
      break;
    }
    case 'classes':
      teacherContent.append(table(['Mã lớp', 'Tên lớp', 'Sĩ số', 'Trạng thái'], data.map(item => [
        item.classCode, link(item.className, '/Teacher/class-detail.html?id=' + item.id),
        item.students + ' / ' + item.maxStudents, statusLabel(item.status)
      ]), 'Chưa được phân công lớp.'));
      break;
    case 'class-detail':
      document.querySelector('.page-header-title').textContent = data.class.className;
      document.querySelector('.page-header-subtitle').textContent = data.class.classCode + ' · ' + date(data.class.startDate) + ' – ' + date(data.class.endDate);
      teacherContent.append(
        element('h2', data.class.classCode + ' · ' + data.class.className),
        element('p', date(data.class.startDate) + ' – ' + date(data.class.endDate) + ' · ' + statusLabel(data.class.status)),
        table(['Học viên', 'Email', 'Trạng thái ghi danh'], data.students.map(item => [
          item.fullName, item.email, statusBadge(item.status)
        ]), 'Lớp chưa có học viên.'));
      break;
    case 'schedule':
      weeklySchedule(data);
      break;
    case 'lessons':
      teacherContent.append(table(['Khóa học', 'Thứ tự', 'Bài học', 'Loại', 'Nội dung'], data.map(item => [
        item.course, item.orderIndex, item.title, item.contentType, item.content
      ]), 'Chưa có bài giảng trong các khóa được phân công.'));
      break;
    case 'questions':
      teacherContent.append(table(['Câu hỏi', 'Loại', 'Điểm', 'Đáp án'], data.map(item => [
        item.questionText, item.questionType, item.defaultPoints,
        [...item.options.map(option => option.optionText + (option.isCorrect ? ' · Đáp án đúng' : '')),
          ...item.answers].join(' · ')
      ]), 'Chưa có câu hỏi trong bài giảng hoặc đề thi của bạn.'));
      break;
    case 'exams':
      teacherContent.append(table(['Đề thi', 'Khóa học', 'Số phút', 'Số câu', 'Lượt nộp', 'Trạng thái'], data.map(item => [
        item.title, item.course, item.durationMinutes, item.questions, item.submissions,
        badge(item.isPublished ? 'Đã công bố' : 'Bản nháp', item.isPublished ? 'success' : 'neutral')
      ]), 'Chưa có đề thi.'));
      break;
    case 'grading':
      teacherContent.append(table(['Đề thi', 'Học viên', 'Câu hỏi', 'Câu trả lời'], data.map(item => [
        item.title, item.fullName, item.questionText, item.textAnswer
      ]), 'Chưa có bài cần chấm.'));
      break;
    case 'results':
      teacherContent.append(table(['Đề thi', 'Học viên', 'Lượt thi', 'Điểm', 'Trạng thái'], data.map(item => [
        item.title, item.fullName, item.attemptNumber, item.totalScore, statusBadge(item.status)
      ]), 'Chưa có bài thi đã nộp.'));
      break;
    case 'profile': {
      const card = element('div', null, 'card');
      card.append(element('h2', profile.fullName), element('p', profile.email),
        element('p', profile.qualification || 'Chưa có thông tin bằng cấp.'),
        element('p', profile.bio || 'Chưa có giới thiệu.'),
        element('p', profile.isVerified ? 'Đã xác minh' : 'Chưa xác minh'));
      teacherContent.append(card);
      break;
    }
  }
}

async function loadTeacherData() {
  teacherContent.setAttribute('aria-busy', 'true');
  const loading = element('div', null, 'teacher-skeleton');
  loading.setAttribute('aria-hidden', 'true');
  for (let i = 0; i < 4; i++) loading.append(element('span'));
  teacherContent.replaceChildren(element('p', 'Đang tải không gian giảng dạy…', 'teacher-muted'), loading);
  const page = teacherPage.dataset.teacherPage;
  const id = new URLSearchParams(window.location.search).get('id');
  const url = '/api/teacher/' + page + (id ? '?id=' + encodeURIComponent(id) : '');
  try {
    const response = await fetch(url, { cache: 'no-store' });
    if (response.status === 401) {
      showState('Phiên đăng nhập đã kết thúc', 'Đăng nhập để xem lớp học và công việc của bạn.', '/login.html');
      return;
    }
    if (response.status === 403) {
      showState('Chưa có quyền truy cập', 'Vui lòng liên hệ quản trị viên để kiểm tra hồ sơ giảng viên.');
      return;
    }
    if (response.status === 404 && page === 'class-detail') {
      showState('Không tìm thấy lớp học', 'Lớp học này không thuộc danh sách được phân công.');
      return;
    }
    if (response.status === 404) {
      showState('Chưa kết nối được dữ liệu', 'Vui lòng thử lại sau khi ứng dụng khởi động xong.');
      return;
    }
    if (!response.ok) throw new Error('Unable to load teacher data');

    const { profile, data } = await response.json();
    const initials = profile.fullName.trim().split(/\s+/).slice(-2).map(part => part[0]).join('');
    const fields = {
      name: profile.fullName,
      email: profile.email,
      initials,
      qualification: profile.qualification
    };
    for (const [field, value] of Object.entries(fields)) {
      document.querySelectorAll('[data-teacher-' + field + ']').forEach(node => {
        node.textContent = value ?? '';
        node.hidden = !value;
      });
    }
    teacherContent.replaceChildren();
    render(page, data, profile);
    addPageActions(page, data);
  } catch (_) {
    showState('Không tải được dữ liệu', 'Kết nối bị gián đoạn. Bạn có thể thử tải lại.');
  } finally {
    teacherContent.setAttribute('aria-busy', 'false');
  }
}

document.querySelectorAll('[data-teacher-logout]').forEach(node => {
  node.addEventListener('click', async event => {
    event.preventDefault();
    try {
      const response = await fetch('/Account/TeacherSession', { method: 'DELETE' });
      if (!response.ok) throw new Error('Unable to sign out');
      localStorage.removeItem('session_user');
      window.location.href = '/';
    } catch (_) {
      teacherContent.textContent = 'Chưa đăng xuất được. Vui lòng thử lại.';
    }
  });
});

initializeTeacherShell();
loadTeacherData();

function panel(title, className = '') {
  const card = element('section', null, 'card ' + className);
  card.style.padding = '1.5rem';
  if (title) card.append(element('h3', title, 'card-title'));
  return card;
}

function badge(value, tone = 'primary') {
  return element('span', value, 'badge badge-' + tone);
}

function calendar(rows) {
  const times = [...new Set(rows.map(r => r.startTime + ' – ' + r.endTime))].sort();
  return table(['Ca dạy', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật'],
    times.map(time => [time.split(' – ').map(t => t.slice(0, 5)).join(' – '),
      ...[2, 3, 4, 5, 6, 7, 1].map(day => {
        const cell = element('div');
        const matches = rows.filter(r => r.startTime + ' – ' + r.endTime === time && r.dayOfWeek === day);
        for (const row of matches) {
          const session = element('div', null, 'teacher-session');
          session.append(element('strong', row.classCode), element('div', row.className, 'teacher-muted'),
            element('div', row.room, 'teacher-muted'));
          cell.append(session);
        }
        if (!matches.length) cell.textContent = '—';
        return cell;
      })
    ]), 'Tuần này chưa có lịch dạy.');
}

function render(page, data, profile) {
  if (page === 'grading') {
    const params = new URLSearchParams(window.location.search);
    const classId = Number(params.get('classId')), examId = Number(params.get('examId')), attemptId = Number(params.get('attemptId'));
    if ([classId, examId, attemptId].every(id => Number.isSafeInteger(id) && id > 0)) openGradingAttempt(classId, examId, attemptId);
    else if ([classId, examId].every(id => Number.isSafeInteger(id) && id > 0)) openGradingRoster(classId, examId);
    else openGradingClasses();
    return;
  }
  if (page === 'results') { openGradebook(); return; }
  if (page === 'lessons') { openLessonWorkspace(); return; }
  if (page === 'questions') { openQuestionBank(); return; }
  if (page === 'exams') { openExamWorkspace(); return; }
  if (page === 'dashboard') {
    document.querySelector('.page-header-title').textContent = 'Chào mừng trở lại, ' + profile.fullName + '!';
    const grid = element('div', null, 'grid grid-cols-4 gap-6 teacher-stats');
    for (const [label, value, subtitle, tone] of [
      ['Lớp đang phụ trách', data.activeClasses, 'Lớp đang diễn ra', 'primary'],
      ['Ca dạy hôm nay', data.today.length, 'Theo lịch giảng dạy', 'accent'],
      ['Bài nộp chờ chấm', data.pending, 'Câu trả lời cần đánh giá', 'warning'],
      ['Học viên đang học', data.students, 'Trong các lớp được phân công', 'success']
    ]) {
      const card = element('div', null, 'stat-card');
      const info = element('div', null, 'stat-info');
      info.append(element('span', label, 'stat-label'), element('span', value, 'stat-value'),
        element('span', subtitle, 'stat-sub text-muted'));
      const icon = element('div', null, 'stat-icon ' + (tone === 'warning' ? 'warning' : 'primary'));
      const iconPages = { primary: 'classes', accent: 'schedule', warning: 'grading', success: 'profile' };
      const source = document.querySelector('.sidebar-nav a[href="/Teacher/' + iconPages[tone] + '.html"] svg');
      if (source) icon.append(source.cloneNode(true));
      card.append(info, icon);
      grid.append(card);
    }
    const columns = element('div', null, 'grid grid-cols-12 gap-6');
    const today = panel('Ca dạy hôm nay', 'col-span-7');
    const stack = element('div', null, 'teacher-stack');
    for (const row of data.today) {
      const item = element('div', null, 'teacher-row teacher-agenda');
      item.style.borderLeft = '4px solid var(--primary-600)';
      item.append(badge(row.classCode), element('h3', row.className),
        element('p', row.startTime.slice(0, 5) + ' – ' + row.endTime.slice(0, 5) + ' · ' + row.room, 'teacher-muted'));
      stack.append(item);
    }
    if (!data.today.length) stack.append(element('p', 'Hôm nay chưa có lịch dạy.', 'teacher-muted'));
    today.append(stack);
    const grading = panel('Bài nộp cần chấm', 'col-span-5');
    const summary = element('div', null, 'teacher-grading-summary');
    summary.append(element('strong', data.pending), element('p', 'câu trả lời đang chờ đánh giá', 'teacher-muted'));
    grading.append(summary);
    const action = link('Vào chấm bài', '/Teacher/grading.html');
    action.className = 'btn btn-primary';
    action.style.marginTop = '1rem';
    grading.append(action);
    columns.append(today, grading);
    teacherContent.append(grid, columns);
    return;
  }

  if (page === 'classes') {
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const item of data) {
      const card = panel('', 'card-hover');
      const top = element('div', null, 'teacher-split');
      top.append(badge(item.classCode), badge(statusLabel(item.status), item.status === 'ongoing' ? 'success' : 'neutral'));
      const title = element('h3', item.className);
      title.style.cssText = 'font-size:1.125rem;font-weight:700;margin:1rem 0';
      const detail = element('div', 'Sĩ số: ' + item.students + ' / ' + item.maxStudents, 'teacher-muted');
      detail.style.cssText += ';border-top:1px dashed var(--border-color);padding-top:1rem';
      const action = link('Chi tiết lớp', '/Teacher/class-detail.html?id=' + item.id);
      action.className = 'btn btn-sm btn-primary';
      action.style.cssText = 'width:100%;margin-top:1.25rem';
      const progress = element('div', null, 'teacher-progress');
      const fill = element('span');
      fill.style.width = Math.min(100, item.maxStudents > 0 ? item.students / item.maxStudents * 100 : 0) + '%';
      progress.append(fill);
      card.append(top, title, detail, progress, action);
      grid.append(card);
    }
    teacherContent.append(data.length ? grid : element('p', 'Chưa được phân công lớp.', 'card teacher-empty'));
    return;
  }

  if (page === 'lessons' || page === 'questions' || page === 'grading') {
    const card = panel('');
    const stack = element('div', null, 'teacher-stack');
    for (const item of data) {
      const row = element('article', null, 'teacher-row');
      if (page === 'lessons') {
        row.append(badge('Bài ' + item.orderIndex), element('h3', item.title),
          element('p', item.course + ' · ' + typeLabel(item.contentType), 'teacher-muted'));
        const details = element('details');
        details.append(element('summary', 'Xem nội dung bài học'), element('div', item.content || 'Chưa có nội dung.', 'teacher-copy'));
        row.append(details);
      } else if (page === 'questions') {
        row.style.background = 'var(--neutral-50)';
        const top = element('div', null, 'teacher-split');
        top.append(badge(typeLabel(item.questionType), 'info'), badge(item.defaultPoints + ' điểm', 'neutral'));
        row.append(top, element('p', item.questionText, 'teacher-copy'));
        for (const option of item.options)
          row.append(element('div', option.optionText + (option.isCorrect ? ' · Đáp án đúng' : ''), option.isCorrect ? 'text-success' : 'teacher-muted'));
        for (const answer of item.answers) row.append(element('div', 'Đáp án: ' + answer, 'text-success'));
      } else {
        const top = element('div', null, 'teacher-split');
        top.append(element('h3', 'Bài nộp: ' + item.fullName), badge('Chờ chấm điểm', 'warning'));
        row.append(top, element('p', item.title, 'teacher-muted'), element('p', item.questionText, 'teacher-copy'));
        const answer = element('div', item.textAnswer || 'Chưa có nội dung văn bản.', 'teacher-copy teacher-row');
        answer.style.background = 'var(--neutral-50)';
        row.append(answer);
      }
      stack.append(row);
    }
    if (!data.length) stack.append(element('p', 'Chưa có dữ liệu.', 'teacher-empty'));
    card.append(stack);
    teacherContent.append(card);
    return;
  }

  if (page === 'profile') {
    const grid = element('div', null, 'grid grid-cols-12 gap-6');
    const identity = panel('', 'col-span-4');
    identity.style.textAlign = 'center';
    const initials = profile.fullName.trim().split(/\s+/).slice(-2).map(p => p[0]).join('');
    identity.append(element('div', initials, 'avatar avatar-lg teacher-profile-avatar'),
      element('h2', profile.fullName), element('p', 'Giảng viên', 'teacher-muted'),
      badge(profile.isVerified ? 'Đã xác minh' : 'Chưa xác minh', profile.isVerified ? 'success' : 'neutral'));
    const details = panel('Thông tin chi tiết', 'col-span-8');
    for (const [label, value] of [['Email công tác', profile.email], ['Bằng cấp', profile.qualification], ['Giới thiệu', profile.bio]]) {
      details.append(element('p', label, 'teacher-muted'), element('p', value || 'Chưa cập nhật', 'teacher-copy'));
    }
    grid.append(identity, details);
    teacherContent.append(grid);
    return;
  }
  renderTablePage(page, data, profile);
}

function typeLabel(value) {
  return {
    speaking: 'Luyện nói', writing: 'Luyện viết', listening: 'Luyện nghe', reading: 'Đọc hiểu',
    vocabulary: 'Từ vựng', grammar: 'Ngữ pháp', quiz: 'Bài kiểm tra',
    multiple_choice: 'Trắc nghiệm', fill_blank: 'Điền từ', pronunciation: 'Phát âm', essay: 'Tự luận'
  }[value] || value;
}

function statusBadge(value) {
  const tone = { active: 'success', ongoing: 'success', passed: 'success', completed: 'success',
    cancelled: 'danger', failed: 'danger', upcoming: 'warning', submitted: 'warning' }[value] || 'neutral';
  return badge(statusLabel(value), tone);
}

function showState(title, message, loginUrl) {
  const state = element('section', null, 'teacher-state teacher-error');
  state.setAttribute('role', 'alert');
  state.append(element('h2', title), element('p', message));
  if (loginUrl) {
    const button = link('Đăng nhập', loginUrl);
    button.className = 'btn btn-primary';
    state.append(button);
  } else {
    const retry = element('button', 'Thử lại', 'btn btn-primary');
    retry.addEventListener('click', loadTeacherData);
    state.append(retry);
  }
  teacherContent.replaceChildren(state);
}

function addPageActions(page, data) {
  if (['grading', 'results', 'lessons', 'questions', 'exams'].includes(page)) return;
  const header = document.querySelector('.page-header');
  header.querySelector('.teacher-primary-action')?.remove();
  const destinations = {
    dashboard: ['Xem lịch giảng dạy', 'schedule'],
    classes: ['Xem lịch tuần', 'schedule'],
    'class-detail': ['Danh sách lớp', 'classes'],
    exams: ['Xem bài chờ chấm', 'grading'],
    profile: ['Lớp đang phụ trách', 'classes']
  };
  if (destinations[page]) {
    const [label, target] = destinations[page];
    const action = link(label, '/Teacher/' + target + '.html');
    action.className = 'btn btn-primary teacher-primary-action';
    header.append(action);
  } else {
    const action = element('button', 'Làm mới dữ liệu', 'btn btn-primary teacher-primary-action');
    action.addEventListener('click', loadTeacherData);
    header.append(action);
  }

  if (!Array.isArray(data) || page === 'schedule' || !data.length) return;
  const candidates = page === 'classes' ? '.card-hover'
    : ['lessons', 'questions', 'grading'].includes(page) ? '.teacher-stack > article' : 'tbody tr';
  const items = [...teacherContent.querySelectorAll(candidates)];
  const toolbar = element('div', null, 'teacher-toolbar');
  const search = element('input', null, 'teacher-search');
  search.type = 'search';
  search.placeholder = 'Tìm trong danh sách…';
  search.setAttribute('aria-label', 'Tìm trong danh sách');
  const count = element('span', items.length + ' kết quả', 'teacher-count');
  count.setAttribute('aria-live', 'polite');
  const empty = element('p', 'Không có kết quả phù hợp. Thử từ khóa khác.', 'teacher-empty');
  empty.hidden = true;
  function normalize(value) {
    return value.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd').toLowerCase();
  }
  search.addEventListener('input', () => {
    const term = normalize(search.value.trim());
    let visible = 0;
    for (const item of items) {
      item.hidden = !normalize(item.textContent).includes(term);
      if (!item.hidden) visible++;
    }
    count.textContent = visible + ' / ' + items.length + ' kết quả';
    empty.hidden = visible !== 0;
  });
  toolbar.append(search, count);
  teacherContent.prepend(toolbar);
  teacherContent.append(empty);
}

function initializeTeacherShell() {
  document.getElementById('teacher-current-date').textContent = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long', day: 'numeric', month: 'long', timeZone: 'Asia/Ho_Chi_Minh'
  }).format(new Date());
  const menu = document.getElementById('btn-sidebar-toggle');
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.getElementById('sidebar-backdrop');
  const avatar = document.getElementById('avatar-dropdown-btn');
  const dropdown = document.getElementById('avatar-dropdown-menu');

  // Shared site.js handles pointer toggles. Keep accessible states synchronized.
  new MutationObserver(() => {
    menu.setAttribute('aria-expanded', String(sidebar.classList.contains('sidebar-open')));
    avatar.setAttribute('aria-expanded', String(dropdown.classList.contains('show')));
  }).observe(document.querySelector('.app-layout'), { subtree: true, attributes: true, attributeFilter: ['class'] });

  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    if (sidebar.classList.contains('sidebar-open')) {
      sidebar.classList.remove('sidebar-open');
      backdrop.classList.remove('active');
      menu.focus();
    }
    if (dropdown.classList.contains('show')) {
      dropdown.classList.remove('show');
      avatar.focus();
    }
  });
}

async function gradingRequest(path = '', options = {}) {
  const response = await fetch('/api/teacher/grading-workspace' + path, { cache: 'no-store', ...options });
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    const messages = {
      401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
      403: 'Tài khoản không có quyền truy cập lớp hoặc bài thi này.',
      404: 'Không tìm thấy lớp hoặc đề thi. Nếu vừa cập nhật code, hãy khởi động lại ứng dụng.',
      500: 'Máy chủ gặp lỗi khi đọc dữ liệu. Kiểm tra terminal đang chạy dotnet run.',
      503: 'Máy chủ chưa kết nối được database. Vui lòng thử lại.'
    };
    const message = body.message || messages[response.status] || 'Không tải hoặc lưu được dữ liệu.';
    throw new Error(message + ' (HTTP ' + response.status + (body.traceId ? ' · Mã tra cứu: ' + body.traceId : '') + ')');
  }
  return response.json();
}

function gradingButton(label, action, primary = false) {
  const button = element('button', label, 'btn ' + (primary ? 'btn-primary' : 'btn-secondary'));
  button.type = 'button';
  button.addEventListener('click', action);
  return button;
}

async function gradingView(load) {
  teacherContent.replaceChildren(element('p', 'Đang tải dữ liệu…', 'teacher-empty'));
  teacherContent.setAttribute('aria-busy', 'true');
  try { await load(); }
  catch (error) {
    const state = panel('Chưa tải được dữ liệu');
    state.setAttribute('role', 'alert');
    state.append(element('p', error.message), gradingButton('Thử lại', () => gradingView(load)));
    teacherContent.replaceChildren(state);
  } finally { teacherContent.setAttribute('aria-busy', 'false'); }
}

function openGradingClasses() {
  return gradingView(async () => {
    const classes = await gradingRequest();
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const item of classes) {
      const card = panel('');
      const top = element('div', null, 'teacher-split');
      top.append(badge(item.classCode), statusBadge(item.status));
      card.append(top, element('h2', item.className, 'teacher-copy'), element('p', item.students + ' học viên', 'teacher-muted'),
        gradingButton('Xem danh sách học viên', () => openGradingRoster(item.id), true));
      grid.append(card);
    }
    teacherContent.replaceChildren(classes.length ? grid : element('p', 'Bạn chưa được phân công lớp học.', 'card teacher-empty'));
  });
}

function openGradingRoster(classId, examId, success) {
  return gradingView(async () => {
    const data = await gradingRequest('?classId=' + classId + (examId ? '&examId=' + examId : ''));
    const toolbar = element('div', null, 'teacher-toolbar');
    toolbar.append(gradingButton('← Danh sách lớp', openGradingClasses));
    const heading = element('h2', data.class.classCode + ' · ' + data.class.className);
    const note = element('p', 'Đề thi dùng chung theo khóa học. Trạng thái hiển thị theo lượt làm mới nhất của học viên.', 'teacher-muted');
    teacherContent.replaceChildren(toolbar, heading, note);
    if (success) {
      const notice = element('p', success, 'teacher-save-success');
      notice.setAttribute('role', 'status'); teacherContent.append(notice);
    }
    if (!data.exams.length) {
      teacherContent.append(element('p', 'Khóa học chưa có đề đã công bố chứa câu tự luận do bạn tạo.', 'card teacher-empty')); return;
    }
    const label = element('label', 'Đề thi cần chấm', 'teacher-field');
    const select = element('select', null, 'teacher-search');
    for (const exam of data.exams) {
      const option = element('option', exam.title); option.value = exam.id; select.append(option);
    }
    select.value = data.examId;
    select.addEventListener('change', () => openGradingRoster(classId, select.value));
    label.append(select); teacherContent.append(label);
    const labels = { not_submitted: 'Chưa nộp bài', in_progress: 'Chưa nộp', pending_review: 'Chờ chấm', graded: 'Đã chấm' };
    const counts = element('p', `${data.students.length} học viên · ${data.students.filter(s => s.submittedAt).length} đã nộp · ${data.students.filter(s => s.status === 'pending_review').length} chờ chấm`, 'teacher-muted');
    teacherContent.append(counts, table(['Học viên', 'Email', 'Trạng thái', 'Nộp lúc', 'Thao tác'], data.students.map(student => [
      student.fullName, student.email, badge(labels[student.status], student.status === 'graded' ? 'success' : student.status === 'pending_review' ? 'warning' : 'neutral'),
      student.submittedAt ? new Date(student.submittedAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '—',
      student.submittedAt ? gradingButton(student.status === 'graded' ? 'Xem / sửa điểm' : 'Chấm bài', () => openGradingAttempt(classId, data.examId, student.attemptId), student.status !== 'graded') : 'Chưa có bài để chấm'
    ]), 'Lớp chưa có học viên.'));
  });
}

function openGradingAttempt(classId, examId, attemptId) {
  return gradingView(async () => {
    const data = await gradingRequest('/attempts/' + attemptId + '?classId=' + classId);
    const form = element('form', null, 'teacher-stack');
    const alreadyGraded = data.answers.length > 0 && data.answers.every(a => a.gradingStatus === 'graded');
    let readonly = alreadyGraded;
    let dirty = false;
    const protect = event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', protect);
    const leave = () => {
      if (dirty && !window.confirm('Bạn có điểm hoặc nhận xét chưa lưu. Rời bài chấm này?')) return;
      window.removeEventListener('beforeunload', protect);
      openGradingRoster(classId, examId);
    };
    const header = panel(data.attempt.fullName);
    header.append(element('p', data.attempt.title + ' · Lượt ' + data.attempt.attemptNumber, 'teacher-muted'), gradingButton('← Danh sách học viên', leave));
    if (new URLSearchParams(window.location.search).get('from') === 'results') {
      const back = gradingButton('← Sổ điểm', () => {
        if (dirty && !window.confirm('Rời bài chấm và bỏ thay đổi chưa lưu?')) return;
        dirty = false; window.removeEventListener('beforeunload', protect);
        window.location.href = '/Teacher/results.html?classId=' + classId + '&examId=' + examId;
      });
      header.append(back);
    }
    const fields = [];
    for (const [index, answer] of data.answers.entries()) {
      const card = panel('Câu ' + (index + 1) + ' · Tối đa ' + answer.points + ' điểm');
      card.append(element('p', answer.questionText, 'teacher-copy'), element('p', answer.textAnswer || 'Học viên không nhập câu trả lời.', 'teacher-row teacher-copy'));
      const scoreLabel = element('label', 'Điểm', 'teacher-field');
      const score = element('input', null, 'teacher-search');
      score.type = 'number'; score.min = '0'; score.max = answer.points; score.step = '0.01'; score.required = true;
      score.value = answer.scoreEarned ?? ''; score.disabled = readonly;
      scoreLabel.append(score);
      const feedbackLabel = element('label', 'Nhận xét', 'teacher-field');
      const feedback = element('textarea', null, 'teacher-search'); feedback.rows = 3; feedback.maxLength = 4000;
      feedback.value = answer.feedback || ''; feedback.disabled = readonly; feedbackLabel.append(feedback);
      card.append(scoreLabel, feedbackLabel); form.append(card); fields.push({ id: answer.id, score, feedback, originalScore: answer.scoreEarned, originalFeedback: answer.feedback, expectedGradedAt: answer.gradedAt });
    }
    form.addEventListener('input', () => { dirty = true; });
    const message = element('p'); message.setAttribute('role', 'alert');
    const actions = element('div', null, 'teacher-grading-actions');
    const saveLabel = alreadyGraded ? 'Lưu thay đổi điểm' : 'Lưu điểm và nhận xét';
    const save = element('button', saveLabel, 'btn btn-primary'); save.type = 'submit';
    actions.append(message, save);
    if (data.answers.length) form.append(actions);
    actions.hidden = readonly;
    if (alreadyGraded) {
      const edit = gradingButton('Sửa điểm', () => setEditing(true), true);
      const cancel = gradingButton('Hủy chỉnh sửa', () => {
        if (dirty && !window.confirm('Bỏ các thay đổi điểm và nhận xét chưa lưu?')) return;
        fields.forEach(f => { f.score.value = f.originalScore ?? ''; f.feedback.value = f.originalFeedback || ''; });
        dirty = false; message.textContent = ''; setEditing(false);
      });
      function setEditing(editing) {
        readonly = !editing;
        fields.forEach(f => { f.score.disabled = readonly; f.feedback.disabled = readonly; });
        actions.hidden = readonly; edit.hidden = editing; cancel.hidden = readonly;
        if (editing) fields[0].score.focus();
        else edit.focus();
      }
      cancel.hidden = true;
      header.append(edit, cancel);
    }
    if (!data.answers.length) form.append(element('p', 'Bài nộp không có câu trả lời tự luận để chấm.', 'teacher-empty'));
    form.addEventListener('submit', async event => {
      event.preventDefault();
      if (readonly || save.disabled || !form.reportValidity() || !window.confirm((alreadyGraded ? 'Xác nhận sửa điểm và cập nhật kết quả của ' : 'Xác nhận lưu điểm và nhận xét cho bài nộp của ') + data.attempt.fullName + '?')) return;
      save.disabled = true; save.textContent = 'Đang lưu…'; message.textContent = '';
      try {
        const result = await gradingRequest('/attempts/' + attemptId, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': data.token }, body: JSON.stringify({ classId, answers: fields.map(f => ({ id: f.id, score: Number(f.score.value), feedback: f.feedback.value, expectedGradedAt: f.expectedGradedAt })) }) });
        dirty = false; window.removeEventListener('beforeunload', protect);
        if (new URLSearchParams(window.location.search).get('from') === 'results') {
          window.location.href = '/Teacher/results.html?classId=' + classId + '&examId=' + examId;
        } else await openGradingRoster(classId, examId, result.message);
      } catch (error) { message.textContent = error.message; }
      finally { save.disabled = false; save.textContent = saveLabel; }
    });
    teacherContent.replaceChildren(header, form);
  });
}
