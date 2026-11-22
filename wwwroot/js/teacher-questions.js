// Question bank: content stays plain text; editing never changes exam question points.
const questionBankFilters = new Map();
async function questionBankRequest(path = '', options = {}) {
  const response = await fetch('/api/teacher/questions-workspace' + path, { cache: 'no-store', ...options });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.message || ({
    400: 'Yêu cầu không hợp lệ hoặc phiên biểu mẫu đã hết hạn. Kiểm tra nội dung và mở lại biểu mẫu nếu cần.',
    401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
    403: 'Bạn không có quyền thao tác với câu hỏi này.',
    404: 'Không tìm thấy khóa, bài giảng hoặc câu hỏi được phân công.'
  }[response.status] || 'Không tải hoặc lưu được câu hỏi (HTTP ' + response.status + '). Vui lòng thử lại.'));
  return body;
}
function questionBankDate(value) {
  return value ? new Date(value).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'Chưa cập nhật';
}
function questionBankNotice(message) {
  const notice = element('p', message, 'teacher-save-success'); notice.setAttribute('role', 'status'); return notice;
}
function openQuestionBank() {
  const id = Number(new URLSearchParams(location.search).get('courseId'));
  return Number.isSafeInteger(id) && id > 0 ? openQuestionCourse(id) : openQuestionCourses();
}
function openQuestionCourses() {
  history.replaceState(null, '', '/Teacher/questions.html');
  return gradingView(async () => {
    const courses = await questionBankRequest();
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const course of courses) {
      const card = panel(course.title);
      card.append(element('p', course.questions + ' câu hỏi', 'teacher-muted'),
        gradingButton('Mở ngân hàng câu hỏi', () => openQuestionCourse(course.id), true)); grid.append(card);
    }
    teacherContent.replaceChildren(courses.length ? grid : element('p', 'Chưa có khóa học được phân công.', 'card teacher-empty'));
  });
}
function openQuestionCourse(courseId, success) {
  return gradingView(async () => {
    const data = await questionBankRequest('?courseId=' + courseId);
    history.replaceState(null, '', '/Teacher/questions.html?courseId=' + courseId);
    const bar = element('div', null, 'teacher-toolbar question-toolbar');
    const add = gradingButton('Thêm câu hỏi', () => showQuestionForm(data), true); add.disabled = !data.lessons.length;
    bar.append(gradingButton('← Khóa học', openQuestionCourses), add);
    teacherContent.replaceChildren(bar, element('h2', data.course.title),
      element('p', 'Câu hỏi thuộc bài giảng và dùng chung theo khóa học. Câu đã dùng trong đề hoặc bài nộp chỉ được xem và sao chép.', 'teacher-muted'));
    if (success) teacherContent.append(questionBankNotice(success));
    if (!data.lessons.length) {
      const empty = panel('Khóa học chưa có bài giảng');
      empty.append(element('p', 'Tạo bài giảng trước để gắn câu hỏi.', 'teacher-muted'), link('Đến Bài giảng', '/Teacher/lessons.html?courseId=' + courseId));
      teacherContent.append(empty); return;
    }
    const filters = element('div', null, 'teacher-toolbar question-toolbar');
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm nội dung câu hỏi'; search.setAttribute('aria-label', 'Tìm nội dung câu hỏi');
    const lessonSelect = element('select', null, 'teacher-search'); lessonSelect.setAttribute('aria-label', 'Lọc theo bài giảng');
    const all = element('option', 'Tất cả bài giảng'); all.value = 'all'; lessonSelect.append(all);
    data.lessons.forEach(l => { const option = element('option', 'Bài ' + l.orderIndex + ' · ' + l.title); option.value = l.id; lessonSelect.append(option); });
    const usage = element('select', null, 'teacher-search'); usage.setAttribute('aria-label', 'Lọc tình trạng sử dụng');
    for (const [value, text] of [['all', 'Tất cả câu hỏi'], ['unused', 'Chưa sử dụng'], ['used', 'Đã sử dụng']]) { const option = element('option', text); option.value = value; usage.append(option); }
    const saved = questionBankFilters.get(courseId);
    search.value = saved?.search || ''; lessonSelect.value = data.lessons.some(l => String(l.id) === saved?.lesson) ? saved.lesson : 'all'; usage.value = saved?.usage || 'all';
    const typeFilter = element('select', null, 'teacher-search'); typeFilter.setAttribute('aria-label', 'Lọc loại câu hỏi');
    for (const value of ['all', 'essay', 'multiple_choice', 'fill_blank']) { const option = element('option', value === 'all' ? 'Tất cả loại câu' : typeLabel(value)); option.value = value; typeFilter.append(option); }
    typeFilter.value = saved?.type || 'all';
    filters.append(search, lessonSelect, usage, typeFilter);
    const count = element('p', null, 'teacher-muted'); count.setAttribute('aria-live', 'polite');
    const list = element('div', null, 'teacher-stack'); teacherContent.append(filters, count, list);
    const normalize = value => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    function draw() {
      questionBankFilters.set(courseId, { search: search.value, lesson: lessonSelect.value, usage: usage.value, type: typeFilter.value });
      const rows = data.questions.filter(q => normalize(q.questionText).includes(normalize(search.value.trim()))
        && (lessonSelect.value === 'all' || String(q.lessonId) === lessonSelect.value)
        && (typeFilter.value === 'all' || q.questionType === typeFilter.value)
        && (usage.value === 'all' || q.isUsed === (usage.value === 'used')));
      count.textContent = rows.length + ' / ' + data.questions.length + ' câu hỏi'; list.replaceChildren();
      for (const question of rows) {
        const card = panel('Câu #' + question.id);
        const info = element('div', null, 'teacher-split');
        info.append(badge(typeLabel(question.questionType)), badge(question.isUsed ? 'Đã sử dụng · Đã khóa' : 'Chưa sử dụng', question.isUsed ? 'warning' : 'neutral'), badge(question.defaultPoints + ' điểm mặc định'));
        const lesson = data.lessons.find(l => l.id === question.lessonId);
        const actions = element('div', null, 'question-actions');
        actions.append(gradingButton('Xem chi tiết', () => openQuestionDetail(data, question.id)),
          gradingButton('Tạo bản sao', () => loadQuestionForm(data, question.id, true)));
        if (!question.isUsed) actions.append(gradingButton('Sửa câu hỏi', () => loadQuestionForm(data, question.id)));
        card.append(info, element('p', question.questionText.length > 240 ? question.questionText.slice(0, 240) + '…' : question.questionText, 'teacher-copy'),
          element('p', lesson ? 'Bài ' + lesson.orderIndex + ' · ' + lesson.title : 'Bài giảng chưa xác định', 'teacher-muted'), actions);
        list.append(card);
      }
      if (!rows.length) list.append(element('p', data.questions.length ? 'Không có câu hỏi phù hợp với bộ lọc.' : 'Chưa có câu hỏi. Chọn “Thêm câu hỏi” để bắt đầu.', 'card teacher-empty'));
    }
    search.addEventListener('input', draw); lessonSelect.addEventListener('change', draw); usage.addEventListener('change', draw); typeFilter.addEventListener('change', draw); draw();
  });
}
function openQuestionDetail(data, id, success) {
  return gradingView(async () => {
    const response = await questionBankRequest('/' + id), q = response.question;
    const bar = element('div', null, 'teacher-toolbar question-toolbar');
    bar.append(gradingButton('← Danh sách câu hỏi', () => openQuestionCourse(data.course.id)), gradingButton('Tạo bản sao', () => showQuestionForm({ ...data, token: response.token }, { ...q, options: response.options, answers: response.answers }, true), true));
    if (!response.isUsed) bar.append(gradingButton('Sửa câu hỏi', () => showQuestionForm({ ...data, token: response.token }, { ...q, options: response.options, answers: response.answers })));
    const lesson = data.lessons.find(l => l.id === q.lessonId);
    const card = panel(typeLabel(q.questionType) + ' #' + q.id);
    card.append(badge(response.isUsed ? 'Đã sử dụng · Khóa sửa và xóa' : 'Chưa sử dụng', response.isUsed ? 'warning' : 'neutral'),
      element('p', data.course.title + ' · ' + (lesson?.title || 'Bài giảng chưa xác định'), 'teacher-muted'),
      element('p', q.questionText, 'teacher-copy'), element('p', 'Điểm mặc định: ' + q.defaultPoints),
      element('p', 'Điểm mặc định chỉ là gợi ý khi tạo đề. Điểm trong đề thi được lưu riêng và không đổi theo câu hỏi này.', 'teacher-muted'),
      element('p', 'Ngày tạo: ' + questionBankDate(q.createdAt) + ' · Cập nhật: ' + questionBankDate(q.updatedAt), 'teacher-muted'));
    for (const [index, option] of (response.options || []).entries()) card.append(element('p', String.fromCharCode(65 + index) + '. ' + option.optionText + (option.isCorrect ? ' · Đáp án đúng' : ''), option.isCorrect ? 'text-success teacher-copy' : 'teacher-copy'));
    for (const answer of response.answers || []) card.append(element('p', 'Đáp án: ' + answer.answerText + (answer.isCaseSensitive ? ' · Phân biệt hoa/thường' : ' · Không phân biệt hoa/thường'), 'teacher-copy'));
    if (response.isUsed) card.append(element('p', 'Đã gắn với ' + response.examCount + ' đề thi' + (response.hasSubmissions ? ' và đã có câu trả lời của học viên.' : '.') + ' Tạo bản sao để chỉnh sửa nội dung mới.', 'question-shared-note'));
    if (response.exams.length) card.append(table(['Đề thi của bạn', 'Trạng thái', 'Điểm trong đề'], response.exams.map(e => [e.title, e.isPublished ? 'Đã công bố' : 'Bản nháp', e.points])));
    if (response.examCount > response.exams.length) card.append(element('p', 'Có đề thi của giảng viên khác đang sử dụng câu này.', 'teacher-muted'));
    const error = element('p', null, 'question-form-error'); error.setAttribute('role', 'alert'); card.append(error);
    if (!response.isUsed) {
      const remove = gradingButton('Xóa câu chưa sử dụng', async () => {
        if (remove.disabled || !window.confirm('Xóa vĩnh viễn câu hỏi #' + id + '? Thao tác này không thể hoàn tác.')) return;
        remove.disabled = true; error.textContent = '';
        try {
          const result = await questionBankRequest('/' + id, { method: 'DELETE', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': response.token }, body: JSON.stringify({ expectedUpdatedAt: q.updatedAt }) });
          await openQuestionCourse(data.course.id, result.message);
        } catch (e) { error.textContent = e.message; }
        finally { remove.disabled = false; }
      });
      remove.classList.add('question-delete'); card.append(remove);
    }
    teacherContent.replaceChildren(bar);
    if (success) teacherContent.append(questionBankNotice(success));
    teacherContent.append(card);
  });
}
function loadQuestionForm(data, id, copy = false) {
  return gradingView(async () => {
    const response = await questionBankRequest('/' + id);
    if (response.isUsed && !copy) { await openQuestionDetail(data, id); return; }
    showQuestionForm({ ...data, token: response.token }, { ...response.question, options: response.options, answers: response.answers }, copy);
  });
}
function showQuestionForm(data, original = null, copy = false) {
  let dirty = false, saving = false;
  const form = element('form', null, 'card question-form');
  form.append(element('h2', copy ? 'Tạo bản sao câu #' + original.id : original ? 'Sửa câu #' + original.id : 'Thêm câu hỏi'),
    element('p', data.course.title, 'teacher-muted'), element('p', copy ? 'Bản sao độc lập, chưa gắn với đề thi.' : 'Câu đã dùng trong đề hoặc bài nộp bị khóa sửa/xóa. Tạo bản sao khi cần nội dung mới.', 'question-shared-note'));
  function field(label, node) { const wrap = element('label', label, 'teacher-field'); wrap.append(node); form.append(wrap); return node; }
  const lesson = field('Bài giảng *', element('select', null, 'teacher-search')); lesson.required = true;
  for (const l of data.lessons) { const option = element('option', 'Bài ' + l.orderIndex + ' · ' + l.title); option.value = l.id; lesson.append(option); }
  lesson.value = original?.lessonId ?? data.lessons[0]?.id ?? '';
  const type = field('Loại câu hỏi *', element('select', null, 'teacher-search'));
  for (const value of ['essay', 'multiple_choice', 'fill_blank']) { const option = element('option', typeLabel(value)); option.value = value; type.append(option); }
  type.value = original?.questionType || 'essay';
  const text = field('Nội dung câu hỏi *', element('textarea', null, 'teacher-search')); text.rows = 8; text.required = true; text.maxLength = 20000; text.value = original?.questionText || '';
  const points = field('Điểm mặc định *', element('input', null, 'teacher-search')); points.type = 'number'; points.min = '0.01'; points.max = '1000'; points.step = '0.01'; points.required = true; points.value = original?.defaultPoints ?? 1;
  const optionsSection = element('section', null, 'teacher-stack');
  optionsSection.append(element('h3', 'Lựa chọn trắc nghiệm'), element('p', 'Nhập 2–6 lựa chọn và đánh dấu đúng một đáp án đúng.', 'teacher-muted'));
  const optionRows = element('div', null, 'teacher-stack'); optionsSection.append(optionRows);
  const choices = original?.options?.length ? original.options.map(o => ({ ...o })) : Array.from({ length: 4 }, () => ({ optionText: '', isCorrect: false }));
  const addChoice = gradingButton('Thêm lựa chọn', () => { if (choices.length >= 6) return; choices.push({ optionText: '', isCorrect: false }); dirty = true; drawChoices(); });
  optionsSection.append(addChoice);
  function drawChoices() {
    optionRows.replaceChildren(); addChoice.disabled = choices.length >= 6;
    choices.forEach((choice, i) => {
      const row = element('div', null, 'teacher-row');
      const label = element('label', 'Lựa chọn ' + String.fromCharCode(65 + i), 'teacher-field');
      const input = element('input', null, 'teacher-search'); input.required = true; input.maxLength = 2000; input.value = choice.optionText;
      input.addEventListener('input', () => { choice.optionText = input.value; }); label.append(input);
      const correctLabel = element('label', null, 'question-choice-correct');
      const correct = element('input'); correct.type = 'radio'; correct.name = 'correct-option'; correct.checked = choice.isCorrect; correct.required = true;
      correct.addEventListener('change', () => { choices.forEach((c, j) => c.isCorrect = j === i); dirty = true; });
      correctLabel.append(correct, element('span', 'Đáp án đúng'));
      const remove = gradingButton('Bỏ lựa chọn', () => { choices.splice(i, 1); dirty = true; drawChoices(); }); remove.disabled = choices.length <= 2;
      row.append(label, correctLabel, remove); optionRows.append(row);
    });
  }
  const answersSection = element('section', null, 'teacher-stack');
  answersSection.append(element('h3', 'Đáp án điền từ'), element('p', 'Mỗi câu có một ô nhập. Thêm các cách trả lời được chấp nhận. Chỉ bỏ khoảng trắng đầu/cuối; không tự sửa chính tả.', 'teacher-muted'));
  const answerRows = element('div', null, 'teacher-stack'); answersSection.append(answerRows);
  const keys = original?.answers?.length ? original.answers.map(a => ({ ...a })) : [{ answerText: '', isCaseSensitive: false }];
  const addAnswer = gradingButton('Thêm đáp án hợp lệ', () => { if (keys.length >= 20) return; keys.push({ answerText: '', isCaseSensitive: false }); dirty = true; drawKeys(); });
  answersSection.append(addAnswer);
  function drawKeys() {
    answerRows.replaceChildren(); addAnswer.disabled = keys.length >= 20;
    keys.forEach((key, i) => {
      const row = element('div', null, 'teacher-row');
      const label = element('label', 'Đáp án ' + (i + 1), 'teacher-field');
      const input = element('input', null, 'teacher-search'); input.required = true; input.maxLength = 2000; input.value = key.answerText;
      input.addEventListener('input', () => { key.answerText = input.value; }); label.append(input);
      const caseLabel = element('label', null, 'question-choice-correct');
      const sensitive = element('input'); sensitive.type = 'checkbox'; sensitive.checked = key.isCaseSensitive;
      sensitive.addEventListener('change', () => { key.isCaseSensitive = sensitive.checked; dirty = true; });
      caseLabel.append(sensitive, element('span', 'Phân biệt chữ hoa/thường'));
      const remove = gradingButton('Bỏ đáp án', () => { keys.splice(i, 1); dirty = true; drawKeys(); }); remove.disabled = keys.length <= 1;
      row.append(label, caseLabel, remove); answerRows.append(row);
    });
  }
  function changeType() {
    drawChoices(); drawKeys();
    optionsSection.hidden = type.value !== 'multiple_choice'; answersSection.hidden = type.value !== 'fill_blank';
    for (const section of [optionsSection, answersSection]) if (section.hidden) section.querySelectorAll('input,button').forEach(n => n.disabled = true);
    form.querySelector('.question-auto-note')?.remove();
    if (type.value !== 'essay') form.append(element('p', 'Hệ thống dùng đáp án này để tự chấm sau khi học viên nộp bài.', 'question-auto-note teacher-muted'));
  }
  type.addEventListener('change', () => { dirty = true; changeType(); });
  form.append(optionsSection, answersSection);
  const error = element('p', null, 'question-form-error'); error.setAttribute('role', 'alert');
  const actions = element('div', null, 'teacher-grading-actions question-actions');
  const label = copy ? 'Lưu bản sao' : original ? 'Lưu thay đổi' : 'Thêm câu hỏi';
  const save = element('button', label, 'btn btn-primary'); save.type = 'submit';
  const protect = event => { if (dirty || saving) { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', protect);
  const cancel = gradingButton('Hủy', () => {
    if (saving || (dirty && !window.confirm('Bỏ nội dung câu hỏi chưa lưu?'))) return;
    window.removeEventListener('beforeunload', protect); openQuestionCourse(data.course.id);
  });
  actions.append(save, cancel); form.append(error, actions);
  form.addEventListener('input', () => { dirty = true; });
  form.addEventListener('submit', async event => {
    event.preventDefault(); if (saving || !form.reportValidity()) return;
    if (!text.value.trim()) { error.textContent = 'Nội dung câu hỏi không được chỉ chứa khoảng trắng.'; text.focus(); return; }
    if (type.value === 'multiple_choice' && (choices.some(c => !c.optionText.trim()) || choices.filter(c => c.isCorrect).length !== 1)) { error.textContent = 'Điền đủ lựa chọn và đánh dấu đúng một đáp án đúng.'; return; }
    if (type.value === 'fill_blank' && keys.some(k => !k.answerText.trim())) { error.textContent = 'Đáp án điền từ không được để trống.'; return; }
    if (original && !copy && !window.confirm('Lưu thay đổi câu hỏi dùng chung theo khóa học?')) return;
    const body = { courseId: data.course.id, lessonId: Number(lesson.value), questionType: type.value, questionText: text.value.trim(), defaultPoints: Number(points.value), expectedUpdatedAt: original?.updatedAt ?? null,
      options: type.value === 'multiple_choice' ? choices.map(c => ({ optionText: c.optionText.trim(), isCorrect: c.isCorrect })) : [],
      answers: type.value === 'fill_blank' ? keys.map(k => ({ answerText: k.answerText.trim(), isCaseSensitive: k.isCaseSensitive })) : [] };
    saving = true; error.textContent = '';
    const controls = [...form.querySelectorAll('input,textarea,select,button')].map(node => ({ node, disabled: node.disabled })); controls.forEach(c => c.node.disabled = true); save.textContent = 'Đang lưu…';
    try {
      const path = original ? '/' + original.id + (copy ? '/copy' : '') : '';
      const result = await questionBankRequest(path, { method: original && !copy ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': data.token }, body: JSON.stringify(body) });
      dirty = false; saving = false; window.removeEventListener('beforeunload', protect);
      await openQuestionDetail(data, result.id, result.message);
    } catch (e) { error.textContent = e.message; }
    finally { saving = false; controls.forEach(c => c.node.disabled = c.disabled); save.textContent = label; }
  });
  changeType(); teacherContent.replaceChildren(form); text.focus();
}
