/**
 * StudentFlashcardsController - Điều khiển màn hình Flashcard và Ôn tập SRS
 */

import { auth } from '../../core/auth.js';
import { FlashcardModel } from '../../models/index.js';
import { StudentFlashcardsView } from '../../views/student/StudentFlashcardsView.js';
import { modal } from '../../ui/modal.js';
import { toast } from '../../ui/toast.js';
import { confirmDialog } from '../../ui/confirmDialog.js';

export class StudentFlashcardsController {
  static _state = {
    isReviewing: false,
    reviewCards: [],
    reviewIndex: 0,
    searchKeyword: ''
  };

  static async render(arg1, arg2, arg3) {
    const container = (arg1 && arg1.nodeType) ? arg1 : (arg3 || document.getElementById('app-main-content'));
    if (!container) return;

    if (this._state.isReviewing) {
      return this._renderReview(container);
    }

    return this._renderDeck(container);
  }

  static async _renderDeck(container) {
    const currentUser = auth.getCurrentUser();
    const allCards = await FlashcardModel.getByUser(currentUser.id);
    const dueCards = await FlashcardModel.getDueCards(currentUser.id);

    let displayCards = allCards;
    if (this._state.searchKeyword) {
      const kw = this._state.searchKeyword.toLowerCase().trim();
      displayCards = allCards.filter(c => 
        (c.word && c.word.toLowerCase().includes(kw)) ||
        (c.meaning && c.meaning.toLowerCase().includes(kw))
      );
    }

    container.innerHTML = StudentFlashcardsView.renderDeckView({
      flashcards: displayCards,
      dueCount: dueCards.length,
      searchKeyword: this._state.searchKeyword
    });

    this._bindDeckEvents(container, currentUser, dueCards, allCards);
  }

  static _bindDeckEvents(container, currentUser, dueCards, allCards) {
    // 1. Tìm kiếm thẻ
    const searchInput = container.querySelector('#search-flashcard-input');
    if (searchInput) {
      searchInput.addEventListener('input', (e) => {
        this._state.searchKeyword = e.target.value;
        this._renderDeck(container);
      });
    }

    // 2. Phát âm từ
    container.querySelectorAll('.btn-speak').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const word = e.currentTarget.getAttribute('data-word');
        if (word && 'speechSynthesis' in window) {
          const u = new SpeechSynthesisUtterance(word);
          u.lang = 'en-US';
          window.speechSynthesis.speak(u);
        }
      });
    });

    // 3. Bắt đầu phiên ôn tập
    const startReviewBtn = container.querySelector('#btn-start-review');
    if (startReviewBtn) {
      startReviewBtn.addEventListener('click', () => {
        const cardsToReview = dueCards.length > 0 ? dueCards : allCards;
        if (cardsToReview.length === 0) {
          toast.info('Bạn chưa có thẻ nào để ôn tập. Hãy thêm thẻ mới!');
          return;
        }
        this._state.isReviewing = true;
        this._state.reviewCards = [...cardsToReview];
        this._state.reviewIndex = 0;
        this._renderReview(container);
      });
    }

    // 4. Thêm thẻ mới
    const openAddModal = () => this._showCardModal(container, null);
    const addBtn = container.querySelector('#btn-open-create-modal');
    const emptyAddBtn = container.querySelector('#btn-empty-add-card');
    if (addBtn) addBtn.addEventListener('click', openAddModal);
    if (emptyAddBtn) emptyAddBtn.addEventListener('click', openAddModal);

    // 5. Sửa thẻ
    container.querySelectorAll('.btn-edit-card').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        const cardId = e.currentTarget.getAttribute('data-card-id');
        const card = await FlashcardModel.getById(cardId);
        if (card) this._showCardModal(container, card);
      });
    });

    // 6. Xóa thẻ
    container.querySelectorAll('.btn-delete-card').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        const cardId = e.currentTarget.getAttribute('data-card-id');
        const confirmed = await confirmDialog({
          title: 'Xóa thẻ từ vựng',
          message: 'Bạn có chắc chắn muốn xóa thẻ này khỏi bộ từ vựng cá nhân?',
          confirmText: 'Xóa thẻ',
          cancelText: 'Hủy',
          type: 'danger'
        });
        if (confirmed) {
          await FlashcardModel.delete(cardId);
          toast.success('Đã xóa thẻ từ vựng.');
          this._renderDeck(container);
        }
      });
    });
  }

  static _showCardModal(container, card = null) {
    const isEdit = !!card;
    const currentUser = auth.getCurrentUser();

    modal.open({
      title: isEdit ? 'Chỉnh sửa thẻ từ vựng' : 'Thêm thẻ từ vựng mới',
      content: `
        <form id="form-flashcard-save">
          <div class="form-group" style="margin-bottom: 1rem;">
            <label class="form-label">Từ vựng tiếng Anh (*)</label>
            <input type="text" id="fc-word" class="form-control" required placeholder="Ví dụ: Persistent" value="${card ? card.word : ''}">
          </div>

          <div class="form-group" style="margin-bottom: 1rem;">
            <label class="form-label">Phiên âm (Phonetic)</label>
            <input type="text" id="fc-phonetic" class="form-control" placeholder="Ví dụ: /pəˈsɪs.tənt/" value="${card ? card.phonetic || '' : ''}">
          </div>

          <div class="form-group" style="margin-bottom: 1rem;">
            <label class="form-label">Nghĩa tiếng Việt (*)</label>
            <input type="text" id="fc-meaning" class="form-control" required placeholder="Ví dụ: Kiên trì, bền bỉ" value="${card ? card.meaning : ''}">
          </div>

          <div class="form-group" style="margin-bottom: 1.5rem;">
            <label class="form-label">Câu ví dụ minh họa</label>
            <textarea id="fc-example" class="form-control" rows="2" placeholder="Ví dụ: Success requires persistent effort.">${card ? card.example_sentence || '' : ''}</textarea>
          </div>

          <div style="display: flex; justify-content: flex-end; gap: 8px;">
            <button type="button" class="btn btn-secondary" id="btn-modal-cancel">Hủy</button>
            <button type="submit" class="btn btn-primary">${isEdit ? 'Lưu thay đổi' : 'Tạo thẻ mới'}</button>
          </div>
        </form>
      `
    });

    const modalCancel = document.getElementById('btn-modal-cancel');
    if (modalCancel) modalCancel.addEventListener('click', () => modal.close());

    const form = document.getElementById('form-flashcard-save');
    if (form) {
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const word = document.getElementById('fc-word').value.trim();
        const phonetic = document.getElementById('fc-phonetic').value.trim();
        const meaning = document.getElementById('fc-meaning').value.trim();
        const example_sentence = document.getElementById('fc-example').value.trim();

        if (isEdit) {
          await FlashcardModel.update(card.id, {
            word,
            phonetic,
            meaning,
            example_sentence,
            updated_at: new Date().toISOString()
          });
          toast.success('Đã cập nhật thẻ từ vựng!');
        } else {
          await FlashcardModel.create({
            user_id: Number(currentUser.id),
            word,
            phonetic,
            meaning,
            example_sentence,
            interval_days: 1,
            next_review_at: new Date().toISOString()
          });
          toast.success('Đã tạo thẻ từ vựng mới!');
        }

        modal.close();
        this._renderDeck(container);
      });
    }
  }

  static _renderReview(container) {
    const card = this._state.reviewCards[this._state.reviewIndex];
    container.innerHTML = StudentFlashcardsView.renderReviewMode({
      card,
      currentIndex: this._state.reviewIndex,
      totalCards: this._state.reviewCards.length
    });

    if (!card) {
      this._state.isReviewing = false;
      return;
    }

    // 1. Lật thẻ 3D
    const cardEl = container.querySelector('#active-flashcard-card');
    if (cardEl) {
      cardEl.addEventListener('click', (e) => {
        if (e.target.closest('.btn-speak')) return;
        cardEl.classList.toggle('is-flipped');
      });
    }

    // 2. Phát âm
    container.querySelectorAll('.btn-speak').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const word = e.currentTarget.getAttribute('data-word');
        if (word && 'speechSynthesis' in window) {
          const u = new SpeechSynthesisUtterance(word);
          u.lang = 'en-US';
          window.speechSynthesis.speak(u);
        }
      });
    });

    // 3. Thoát ôn tập
    const exitBtn = container.querySelector('#btn-exit-review');
    if (exitBtn) {
      exitBtn.addEventListener('click', () => {
        this._state.isReviewing = false;
        this._renderDeck(container);
      });
    }

    // 4. Đánh giá SRS 4 nút
    container.querySelectorAll('.btn-rate-card').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        const rating = e.currentTarget.getAttribute('data-rating');
        await FlashcardModel.reviewWithRating(card.id, rating);

        this._state.reviewIndex++;
        this._renderReview(container);
      });
    });
  }
}
