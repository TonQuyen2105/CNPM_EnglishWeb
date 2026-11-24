(() => {
  const api = '/api/admin/exam-reviews';
  const list = document.getElementById('exam-review-list');
  const message = document.getElementById('exam-review-message');
  const reload = document.getElementById('reload-exam-reviews');
  function element(tag, text) {
    const node = document.createElement(tag);
    node.textContent = text;
    return node;
  }
  async function load(notice = '') {
    reload.disabled = true;
    list.replaceChildren();
    message.textContent = 'Đang tải đề chờ duyệt…';
    try {
      const response = await fetch(api, { cache: 'no-store' });
      if (!response.ok) throw new Error(response.status === 401 || response.status === 403
        ? 'Hãy đăng nhập bằng tài khoản quản trị viên.' : 'Không tải được danh sách đề chờ duyệt.');
      const { exams, token } = await response.json();
      for (const exam of exams) {
        const details = document.createElement('details');
        details.append(element('summary', exam.title + ' · ' + exam.teacher));
        details.append(element('p', exam.durationMinutes + ' phút · Điểm đạt ' + exam.passScore));
        if (exam.description) details.append(element('p', exam.description));
        const questions = document.createElement('ol');
        for (const q of exam.questions) {
          const item = element('li', q.questionText + ' (' + q.points + ' điểm)');
          for (const o of q.options) item.append(element('div', o.optionText + (o.isCorrect ? ' ✓' : '')));
          if (q.answers.length) item.append(element('div', 'Đáp án: ' + q.answers.join(' / ')));
          questions.append(item);
        }
        details.append(questions);
        for (const approve of [true, false]) {
          const label = approve ? 'Duyệt và công bố' : 'Từ chối';
          const btn = element('button', label);
          btn.type = 'button';
          btn.className = approve ? 'btn btn-primary' : 'btn btn-secondary';
          btn.onclick = async () => {
            if (!window.confirm(label + ' đề “' + exam.title + '”?')) return;
            const buttons = details.querySelectorAll('button');
            buttons.forEach(b => b.disabled = true);
            try {
              const result = await fetch(api + '/' + exam.id, { method: 'POST',
                headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
                body: JSON.stringify({ approve, expectedUpdatedAt: exam.updatedAt }) });
              const body = await result.json().catch(() => ({}));
              if (!result.ok) throw new Error(body.message || 'Không lưu được quyết định duyệt đề.');
              await load(body.message);
            } catch (error) { message.textContent = error.message; buttons.forEach(b => b.disabled = false); }
          };
          details.append(btn);
        }
        list.append(details);
      }
      message.textContent = [notice, exams.length ? '' : 'Không có đề chờ duyệt.'].filter(Boolean).join(' ');
    } catch (error) { message.textContent = error.message; }
    finally { reload.disabled = false; }
  }
  reload.onclick = () => load();
  load();
})();
