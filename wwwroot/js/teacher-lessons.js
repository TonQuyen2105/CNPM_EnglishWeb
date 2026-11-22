// Class-scoped lesson workspace. Text is rendered with textContent, never as HTML.
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
  const classId = Number(params.get('classId'));
  if (Number.isSafeInteger(classId) && classId > 0) return openLessonClass(classId);
  return openLessonClasses();
}
function openLessonClasses() {
  history.replaceState(null, '', '/Teacher/lessons.html');
  return gradingView(async () => {
    const classes = await lessonRequest();
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const classroom of classes) {
      const card = panel(classroom.className);
      card.append(badge(classroom.classCode), badge(statusLabel(classroom.status)),
        element('p', classroom.course, 'teacher-muted'), element('p', classroom.lessons + ' bài giảng', 'teacher-muted'),
        gradingButton('Mở bài giảng', () => openLessonClass(classroom.id), true)); grid.append(card);
    }
    teacherContent.replaceChildren(classes.length ? grid : element('p', 'Chưa có lớp được phân công. Lớp sẽ xuất hiện khi admin phân công cho bạn.', 'card teacher-empty'));
  });
}
function openLessonClass(classId, success) {
  return gradingView(async () => {
    const data = await lessonRequest('?classId=' + classId);
    history.replaceState(null, '', '/Teacher/lessons.html?classId=' + classId);
    const bar = element('div', null, 'teacher-toolbar lesson-toolbar');
    bar.append(gradingButton('← Danh sách lớp', openLessonClasses), gradingButton('Thêm bài giảng', () => showLessonForm(data), true));
    teacherContent.replaceChildren(bar, element('h2', data.class.classCode + ' · ' + data.class.className), element('p', 'Danh sách bài giảng đã gán cho lớp.', 'teacher-muted'));
    if (success) teacherContent.append(lessonNotice(success));
    const filters = element('div', null, 'teacher-toolbar lesson-toolbar');
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm tiêu đề bài giảng'; search.setAttribute('aria-label', 'Tìm tiêu đề bài giảng');
    const type = element('select', null, 'teacher-search'); type.setAttribute('aria-label', 'Lọc loại bài');
    for (const [value, text] of Object.entries({ all: 'Tất cả loại bài', ...lessonTypes })) { const option = element('option', text); option.value = value; type.append(option); }
    const saved = lessonFilters.get(classId); search.value = saved?.search || ''; type.value = saved?.type || 'all';
    filters.append(search, type); const list = element('div', null, 'teacher-stack');
    const count = element('p', null, 'teacher-muted'); count.setAttribute('aria-live', 'polite');
    teacherContent.append(filters, count, list);
    const normalize = value => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    function draw() {
      lessonFilters.set(classId, { search: search.value, type: type.value });
      const rows = data.lessons.filter(l => normalize(l.title).includes(normalize(search.value.trim())) && (type.value === 'all' || l.contentType === type.value));
      count.textContent = rows.length + ' / ' + data.lessons.length + ' bài giảng'; list.replaceChildren();
      for (const lesson of rows) {
        const card = panel('Bài ' + lesson.orderIndex + ' · ' + lesson.title);
        const buttons = element('div', null, 'lesson-actions');
        buttons.append(gradingButton('Xem chi tiết', () => openLessonDetail(data, lesson.id)), gradingButton('Sửa bài', () => editLesson(data, lesson.id)));
        card.append(badge(lessonTypes[lesson.contentType] || lesson.contentType), element('p', 'Cập nhật: ' + lessonDate(lesson.updatedAt || lesson.createdAt), 'teacher-muted'), buttons);
        list.append(card);
      }
      if (!rows.length) list.append(element('p', data.lessons.length ? 'Không có bài phù hợp với bộ lọc.' : 'Lớp chưa có bài giảng. Chọn “Thêm bài giảng” để bắt đầu.', 'card teacher-empty'));
    }
    if (data.available?.length) {
      const legacy = panel('Bài giảng có thể gán cho lớp');
      legacy.append(element('p', 'Chọn bài cùng khóa học để dùng chung nội dung, video và PDF.', 'teacher-muted'));
      for (const old of data.available) {
        const row = element('div', null, 'teacher-row');
        const message = element('p'); message.setAttribute('role', 'status');
        const copy = gradingButton('Gán cho lớp', async () => {
          if (!window.confirm('Gán bài “' + old.title + '” cho lớp này?')) return;
          copy.disabled = true;
          try {
            const result = await lessonRequest('/classes/' + classId + '/assign/' + old.id, { method: 'POST', headers: { 'RequestVerificationToken': data.token } });
            await openLessonClass(classId, result.message);
          } catch (e) { message.textContent = e.message; copy.disabled = false; }
        });
        row.append(element('h3', old.title), copy, message); legacy.append(row);
      }
      teacherContent.append(legacy);
    }
    search.addEventListener('input', draw); type.addEventListener('change', draw); draw();
  });
}
function openLessonDetail(data, id, success) {
  return gradingView(async () => {
    const response = await lessonRequest('/' + id + '?classId=' + data.class.id), lesson = response.lesson;
    const card = panel('Bài ' + lesson.orderIndex + ' · ' + lesson.title);
    const buttons = element('div', null, 'teacher-toolbar lesson-toolbar');
    buttons.append(gradingButton('← Danh sách bài', () => openLessonClass(data.class.id)), gradingButton('Sửa bài giảng', () => showLessonForm({ ...data, token: response.token }, lesson), true));
    card.append(badge(lessonTypes[lesson.contentType] || lesson.contentType), element('p', data.class.classCode + ' · ' + data.class.className, 'teacher-muted'),
      element('p', 'Ngày tạo: ' + lessonDate(lesson.createdAt) + ' · Cập nhật: ' + lessonDate(lesson.updatedAt), 'teacher-muted'),
      element('div', lesson.content || 'Bài giảng chưa có nội dung.', 'teacher-copy lesson-content'));
    teacherContent.replaceChildren(buttons);
    if (success) teacherContent.append(lessonNotice(success));
    teacherContent.append(card, lessonMediaPanel(lesson));
  });
}
function lessonMediaPanel(lesson) {
  const section = panel('Video và tài liệu PDF');
  section.append(element('p', 'Nhấn “Sửa bài giảng” để thay video hoặc PDF.', 'teacher-muted'));
  const studentLink = link('Xem trang học', '/Student/learn.html?lessonId=' + lesson.id + '&classId=' + lesson.classId); studentLink.target = '_blank'; studentLink.rel = 'noopener'; section.append(studentLink);
  for (const kind of ['video', 'pdf']) {
    const asset = lesson[kind];
    const block = element('section', null, 'teacher-row');
    block.append(element('h3', kind === 'video' ? 'Video bài giảng' : 'Tài liệu PDF'));
    if (asset) {
      block.append(element('p', asset.name + ' · ' + (asset.size / 1024 / 1024).toFixed(1) + ' MB', 'teacher-muted'));
      if (kind === 'video') { const video = element('video'); video.controls = true; video.preload = 'metadata'; video.src = asset.url; video.className = 'lesson-video'; block.append(video); }
      else { const a = link('Mở tài liệu PDF', asset.url); a.target = '_blank'; a.rel = 'noopener'; block.append(a); }
    } else block.append(element('p', 'Chưa có ' + (kind === 'video' ? 'video.' : 'tài liệu PDF.'), 'teacher-muted'));
    section.append(block);
  }
  return section;
}
function editLesson(data, id) {
  return gradingView(async () => { const response = await lessonRequest('/' + id + '?classId=' + data.class.id); showLessonForm({ ...data, token: response.token }, response.lesson); });
}
function showLessonForm(data, lesson = null) {
  let dirty = false, saving = false;
  const form = element('form', null, 'card lesson-form');
  form.append(element('h2', lesson ? 'Sửa bài giảng' : 'Thêm bài giảng'), element('p', data.class.classCode + ' · ' + data.class.className, 'teacher-muted'),
    element('p', 'Nội dung và tài liệu được cập nhật cho mọi lớp dùng bài này. Thứ tự chỉ áp dụng trong lớp đang chọn.', 'lesson-shared-note'));
  function field(text, input) { const label = element('label', text, 'teacher-field'); label.append(input); form.append(label); return input; }
  const title = field('Tiêu đề *', element('input', null, 'teacher-search')); title.required = true; title.maxLength = 200; title.value = lesson?.title || '';
  const order = field('Thứ tự bài *', element('input', null, 'teacher-search')); order.type = 'number'; order.required = true; order.min = '1'; order.max = '100000'; order.step = '1';
  order.value = lesson?.orderIndex ?? Math.min(100000, Math.max(0, ...data.lessons.map(l => l.orderIndex)) + 1);
  const type = field('Loại bài *', element('select', null, 'teacher-search')); type.required = true;
  Object.entries(lessonTypes).forEach(([value, text]) => { const option = element('option', text); option.value = value; type.append(option); }); type.value = lesson?.contentType || 'vocabulary';
  const content = field('Nội dung bài giảng', element('textarea', null, 'teacher-search')); content.rows = 14; content.maxLength = 100000; content.value = lesson?.content || '';
  const mediaInputs = {};
  const removedMedia = new Set();
  const mediaButtons = [];
  for (const kind of ['video', 'pdf']) {
    const input = field(kind === 'video' ? 'Video MP4/WebM (tối đa 50 MB)' : 'Tài liệu PDF (tối đa 20 MB)', element('input', null, 'teacher-search'));
    input.type = 'file'; input.accept = kind === 'video' ? '.mp4,.webm' : '.pdf'; mediaInputs[kind] = input;
    const current = lesson?.[kind];
    if (!current) continue;
    const label = kind === 'video' ? 'video' : 'PDF';
    const status = element('p', null, 'teacher-muted');
    status.setAttribute('role', 'status');
    const toggle = gradingButton('', () => {
      if (removedMedia.has(kind)) removedMedia.delete(kind);
      else removedMedia.add(kind);
      input.value = '';
      dirty = true;
      renderMedia();
    });
    function renderMedia() {
      const replacement = input.files[0];
      const removed = removedMedia.has(kind);
      if (replacement) status.textContent = replacement.name + ' sẽ thay ' + current.name + ' khi lưu.';
      else if (removed) status.textContent = current.name + ' sẽ được gỡ khi lưu.';
      else status.textContent = 'Tệp hiện tại: ' + current.name;
      toggle.textContent = removed ? 'Hoàn tác gỡ ' + label : 'Gỡ ' + label;
    }
    input.addEventListener('change', renderMedia);
    mediaButtons.push(toggle);
    renderMedia();
    form.append(status, toggle);
  }
  form.append(element('p', 'Video, PDF và nội dung sẽ được lưu khi bạn nhấn nút bên dưới.', 'teacher-muted'));
  const error = element('p', null, 'lesson-form-error'); error.setAttribute('role', 'alert');
  const actions = element('div', null, 'teacher-grading-actions lesson-actions');
  const save = element('button', lesson ? 'Lưu thay đổi' : 'Tạo bài giảng', 'btn btn-primary'); save.type = 'submit';
  const protect = event => { if (dirty || saving) { event.preventDefault(); event.returnValue = ''; } };
  window.addEventListener('beforeunload', protect);
  const cancel = gradingButton('Hủy', () => {
    if (saving || (dirty && !window.confirm('Bỏ nội dung chưa lưu và quay lại danh sách bài?'))) return;
    window.removeEventListener('beforeunload', protect); openLessonClass(data.class.id);
  });
  actions.append(save, cancel); form.append(error, actions);
  const inputs = [title, order, type, content, ...Object.values(mediaInputs), ...mediaButtons];
  form.addEventListener('input', () => { dirty = true; });
  form.addEventListener('change', () => { dirty = true; });
  form.addEventListener('submit', async event => {
    event.preventDefault(); if (saving || !form.reportValidity()) return;
    if (!title.value.trim()) { error.textContent = 'Vui lòng nhập tiêu đề, không chỉ chứa khoảng trắng.'; title.focus(); return; }
    if (lesson && !window.confirm('Lưu nội dung cho các lớp dùng bài giảng này?')) return;
    const body = new FormData();
    for (const [key, value] of Object.entries({ classId: data.class.id, title: title.value.trim(), orderIndex: order.value, contentType: type.value, content: content.value })) body.append(key, value);
    if (lesson?.updatedAt) body.append('expectedUpdatedAt', lesson.updatedAt);
    for (const [kind, input] of Object.entries(mediaInputs)) {
      const file = input.files[0];
      if (!file) {
        if (removedMedia.has(kind)) body.append(kind === 'video' ? 'removeVideo' : 'removePdf', 'true');
        continue;
      }
      const allowed = kind === 'video' ? /\.(mp4|webm)$/i : /\.pdf$/i;
      if (!file.size || file.size > (kind === 'video' ? 50 : 20) * 1024 * 1024 || !allowed.test(file.name)) {
        error.textContent = kind === 'video' ? 'Chọn video MP4/WebM không trống, tối đa 50 MB.' : 'Chọn PDF không trống, tối đa 20 MB.'; input.focus(); return;
      }
      body.append(kind, file);
    }
    saving = true; error.textContent = ''; inputs.forEach(i => i.disabled = true); save.disabled = cancel.disabled = true; save.textContent = 'Đang lưu…';
    try {
      const result = await lessonRequest(lesson ? '/' + lesson.id + '/with-media' : '/with-media', { method: lesson ? 'PUT' : 'POST', headers: { 'RequestVerificationToken': data.token }, body });
      dirty = false; saving = false; window.removeEventListener('beforeunload', protect);
      await openLessonDetail(data, result.id, result.message);
    } catch (e) { error.textContent = e.message; }
    finally { saving = false; inputs.forEach(i => i.disabled = false); save.disabled = cancel.disabled = false; save.textContent = lesson ? 'Lưu thay đổi' : 'Tạo bài giảng'; }
  });
  teacherContent.replaceChildren(form); title.focus();
}
