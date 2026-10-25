/**
 * StudentProgressController - Điều khiển màn hình Tiến độ học tập & Thành tích
 */

import { auth } from '../../core/auth.js';
import {
  UserProgressModel,
  UserExamModel,
  EnrollmentModel,
  FlashcardModel
} from '../../models/index.js';
import { StudentProgressView } from '../../views/student/StudentProgressView.js';

export class StudentProgressController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    const currentUser = auth.getCurrentUser();
    const streakDays = 5;

    // 1. Tiến độ bài học & các khóa đang học
    const myEnrollments = await EnrollmentModel.getDetailedByUser(currentUser.id);
    const userProgress = await UserProgressModel.getByUser(currentUser.id);
    const completedProgress = userProgress.filter(p => p.is_completed === 1 || p.is_completed === true);

    const courseProgressList = myEnrollments.map(e => ({
      title: e.course_title || e.class_name,
      completed_lessons: e.completed_lessons || 0,
      total_lessons: e.total_lessons || 1,
      progress_percent: e.progress_percent || 0
    }));

    // 2. Lịch sử điểm thi theo thời gian
    const myAttempts = await UserExamModel.getByUser(currentUser.id);
    const completedAttempts = myAttempts.filter(a => a.total_score !== null && a.status === 'completed');
    completedAttempts.sort((a, b) => new Date(a.submitted_at || a.created_at) - new Date(b.submitted_at || b.created_at));

    const scoreHistory = completedAttempts.map((a, idx) => ({
      label: `Lần ${idx + 1}`,
      score: Number(a.total_score) || 0
    }));

    const avgScore = completedAttempts.length > 0
      ? Math.round(completedAttempts.reduce((sum, a) => sum + Number(a.total_score), 0) / completedAttempts.length)
      : 80;

    // 3. Thẻ từ vựng flashcards
    const allFlashcards = await FlashcardModel.getByUser(currentUser.id);

    // 4. Đánh giá mở khóa huy hiệu
    const badges = [
      {
        id: 'newbie',
        icon: '🏅',
        title: 'Tân binh chăm chỉ',
        description: 'Hoàn thành bài học đầu tiên trong hệ thống.',
        isUnlocked: completedProgress.length >= 1
      },
      {
        id: 'vocab_master',
        icon: '📚',
        title: 'Vua từ vựng',
        description: 'Xây dựng bộ từ vựng cá nhân từ 5 thẻ trở lên.',
        isUnlocked: allFlashcards.length >= 5
      },
      {
        id: 'grammar_guru',
        icon: '🧠',
        title: 'Bậc thầy ngữ pháp',
        description: 'Hoàn thành 100% nội dung một khóa học.',
        isUnlocked: courseProgressList.some(cp => cp.progress_percent === 100)
      },
      {
        id: 'exam_champion',
        icon: '🏆',
        title: 'Thủ khoa đề thi',
        description: 'Đạt từ 80 điểm trở lên trong một bài kiểm tra.',
        isUnlocked: completedAttempts.some(a => Number(a.total_score) >= 80)
      }
    ];

    container.innerHTML = StudentProgressView.render({
      streakDays,
      courseProgressList,
      scoreHistory: scoreHistory.length > 0 ? scoreHistory : null,
      badges,
      overallStats: {
        completedLessons: completedProgress.length,
        avgScore
      }
    });
  }
}
