(() => {
  const root = document.getElementById('student-lesson-content');
  const el = (tag, text, cls) => { const n = document.createElement(tag); if (text) n.textContent = text; if (cls) n.className = cls; return n; };
  const link = (text, href) => { const n = el('a', text); n.href = href; return n; };
  const card = (title) => { const n = el('section', '', 'learning-card'); n.append(el('h2', title)); return n; };
  async function load() {
    root.replaceChildren(el('p', 'Đang tải bài giảng…')); root.setAttribute('aria-busy', 'true');
    try {
      const params = new URLSearchParams(location.search), query = new URLSearchParams();
      const id = params.get('lessonId') || params.get('id');
      for (const [key, value] of [['lessonId', id], ['classId', params.get('classId')]]) {
        if (value !== null) { if (!/^[1-9]\d*$/.test(value)) throw new Error('Mã bài giảng hoặc lớp không hợp lệ.'); query.set(key, value); }
      }
      const response = await fetch('/api/lessons/learn?' + query, { cache: 'no-store' });
      if (response.status === 401) { root.replaceChildren(el('p', 'Vui lòng đăng nhập để xem bài giảng.'), link('Đăng nhập', '/Account/Login')); return; }
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Không tải được bài giảng. Vui lòng thử lại.');
      root.replaceChildren();
      document.title = data.title + ' - EnglishStudent';
      document.querySelectorAll('[data-lesson-name]').forEach(n => n.textContent = data.profile.fullName);
      document.querySelectorAll('[data-lesson-email]').forEach(n => n.textContent = data.profile.email);
      document.querySelectorAll('[data-lesson-avatar]').forEach(n => n.textContent = (data.profile.fullName || '?').split(/\s+/).slice(-2).map(x => x[0]).join(''));
      root.append(link('← Quay lại lớp', '/Student/my-classes.html'), el('h1', data.title, 'learning-title'), el('p', 'Lớp: ' + data.class.classCode + ' · ' + data.class.className + ' — Khóa học: ' + data.course.title, 'learning-subtitle'));
      const grid = el('div', '', 'learning-grid'), body = el('div', '', 'learning-body');
      const media = card('Video bài giảng');
      if (data.video) {
        const video = el('video'); video.controls = true; video.preload = 'metadata'; video.playsInline = true; video.src = data.video.url;
        video.setAttribute('aria-label', data.video.name);
        video.addEventListener('error', () => { if (!media.querySelector('[role="alert"]')) { const msg = el('p', 'Không phát được video. Hãy tải lại trang hoặc báo giáo viên kiểm tra file và định dạng video.'); msg.setAttribute('role', 'alert'); media.append(msg); } });
        media.append(video, el('p', data.video.name, 'learning-subtitle'));
      } else media.append(el('p', 'Giáo viên chưa thêm video cho bài này.'));
      const content = card('Nội dung bài giảng');
      content.append(el('div', data.content || 'Chưa có nội dung văn bản.', 'learning-text'));
      if (data.pdf) {
        content.append(el('h3', 'Tài liệu PDF'));
        const open = link('Mở tài liệu: ' + data.pdf.name, data.pdf.url); open.target = '_blank'; open.rel = 'noopener';
        const frame = el('iframe'); frame.src = data.pdf.url; frame.title = 'Tài liệu PDF: ' + data.pdf.name; frame.loading = 'lazy';
        content.append(open, el('p', 'Nếu trình duyệt không hiển thị PDF bên dưới, hãy dùng liên kết mở tài liệu.', 'learning-subtitle'), frame);
      }
      const toc = card('Mục lục bài học'), nav = el('nav'); nav.setAttribute('aria-label', 'Mục lục bài học');
      data.contents.forEach(item => { const a = link(item.title, '/Student/learn.html?lessonId=' + item.id + '&classId=' + data.class.id); if (item.id === data.id) a.setAttribute('aria-current', 'page'); nav.append(a); });
      toc.append(nav); body.append(media, content); grid.append(body, toc); root.append(grid);
    } catch (error) {
      const message = el('p', error.message || 'Không tải được bài giảng.'); message.setAttribute('role', 'alert');
      const retry = el('button', 'Thử lại', 'btn btn-primary'); retry.type = 'button'; retry.addEventListener('click', load); root.replaceChildren(message, retry);
    } finally { root.setAttribute('aria-busy', 'false'); }
  }
  load();
})();
