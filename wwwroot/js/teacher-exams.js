// Mixed exam workflow: bank questions are referenced, while order and points belong to the exam.
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
const examStatusLabels = { awaiting: 'Chờ soạn đề', draft: 'Bản nháp', pending: 'Chờ duyệt', rejected: 'Bị từ chối', published: 'Đã công bố' };
function examStatus(exam, questions = exam.questions, hasAttempts = exam.hasAttempts) {
  if (exam.isPublished) return 'published';
  if (exam.reviewStatus) return exam.reviewStatus;
  return questions === 0 && !hasAttempts ? 'awaiting' : 'draft';
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
    top.append(element('h2', 'Đề thi của tôi'));
    teacherContent.replaceChildren(top, element('p', 'Đề dùng chung theo khóa học. Đề chỉ được công bố sau khi quản trị viên duyệt.', 'teacher-muted'));
    if (success) teacherContent.append(examNotice(success));
    if (!data.courses.length) { teacherContent.append(element('p', 'Chưa có khóa học được phân công.', 'card teacher-empty')); return; }
    const course = examSelect('Lọc khóa học', [['all', 'Tất cả khóa học'], ...data.courses.map(c => [c.id, c.title])], examListFilter.course);
    if (!course.value) course.value = 'all';
    const status = examSelect('Lọc trạng thái đề', [['all', 'Tất cả trạng thái'], ...Object.entries(examStatusLabels)], examListFilter.status);
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm tên đề'; search.setAttribute('aria-label', 'Tìm tên đề'); search.value = examListFilter.text;
    const filters = element('div', null, 'teacher-toolbar exam-toolbar'); filters.append(course, status, search);
    const count = element('p', null, 'teacher-muted'); count.setAttribute('aria-live', 'polite'); const list = element('div', null, 'exam-list-table');
    teacherContent.append(filters, count, list);
    function draw() {
      Object.assign(examListFilter, { course: course.value, status: status.value, text: search.value });
      const rows = data.exams.filter(e => (course.value === 'all' || String(e.courseId) === course.value)
        && (status.value === 'all' || examStatus(e) === status.value) && examNormalize(e.title).includes(examNormalize(search.value.trim())));
      count.textContent = rows.length + ' / ' + data.exams.length + ' đề thi';
      list.replaceChildren(table(['Đề thi', 'Khóa học', 'Câu / Điểm', 'Thời gian', 'Lượt nộp', 'Trạng thái', 'Thao tác'], rows.map(e => {
        const actions = element('div', null, 'exam-actions');
        actions.append(gradingButton('Xem đề', () => openExamDetail(e.id)));
        if (!e.isPublished && e.reviewStatus !== 'pending' && !e.hasAttempts && e.supported) {
          actions.append(gradingButton(e.questions === 0 ? 'Thêm đề' : 'Sửa nháp', () => editExam(e.id)));
          const send = gradingButton('Gửi đi', async () => {
            if (!window.confirm('Gửi đề cho quản trị viên duyệt?')) return;
            send.disabled = true;
            try {
              const result = await examRequest('/' + e.id + '/submit', { method: 'POST',
                headers: { 'Content-Type': 'application/json', RequestVerificationToken: data.token },
                body: JSON.stringify({ expectedUpdatedAt: e.updatedAt }) });
              await openExamList(result.message);
            } catch (err) { window.alert(err.message); send.disabled = false; }
          }, true);
          send.disabled = e.totalPoints !== 10;
          actions.append(send);
        }
        const names = [e.title, data.courses.find(c => c.id === e.courseId)?.title || '—'].map(text => {
          const name = element('span', text, 'exam-list-name');
          name.title = text;
          return name;
        });
        return [...names, e.questions + ' câu · ' + e.totalPoints + ' điểm', e.durationMinutes + ' phút', e.submissions,
          badge(examStatusLabels[examStatus(e)], e.isPublished ? 'success' : 'neutral'), actions];
      }), data.exams.length ? 'Không có đề phù hợp với bộ lọc.' : 'Chưa có đề thi được giao.'));
    }
    [course, status].forEach(s => s.addEventListener('change', draw)); search.addEventListener('input', draw); draw();
  });
}
function editExam(id) {
  return gradingView(async () => {
    const data = await examRequest('/' + id);
    if (data.exam.isPublished || data.exam.reviewStatus === 'pending' || data.hasAttempts || data.questions.some(q => !['essay', 'multiple_choice', 'fill_blank'].includes(q.questionType))) { await openExamDetail(id); return; }
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
    row.append(element('h3', 'Câu ' + (i + 1) + ' · ' + typeLabel(q.questionType) + ' · ' + q.points + ' điểm'), element('p', q.questionText, 'teacher-copy'));
    
    // 1. Trắc nghiệm: Hiển thị các lựa chọn và làm nổi bật đáp án đúng
    if (q.questionType === 'multiple_choice') {
      const optList = element('div', null, 'exam-options-list');
      optList.style.display = 'flex';
      optList.style.flexDirection = 'column';
      optList.style.gap = '6px';
      optList.style.margin = '8px 0';
      for (const [index, option] of (q.options || []).entries()) {
        const letter = String.fromCharCode(65 + index);
        const optP = element('div', null, 'teacher-copy');
        optP.style.display = 'flex';
        optP.style.alignItems = 'center';
        optP.style.padding = '8px 12px';
        optP.style.borderRadius = '8px';
        if (option.isCorrect) {
          optP.style.background = '#f0fdf4';
          optP.style.border = '1px solid #86efac';
          optP.style.color = '#15803d';
          optP.style.fontWeight = '600';
        } else {
          optP.style.border = '1px solid transparent';
          optP.style.color = 'inherit';
        }
        optP.textContent = `${letter}. ${option.optionText}`;
        optList.append(optP);
      }
      row.append(optList);
    }
    // 2. Điền từ: Hiển thị ô trống và các đáp án chuẩn được chấp nhận
    else if (q.questionType === 'fill_blank') {
      row.append(element('p', 'Ô trả lời: ____________________', 'teacher-muted'));
      const ansBox = element('div', null, 'exam-answer-box');
      ansBox.style.background = '#f0fdf4';
      ansBox.style.border = '1px solid #bbf7d0';
      ansBox.style.borderRadius = '8px';
      ansBox.style.padding = '8px 12px';
      ansBox.style.margin = '8px 0';
      ansBox.style.display = 'flex';
      ansBox.style.flexWrap = 'wrap';
      ansBox.style.alignItems = 'center';
      ansBox.style.gap = '8px';
      const labelSpan = element('strong', '🔑 Đáp án chấp nhận:');
      labelSpan.style.color = '#166534';
      labelSpan.style.fontSize = '0.875rem';
      ansBox.append(labelSpan);
      if (q.answers && q.answers.length) {
        q.answers.forEach(ans => {
          const badgeText = ans.answerText + (ans.isCaseSensitive ? ' (Phân biệt hoa/thường)' : '');
          ansBox.append(badge(badgeText, 'success'));
        });
      } else {
        ansBox.append(element('span', 'Chưa cấu hình đáp án chuẩn', 'teacher-muted'));
      }
      row.append(ansBox);
    }
    // 3. Tự luận: Hiển thị ghi chú câu tự luận
    else if (q.questionType === 'essay') {
      const essayBox = element('div', null, 'exam-essay-note');
      essayBox.style.background = '#f8fafc';
      essayBox.style.border = '1px dashed #cbd5e1';
      essayBox.style.borderRadius = '8px';
      essayBox.style.padding = '8px 12px';
      essayBox.style.margin = '8px 0';
      essayBox.style.color = '#475569';
      essayBox.style.fontSize = '0.875rem';
      essayBox.append(element('span', '📝 Câu hỏi tự luận: Học viên nhập bài làm bằng văn bản. Giáo viên chấm và nhập điểm ở trang "Chấm bài".'));
      row.append(essayBox);
    }

    card.append(row);
  });
  if (!questions.length) card.append(element('p', 'Đề nháp chưa có câu hỏi.', 'teacher-empty'));
  return card;
}
function openExamDetail(id, success) {
  history.replaceState(null, '', '/Teacher/exams.html?id=' + id);
  return gradingView(async () => {
    const data = await examRequest('/' + id), exam = data.exam;
    const supported = data.questions.every(q => ['essay', 'multiple_choice', 'fill_blank'].includes(q.questionType));
    const editable = !exam.isPublished && exam.reviewStatus !== 'pending' && !data.hasAttempts && supported;
    const top = element('div', null, 'teacher-toolbar exam-toolbar'); top.append(gradingButton('← Danh sách đề', () => openExamList()));
    if (editable) top.append(gradingButton(data.questions.length ? 'Sửa bản nháp' : 'Thêm đề', () => editExam(id), true));
    teacherContent.replaceChildren(top);
    if (success) teacherContent.append(examNotice(success));
    teacherContent.append(badge(examStatusLabels[examStatus(exam, data.questions.length, data.hasAttempts)], exam.isPublished ? 'success' : 'neutral'));
    if (!supported) teacherContent.append(element('p', 'Đề có loại câu hỏi chưa hỗ trợ. Trình soạn hỗ trợ tự luận, trắc nghiệm và điền từ.', 'exam-note'));
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
    if (supported) actions.append(gradingButton('Sao chép thành bản nháp', () => mutate('copy', 'Tạo đề nháp mới dùng lại câu hỏi, với điểm và thứ tự riêng?')));
    if (editable) {
      const total = Math.round(data.questions.reduce((sum, q) => sum + q.points, 0) * 100) / 100;
      const publish = gradingButton('Gửi đi', () => mutate('submit', 'Gửi đề cho quản trị viên duyệt?'), true);
      publish.disabled = total !== 10;
      actions.append(publish);
      if (publish.disabled) teacherContent.append(element('p', 'Chưa thể gửi duyệt: tổng điểm các câu hỏi phải bằng 10.', 'exam-note'));
    }
    if (!exam.isPublished && exam.reviewStatus !== 'pending' && !data.hasAttempts) actions.append(gradingButton('Xóa bản nháp', () => mutate('delete', 'Xóa vĩnh viễn bản nháp này? Câu hỏi trong ngân hàng được giữ nguyên.')));
    teacherContent.append(error, actions);
    if (exam.isPublished && data.questions.some(q => ['essay', 'multiple_choice', 'fill_blank'].includes(q.questionType))) {
      const section = panel('Theo dõi bài nộp theo lớp');
      section.append(element('p', 'Đề dùng chung theo khóa học; chọn lớp để xem học viên.', 'teacher-muted'));
      section.append(table(['Lớp', 'Chấm bài', 'Sổ điểm'], data.classes.map(c => [c.classCode + ' · ' + c.className,
        link('Mở chấm bài', '/Teacher/grading.html?classId=' + c.id + '&examId=' + id),
        link('Xem sổ điểm', '/Teacher/results.html?classId=' + c.id + '&examId=' + id)
      ]), 'Không có lớp được phân công trong khóa này.'));
      teacherContent.append(section);
    }
  });
}
function showExamEditor(course, bank, existing = null) {
  const exam = existing?.exam; let dirty = false, saving = false;
  const selected = (existing?.questions || []).map(q => ({ id: q.id, questionText: q.questionText, points: q.points, updatedAt: q.updatedAt, questionType: q.questionType, options: q.options, answers: q.answers }));
  const form = element('form', null, 'exam-form');
  const metadata = panel(exam ? 'Sửa bản nháp' : 'Tạo đề thi'); metadata.append(element('p', course.title, 'teacher-muted'), element('p', 'Lưu nháp chưa công bố đề. Điểm câu hỏi trong đề độc lập với điểm mặc định của ngân hàng.', 'exam-note'));
  function field(label, input, required = false) { const wrap = element('label', label, 'teacher-field'); input.required = required; wrap.append(input); metadata.append(wrap); return input; }
  const title = field('Tên đề *', element('input', null, 'teacher-search'), true); title.maxLength = 200; title.value = exam?.title || '';
  const description = field('Hướng dẫn làm bài', element('textarea', null, 'teacher-search')); description.rows = 4; description.maxLength = 20000; description.value = exam?.description || '';
  const duration = field('Thời gian làm bài (phút) *', element('input', null, 'teacher-search'), true); duration.type = 'number'; duration.min = '1'; duration.max = '1440'; duration.step = '1'; duration.value = exam?.durationMinutes ?? 45;
  metadata.append(element('p', 'Điểm đạt từ 5/10 trở lên.', 'teacher-muted'));
  const attempts = field('Số lượt làm tối đa (để trống nếu không giới hạn)', element('input', null, 'teacher-search')); attempts.type = 'number'; attempts.min = '1'; attempts.max = '100'; attempts.step = '1'; attempts.value = exam ? exam.maxAttempts ?? '' : 1;
  form.append(metadata);
  const bankPanel = panel('Chọn câu hỏi từ ngân hàng');
  const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm nội dung câu hỏi'; search.setAttribute('aria-label', 'Tìm câu hỏi trong ngân hàng');
  const lesson = examSelect('Lọc bài giảng', [['all', 'Tất cả bài giảng'], ...bank.lessons.map(l => [l.id, 'Bài ' + l.orderIndex + ' · ' + l.title])], 'all');
  const filters = element('div', null, 'teacher-toolbar exam-toolbar'); const type = examSelect('Lọc loại câu hỏi', [['all', 'Tất cả loại câu'], ['essay', 'Tự luận'], ['multiple_choice', 'Trắc nghiệm'], ['fill_blank', 'Điền từ']], 'all'); filters.append(search, lesson, type);
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
  function values() { return { title: title.value, description: description.value, durationMinutes: Number(duration.value), passScore: 5, maxAttempts: attempts.value === '' ? null : Number(attempts.value) }; }
  const previewButton = gradingButton('Xem trước', () => { if (!form.reportValidity()) return; preview.replaceChildren(renderExamPreview(values(), selected)); preview.hidden = false; preview.tabIndex = -1; preview.focus(); });
  actions.append(save, previewButton, cancel); form.append(error, actions, preview);
  function changed() { dirty = true; preview.hidden = true; }
  function updateSummary() {
    const total = Math.round(selected.reduce((sum, q) => sum + Number(q.points || 0), 0) * 100) / 100;
    summary.textContent = selected.length + ' / 100 câu · Tổng ' + total + ' / 10 điểm';
    save.disabled = selected.length > 0 && total !== 10;
    if (save.disabled) summary.textContent += ' · Điều chỉnh điểm các câu để lưu đề.';
  }
  function drawBank() {
    candidates.replaceChildren();
    const rows = bank.questions.filter(q => (type.value === 'all' || q.questionType === type.value) && (lesson.value === 'all' || String(q.lessonId) === lesson.value) && examNormalize(q.questionText).includes(examNormalize(search.value.trim())));
    for (const q of rows) {
      const row = element('div', null, 'teacher-row');
      const already = selected.some(s => s.id === q.id);
      const add = gradingButton(already ? 'Đã chọn' : 'Thêm câu', () => {
        if (saving || selected.some(s => s.id === q.id) || selected.length >= 100) return;
        selected.push({ id: q.id, questionText: q.questionText, points: q.defaultPoints, updatedAt: q.updatedAt, questionType: q.questionType, options: q.options, answers: q.answers }); changed(); drawSelected(); drawBank();
      }); add.disabled = already || selected.length >= 100;
      row.append(element('p', '#' + q.id + ' · ' + q.questionText, 'teacher-copy'), element('p', typeLabel(q.questionType) + ' · ' + q.defaultPoints + ' điểm mặc định', 'teacher-muted'), add); candidates.append(row);
    }
    if (!rows.length) candidates.append(element('p', bank.questions.length ? 'Không có câu phù hợp.' : 'Chưa có câu hỏi trong khóa này. Bạn vẫn có thể lưu đề nháp trống.', 'teacher-empty'));
  }
  function drawSelected() {
    chosen.replaceChildren();
    selected.forEach((q, index) => {
      const row = element('article', null, 'teacher-row'); row.append(element('h3', 'Câu ' + (index + 1) + ' · #' + q.id + ' · ' + typeLabel(q.questionType)), element('p', q.questionText, 'teacher-copy'));
      const label = element('label', 'Điểm trong đề *', 'teacher-field'), score = element('input', null, 'teacher-search');
      score.type = 'number'; score.min = '0.01'; score.max = '1000'; score.step = '0.01'; score.required = true; score.value = q.points;
      score.addEventListener('input', () => { q.points = score.value === '' ? null : Number(score.value); changed(); updateSummary(); }); label.append(score); row.append(label);
      if (q.questionType === 'multiple_choice' && q.options) {
        const correct = q.options.find(o => o.isCorrect);
        if (correct) {
          const ansP = element('p', '✓ Đáp án đúng: ' + correct.optionText, 'teacher-muted');
          ansP.style.color = '#15803d';
          ansP.style.fontSize = '0.8125rem';
          ansP.style.marginTop = '4px';
          row.append(ansP);
        }
      } else if (q.questionType === 'fill_blank' && q.answers && q.answers.length) {
        const ansP = element('p', '🔑 Đáp án chấp nhận: ' + q.answers.map(a => a.answerText).join(', '), 'teacher-muted');
        ansP.style.color = '#15803d';
        ansP.style.fontSize = '0.8125rem';
        ansP.style.marginTop = '4px';
        row.append(ansP);
      }
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
  search.addEventListener('input', drawBank); lesson.addEventListener('change', drawBank); type.addEventListener('change', drawBank);
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
