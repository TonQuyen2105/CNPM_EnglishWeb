/**
 * FlashcardModel - Quản lý thẻ từ vựng ghi nhớ ngắt quãng (Spaced Repetition)
 */

import { BaseModel } from './BaseModel.js';

class FlashcardModelClass extends BaseModel {
  constructor() {
    super('flashcards');
  }

  async getByUser(userId) {
    return this.getAll(f => String(f.user_id) === String(userId));
  }

  /**
   * Lấy các từ vựng đã đến hạn hoặc quá hạn cần ôn tập hôm nay
   */
  async getDueCards(userId) {
    const now = new Date().toISOString();
    return this.getAll(f => String(f.user_id) === String(userId) && (!f.next_review_at || f.next_review_at <= now));
  }

  /**
   * Đánh dấu ôn tập một từ và tính chu kỳ lặp lại tiếp theo
   */
  async reviewCard(cardId, isRemembered) {
    const card = await this.getById(cardId);
    if (!card) return null;

    let interval = Number(card.interval_days) || 1;
    if (isRemembered) {
      interval = Math.min(interval * 2, 30); // Tăng gấp đôi chu kỳ
    } else {
      interval = 1; // Quên thì reset về 1 ngày
    }

    const nextDate = new Date();
    nextDate.setDate(nextDate.getDate() + interval);

    return this.update(cardId, {
      interval_days: interval,
      next_review_at: nextDate.toISOString()
    });
  }

  /**
   * Thuật toán lặp ngắt quãng SRS 4 mức độ: Quên / Khó / Được / Dễ
   * @param {number|string} cardId 
   * @param {'again'|'hard'|'good'|'easy'} rating 
   */
  async reviewWithRating(cardId, rating = 'good') {
    const card = await this.getById(cardId);
    if (!card) return null;

    let currentInterval = Number(card.interval_days) || 1;
    let newInterval = 1;

    switch (rating) {
      case 'again': // Quên -> Ôn lại ngay sau 1 ngày
        newInterval = 1;
        break;
      case 'hard': // Khó -> Tăng nhẹ 2 ngày
        newInterval = Math.max(2, Math.round(currentInterval * 1.2));
        break;
      case 'good': // Được -> Tăng gấp đôi
        newInterval = Math.max(4, Math.round(currentInterval * 2));
        break;
      case 'easy': // Dễ -> Tăng gấp 2.5 lần
        newInterval = Math.max(7, Math.round(currentInterval * 2.5));
        break;
      default:
        newInterval = 2;
    }

    newInterval = Math.min(newInterval, 60); // Giới hạn tối đa 60 ngày

    const nextDate = new Date();
    nextDate.setDate(nextDate.getDate() + newInterval);

    return this.update(cardId, {
      interval_days: newInterval,
      next_review_at: nextDate.toISOString(),
      updated_at: new Date().toISOString()
    });
  }
}

export const FlashcardModel = new FlashcardModelClass();
