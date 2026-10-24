/**
 * ExamModel - Quản lý bài thi, đề kiểm tra và các câu hỏi trong đề
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class ExamModelClass extends BaseModel {
  constructor() {
    super('exams');
  }

  async getAllWithTeacher() {
    const exams = await this.getAll();
    const teachers = storage.getTable('teachers') || [];
    const users = storage.getTable('users') || [];
    const courses = storage.getTable('courses') || [];

    return exams.map(e => {
      const teacher = teachers.find(t => t.id === e.created_by_teacher_id);
      let teacherName = 'Giáo viên';
      if (teacher) {
        const u = users.find(user => user.id === teacher.user_id);
        if (u) teacherName = u.full_name;
      }

      const course = courses.find(c => c.id === e.course_id);

      return {
        ...e,
        teacher_name: teacherName,
        course_title: course ? course.title : 'Đề thi tự do'
      };
    });
  }

  async getWithQuestions(examId) {
    const exam = await this.getById(examId);
    if (!exam) return null;

    const examQuestions = (storage.getTable('exam_questions') || [])
      .filter(eq => eq.exam_id === exam.id)
      .sort((a, b) => a.order_index - b.order_index);

    const questions = storage.getTable('questions') || [];
    const options = storage.getTable('question_options') || [];

    const detailedQuestions = examQuestions.map(eq => {
      const q = questions.find(item => item.id === eq.question_id) || {};
      const opts = options.filter(opt => opt.question_id === q.id);
      return {
        ...q,
        points: eq.points,
        order_index: eq.order_index,
        options: opts
      };
    });

    return {
      ...exam,
      questions: detailedQuestions
    };
  }

  /**
   * Lấy danh sách đề thi do giáo viên tự biên soạn
   * @param {number|string} teacherId 
   */
  async getByTeacher(teacherId) {
    const all = await this.getAll(e => String(e.created_by_teacher_id) === String(teacherId));
    const courses = storage.getTable('courses') || [];
    const examQuestions = storage.getTable('exam_questions') || [];
    const userExams = storage.getTable('user_exams') || [];

    return all.map(exam => {
      const course = courses.find(c => c.id === exam.course_id);
      const myQuestions = examQuestions.filter(eq => eq.exam_id === exam.id);
      const totalPoints = myQuestions.reduce((sum, q) => sum + (Number(q.points) || 0), 0);
      const attempts = userExams.filter(ue => ue.exam_id === exam.id);

      return {
        ...exam,
        course_title: course ? course.title : 'Đề thi chung',
        total_questions: myQuestions.length,
        total_points: totalPoints,
        attempts_count: attempts.length
      };
    });
  }

  /**
   * Lưu danh sách câu hỏi trong đề (Trình dựng đề Exam Builder)
   * @param {number|string} examId 
   * @param {Array<{question_id: number, points: number, order_index: number}>} questions 
   */
  async saveExamQuestions(examId, questions = []) {
    const id = Number(examId);
    let allExamQuestions = storage.getTable('exam_questions') || [];

    // Xoá các câu hỏi cũ của exam này
    allExamQuestions = allExamQuestions.filter(eq => eq.exam_id !== id);

    let nextId = allExamQuestions.reduce((max, item) => Math.max(max, item.id || 0), 0) + 1;

    questions.forEach((q, idx) => {
      allExamQuestions.push({
        id: nextId++,
        exam_id: id,
        question_id: Number(q.question_id),
        points: Number(q.points) || 10,
        order_index: q.order_index !== undefined ? Number(q.order_index) : idx + 1
      });
    });

    storage.setTable('exam_questions', allExamQuestions);
    return this.getWithQuestions(id);
  }
}

export const ExamModel = new ExamModelClass();
