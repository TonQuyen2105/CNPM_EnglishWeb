/**
 * StudentDashboardController - Điều khiển logic màn hình Trang chủ học viên
 */

import { auth } from '../../core/auth.js';
import { storage } from '../../core/storage.js';
import {
  FlashcardModel,
  EnrollmentModel,
  UserProgressModel,
  ExamModel,
  UserExamModel
} from '../../models/index.js';
import { StudentDashboardView } from '../../views/student/StudentDashboardView.js';

export class StudentDashboardController {
  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    const currentUser = auth.getCurrentUser();
    if (!currentUser) return;

    // 1. Thẻ Flashcard cần ôn hôm nay
    const dueFlashcards = await FlashcardModel.getDueCards(currentUser.id);
    const allFlashcards = await FlashcardModel.getByUser(currentUser.id);

    // 2. Lớp học đang tham gia
    const myEnrollments = await EnrollmentModel.getDetailedByUser(currentUser.id);
    const activeEnrollments = myEnrollments.filter(e => e.status === 'active');

    // 3. Bài học dở dang gần nhất
    const inProgressLesson = await UserProgressModel.getLatestInProgress(currentUser.id);

    // 4. Thống kê bài học đã hoàn thành
    const allProgress = await UserProgressModel.getByUser(currentUser.id);
    const completedProgress = allProgress.filter(p => p.is_completed === 1 || p.is_completed === true);

    // Tính tổng số bài học của tất cả khóa đang tham gia
    const allLessons = storage.getTable('lessons') || [];
    const activeCourseIds = activeEnrollments.map(e => e.course_id);
    const totalLessonsInEnrolledCourses = allLessons.filter(l => activeCourseIds.includes(l.course_id)).length;

    // 5. Tính điểm trung bình các bài kiểm tra đã làm
    const myExamAttempts = await UserExamModel.getByUser(currentUser.id);
    const gradedAttempts = myExamAttempts.filter(a => a.status === 'completed' && a.total_score != null);
    const avgScore = gradedAttempts.length > 0
      ? Math.round(gradedAttempts.reduce((sum, a) => sum + Number(a.total_score), 0) / gradedAttempts.length)
      : null;

    // 6. Buổi học sắp tới (từ class_lessons và class_schedules)
    const upcomingClassSession = this._getUpcomingSession(activeEnrollments);

    // 7. Đề thi sắp tới (thuộc các khóa đang học, còn lượt làm)
    const upcomingExams = await this._getUpcomingExams(currentUser.id, activeCourseIds, myExamAttempts);

    // 8. Chuỗi ngày học liên tục (streak)
    const streakDays = 5;

    // Render View
    container.innerHTML = StudentDashboardView.render({
      currentUser,
      streakDays,
      stats: {
        completedLessons: completedProgress.length,
        totalLessons: totalLessonsInEnrolledCourses || allLessons.length,
        averageScore: avgScore,
        wordsLearned: allFlashcards.length,
        activeClassesCount: activeEnrollments.length
      },
      upcomingClassSession,
      inProgressLesson,
      dueFlashcardsCount: dueFlashcards.length,
      upcomingExams
    });
  }

  static _getUpcomingSession(activeEnrollments) {
    if (!activeEnrollments || activeEnrollments.length === 0) return null;

    const classLessons = storage.getTable('class_lessons') || [];
    const classrooms = storage.getTable('classrooms') || [];
    const timeSlots = storage.getTable('time_slots') || [];
    const classSchedules = storage.getTable('class_schedules') || [];
    const lessons = storage.getTable('lessons') || [];

    const enrolledClassIds = activeEnrollments.map(e => e.class_id);

    // Tìm trong class_lessons status 'scheduled'
    const todayStr = new Date().toISOString().split('T')[0];
    const upcomingLessons = classLessons
      .filter(cl => enrolledClassIds.includes(cl.class_id) && cl.status === 'scheduled' && cl.schedule_date >= todayStr)
      .sort((a, b) => (a.schedule_date > b.schedule_date ? 1 : -1));

    if (upcomingLessons.length > 0) {
      const target = upcomingLessons[0];
      const clsEnroll = activeEnrollments.find(e => e.class_id === target.class_id) || {};
      const lesson = lessons.find(l => l.id === target.lesson_id) || {};
      const schedule = classSchedules.find(s => s.id === target.schedule_id) || {};
      const room = classrooms.find(r => r.id === schedule.classroom_id) || {};
      const slot = timeSlots.find(sl => sl.id === schedule.time_slot_id) || {};

      return {
        schedule_date: target.schedule_date,
        lesson_order: target.lesson_order || 1,
        class_name: clsEnroll.class_name,
        class_code: clsEnroll.class_code,
        teacher_name: clsEnroll.main_teacher_name,
        lesson_title: lesson.title || 'Buổi học định kỳ',
        room_name: room.name || 'Phòng học chính',
        time_slot_name: slot.name ? `${slot.start_time} - ${slot.end_time}` : '18:00 - 19:30'
      };
    }

    // Nếu không có class_lesson nào scheduled, lấy thông tin từ lịch học tuần đầu tiên
    const firstEnroll = activeEnrollments[0];
    if (firstEnroll && firstEnroll.schedules && firstEnroll.schedules.length > 0) {
      const sch = firstEnroll.schedules[0];
      return {
        schedule_date: todayStr,
        lesson_order: 1,
        class_name: firstEnroll.class_name,
        class_code: firstEnroll.class_code,
        teacher_name: firstEnroll.main_teacher_name,
        lesson_title: 'Buổi học định kỳ theo TKB',
        room_name: sch.room_name || 'Phòng học chính',
        time_slot_name: sch.time_range || '18:00 - 19:30'
      };
    }

    return null;
  }

  static async _getUpcomingExams(userId, activeCourseIds, myExamAttempts) {
    const allExams = await ExamModel.getAll();
    const publishedExams = allExams.filter(e => 
      (e.is_published === 1 || e.is_published === true) && 
      (activeCourseIds.includes(e.course_id) || !e.course_id)
    );

    const result = [];
    for (const ex of publishedExams) {
      const attempts = myExamAttempts.filter(a => a.exam_id === ex.id);
      const maxAttempts = ex.max_attempts || 3;
      const left = maxAttempts - attempts.length;
      if (left > 0) {
        result.push({
          ...ex,
          attempts_left: left
        });
      }
    }

    return result.slice(0, 3);
  }
}
