/**
 * QuestionModel - Quản lý ngân hàng câu hỏi (trắc nghiệm, điền từ, phát âm, tự luận)
 */

import { BaseModel } from './BaseModel.js';
import { storage } from '../core/storage.js';

class QuestionModelClass extends BaseModel {
  constructor() {
    super('questions');
  }

  /**
   * Lấy câu hỏi kèm theo options (nếu có) và answers (nếu có)
   */
  async getWithDetails(id) {
    const q = await this.getById(id);
    if (!q) return null;

    const options = storage.getTable('question_options') || [];
    const answers = storage.getTable('question_answers') || [];

    return {
      ...q,
      options: options.filter(opt => opt.question_id === q.id),
      answers: answers.filter(ans => ans.question_id === q.id)
    };
  }

  async getByType(type) {
    return this.getAll(q => q.question_type === type);
  }

  /**
   * Tạo câu hỏi mới kèm options / answers tương ứng theo loại câu
   */
  async createWithDetails({ lesson_id, question_text, question_type, default_points, audio_url = '', options = [], answers = [] }) {
    const newQuestion = await this.create({
      lesson_id: lesson_id ? Number(lesson_id) : 1,
      question_text: question_text.trim(),
      question_type,
      default_points: Number(default_points) || 10,
      audio_url: audio_url || ''
    });

    if (question_type === 'multiple_choice' && options.length > 0) {
      const allOpts = storage.getTable('question_options') || [];
      let maxOptId = allOpts.reduce((max, o) => Math.max(max, o.id || 0), 0);
      options.forEach(opt => {
        allOpts.push({
          id: ++maxOptId,
          question_id: newQuestion.id,
          option_text: opt.option_text || '',
          is_correct: opt.is_correct ? 1 : 0
        });
      });
      storage.setTable('question_options', allOpts);
    }

    if (question_type === 'fill_blank' && answers.length > 0) {
      const allAns = storage.getTable('question_answers') || [];
      let maxAnsId = allAns.reduce((max, a) => Math.max(max, a.id || 0), 0);
      answers.forEach(ans => {
        allAns.push({
          id: ++maxAnsId,
          question_id: newQuestion.id,
          answer_text: ans.answer_text || '',
          reference_audio_url: ans.reference_audio_url || '',
          is_case_sensitive: ans.is_case_sensitive ? 1 : 0
        });
      });
      storage.setTable('question_answers', allAns);
    }

    return this.getWithDetails(newQuestion.id);
  }

  /**
   * Cập nhật câu hỏi kèm options / answers
   */
  async updateWithDetails(id, { lesson_id, question_text, question_type, default_points, audio_url = '', options = [], answers = [] }) {
    const qId = Number(id);
    await this.update(qId, {
      lesson_id: lesson_id ? Number(lesson_id) : 1,
      question_text: question_text.trim(),
      question_type,
      default_points: Number(default_points) || 10,
      audio_url: audio_url || ''
    });

    // Cập nhật options nếu có
    if (question_type === 'multiple_choice') {
      let allOpts = (storage.getTable('question_options') || []).filter(o => o.question_id !== qId);
      let maxOptId = allOpts.reduce((max, o) => Math.max(max, o.id || 0), 0);
      options.forEach(opt => {
        allOpts.push({
          id: ++maxOptId,
          question_id: qId,
          option_text: opt.option_text || '',
          is_correct: opt.is_correct ? 1 : 0
        });
      });
      storage.setTable('question_options', allOpts);
    }

    // Cập nhật answers nếu có
    if (question_type === 'fill_blank') {
      let allAns = (storage.getTable('question_answers') || []).filter(a => a.question_id !== qId);
      let maxAnsId = allAns.reduce((max, a) => Math.max(max, a.id || 0), 0);
      answers.forEach(ans => {
        allAns.push({
          id: ++maxAnsId,
          question_id: qId,
          answer_text: ans.answer_text || '',
          reference_audio_url: ans.reference_audio_url || '',
          is_case_sensitive: ans.is_case_sensitive ? 1 : 0
        });
      });
      storage.setTable('question_answers', allAns);
    }

    return this.getWithDetails(qId);
  }

  /**
   * Xoá câu hỏi và các liên kết options / answers
   */
  async deleteWithDetails(id) {
    const qId = Number(id);
    let allOpts = (storage.getTable('question_options') || []).filter(o => o.question_id !== qId);
    storage.setTable('question_options', allOpts);

    let allAns = (storage.getTable('question_answers') || []).filter(a => a.question_id !== qId);
    storage.setTable('question_answers', allAns);

    let allExamQuestions = (storage.getTable('exam_questions') || []).filter(eq => eq.question_id !== qId);
    storage.setTable('exam_questions', allExamQuestions);

    return this.delete(qId);
  }
}

export const QuestionModel = new QuestionModelClass();
