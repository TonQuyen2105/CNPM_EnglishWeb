/**
 * StudentLessonPlayerController - Điều khiển Trình phát bài học (Lesson Player)
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  LessonModel,
  CourseModel,
  QuestionModel,
  UserProgressModel
} from '../../models/index.js';
import { StudentLessonPlayerView } from '../../views/student/StudentLessonPlayerView.js';
import { toast } from '../../ui/toast.js';

export class StudentLessonPlayerController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    const params = (arg1 && !arg1.nodeType) ? arg1 : (arg2 || {});

    const courseId = params.courseId;
    const lessonId = params.lessonId;

    if (!courseId || !lessonId) {
      container.innerHTML = `<div class="card" style="padding: 2rem; text-align: center;">Không tìm thấy bài học yêu cầu.</div>`;
      return;
    }

    const currentUser = auth.getCurrentUser();
    const course = await CourseModel.getById(Number(courseId));
    const lesson = await LessonModel.getById(Number(lessonId));

    if (!course || !lesson) {
      container.innerHTML = `<div class="card" style="padding: 2rem; text-align: center;">Bài học không tồn tại.</div>`;
      return;
    }

    const allLessons = storage.getTable('lessons') || [];
    const courseLessons = allLessons
      .filter(l => l.course_id === Number(courseId))
      .sort((a, b) => a.order_index - b.order_index);

    const userProgress = await UserProgressModel.getByUser(currentUser.id);
    const currentProgress = userProgress.find(p => p.lesson_id === Number(lessonId));
    const isCompleted = currentProgress ? (currentProgress.is_completed === 1 || currentProgress.is_completed === true) : false;

    // Lấy câu hỏi quiz nếu có
    let quizQuestions = [];
    if (lesson.content_type === 'quiz') {
      quizQuestions = await QuestionModel.getByLesson(Number(lessonId));
    }

    container.innerHTML = StudentLessonPlayerView.render({
      course,
      lesson,
      courseLessons,
      userProgress,
      quizQuestions,
      isCompleted
    });

    this._bindEvents(container, currentUser, lesson, course, quizQuestions);
  }

  static _bindEvents(container, currentUser, lesson, course, quizQuestions) {
    // 1. Nút phát âm Web Speech API
    container.querySelectorAll('.btn-speak').forEach(btn => {
      btn.addEventListener('click', (e) => {
        const word = e.currentTarget.getAttribute('data-word');
        if (word && 'speechSynthesis' in window) {
          const utterance = new SpeechSynthesisUtterance(word);
          utterance.lang = 'en-US';
          window.speechSynthesis.speak(utterance);
        }
      });
    });

    // 2. Nút Hoàn thành bài học
    const handleComplete = async () => {
      await UserProgressModel.markCompleted(currentUser.id, lesson.id, 100);
      toast.success(`🎉 Chúc mừng! Bạn đã hoàn thành bài học: ${lesson.title}`);
      this.render({ courseId: course.id, lessonId: lesson.id }, {}, container);
    };

    const topBtn = container.querySelector('#btn-complete-lesson');
    const bottomBtn = container.querySelector('#btn-complete-lesson-bottom');
    if (topBtn) topBtn.addEventListener('click', handleComplete);
    if (bottomBtn) bottomBtn.addEventListener('click', handleComplete);

    // 3. Audio Play/Pause simulation
    const playBtn = container.querySelector('#btn-audio-play');
    if (playBtn) {
      let isPlaying = false;
      playBtn.addEventListener('click', () => {
        isPlaying = !isPlaying;
        playBtn.innerHTML = isPlaying 
          ? `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="6" y="4" width="4" height="16"></rect><rect x="14" y="4" width="4" height="16"></rect></svg>`
          : `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5 3 19 12 5 21 5 3"></polygon></svg>`;
        toast.info(isPlaying ? 'Đang phát đoạn audio mẫu...' : 'Đã tạm dừng.');
      });
    }

    // 4. Chấm bài trắc nghiệm quiz
    const quizBtn = container.querySelector('#btn-submit-lesson-quiz');
    if (quizBtn && quizQuestions.length > 0) {
      quizBtn.addEventListener('click', async () => {
        let correctCount = 0;

        quizQuestions.forEach(q => {
          const fb = container.querySelector(`#feedback_q_${q.id}`);
          if (!fb) return;

          if (q.question_type === 'multiple_choice') {
            const selected = container.querySelector(`input[name="quiz_q_${q.id}"]:checked`);
            if (selected) {
              const isCorrect = selected.getAttribute('data-correct') === '1';
              if (isCorrect) {
                correctCount++;
                fb.style.display = 'block';
                fb.style.color = '#16A34A';
                fb.innerHTML = '✓ Chính xác!';
              } else {
                fb.style.display = 'block';
                fb.style.color = '#DC2626';
                fb.innerHTML = '✗ Chưa chính xác. Hãy chọn lại!';
              }
            } else {
              fb.style.display = 'block';
              fb.style.color = '#DC2626';
              fb.innerHTML = 'Vui lòng chọn 1 đáp án.';
            }
          } else {
            // fill_blank
            const input = container.querySelector(`.quiz-fill-input[data-qid="${q.id}"]`);
            if (input && input.value.trim()) {
              correctCount++;
              fb.style.display = 'block';
              fb.style.color = '#16A34A';
              fb.innerHTML = '✓ Đã ghi nhận câu trả lời!';
            }
          }
        });

        const score = Math.round((correctCount / quizQuestions.length) * 100);
        await UserProgressModel.markCompleted(currentUser.id, lesson.id, score);
        toast.success(`Bạn đã đạt ${score}/100 điểm cho bài kiểm tra này!`);
      });
    }
  }
}
