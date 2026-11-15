// Essay exam workflow: bank questions are referenced, while order and points belong to the exam.
const examListFilter = { course: 'all', status: 'all', text: '' };
async function examRequest(path = '', options = {}) {
  const response = await fetch('/api/teacher/exams-workspace' + path, { cache: 'no-store', ...options });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.message || ({
    400: 'Dữ liệu không hợp lệ hoặc phiên biểu mẫu đã hết hạn. Kiểm tra dữ liệu và mở lại biểu mẫu nếu cần.',
    401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 403: 'Bạn không có quyền thao tác đề này.',
    404: 'Không tìm thấy đề hoặc khóa học được phân công.'
  }[response.status] || 'Không xử lý được đề thi (HTTP ' + response.status + '). Vui lòng thử lại.'));
  return body;
}
function examNotice(text) { const n = element('p', text, 'teacher-save-success'); n.setAttribute('role', 'status'); return n; }
function examNormalize(text) { return text.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd'); }
function examSelect(label, entries, value) {
  const select = element('select', null, 'teacher-search'); select.setAttribute('aria-label', label);
  entries.forEach(([id, text]) => { const option = element('option', text); option.value = id; select.append(option); });
  select.value = value; return select;
}
function openExamWorkspace() {
  const id = Number(new URLSearchParams(location.search).get('id'));
  return Number.isSafeInteger(id) && id > 0 ? openExamDetail(id) : openExamList();
}
function openExamList(success) {
  history.replaceState(null, '', '/Teacher/exams.html');
  return gradingView(async () => {
    const data = await examRequest();
    const top = element('div', null, 'teacher-toolbar exam-toolbar');
    const create = gradingButton('Tạo đề tự luận', () => chooseExamCourse(data.courses), true); create.disabled = !data.courses.length;
    top.append(element('h2', 'Đề thi của tôi'), create);
    teacherContent.replaceChildren(top, element('p', 'Đề dùng chung theo khóa học. Chỉ bản nháp chưa có lượt làm mới được sửa.', 'teacher-muted'));
    if (success) teacherContent.append(examNotice(success));
    if (!data.courses.length) { teacherContent.append(element('p', 'Chưa có khóa học được phân công.', 'card teacher-empty')); return; }
    const course = examSelect('Lọc khóa học', [['all', 'Tất cả khóa học'], ...data.courses.map(c => [c.id, c.title])], examListFilter.course);
    if (!course.value) course.value = 'all';
    const status = examSelect('Lọc trạng thái đề', [['all', 'Tất cả trạng thái'], ['draft', 'Bản nháp'], ['published', 'Đã công bố']], examListFilter.status);
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm tên đề'; search.setAttribute('aria-label', 'Tìm tên đề'); search.value = examListFilter.text;
    const filters = element('div', null, 'teacher-toolbar exam-toolbar'); filters.append(course, status, search);
    const count = element('p', null, 'teacher-muted'); count.setAttribute('aria-live', 'polite'); const list = element('div');
    teacherContent.append(filters, count, list);
    function draw() {
      Object.assign(examListFilter, { course: course.value, status: status.value, text: search.value });
      const rows = data.exams.filter(e => (course.value === 'all' || String(e.courseId) === course.value)
        && (status.value === 'all' || e.isPublished === (status.value === 'published')) && examNormalize(e.title).includes(examNormalize(search.value.trim())));
      count.textContent = rows.length + ' / ' + data.exams.length + ' đề thi';
      list.replaceChildren(table(['Đề thi', 'Khóa học', 'Câu / Điểm', 'Thời gian', 'Lượt nộp', 'Trạng thái', 'Thao tác'], rows.map(e => {
        const actions = element('div', null, 'exam-actions');
        actions.append(gradingButton('Xem đề', () => openExamDetail(e.id)));
        if (!e.isPublished && !e.hasAttempts && e.essayOnly) actions.append(gradingButton('Sửa nháp', () => editExam(e.id)));
        return [e.title, data.courses.find(c => c.id === e.courseId)?.title || '—', e.questions + ' câu · ' + e.totalPoints + ' điểm', e.durationMinutes + ' phút', e.submissions,
          badge(e.isPublished ? 'Đã công bố' : e.hasAttempts ? 'Bản nháp · Đã có lượt làm' : 'Bản nháp', e.isPublished ? 'success' : 'neutral'), actions];
      }), data.exams.length ? 'Không có đề phù hợp với bộ lọc.' : 'Chưa có đề thi. Chọn “Tạo đề tự luận” để bắt đầu.'));
    }
    [course, status].forEach(s => s.addEventListener('change', draw)); search.addEventListener('input', draw); draw();
  });
}
function chooseExamCourse(courses) {
  const card = panel('Chọn khóa học để tạo đề');
  const course = examSelect('Khóa học của đề mới', courses.map(c => [c.id, c.title]), examListFilter.course === 'all' ? courses[0].id : examListFilter.course);
  if (!course.value) course.value = courses[0].id;
  card.append(element('p', 'Câu hỏi được lấy từ bài giảng thuộc khóa đã chọn.', 'teacher-muted'), course,
    gradingButton('Tiếp tục', () => newExam(courses.find(c => c.id === Number(course.value))), true), gradingButton('Hủy', () => openExamList()));
  teacherContent.replaceChildren(card);
}
function newExam(course) {
  return gradingView(async () => { const bank = await examRequest('/bank?courseId=' + course.id); showExamEditor(course, bank); });
}
function editExam(id) {
  return gradingView(async () => {
    const data = await examRequest('/' + id);
    if (data.exam.isPublished || data.hasAttempts || data.questions.some(q => q.questionType !== 'essay')) { await openExamDetail(id); return; }
    const bank = await examRequest('/bank?courseId=' + data.exam.courseId);
    const all = await examRequest();
    showExamEditor(all.courses.find(c => c.id === data.exam.courseId), bank, data);
  });
}
function renderExamPreview(exam, questions) {
  const card = panel(exam.title || 'Đề chưa đặt tên');
  const total = Math.round(questions.reduce((sum, q) => sum + Number(q.points || 0), 0) * 100) / 100;
  card.append(element('p', exam.durationMinutes + ' phút · ' + questions.length + ' câu · Tổng ' + total + ' điểm · Điểm đạt ' + exam.passScore, 'teacher-muted'),
    element('p', 'Số lượt tối đa: ' + (exam.maxAttempts ?? 'Không giới hạn'), 'teacher-muted'), element('p', exam.description || 'Chưa có hướng dẫn bổ sung.', 'teacher-copy'));
  questions.forEach((q, i) => {
    const row = element('article', null, 'teacher-row');
    row.append(element('h3', 'Câu ' + (i + 1) + ' · ' + q.points + ' điểm'), element('p', q.questionText, 'teacher-copy')); card.append(row);
  });
  if (!questions.length) card.append(element('p', 'Đề nháp chưa có câu hỏi.', 'teacher-empty'));
  return card;
}
function openExamDetail(id, success) {
  history.replaceState(null, '', '/Teacher/exams.html?id=' + id);
  return gradingView(async () => {
    const data = await examRequest('/' + id), exam = data.exam;
    const essayOnly = data.questions.every(q => q.questionType === 'essay');
    const editable = !exam.isPublished && !data.hasAttempts && essayOnly;
    const top = element('div', null, 'teacher-toolbar exam-toolbar'); top.append(gradingButton('← Danh sách đề', () => openExamList()));
    if (editable) top.append(gradingButton('Sửa bản nháp', () => editExam(id), true));
    teacherContent.replaceChildren(top);
    if (success) teacherContent.append(examNotice(success));
    teacherContent.append(badge(exam.isPublished ? 'Đã công bố · Khóa nội dung' : data.hasAttempts ? 'Đã có lượt làm · Khóa nội dung' : 'Bản nháp', exam.isPublished ? 'success' : 'neutral'));
    if (!essayOnly) teacherContent.append(element('p', 'Đề có loại câu hỏi khác tự luận. Trình soạn hiện chỉ hỗ trợ đề tự luận; đề này được giữ nguyên để xem.', 'exam-note'));
    teacherContent.append(renderExamPreview(exam, data.questions));
    const error = element('p', null, 'exam-error'); error.setAttribute('role', 'alert');
    const actions = element('div', null, 'exam-actions'); let busy = false;
    async function mutate(action, message) {
      if (busy || !window.confirm(message)) return;
      busy = true; error.textContent = '';
      const controls = [...actions.querySelectorAll('button')].map(node => ({ node, disabled: node.disabled })); controls.forEach(c => c.node.disabled = true);
      try {
        const result = await examRequest('/' + id + (action === 'delete' ? '' : '/' + action), { method: action === 'delete' ? 'DELETE' : 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': data.token }, body: JSON.stringify({ expectedUpdatedAt: exam.updatedAt }) });
        if (action === 'delete') await openExamList(result.message); else await openExamDetail(result.id, result.message);
      } catch (e) { error.textContent = e.message; }
      finally { busy = false; controls.forEach(c => c.node.disabled = c.disabled); }
    }
    if (essayOnly) actions.append(gradingButton('Sao chép thành bản nháp', () => mutate('copy', 'Tạo đề nháp mới dùng lại câu hỏi, với điểm và thứ tự riêng?')));
    if (editable) {
      const total = Math.round(data.questions.reduce((sum, q) => sum + q.points, 0) * 100) / 100;
      const publish = gradingButton('Công bố đề', () => mutate('publish', 'Công bố đề cho khóa học? Nội dung và thang điểm sẽ bị khóa; muốn thay đổi cần tạo bản sao.'), true);
      publish.disabled = !data.questions.length || exam.passScore > total;
      actions.append(publish);
      if (publish.disabled) teacherContent.append(element('p', 'Chưa thể công bố: cần có câu hỏi và điểm đạt không vượt tổng điểm.', 'exam-note'));
    }
    if (!exam.isPublished && !data.hasAttempts) actions.append(gradingButton('Xóa bản nháp', () => mutate('delete', 'Xóa vĩnh viễn bản nháp này? Câu hỏi trong ngân hàng được giữ nguyên.')));
    teacherContent.append(error, actions);
    if (exam.isPublished && data.questions.some(q => q.questionType === 'essay')) {
      const section = panel('Theo dõi bài nộp theo lớp');
      section.append(element('p', 'Đề dùng chung theo khóa học; chọn lớp để xem học viên.', 'teacher-muted'));
      section.append(table(['Lớp', 'Chấm bài', 'Sổ điểm'], data.classes.map(c => [c.classCode + ' · ' + c.className,
        link('Mở chấm tự luận', '/Teacher/grading.html?classId=' + c.id + '&examId=' + id),
        link('Xem sổ điểm', '/Teacher/results.html?classId=' + c.id + '&examId=' + id)
      ]), 'Không có lớp được phân công trong khóa này.'));
      teacherContent.append(section);
    }
  });
}
function showExamEditor(course, bank, existing = null) {
  const exam = existing?.exam; let dirty = false, saving = false;
  const selected = (existing?.questions || []).map(q => ({ id: q.id, questionText: q.questionText, points: q.points, updatedAt: q.updatedAt }));
  const form = element('form', null, 'exam-form');
  const metadata = panel(exam ? 'Sửa bản nháp' : 'Tạo đề tự luận'); metadata.append(element('p', course.title, 'teacher-muted'), element('p', 'Lưu nháp chưa công bố đề. Điểm câu hỏi trong đề độc lập với điểm mặc định của ngân hàng.', 'exam-note'));
  function field(label, input, required = false) { const wrap = element('label', label, 'teacher-field'); input.required = required; wrap.append(input); metadata.append(wrap); return input; }
  const title = field('Tên đề *', element('input', null, 'teacher-search'), true); title.maxLength = 200; title.value = exam?.title || '';
  const description = field('Hướng dẫn làm bài', element('textarea', null, 'teacher-search')); description.rows = 4; description.maxLength = 20000; description.value = exam?.description || '';
  const duration = field('Thời gian làm bài (phút) *', element('input', null, 'teacher-search'), true); duration.type = 'number'; duration.min = '1'; duration.max = '1440'; duration.step = '1'; duration.value = exam?.durationMinutes ?? 45;
  const pass = field('Điểm đạt *', element('input', null, 'teacher-search'), true); pass.type = 'number'; pass.min = '0.01'; pass.max = '100000'; pass.step = '0.01'; pass.value = exam?.passScore ?? 5;
  const attempts = field('Số lượt làm tối đa (để trống nếu không giới hạn)', element('input', null, 'teacher-search')); attempts.type = 'number'; attempts.min = '1'; attempts.max = '100'; attempts.step = '1'; attempts.value = exam ? exam.maxAttempts ?? '' : 1;
  form.append(metadata);
  const bankPanel = panel('Chọn câu hỏi từ ngân hàng');
  const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm nội dung câu hỏi'; search.setAttribute('aria-label', 'Tìm câu hỏi trong ngân hàng');
  const lesson = examSelect('Lọc bài giảng', [['all', 'Tất cả bài giảng'], ...bank.lessons.map(l => [l.id, 'Bài ' + l.orderIndex + ' · ' + l.title])], 'all');
  const filters = element('div', null, 'teacher-toolbar exam-toolbar'); filters.append(search, lesson);
  const candidates = element('div', null, 'teacher-stack'); bankPanel.append(filters, candidates);
  if (!bank.questions.length) bankPanel.append(link('Đến Ngân hàng câu hỏi', '/Teacher/questions.html?courseId=' + course.id));
  const selection = panel('Câu hỏi đã chọn'); const summary = element('p', null, 'teacher-muted'); summary.setAttribute('aria-live', 'polite');
  const chosen = element('div', null, 'teacher-stack'); selection.append(summary, chosen); form.append(bankPanel, selection);
  const preview = element('section'); preview.hidden = true;
  const error = element('p', null, 'exam-error'); error.setAttribute('role', 'alert');
  const actions = element('div', null, 'teacher-grading-actions exam-actions');
  const save = element('button', 'Lưu bản nháp', 'btn btn-primary'); save.type = 'submit';
  const protect = event => { if (dirty || saving) { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', protect);
  const cancel = gradingButton('Hủy', () => { if (saving || (dirty && !window.confirm('Bỏ các thay đổi chưa lưu?'))) return; window.removeEventListener('beforeunload', protect); if (exam) openExamDetail(exam.id); else openExamList(); });
  function values() { return { title: title.value, description: description.value, durationMinutes: Number(duration.value), passScore: Number(pass.value), maxAttempts: attempts.value === '' ? null : Number(attempts.value) }; }
  const previewButton = gradingButton('Xem trước', () => { if (!form.reportValidity()) return; preview.replaceChildren(renderExamPreview(values(), selected)); preview.hidden = false; preview.tabIndex = -1; preview.focus(); });
  actions.append(save, previewButton, cancel); form.append(error, actions, preview);
  function changed() { dirty = true; preview.hidden = true; }
  function updateSummary() { const total = Math.round(selected.reduce((sum, q) => sum + Number(q.points || 0), 0) * 100) / 100; summary.textContent = selected.length + ' / 100 câu · Tổng ' + total + ' điểm' + (Number(pass.value) > total ? ' · Điểm đạt đang vượt tổng điểm, chưa thể công bố.' : ''); }
  function drawBank() {
    candidates.replaceChildren();
    const rows = bank.questions.filter(q => (lesson.value === 'all' || String(q.lessonId) === lesson.value) && examNormalize(q.questionText).includes(examNormalize(search.value.trim())));
    for (const q of rows) {
      const row = element('div', null, 'teacher-row');
      const already = selected.some(s => s.id === q.id);
      const add = gradingButton(already ? 'Đã chọn' : 'Thêm câu', () => {
        if (saving || selected.some(s => s.id === q.id) || selected.length >= 100) return;
        selected.push({ id: q.id, questionText: q.questionText, points: q.defaultPoints, updatedAt: q.updatedAt }); changed(); drawSelected(); drawBank();
      }); add.disabled = already || selected.length >= 100;
      row.append(element('p', '#' + q.id + ' · ' + q.questionText, 'teacher-copy'), element('p', q.defaultPoints + ' điểm mặc định', 'teacher-muted'), add); candidates.append(row);
    }
    if (!rows.length) candidates.append(element('p', bank.questions.length ? 'Không có câu phù hợp.' : 'Chưa có câu tự luận trong khóa này. Bạn vẫn có thể lưu đề nháp trống.', 'teacher-empty'));
  }
  function drawSelected() {
    chosen.replaceChildren();
    selected.forEach((q, index) => {
      const row = element('article', null, 'teacher-row'); row.append(element('h3', 'Câu ' + (index + 1) + ' · #' + q.id), element('p', q.questionText, 'teacher-copy'));
      const label = element('label', 'Điểm trong đề *', 'teacher-field'), score = element('input', null, 'teacher-search');
      score.type = 'number'; score.min = '0.01'; score.max = '1000'; score.step = '0.01'; score.required = true; score.value = q.points;
      score.addEventListener('input', () => { q.points = score.value === '' ? null : Number(score.value); changed(); updateSummary(); }); label.append(score); row.append(label);
      const buttons = element('div', null, 'exam-actions');
      const move = delta => { if (saving) return; [selected[index], selected[index + delta]] = [selected[index + delta], selected[index]]; changed(); drawSelected(); };
      const up = gradingButton('Lên', () => move(-1)); up.disabled = index === 0; up.setAttribute('aria-label', 'Đưa câu ' + (index + 1) + ' lên');
      const down = gradingButton('Xuống', () => move(1)); down.disabled = index === selected.length - 1; down.setAttribute('aria-label', 'Đưa câu ' + (index + 1) + ' xuống');
      buttons.append(up, down, gradingButton('Bỏ khỏi đề', () => { if (saving) return; selected.splice(index, 1); changed(); drawSelected(); drawBank(); }));
      row.append(buttons); chosen.append(row);
    });
    if (!selected.length) chosen.append(element('p', 'Chưa chọn câu hỏi.', 'teacher-empty'));
    updateSummary();
  }
  search.addEventListener('input', drawBank); lesson.addEventListener('change', drawBank); pass.addEventListener('input', updateSummary);
  form.addEventListener('input', changed);
  form.addEventListener('submit', async event => {
    event.preventDefault(); if (saving || !form.reportValidity()) return;
    if (!title.value.trim()) { error.textContent = 'Tên đề không được chỉ chứa khoảng trắng.'; title.focus(); return; }
    const body = { ...values(), title: title.value.trim(), courseId: course.id, expectedUpdatedAt: exam?.updatedAt ?? null,
      questions: selected.map(q => ({ questionId: q.id, points: q.points, expectedUpdatedAt: q.updatedAt })) };
    saving = true; error.textContent = '';
    const controls = [...form.querySelectorAll('input,textarea,select,button')].map(node => ({ node, disabled: node.disabled })); controls.forEach(c => c.node.disabled = true); save.textContent = 'Đang lưu…';
    try {
      const result = await examRequest(exam ? '/' + exam.id : '', { method: exam ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': bank.token }, body: JSON.stringify(body) });
      dirty = false; saving = false; window.removeEventListener('beforeunload', protect); await openExamDetail(result.id, result.message);
    } catch (e) { error.textContent = e.message; }
    finally { saving = false; controls.forEach(c => c.node.disabled = c.disabled); save.textContent = 'Lưu bản nháp'; }
  });
  drawSelected(); drawBank(); teacherContent.replaceChildren(form); title.focus();
}
