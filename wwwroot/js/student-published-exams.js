(async () => {
  const list = document.getElementById('published-exams');
  try {
    const response = await fetch('/api/student/published-exams', { cache: 'no-store' });
    if (!response.ok) throw new Error();
    const exams = await response.json();
    list.replaceChildren();
    if (!exams.length) list.textContent = 'Chưa có đề thi được công bố trong khóa học của bạn.';
    for (const exam of exams) {
      const card = document.createElement('article');
      card.className = 'card';
      const title = document.createElement('h3');
      title.textContent = exam.title;
      const info = document.createElement('p');
      info.textContent = exam.durationMinutes + ' phút · Điểm đạt ' + exam.passScore;
      card.append(title, info);
      if (exam.description) {
        const description = document.createElement('p');
        description.textContent = exam.description;
        card.append(description);
      }
      list.append(card);
    }
  } catch { list.textContent = 'Không tải được danh sách đề đã công bố.'; }
})();
