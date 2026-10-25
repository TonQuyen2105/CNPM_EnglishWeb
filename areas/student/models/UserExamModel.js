/**
 * UserExamModel - Quản lý kết quả làm bài thi của học sinh & chấm bài
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class UserExamModelClass extends BaseModel {
  constructor() {
    super('user_exams');
  }

  async getByUser(userId) {
    const attempts = await this.getAll(ue => String(ue.user_id) === String(userId));
    const exams = storage.getTable('exams') || [];

    return attempts.map(att => {
      const exam = exams.find(e => e.id === att.exam_id) || {};
      return {
        ...att,
        exam_title: exam.title || '',
        pass_score: exam.pass_score || 50,
        duration_minutes: exam.duration_minutes || 0
      };
    });
  }

  /**
   * Lấy danh sách các câu trả lời đang chờ giáo viên chấm (pending_review)
   * Có lọc theo teacherId (đề thi của giáo viên hoặc học viên trong lớp của giáo viên)
   */
  async getPendingReviewSubmissions(teacherId = null) {
    const answers = (storage.getTable('user_exam_answers') || []).filter(a => a.grading_status === 'pending_review');
    const userExams = storage.getTable('user_exams') || [];
    const exams = storage.getTable('exams') || [];
    const users = storage.getTable('users') || [];
    const questions = storage.getTable('questions') || [];
    const examQuestions = storage.getTable('exam_questions') || [];
    const classTeachers = storage.getTable('class_teachers') || [];
    const enrollments = storage.getTable('enrollments') || [];

    // Lấy danh sách student_id thuộc các lớp do giáo viên phụ trách
    let myStudentIds = [];
    let myExamIds = [];
    if (teacherId) {
      const myClassIds = classTeachers
        .filter(ct => String(ct.teacher_id) === String(teacherId))
        .map(ct => ct.class_id);
      
      myStudentIds = enrollments
        .filter(e => myClassIds.includes(e.class_id))
        .map(e => e.user_id);

      myExamIds = exams
        .filter(e => String(e.created_by_teacher_id) === String(teacherId))
        .map(e => e.id);
    }

    const detailedAnswers = answers.map(ans => {
      const ue = userExams.find(item => item.id === ans.user_exam_id) || {};
      const exam = exams.find(item => item.id === ue.exam_id) || {};
      const student = users.find(u => u.id === ue.user_id) || {};
      const q = questions.find(item => item.id === ans.question_id) || {};
      const eq = examQuestions.find(item => item.exam_id === ue.exam_id && item.question_id === ans.question_id);

      return {
        ...ans,
        exam_id: ue.exam_id,
        exam_title: exam.title || 'Bài thi học phần',
        student_id: ue.user_id,
        student_name: student.full_name || 'Học sinh',
        student_email: student.email || '',
        student_avatar: student.avatar_url || '',
        question_text: q.question_text || '',
        question_type: q.question_type || 'essay',
        audio_url: q.audio_url || '',
        max_points: eq ? eq.points : (q.default_points || 20),
        submitted_at: ue.submitted_at || ue.started_at || new Date().toISOString()
      };
    });

    if (!teacherId) return detailedAnswers;

    // Lọc theo đề của mình hoặc học sinh trong lớp mình
    const filtered = detailedAnswers.filter(a => 
      myExamIds.includes(a.exam_id) || myStudentIds.includes(a.student_id)
    );

    // Nếu giáo viên có dữ liệu, trả về danh sách lọc; nếu chưa có, cho phép chấm bài mẫu hệ thống
    return filtered.length > 0 ? filtered : detailedAnswers;
  }

  /**
   * Chấm điểm và nhận xét câu trả lời tự luận
   * Tự động tính lại tổng điểm và cập nhật passed/failed cho bài thi
   */
  async gradeSubmission(answerId, { score_earned, feedback = '', teacher_id = null }) {
    const answers = storage.getTable('user_exam_answers') || [];
    const index = answers.findIndex(a => a.id === Number(answerId));
    if (index === -1) return null;

    answers[index] = {
      ...answers[index],
      score_earned: Number(score_earned),
      feedback: feedback.trim(),
      grading_status: 'graded',
      graded_by_teacher_id: teacher_id ? Number(teacher_id) : null,
      graded_at: new Date().toISOString()
    };
    storage.setTable('user_exam_answers', answers);

    // Kiểm tra xem lượt thi user_exam này đã chấm xong hết các câu chưa
    const userExamId = answers[index].user_exam_id;
    const allAnswersForExam = answers.filter(a => a.user_exam_id === userExamId);
    const allGraded = allAnswersForExam.every(a => a.grading_status === 'graded' || a.grading_status === 'auto_graded');

    let updatedUserExam = null;
    if (allGraded) {
      const userExams = storage.getTable('user_exams') || [];
      const ueIndex = userExams.findIndex(ue => ue.id === userExamId);
      if (ueIndex !== -1) {
        const totalScore = allAnswersForExam.reduce((sum, a) => sum + (Number(a.score_earned) || 0), 0);
        const exams = storage.getTable('exams') || [];
        const exam = exams.find(e => e.id === userExams[ueIndex].exam_id) || {};
        const passScore = exam.pass_score || 50;

        userExams[ueIndex] = {
          ...userExams[ueIndex],
          total_score: totalScore,
          status: totalScore >= passScore ? 'passed' : 'failed'
        };
        storage.setTable('user_exams', userExams);
        updatedUserExam = userExams[ueIndex];
      }
    }

    return {
      answer: answers[index],
      userExam: updatedUserExam
    };
  }

  /**
   * Lấy danh sách kết quả thi (user_exams) của các đề hoặc lớp do giáo viên phụ trách
   */
  async getResultsByTeacher(teacherId = null, filter = {}) {
    const userExams = await this.getAll();
    const exams = storage.getTable('exams') || [];
    const users = storage.getTable('users') || [];
    const enrollments = storage.getTable('enrollments') || [];
    const classes = storage.getTable('course_classes') || [];
    const classTeachers = storage.getTable('class_teachers') || [];

    // Danh sách lớp của giáo viên
    let myClassIds = [];
    let myExamIds = [];
    if (teacherId) {
      myClassIds = classTeachers
        .filter(ct => String(ct.teacher_id) === String(teacherId))
        .map(ct => ct.class_id);

      myExamIds = exams
        .filter(e => String(e.created_by_teacher_id) === String(teacherId))
        .map(e => e.id);
    }

    const detailed = userExams.map(ue => {
      const exam = exams.find(e => e.id === ue.exam_id) || {};
      const student = users.find(u => u.id === ue.user_id) || {};
      
      // Lớp của học sinh
      const enroll = enrollments.find(e => e.user_id === ue.user_id && e.status !== 'cancelled');
      const cls = enroll ? classes.find(c => c.id === enroll.class_id) : null;

      return {
        ...ue,
        exam_title: exam.title || 'Đề thi kiểm tra',
        pass_score: exam.pass_score || 50,
        student_name: student.full_name || 'Học viên',
        student_email: student.email || '',
        student_avatar: student.avatar_url || '',
        class_id: cls ? cls.id : null,
        class_name: cls ? cls.class_name : 'Học viên tự do',
        class_code: cls ? cls.class_code : 'TD'
      };
    });

    let results = detailed;
    if (teacherId) {
      results = results.filter(r => 
        myExamIds.includes(r.exam_id) || (r.class_id && myClassIds.includes(r.class_id))
      );
      if (results.length === 0) results = detailed; // fallback nếu chưa có bài nộp
    }

    if (filter.examId) {
      results = results.filter(r => String(r.exam_id) === String(filter.examId));
    }
    if (filter.classId) {
      results = results.filter(r => String(r.class_id) === String(filter.classId));
    }
    if (filter.status && filter.status !== 'all') {
      results = results.filter(r => r.status === filter.status);
    }

    return results;
  }

  /**
   * Học sinh nộp bài thi - Tự động chấm trắc nghiệm & điền từ, câu tự luận đưa vào hàng đợi
   */
  async submitExamAttempt({ userId, examId, answers = [], startedAt = null }) {
    const exams = storage.getTable('exams') || [];
    const exam = exams.find(e => String(e.id) === String(examId));
    if (!exam) return { success: false, message: 'Đề thi không tồn tại.' };

    const questions = storage.getTable('questions') || [];
    const options = storage.getTable('question_options') || [];
    const qAnswers = storage.getTable('question_answers') || [];
    const examQuestions = storage.getTable('exam_questions') || [];
    const userExams = storage.getTable('user_exams') || [];
    const userExamAnswers = storage.getTable('user_exam_answers') || [];

    // Tính attempt_number
    const prevAttempts = userExams.filter(ue => String(ue.user_id) === String(userId) && String(ue.exam_id) === String(examId));
    const attemptNumber = prevAttempts.length + 1;

    // Tạo record user_exams
    let nextUeId = userExams.reduce((max, u) => Math.max(max, u.id || 0), 0) + 1;
    const now = new Date().toISOString();

    const newUe = {
      id: nextUeId,
      user_id: Number(userId),
      exam_id: Number(examId),
      attempt_number: attemptNumber,
      started_at: startedAt || now,
      submitted_at: now,
      total_score: null,
      status: 'submitted'
    };

    // Chấm từng câu hỏi
    let nextAnsId = userExamAnswers.reduce((max, a) => Math.max(max, a.id || 0), 0) + 1;
    let autoScoreSum = 0;
    let hasPendingReview = false;
    const processedAnswers = [];

    answers.forEach(userAns => {
      const q = questions.find(item => item.id === Number(userAns.question_id)) || {};
      const eq = examQuestions.find(item => item.exam_id === Number(examId) && item.question_id === q.id);
      const points = eq ? (Number(eq.points) || 10) : (Number(q.default_points) || 10);

      let scoreEarned = 0;
      let gradingStatus = 'auto_graded';
      let feedback = '';

      if (q.question_type === 'multiple_choice') {
        const selectedOpt = options.find(o => o.id === Number(userAns.selected_option_id));
        if (selectedOpt && (selectedOpt.is_correct === 1 || selectedOpt.is_correct === true)) {
          scoreEarned = points;
          feedback = 'Chính xác!';
        } else {
          scoreEarned = 0;
          feedback = 'Chưa chính xác.';
        }
        autoScoreSum += scoreEarned;
      } else if (q.question_type === 'fill_blank') {
        const correctAnswers = qAnswers.filter(a => a.question_id === q.id);
        const userText = (userAns.text_answer || '').trim();
        const isMatch = correctAnswers.some(ans => {
          if (ans.is_case_sensitive) {
            return ans.answer_text.trim() === userText;
          }
          return ans.answer_text.trim().toLowerCase() === userText.toLowerCase();
        });

        if (isMatch) {
          scoreEarned = points;
          feedback = 'Chính xác!';
        } else {
          scoreEarned = 0;
          feedback = 'Chưa chính xác.';
        }
        autoScoreSum += scoreEarned;
      } else {
        // Essay hoặc Pronunciation -> Chờ giáo viên chấm
        scoreEarned = null;
        gradingStatus = 'pending_review';
        hasPendingReview = true;
      }

      const answerRecord = {
        id: nextAnsId++,
        user_exam_id: newUe.id,
        question_id: q.id,
        selected_option_id: userAns.selected_option_id ? Number(userAns.selected_option_id) : null,
        text_answer: userAns.text_answer || '',
        score_earned: scoreEarned,
        grading_status: gradingStatus,
        graded_by_teacher_id: null,
        graded_at: gradingStatus === 'auto_graded' ? now : null,
        feedback: feedback || null
      };

      processedAnswers.push(answerRecord);
      userExamAnswers.push(answerRecord);
    });

    // Cập nhật điểm và status tổng
    if (!hasPendingReview) {
      newUe.total_score = autoScoreSum;
      const passScore = Number(exam.pass_score) || 50;
      newUe.status = autoScoreSum >= passScore ? 'passed' : 'failed';
    }

    userExams.push(newUe);
    storage.setTable('user_exams', userExams);
    storage.setTable('user_exam_answers', userExamAnswers);

    return {
      success: true,
      userExam: newUe,
      answers: processedAnswers,
      hasPendingReview
    };
  }

  /**
   * Lấy chi tiết một bài thi đã nộp kèm toàn bộ đáp án của học sinh
   */
  async getAttemptDetails(userExamId, userId = null) {
    const userExams = storage.getTable('user_exams') || [];
    const ue = userExams.find(item => String(item.id) === String(userExamId));
    if (!ue) return null;
    if (userId && String(ue.user_id) !== String(userId)) return null;

    const exams = storage.getTable('exams') || [];
    const exam = exams.find(e => e.id === ue.exam_id) || {};
    const courses = storage.getTable('courses') || [];
    const course = courses.find(c => c.id === exam.course_id);

    const questions = storage.getTable('questions') || [];
    const options = storage.getTable('question_options') || [];
    const examQuestions = storage.getTable('exam_questions') || [];
    const allAnswers = storage.getTable('user_exam_answers') || [];
    const myAnswers = allAnswers.filter(a => a.user_exam_id === ue.id);

    const detailedAnswers = myAnswers.map(ans => {
      const q = questions.find(item => item.id === ans.question_id) || {};
      const eq = examQuestions.find(item => item.exam_id === ue.exam_id && item.question_id === ans.question_id);
      const qOpts = options.filter(o => o.question_id === q.id);
      const selectedOpt = qOpts.find(o => o.id === ans.selected_option_id);
      const correctOpt = qOpts.find(o => o.is_correct === 1 || o.is_correct === true);

      return {
        ...ans,
        question_text: q.question_text || '',
        question_type: q.question_type || 'multiple_choice',
        max_points: eq ? (Number(eq.points) || 10) : (Number(q.default_points) || 10),
        options: qOpts,
        selected_option_text: selectedOpt ? selectedOpt.option_text : '',
        correct_option_text: correctOpt ? correctOpt.option_text : ''
      };
    });

    return {
      ...ue,
      exam_title: exam.title || 'Bài kiểm tra',
      exam_description: exam.description || '',
      duration_minutes: exam.duration_minutes || 45,
      pass_score: exam.pass_score || 50,
      course_title: course ? course.title : 'Khóa học',
      answers: detailedAnswers
    };
  }
}

export const UserExamModel = new UserExamModelClass();

