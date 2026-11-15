// Course-scoped lesson workspace. Text is rendered with textContent, never as HTML.
const lessonTypes = { vocabulary: 'Từ vựng', grammar: 'Ngữ pháp', listening: 'Luyện nghe', reading: 'Đọc hiểu', speaking: 'Luyện nói', writing: 'Luyện viết', quiz: 'Bài kiểm tra' };
const lessonFilters = new Map();
async function lessonRequest(path = '', options = {}) {
  const response = await fetch('/api/teacher/lessons-workspace' + path, { cache: 'no-store', ...options });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.message || ({
    400: 'Yêu cầu không hợp lệ hoặc phiên biểu mẫu đã hết hạn. Kiểm tra nội dung và mở lại biểu mẫu nếu cần.',
    401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
    403: 'Bạn không có quyền thao tác với bài giảng này.',
    404: 'Không tìm thấy khóa học hoặc bài giảng được phân công.'
  }[response.status] || 'Không tải hoặc lưu được bài giảng (HTTP ' + response.status + '). Vui lòng thử lại.'));
  return body;
}
function lessonDate(value) {
  return value ? new Date(value).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'Chưa cập nhật';
}
function lessonNotice(message) {
  const notice = element('p', message, 'teacher-save-success'); notice.setAttribute('role', 'status'); return notice;
}
function openLessonWorkspace() {
  const params = new URLSearchParams(location.search);
  const courseId = Number(params.get('courseId'));
  if (Number.isSafeInteger(courseId) && courseId > 0) return openLessonCourse(courseId);
  return openLessonCourses();
}
function openLessonCourses() {
  history.replaceState(null, '', '/Teacher/lessons.html');
  return gradingView(async () => {
    const courses = await lessonRequest();
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const course of courses) {
      const card = panel(course.title);
      const level = { beginner: 'Cơ bản', intermediate: 'Trung cấp', advanced: 'Nâng cao' }[course.level] || course.level;
      card.append(badge(level), element('p', course.lessons + ' bài giảng', 'teacher-muted'),
        gradingButton('Mở khóa học', () => openLessonCourse(course.id), true)); grid.append(card);
    }
    teacherContent.replaceChildren(courses.length ? grid : element('p', 'Chưa có khóa học được phân công cho bạn.', 'card teacher-empty'));
  });
}
function openLessonCourse(courseId, success) {
  return gradingView(async () => {
    const data = await lessonRequest('?courseId=' + courseId);
    history.replaceState(null, '', '/Teacher/lessons.html?courseId=' + courseId);
    const bar = element('div', null, 'teacher-toolbar lesson-toolbar');
    bar.append(gradingButton('← Khóa học', openLessonCourses), gradingButton('Thêm bài giảng', () => showLessonForm(data), true));
    teacherContent.replaceChildren(bar, element('h2', data.course.title), element('p', 'Bài giảng được dùng chung cho các lớp của khóa học này.', 'teacher-muted'));
    if (success) teacherContent.append(lessonNotice(success));
    const filters = element('div', null, 'teacher-toolbar lesson-toolbar');
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm tiêu đề bài giảng'; search.setAttribute('aria-label', 'Tìm tiêu đề bài giảng');
    const type = element('select', null, 'teacher-search'); type.setAttribute('aria-label', 'Lọc loại bài');
    for (const [value, text] of Object.entries({ all: 'Tất cả loại bài', ...lessonTypes })) { const option = element('option', text); option.value = value; type.append(option); }
    const saved = lessonFilters.get(courseId); search.value = saved?.search || ''; type.value = saved?.type || 'all';
    filters.append(search, type); const list = element('div', null, 'teacher-stack');
    const count = element('p', null, 'teacher-muted'); count.setAttribute('aria-live', 'polite');
    teacherContent.append(filters, count, list);
    const normalize = value => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    function draw() {
      lessonFilters.set(courseId, { search: search.value, type: type.value });
      const rows = data.lessons.filter(l => normalize(l.title).includes(normalize(search.value.trim())) && (type.value === 'all' || l.contentType === type.value));
      count.textContent = rows.length + ' / ' + data.lessons.length + ' bài giảng'; list.replaceChildren();
      for (const lesson of rows) {
        const card = panel('Bài ' + lesson.orderIndex + ' · ' + lesson.title);
        const buttons = element('div', null, 'lesson-actions');
        buttons.append(gradingButton('Xem chi tiết', () => openLessonDetail(data, lesson.id)), gradingButton('Sửa bài', () => editLesson(data, lesson.id)));
        card.append(badge(lessonTypes[lesson.contentType] || lesson.contentType), element('p', 'Cập nhật: ' + lessonDate(lesson.updatedAt || lesson.createdAt), 'teacher-muted'), buttons);
        list.append(card);
      }
      if (!rows.length) list.append(element('p', data.lessons.length ? 'Không có bài phù hợp với bộ lọc.' : 'Khóa học chưa có bài giảng. Chọn “Thêm bài giảng” để bắt đầu.', 'card teacher-empty'));
    }
    search.addEventListener('input', draw); type.addEventListener('change', draw); draw();
  });
}
function openLessonDetail(data, id, success) {
  return gradingView(async () => {
    const response = await lessonRequest('/' + id), lesson = response.lesson;
    const card = panel('Bài ' + lesson.orderIndex + ' · ' + lesson.title);
    const buttons = element('div', null, 'teacher-toolbar lesson-toolbar');
    buttons.append(gradingButton('← Danh sách bài', () => openLessonCourse(data.course.id)), gradingButton('Sửa bài giảng', () => showLessonForm({ ...data, token: response.token }, lesson), true));
    card.append(badge(lessonTypes[lesson.contentType] || lesson.contentType), element('p', data.course.title, 'teacher-muted'),
      element('p', 'Ngày tạo: ' + lessonDate(lesson.createdAt) + ' · Cập nhật: ' + lessonDate(lesson.updatedAt), 'teacher-muted'),
      element('div', lesson.content || 'Bài giảng chưa có nội dung.', 'teacher-copy lesson-content'));
    teacherContent.replaceChildren(buttons);
    if (success) teacherContent.append(lessonNotice(success));
    teacherContent.append(card);
  });
}
function editLesson(data, id) {
  return gradingView(async () => { const response = await lessonRequest('/' + id); showLessonForm({ ...data, token: response.token }, response.lesson); });
}
function showLessonForm(data, lesson = null) {
  let dirty = false, saving = false;
  const form = element('form', null, 'card lesson-form');
  form.append(element('h2', lesson ? 'Sửa bài giảng' : 'Thêm bài giảng'), element('p', data.course.title, 'teacher-muted'),
    element('p', 'Nội dung được dùng chung cho các lớp thuộc khóa học. Các giảng viên được phân công khóa này có thể chỉnh sửa.', 'lesson-shared-note'));
  function field(text, input) { const label = element('label', text, 'teacher-field'); label.append(input); form.append(label); return input; }
  const title = field('Tiêu đề *', element('input', null, 'teacher-search')); title.required = true; title.maxLength = 200; title.value = lesson?.title || '';
  const order = field('Thứ tự bài *', element('input', null, 'teacher-search')); order.type = 'number'; order.required = true; order.min = '1'; order.max = '100000'; order.step = '1';
  order.value = lesson?.orderIndex ?? Math.min(100000, Math.max(0, ...data.lessons.map(l => l.orderIndex)) + 1);
  const type = field('Loại bài *', element('select', null, 'teacher-search')); type.required = true;
  Object.entries(lessonTypes).forEach(([value, text]) => { const option = element('option', text); option.value = value; type.append(option); }); type.value = lesson?.contentType || 'vocabulary';
  const content = field('Nội dung bài giảng', element('textarea', null, 'teacher-search')); content.rows = 14; content.maxLength = 100000; content.value = lesson?.content || '';
  const error = element('p', null, 'lesson-form-error'); error.setAttribute('role', 'alert');
  const actions = element('div', null, 'teacher-grading-actions lesson-actions');
  const save = element('button', lesson ? 'Lưu thay đổi' : 'Tạo bài giảng', 'btn btn-primary'); save.type = 'submit';
  const protect = event => { if (dirty || saving) { event.preventDefault(); event.returnValue = ''; } };
  window.addEventListener('beforeunload', protect);
  const cancel = gradingButton('Hủy', () => {
    if (saving || (dirty && !window.confirm('Bỏ nội dung chưa lưu và quay lại danh sách bài?'))) return;
    window.removeEventListener('beforeunload', protect); openLessonCourse(data.course.id);
  });
  actions.append(save, cancel); form.append(error, actions);
  const inputs = [title, order, type, content];
  form.addEventListener('input', () => { dirty = true; });
  form.addEventListener('submit', async event => {
    event.preventDefault(); if (saving || !form.reportValidity()) return;
    if (!title.value.trim()) { error.textContent = 'Vui lòng nhập tiêu đề, không chỉ chứa khoảng trắng.'; title.focus(); return; }
    if (lesson && !window.confirm('Lưu thay đổi bài giảng dùng chung cho các lớp của khóa học này?')) return;
    const body = { courseId: data.course.id, title: title.value.trim(), orderIndex: Number(order.value), contentType: type.value, content: content.value, expectedUpdatedAt: lesson?.updatedAt ?? null };
    saving = true; error.textContent = ''; inputs.forEach(i => i.disabled = true); save.disabled = cancel.disabled = true; save.textContent = 'Đang lưu…';
    try {
      const result = await lessonRequest(lesson ? '/' + lesson.id : '', { method: lesson ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': data.token }, body: JSON.stringify(body) });
      dirty = false; saving = false; window.removeEventListener('beforeunload', protect);
      await openLessonDetail(data, result.id, result.message);
    } catch (e) { error.textContent = e.message; }
    finally { saving = false; inputs.forEach(i => i.disabled = false); save.disabled = cancel.disabled = false; save.textContent = lesson ? 'Lưu thay đổi' : 'Tạo bài giảng'; }
  });
  teacherContent.replaceChildren(form); title.focus();
}
