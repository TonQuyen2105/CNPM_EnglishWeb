// Sổ điểm tổng hợp điểm tự động và tự luận; chỉ kết luận khi chấm đủ câu.
const gradebookLabels = {
  not_submitted: 'Chưa nộp', in_progress: 'Chưa nộp', pending_review: 'Chờ chấm', pending_auto: 'Chờ chấm tự động',
  graded: 'Đã chấm', passed: 'Đạt', failed: 'Chưa đạt'
};
function gradebookBadge(status) {
  return badge(gradebookLabels[status] || status,
    status === 'passed' || status === 'graded' ? 'success' : status === 'failed' ? 'danger' : ['pending_review', 'pending_auto'].includes(status) ? 'warning' : 'neutral');
}
function gradebookScore(score, max) {
  return score == null ? '—' : new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(score) + ' / ' + max;
}
function gradebookTimeNode(value) {
  if (!value) return element('span', '—', 'teacher-muted');
  const d = new Date(value);
  const time = d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit', timeZone: 'Asia/Ho_Chi_Minh' });
  const date = d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh' });
  const wrap = element('div', null, 'gradebook-time-cell');
  wrap.append(
    element('span', time, 'gradebook-time-val'),
    element('span', date, 'gradebook-date-val')
  );
  return wrap;
}
function gradebookTime(value) {
  return value ? new Date(value).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '—';
}
async function gradebookRequest(classId, examId) {
  const params = new URLSearchParams();
  if (classId) params.set('classId', classId);
  if (examId) params.set('examId', examId);
  const response = await fetch('/api/teacher/gradebook?' + params, { cache: 'no-store' });
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body.message || (response.status === 401 ? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
      : response.status === 404 ? 'Không tìm thấy lớp hoặc đề thi thuộc quyền truy cập của bạn.' : 'Không tải được sổ điểm (HTTP ' + response.status + ').'));
  }
  return response.json();
}
function openGradebook() {
  const params = new URLSearchParams(window.location.search);
  const classId = Number(params.get('classId')), examId = Number(params.get('examId'));
  if (Number.isSafeInteger(classId) && classId > 0) return openGradebookRoster(classId, Number.isSafeInteger(examId) && examId > 0 ? examId : null);
  return openGradebookClasses();
}
function openGradebookClasses() {
  history.replaceState(null, '', '/Teacher/results.html');
  return gradingView(async () => {
    const classes = await gradebookRequest();
    const grid = element('div', null, 'grid grid-cols-3 gap-6');
    for (const item of classes) {
      const card = panel('');
      const top = element('div', null, 'teacher-split'); top.append(badge(item.classCode), statusBadge(item.status));
      card.append(top, element('h2', item.className, 'teacher-copy'), element('p', item.students + ' học viên', 'teacher-muted'),
        gradingButton('Xem sổ điểm', () => openGradebookRoster(item.id), true));
      grid.append(card);
    }
    teacherContent.replaceChildren(classes.length ? grid : element('p', 'Bạn chưa được phân công lớp học.', 'card teacher-empty'));
  });
}
function openGradebookRoster(classId, examId) {
  return gradingView(async () => {
    const data = await gradebookRequest(classId, examId);
    history.replaceState(null, '', '/Teacher/results.html?classId=' + classId + (data.examId ? '&examId=' + data.examId : ''));
    const top = element('div', null, 'teacher-toolbar');
    top.append(gradingButton('← Danh sách lớp', openGradebookClasses), gradingButton('Làm mới điểm', () => openGradebookRoster(classId, data.examId)));
    teacherContent.replaceChildren(top, element('h2', data.class.classCode + ' · ' + data.class.className));
    const note = element('p', 'Đề thi dùng chung theo khóa học. Bảng lấy lượt đã nộp mới nhất; bài đang làm lại không thay thế điểm đã có.', 'teacher-muted');
    teacherContent.append(note);
    if (!data.exams.length) {
      teacherContent.append(element('p', 'Khóa học chưa có đề đã công bố thuộc loại hỗ trợ do bạn tạo.', 'card teacher-empty'));
      teacherContent.append(table(['Học viên', 'Email'], data.students.map(s => [s.fullName, s.email]), 'Lớp chưa có học viên.'));
      return;
    }
    const examLabel = element('label', 'Đề thi', 'teacher-field');
    const select = element('select', null, 'teacher-search');
    data.exams.forEach(exam => { const option = element('option', exam.title); option.value = exam.id; select.append(option); });
    select.value = data.examId; select.addEventListener('change', () => openGradebookRoster(classId, Number(select.value)));
    examLabel.append(select); teacherContent.append(examLabel);
    teacherContent.append(element('p', 'Tổng điểm: ' + data.maxScore + ' · Tự động: ' + data.automaticMaxScore + ' · Tự luận: ' + data.essayMaxScore + ' · Điểm đạt: ' + data.passScore + '. Chỉ có tổng điểm khi chấm đủ tất cả câu.', 'teacher-muted'));
    const graded = data.students.filter(s => s.latest?.score != null);
    const stats = element('div', null, 'grid grid-cols-4 gap-6 gradebook-stats');
    const metrics = [['Học viên', data.students.length], ['Đã nộp', data.students.filter(s => s.latest).length],
      ['Chờ chấm', data.students.filter(s => ['pending_review', 'pending_auto'].includes(s.status)).length],
      ['TB bài đã chấm đủ', graded.length ? gradebookScore(graded.reduce((sum, s) => sum + s.latest.score, 0) / graded.length, data.maxScore) : '—']];
    for (const [title, value] of metrics) { const card = panel(''); card.append(element('p', title, 'teacher-muted'), element('strong', value, 'gradebook-number')); stats.append(card); }
    teacherContent.append(stats, element('p', graded.length + ' bài đã chấm đủ', 'teacher-muted'));
    teacherContent.append(element('p', data.students.filter(s => s.status === 'passed').length + ' đạt · ' + data.students.filter(s => s.status === 'failed').length + ' chưa đạt', 'teacher-muted'));
    const filters = element('div', null, 'teacher-toolbar gradebook-filters');
    const search = element('input', null, 'teacher-search'); search.type = 'search'; search.placeholder = 'Tìm tên hoặc email'; search.setAttribute('aria-label', 'Tìm học viên');
    const status = element('select', null, 'teacher-search'); status.setAttribute('aria-label', 'Lọc trạng thái');
    const options = { all: 'Tất cả trạng thái', ...gradebookLabels };
    Object.entries(options).forEach(([value, text]) => { const option = element('option', text); option.value = value; status.append(option); });
    const exportButton = gradingButton('Xuất CSV', () => exportGradebookCsv(data, filtered), true);
    filters.append(search, status, exportButton); teacherContent.append(filters);
    const counter = element('p', null, 'teacher-muted'); counter.setAttribute('aria-live', 'polite');
    const list = element('div', null, 'gradebook-table-wrap'), detail = element('div', null, 'gradebook-detail-wrap');
    teacherContent.append(counter, list, detail);
    let filtered = [];
    const normalize = value => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    const attemptLink = (attempt, label) => {
      const a = link(label, '/Teacher/grading.html?classId=' + classId + '&examId=' + data.examId + '&attemptId=' + attempt.attemptId + '&from=results');
      a.className = 'btn btn-secondary'; return gradingActionIcon(a, 'view', label);
    };
    function showHistory(student) {
      const card = panel('Lịch sử bài nộp · ' + student.fullName);
      card.tabIndex = -1;
      const historyTableWrap = element('div', null, 'gradebook-history-table-wrap');
      historyTableWrap.append(table(['Lượt', 'Nộp lúc', 'Điểm tự động', 'Điểm tự luận', 'Tổng điểm', 'Trạng thái', 'Thao tác'], student.history.map(a => [
        a.attemptNumber,
        gradebookTimeNode(a.submittedAt),
        gradebookScore(a.automaticScore, data.automaticMaxScore),
        gradebookScore(a.essayScore, data.essayMaxScore),
        gradebookScore(a.score, data.maxScore),
        gradebookBadge(a.status),
        attemptLink(a, 'Xem chi tiết')
      ]), 'Chưa có lượt nộp.'));
      card.append(gradingButton('Đóng lịch sử', () => { detail.replaceChildren(); search.focus(); }), historyTableWrap);
      detail.replaceChildren(card); card.focus();
    }
    function draw() {
      const term = normalize(search.value.trim());
      filtered = data.students.filter(s => normalize(s.fullName + ' ' + s.email).includes(term) && (status.value === 'all' || s.status === status.value));
      counter.textContent = filtered.length + ' / ' + data.students.length + ' học viên · CSV xuất theo bộ lọc hiện tại';
      exportButton.disabled = !filtered.length; detail.replaceChildren();
      list.replaceChildren(table(['Học viên', 'Email', 'Lượt nộp', 'Nộp lúc', 'Điểm tự động', 'Điểm tự luận', 'Tổng điểm', 'Trạng thái', 'Thao tác'], filtered.map(s => {
        const actions = element('div', null, 'teacher-grading-row-actions');
        if (s.latest) actions.append(attemptLink(s.latest, 'Xem bài · ' + s.fullName), gradingActionIcon(gradingButton('Lịch sử', () => showHistory(s)), 'history', 'Lịch sử (' + s.history.length + ' lượt) · ' + s.fullName));
        else actions.append(element('span', 'Chưa có bài nộp', 'teacher-muted'));

        const nameSpan = element('span', s.fullName, 'gradebook-student-name');
        nameSpan.title = s.fullName;

        const emailSpan = element('span', s.email, 'gradebook-student-email');
        emailSpan.title = s.email;

        const timeNode = s.latest?.submittedAt ? gradebookTimeNode(s.latest.submittedAt) : element('span', '—', 'teacher-muted');

        return [
          nameSpan,
          emailSpan,
          s.latest?.attemptNumber ?? '—',
          timeNode,
          gradebookScore(s.latest?.automaticScore, data.automaticMaxScore),
          gradebookScore(s.latest?.essayScore, data.essayMaxScore),
          gradebookScore(s.latest?.score, data.maxScore),
          gradebookBadge(s.status),
          actions
        ];
      }), data.students.length ? 'Không có học viên phù hợp với bộ lọc.' : 'Lớp chưa có học viên.'));
    }
    search.addEventListener('input', draw); status.addEventListener('change', draw); draw();
  });
}
// Quote all fields and neutralize formula prefixes when CSV is opened in Excel.
function gradebookCsvCell(value) {
  let text = String(value ?? '');
  if (/^[\s]*[=+@-]/.test(text) || /^[\t\r\n]/.test(text)) text = "'" + text;
  return '"' + text.replace(/"/g, '""') + '"';
}
function buildGradebookCsv(data, students) {
  const exam = data.exams.find(e => e.id === data.examId);
  const rows = [['Mã lớp', 'Đề thi', 'Học viên', 'Email', 'Lượt nộp', 'Nộp lúc', 'Điểm tự động', 'Điểm tự luận', 'Tổng điểm', 'Điểm tối đa', 'Trạng thái'],
    ...students.map(s => [data.class.classCode, exam?.title, s.fullName, s.email, s.latest?.attemptNumber,
      s.latest?.submittedAt ? gradebookTime(s.latest.submittedAt) : '', s.latest?.automaticScore ?? '', s.latest?.essayScore ?? '', s.latest?.score ?? '', data.maxScore, gradebookLabels[s.status]])];
  return '\uFEFF' + rows.map(row => row.map(gradebookCsvCell).join(',')).join('\r\n');
}
function exportGradebookCsv(data, students) {
  const url = URL.createObjectURL(new Blob([buildGradebookCsv(data, students)], { type: 'text/csv;charset=utf-8;' }));
  const a = document.createElement('a'); a.href = url;
  a.download = 'so-diem-lop-' + data.class.id + '-de-' + data.examId + '.csv';
  document.body.append(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
}
