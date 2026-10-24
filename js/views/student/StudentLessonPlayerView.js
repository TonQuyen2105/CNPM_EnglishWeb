/**
 * StudentLessonPlayerView - Giao diện Trình phát bài học (Lesson Player)
 */

import { icon } from '../../ui/icons.js';

export class StudentLessonPlayerView {
  static render({
    course = {},
    lesson = {},
    courseLessons = [],
    userProgress = [],
    quizQuestions = [],
    isCompleted = false
  }) {
    const completedLessonIds = userProgress
      .filter(p => p.is_completed === 1 || p.is_completed === true)
      .map(p => p.lesson_id);

    return `
      <div class="student-lesson-player">
        <!-- Top Navigator Bar -->
        <div class="flex items-center justify-between flex-wrap gap-4" style="margin-bottom: 1.5rem;">
          <div class="flex items-center gap-3">
            <a href="#/courses/${course.id}" class="btn btn-secondary btn-sm" style="background: white; border-radius: var(--radius-full);">
              <span class="nav-icon">${icon('arrow-left', { size: 16 })}</span>
              <span>Khóa học</span>
            </a>
            <div>
              <div style="font-size: 0.75rem; color: var(--text-muted); font-weight: 600;">${course.title}</div>
              <h1 style="font-size: 1.25rem; font-weight: 800; color: var(--text-primary); margin: 0;">
                Bài ${lesson.order_index}: ${lesson.title}
              </h1>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <button type="button" class="btn ${isCompleted ? 'btn-success' : 'btn-primary'}" id="btn-complete-lesson" data-lesson-id="${lesson.id}">
              <span class="nav-icon">${icon(isCompleted ? 'check-circle' : 'check', { size: 18 })}</span>
              <span>${isCompleted ? 'Đã hoàn thành bài học' : 'Đánh dấu Hoàn thành'}</span>
            </button>
          </div>
        </div>

        <!-- Layout 2 Cột: Nội dung bài học (8 cột) & Mục lục khóa học (4 cột) -->
        <div class="grid grid-cols-12 gap-6">
          
          <!-- Cột Trái (8 cột): Nội dung chi tiết theo content_type -->
          <div class="col-span-8 flex flex-col gap-6">
            <div class="card" style="border-radius: var(--radius-xl); padding: 2rem; border: 1px solid var(--border-color); background: #FFFFFF;">
              
              <!-- Tag content_type -->
              <div class="flex items-center justify-between" style="margin-bottom: 1.5rem; padding-bottom: 1rem; border-bottom: 1px solid var(--border-color);">
                <div class="flex items-center gap-2">
                  <span class="badge badge-primary" style="text-transform: uppercase; font-weight: 700;">
                    Chủ đề: ${lesson.content_type || 'vocabulary'}
                  </span>
                  <span style="font-size: 0.8125rem; color: var(--text-muted);">
                    Thời lượng ước tính: 15-20 phút
                  </span>
                </div>
                ${isCompleted ? `
                  <span class="badge badge-success flex items-center gap-1">
                    <span>✓</span> <span>Đã đạt</span>
                  </span>
                ` : ''}
              </div>

              <!-- Nội dung động theo content_type -->
              <div class="lesson-dynamic-content">
                ${this._renderContentByType(lesson, quizQuestions)}
              </div>

              <!-- Nút Hoàn thành ở cuối bài -->
              <div style="margin-top: 2.5rem; padding-top: 1.5rem; border-top: 1px solid var(--border-color); text-align: center;">
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin-bottom: 1rem;">
                  Bạn đã nắm vững kiến thức trong bài học này? Nhấn nút bên dưới để ghi nhận tiến độ học tập.
                </p>
                <button type="button" class="btn ${isCompleted ? 'btn-outline-success' : 'btn-primary'} btn-lg" id="btn-complete-lesson-bottom" data-lesson-id="${lesson.id}" style="padding: 10px 28px; border-radius: var(--radius-full);">
                  <span class="nav-icon">${icon(isCompleted ? 'check-circle' : 'check', { size: 20 })}</span>
                  <span>${isCompleted ? 'Đã hoàn thành bài (Nhấn để cập nhật lại)' : 'Hoàn thành bài học này'}</span>
                </button>
              </div>

            </div>
          </div>

          <!-- Cột Phải (4 cột): Mục lục bài học trong khóa -->
          <div class="col-span-4">
            <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color); position: sticky; top: 90px;">
              <div class="flex items-center justify-between" style="margin-bottom: 1.25rem;">
                <h3 style="font-size: 1rem; font-weight: 800; margin: 0; color: var(--text-primary);">
                  Mục lục khóa học
                </h3>
                <span class="badge badge-primary">
                  ${completedLessonIds.length}/${courseLessons.length} bài
                </span>
              </div>

              <div class="flex flex-col gap-2" style="max-height: 520px; overflow-y: auto;">
                ${courseLessons.map(l => {
                  const isCurrent = l.id === lesson.id;
                  const isDone = completedLessonIds.includes(l.id);

                  return `
                    <a href="#/learn/${course.id}/${l.id}" style="display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; border-radius: var(--radius-md); text-decoration: none; border: 1px solid ${isCurrent ? 'var(--primary-500)' : 'var(--border-color)'}; background: ${isCurrent ? '#EFF6FF' : isDone ? '#F0FDF4' : '#FFFFFF'}; transition: all var(--transition-fast);">
                      <div class="flex items-center gap-3">
                        <div style="width: 26px; height: 26px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.75rem; font-weight: 800; background: ${isDone ? '#16A34A' : isCurrent ? 'var(--primary-600)' : 'var(--neutral-200)'}; color: ${isDone || isCurrent ? 'white' : 'var(--text-secondary)'};">
                          ${isDone ? '✓' : l.order_index}
                        </div>
                        <div>
                          <div style="font-size: 0.8125rem; font-weight: ${isCurrent ? '800' : '600'}; color: ${isCurrent ? 'var(--primary-700)' : 'var(--text-primary)'}; line-height: 1.3;">
                            ${l.title}
                          </div>
                          <span style="font-size: 0.6875rem; color: var(--text-muted); text-transform: uppercase;">
                            ${l.content_type}
                          </span>
                        </div>
                      </div>

                      <span style="color: ${isCurrent ? 'var(--primary-600)' : 'var(--text-muted)'}; font-size: 0.8125rem;">
                        ${isCurrent ? '▶' : ''}
                      </span>
                    </a>
                  `;
                }).join('')}
              </div>
            </div>
          </div>

        </div>
      </div>
    `;
  }

  static _renderContentByType(lesson, quizQuestions) {
    const type = lesson.content_type || 'vocabulary';

    switch (type) {
      case 'vocabulary':
        return `
          <div class="lesson-vocab-section">
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 1rem; color: var(--text-primary);">
              Từ vựng trọng tâm trong bài
            </h3>
            <p style="color: var(--text-secondary); font-size: 0.875rem; margin-bottom: 1.5rem;">
              Bấm vào biểu tượng loa để nghe phát âm chuẩn bản xứ (American English) và luyện đọc theo.
            </p>

            <div class="grid grid-cols-2 gap-4">
              <div style="padding: 1.25rem; background: var(--neutral-50); border-radius: var(--radius-lg); border: 1px solid var(--border-color);">
                <div class="flex items-center justify-between" style="margin-bottom: 6px;">
                  <span style="font-size: 1.25rem; font-weight: 800; color: var(--primary-700);">Fluent</span>
                  <button type="button" class="btn-icon btn-speak" data-word="Fluent" title="Nghe phát âm">
                    ${icon('volume-2', { size: 20 })}
                  </button>
                </div>
                <div style="font-family: monospace; color: var(--text-muted); font-size: 0.875rem; margin-bottom: 8px;">/ˈfluː.ənt/ (adj)</div>
                <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 6px;">Lưu loát, trôi chảy</div>
                <div style="font-size: 0.8125rem; color: var(--text-secondary); font-style: italic;">"She is fluent in both English and French."</div>
              </div>

              <div style="padding: 1.25rem; background: var(--neutral-50); border-radius: var(--radius-lg); border: 1px solid var(--border-color);">
                <div class="flex items-center justify-between" style="margin-bottom: 6px;">
                  <span style="font-size: 1.25rem; font-weight: 800; color: var(--primary-700);">Vocabulary</span>
                  <button type="button" class="btn-icon btn-speak" data-word="Vocabulary" title="Nghe phát âm">
                    ${icon('volume-2', { size: 20 })}
                  </button>
                </div>
                <div style="font-family: monospace; color: var(--text-muted); font-size: 0.875rem; margin-bottom: 8px;">/vəˈkæb.jə.lər.i/ (n)</div>
                <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 6px;">Vốn từ vựng</div>
                <div style="font-size: 0.8125rem; color: var(--text-secondary); font-style: italic;">"Reading helps expand your active vocabulary."</div>
              </div>

              <div style="padding: 1.25rem; background: var(--neutral-50); border-radius: var(--radius-lg); border: 1px solid var(--border-color);">
                <div class="flex items-center justify-between" style="margin-bottom: 6px;">
                  <span style="font-size: 1.25rem; font-weight: 800; color: var(--primary-700);">Comprehension</span>
                  <button type="button" class="btn-icon btn-speak" data-word="Comprehension" title="Nghe phát âm">
                    ${icon('volume-2', { size: 20 })}
                  </button>
                </div>
                <div style="font-family: monospace; color: var(--text-muted); font-size: 0.875rem; margin-bottom: 8px;">/ˌkɒm.prɪˈhen.ʃən/ (n)</div>
                <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 6px;">Khả năng hiểu, đọc hiểu</div>
                <div style="font-size: 0.8125rem; color: var(--text-secondary); font-style: italic;">"Listening comprehension requires regular practice."</div>
              </div>

              <div style="padding: 1.25rem; background: var(--neutral-50); border-radius: var(--radius-lg); border: 1px solid var(--border-color);">
                <div class="flex items-center justify-between" style="margin-bottom: 6px;">
                  <span style="font-size: 1.25rem; font-weight: 800; color: var(--primary-700);">Pronunciation</span>
                  <button type="button" class="btn-icon btn-speak" data-word="Pronunciation" title="Nghe phát âm">
                    ${icon('volume-2', { size: 20 })}
                  </button>
                </div>
                <div style="font-family: monospace; color: var(--text-muted); font-size: 0.875rem; margin-bottom: 8px;">/prəˌnʌn.siˈeɪ.ʃən/ (n)</div>
                <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 6px;">Cách phát âm</div>
                <div style="font-size: 0.8125rem; color: var(--text-secondary); font-style: italic;">"Good pronunciation builds speaking confidence."</div>
              </div>
            </div>
          </div>
        `;

      case 'grammar':
        return `
          <div class="lesson-grammar-section">
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 1rem; color: var(--text-primary);">
              Cấu trúc ngữ pháp: Present Perfect vs. Past Simple
            </h3>
            
            <div style="background: #EFF6FF; border-left: 4px solid var(--primary-600); padding: 1.25rem; border-radius: var(--radius-md); margin-bottom: 1.5rem;">
              <h4 style="font-size: 0.9375rem; font-weight: 700; color: var(--primary-800); margin-bottom: 0.5rem;">
                📌 Công thức tổng quát:
              </h4>
              <p style="margin: 0; font-size: 0.875rem; color: var(--primary-900);">
                <strong>S + have / has + V3/ed</strong> (Hiện tại hoàn thành: diễn tả hành động đã xảy ra không rõ thời điểm hoặc kéo dài từ quá khứ đến hiện tại).
              </p>
            </div>

            <table style="width: 100%; border-collapse: collapse; margin-bottom: 1.5rem; font-size: 0.875rem;">
              <thead>
                <tr style="background: var(--neutral-100); border-bottom: 2px solid var(--border-color);">
                  <th style="padding: 10px; text-align: left;">Thì Hiện tại hoàn thành</th>
                  <th style="padding: 10px; text-align: left;">Thì Quá khứ đơn</th>
                </tr>
              </thead>
              <tbody>
                <tr style="border-bottom: 1px solid var(--border-color);">
                  <td style="padding: 10px;">I have lived in Hanoi for 5 years. (Vẫn đang sống)</td>
                  <td style="padding: 10px;">I lived in Hanoi in 2018. (Hiện tại không còn sống)</td>
                </tr>
                <tr style="border-bottom: 1px solid var(--border-color);">
                  <td style="padding: 10px;">She has visited London twice. (Trải nghiệm)</td>
                  <td style="padding: 10px;">She visited London last summer. (Thời điểm xác định)</td>
                </tr>
              </tbody>
            </table>
          </div>
        `;

      case 'listening':
        return `
          <div class="lesson-listening-section">
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 1rem; color: var(--text-primary);">
              Luyện nghe: Conversation at the Airport
            </h3>
            <p style="color: var(--text-secondary); font-size: 0.875rem; margin-bottom: 1.5rem;">
              Hãy lắng nghe đoạn hội thoại dưới đây và đọc theo lời thoại transcript.
            </p>

            <!-- Custom Audio Player Controller -->
            <div style="background: linear-gradient(135deg, #1E293B, #0F172A); color: white; padding: 1.5rem; border-radius: var(--radius-xl); margin-bottom: 1.5rem;">
              <div class="flex items-center justify-between" style="margin-bottom: 1rem;">
                <div class="flex items-center gap-3">
                  <button type="button" class="btn-icon" id="btn-audio-play" style="width: 44px; height: 44px; border-radius: 50%; background: var(--primary-600); color: white; border: none; display: flex; align-items: center; justify-content: center; cursor: pointer;">
                    ${icon('play', { size: 20 })}
                  </button>
                  <div>
                    <div style="font-weight: 700; font-size: 0.9375rem;">Airport_Dialogue_Part1.mp3</div>
                    <div style="font-size: 0.75rem; color: #94A3B8;">Audio chất lượng cao • 128 kbps</div>
                  </div>
                </div>
                <div id="audio-time-display" style="font-family: monospace; font-size: 0.875rem; color: #38BDF8;">
                  00:15 / 01:45
                </div>
              </div>

              <!-- Seekbar Range -->
              <input type="range" id="audio-seekbar" min="0" max="100" value="15" style="width: 100%; cursor: pointer;">
            </div>

            <!-- Transcript Accordion -->
            <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 1.25rem; background: var(--neutral-50);">
              <h4 style="font-size: 0.9375rem; font-weight: 700; margin-bottom: 0.75rem; color: var(--primary-700);">
                Lược dịch & Transcript đoạn hội thoại:
              </h4>
              <p style="font-size: 0.875rem; line-height: 1.6; margin-bottom: 0.5rem;">
                <strong>Agent:</strong> Good morning! May I have your passport and ticket, please?<br>
                <strong>Passenger:</strong> Sure, here you go. Can I get a window seat if possible?<br>
                <strong>Agent:</strong> Let me check... Yes, seat 14A is available. How many bags are you checking in today?
              </p>
            </div>
          </div>
        `;

      case 'quiz':
        return `
          <div class="lesson-quiz-section">
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 1rem; color: var(--text-primary);">
              Kiểm tra nhanh kiến thức bài học
            </h3>
            <p style="color: var(--text-secondary); font-size: 0.875rem; margin-bottom: 1.5rem;">
              Làm các câu hỏi trắc nghiệm và điền từ để kiểm tra mức độ hiểu bài.
            </p>

            ${quizQuestions && quizQuestions.length > 0 ? `
              <div class="flex flex-col gap-6" id="quiz-container">
                ${quizQuestions.map((q, qIndex) => `
                  <div class="quiz-question-card" style="padding: 1.25rem; border: 1px solid var(--border-color); border-radius: var(--radius-lg); background: var(--neutral-50);">
                    <div class="flex items-center justify-between" style="margin-bottom: 0.75rem;">
                      <span style="font-weight: 800; color: var(--primary-700);">Câu ${qIndex + 1}:</span>
                      <span class="badge badge-primary">${q.question_type === 'multiple_choice' ? 'Trắc nghiệm' : 'Điền từ'}</span>
                    </div>

                    <p style="font-weight: 600; font-size: 0.9375rem; margin-bottom: 1rem; color: var(--text-primary);">
                      ${q.question_text}
                    </p>

                    ${q.question_type === 'multiple_choice' ? `
                      <div class="flex flex-col gap-2">
                        ${(q.options || []).map(opt => `
                          <label style="display: flex; align-items: center; gap: 10px; padding: 10px 14px; background: white; border: 1px solid var(--border-color); border-radius: var(--radius-md); cursor: pointer;">
                            <input type="radio" name="quiz_q_${q.id}" value="${opt.id}" class="quiz-option-radio" data-correct="${opt.is_correct ? '1' : '0'}">
                            <span style="font-size: 0.875rem;">${opt.option_text}</span>
                          </label>
                        `).join('')}
                      </div>
                    ` : `
                      <div>
                        <input type="text" class="form-control quiz-fill-input" data-qid="${q.id}" placeholder="Nhập đáp án của bạn vào đây..." style="background: white;">
                      </div>
                    `}
                    <div class="quiz-feedback-box" id="feedback_q_${q.id}" style="margin-top: 0.75rem; display: none; font-size: 0.8125rem;"></div>
                  </div>
                `).join('')}

                <div style="text-align: right;">
                  <button type="button" class="btn btn-warning" id="btn-submit-lesson-quiz" style="font-weight: 700;">
                    Chấm điểm bài trắc nghiệm
                  </button>
                </div>
              </div>
            ` : `
              <div style="padding: 1.5rem; background: var(--neutral-50); border-radius: var(--radius-lg); text-align: center; color: var(--text-muted);">
                Bài học này không có câu hỏi trắc nghiệm tự động.
              </div>
            `}
          </div>
        `;

      default:
        return `
          <div class="lesson-reading-section">
            <h3 style="font-size: 1.125rem; font-weight: 700; margin-bottom: 1rem; color: var(--text-primary);">
              Nội dung bài học: ${lesson.title}
            </h3>
            <p style="font-size: 0.9375rem; line-height: 1.8; color: var(--text-primary);">
              Tiếng Anh là chìa khóa mở ra cánh cửa giao lưu văn hóa và phát triển sự nghiệp toàn cầu. Hãy duy trì thói quen đọc và luyện tập hàng ngày.
            </p>
          </div>
        `;
    }
  }
}
