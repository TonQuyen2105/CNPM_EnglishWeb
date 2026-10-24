/**
 * StudentFlashcardsView - Giao diện Flashcard Ôn tập Spaced Repetition (SRS)
 */

import { icon } from '../../ui/icons.js';

export class StudentFlashcardsView {
  /**
   * Render màn hình danh sách bộ thẻ & tổng quan
   */
  static renderDeckView({
    flashcards = [],
    dueCount = 0,
    searchKeyword = ''
  }) {
    return `
      <div class="student-flashcards-page">
        <!-- Header -->
        <div class="flex items-center justify-between flex-wrap gap-4" style="margin-bottom: 1.5rem;">
          <div>
            <h1 class="page-header-title">Thẻ từ vựng thông minh (SRS)</h1>
            <p class="page-header-subtitle">
              Ghi nhớ từ vựng lâu dài theo phương pháp Lặp lại ngắt quãng (Spaced Repetition System).
            </p>
          </div>

          <div class="flex items-center gap-3">
            ${dueCount > 0 ? `
              <button type="button" class="btn btn-warning" id="btn-start-review" style="font-weight: 700; box-shadow: 0 4px 12px rgba(245,158,11,0.25);">
                <span class="nav-icon">${icon('play', { size: 18 })}</span>
                <span>Ôn tập ngay (${dueCount} từ đến hạn)</span>
              </button>
            ` : `
              <button type="button" class="btn btn-outline-success" id="btn-start-review" style="font-weight: 600;">
                <span class="nav-icon">${icon('repeat', { size: 18 })}</span>
                <span>Ôn luyện lại toàn bộ (${flashcards.length} từ)</span>
              </button>
            `}

            <button type="button" class="btn btn-primary" id="btn-open-create-modal">
              <span class="nav-icon">${icon('plus', { size: 18 })}</span>
              <span>Thêm thẻ mới</span>
            </button>
          </div>
        </div>

        <!-- Banner Tổng quan Trạng thái Ôn Tập -->
        <div class="grid grid-cols-3 gap-6" style="margin-bottom: 2rem;">
          <div class="stat-card" style="border-radius: var(--radius-xl); background: linear-gradient(135deg, #FFFBEB, #FEF3C7); border: 1px solid #FDE68A;">
            <div class="stat-info">
              <span class="stat-label" style="color: #92400E;">Cần ôn hôm nay</span>
              <span class="stat-value" style="color: #B45309;">${dueCount}</span>
              <span class="stat-sub" style="color: #B45309;">
                ${dueCount > 0 ? '🔥 Cần xem lại để không bị quên' : '✓ Bạn đã hoàn thành mục tiêu hôm nay'}
              </span>
            </div>
            <div class="stat-icon warning" style="background: white;">${icon('clock', { size: 24 })}</div>
          </div>

          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Tổng số thẻ trong bộ</span>
              <span class="stat-value">${flashcards.length}</span>
              <span class="stat-sub text-success">Đang hoạt động</span>
            </div>
            <div class="stat-icon primary">${icon('layers', { size: 24 })}</div>
          </div>

          <div class="stat-card" style="border-radius: var(--radius-xl);">
            <div class="stat-info">
              <span class="stat-label">Từ đã ghi nhớ tốt (SRS >= 4d)</span>
              <span class="stat-value">
                ${flashcards.filter(f => (f.interval_days || 1) >= 4).length}
              </span>
              <span class="stat-sub text-info">Trí nhớ dài hạn</span>
            </div>
            <div class="stat-icon success">${icon('award', { size: 24 })}</div>
          </div>
        </div>

        <!-- Danh sách Thẻ Từ Vựng -->
        <div class="card" style="border-radius: var(--radius-xl); padding: 1.5rem; border: 1px solid var(--border-color);">
          <div class="flex items-center justify-between gap-4 flex-wrap" style="margin-bottom: 1.25rem;">
            <div class="flex items-center gap-2">
              <span style="color: var(--primary-600);">${icon('book', { size: 22 })}</span>
              <h3 style="font-size: 1.125rem; font-weight: 700; margin: 0;">Danh sách thẻ từ vựng của bạn</h3>
            </div>

            <!-- Ô tìm kiếm thẻ -->
            <div style="width: 280px; position: relative;">
              <span style="position: absolute; left: 12px; top: 50%; transform: translateY(-50%); color: var(--text-muted);">
                ${icon('search', { size: 16 })}
              </span>
              <input 
                type="text" 
                id="search-flashcard-input" 
                class="form-control" 
                style="padding-left: 36px; border-radius: var(--radius-full);" 
                placeholder="Tìm từ vựng, nghĩa..." 
                value="${searchKeyword || ''}"
              >
            </div>
          </div>

          ${flashcards.length > 0 ? `
            <div class="grid grid-cols-3 gap-4" id="flashcard-grid-container">
              ${flashcards.map(card => this._renderMiniCard(card)).join('')}
            </div>
          ` : `
            <div style="padding: 3rem; text-align: center; color: var(--text-muted);">
              <div style="margin-bottom: 0.5rem;">${icon('layers', { size: 40 })}</div>
              <p style="margin: 0 0 1rem 0;">Bạn chưa có thẻ từ vựng nào trong bộ nhớ.</p>
              <button type="button" class="btn btn-primary btn-sm" id="btn-empty-add-card">
                Thêm thẻ đầu tiên
              </button>
            </div>
          `}
        </div>
      </div>
    `;
  }

  static _renderMiniCard(c) {
    return `
      <div class="card" style="padding: 1.25rem; border-radius: var(--radius-lg); border: 1px solid var(--border-color); background: var(--neutral-50); display: flex; flex-direction: column; justify-content: space-between;">
        <div>
          <div class="flex items-start justify-between" style="margin-bottom: 6px;">
            <span style="font-size: 1.125rem; font-weight: 800; color: var(--primary-700);">${c.word}</span>
            <div class="flex items-center gap-1">
              <button type="button" class="btn-icon btn-speak" data-word="${c.word}" title="Phát âm">
                ${icon('volume-2', { size: 16 })}
              </button>
              <button type="button" class="btn-icon btn-edit-card" data-card-id="${c.id}" title="Sửa thẻ">
                ${icon('edit', { size: 15 })}
              </button>
              <button type="button" class="btn-icon btn-delete-card text-danger" data-card-id="${c.id}" title="Xóa thẻ">
                ${icon('trash-2', { size: 15 })}
              </button>
            </div>
          </div>

          <div style="font-family: monospace; color: var(--text-muted); font-size: 0.8125rem; margin-bottom: 8px;">
            ${c.phonetic || ''}
          </div>

          <div style="font-weight: 600; font-size: 0.875rem; color: var(--text-primary); margin-bottom: 6px;">
            ${c.meaning}
          </div>

          ${c.example_sentence ? `
            <div style="font-size: 0.75rem; color: var(--text-secondary); font-style: italic; line-height: 1.4;">
              "${c.example_sentence}"
            </div>
          ` : ''}
        </div>

        <div style="margin-top: 1rem; padding-top: 0.5rem; border-top: 1px dashed var(--border-color); display: flex; align-items: center; justify-content: space-between; font-size: 0.7rem; color: var(--text-muted);">
          <span>Chu kỳ: ${c.interval_days || 1} ngày</span>
          <span>Ôn lần tới: ${c.next_review_at ? new Date(c.next_review_at).toLocaleDateString('vi-VN') : 'Hôm nay'}</span>
        </div>
      </div>
    `;
  }

  /**
   * Render Chế độ Ôn tập Flashcard Lật 3D (Review Mode)
   */
  static renderReviewMode({ card, currentIndex, totalCards }) {
    if (!card) {
      return `
        <div class="card" style="padding: 4rem 2rem; text-align: center; border-radius: var(--radius-xl); max-width: 600px; margin: 2rem auto;">
          <div style="font-size: 3rem; margin-bottom: 1rem;">🎉</div>
          <h2 style="font-size: 1.5rem; font-weight: 800; color: var(--text-primary); margin-bottom: 0.5rem;">
            Xuất sắc! Bạn đã hoàn thành phiên ôn tập!
          </h2>
          <p style="color: var(--text-secondary); margin-bottom: 2rem;">
            Tất cả thẻ từ vựng hôm nay đã được cập nhật chu kỳ ghi nhớ ngắt quãng (SRS).
          </p>
          <a href="#/flashcards" class="btn btn-primary" style="display: inline-flex;">
            Quay lại Danh sách Flashcard
          </a>
        </div>
      `;
    }

    return `
      <div class="student-flashcard-review-page" style="max-width: 700px; margin: 0 auto;">
        <!-- Top bar -->
        <div class="flex items-center justify-between" style="margin-bottom: 1.5rem;">
          <button type="button" class="btn btn-secondary btn-sm" id="btn-exit-review" style="background: white; border-radius: var(--radius-full);">
            <span class="nav-icon">${icon('arrow-left', { size: 16 })}</span>
            <span>Kết thúc ôn tập</span>
          </button>

          <div style="font-weight: 700; font-size: 0.875rem; color: var(--primary-700);">
            Thẻ ${currentIndex + 1} / ${totalCards}
          </div>
        </div>

        <!-- Progress bar -->
        <div class="progress" style="height: 6px; border-radius: var(--radius-full); margin-bottom: 2rem; background: var(--neutral-200);">
          <div class="progress-bar" style="width: ${Math.round(((currentIndex + 1) / totalCards) * 100)}%; background: var(--warning-main); border-radius: var(--radius-full);"></div>
        </div>

        <!-- 3D Flip Card Scene -->
        <div class="flashcard-scene" id="flashcard-scene">
          <div class="flashcard-card" id="active-flashcard-card">
            <!-- Mặt Trước (Front) -->
            <div class="flashcard-face flashcard-front">
              <div style="position: absolute; top: 20px; right: 20px;">
                <button type="button" class="btn-icon btn-speak" data-word="${card.word}" title="Nghe phát âm">
                  ${icon('volume-2', { size: 24 })}
                </button>
              </div>

              <div style="font-size: 2.25rem; font-weight: 800; color: var(--primary-700); margin-bottom: 0.75rem;">
                ${card.word}
              </div>
              <div style="font-family: monospace; font-size: 1.125rem; color: var(--text-muted); margin-bottom: 2rem;">
                ${card.phonetic || ''}
              </div>

              <div style="font-size: 0.8125rem; color: var(--text-muted); display: flex; align-items: center; gap: 6px; margin-top: auto;">
                <span>👆</span> <span>Nhấn vào thẻ để xem nghĩa và ví dụ</span>
              </div>
            </div>

            <!-- Mặt Sau (Back) -->
            <div class="flashcard-face flashcard-back">
              <div style="font-size: 1.125rem; font-weight: 700; color: var(--primary-600); margin-bottom: 4px;">
                ${card.word}
              </div>
              <div style="font-size: 1.75rem; font-weight: 800; color: #166534; margin-bottom: 1rem;">
                ${card.meaning}
              </div>

              ${card.example_sentence ? `
                <div style="background: white; padding: 1rem 1.25rem; border-radius: var(--radius-md); border: 1px solid var(--border-color); font-size: 0.9375rem; color: var(--text-secondary); font-style: italic; max-width: 480px; text-align: center; margin-bottom: 1rem;">
                  "${card.example_sentence}"
                </div>
              ` : ''}

              <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: auto;">
                Nhấn lại vào thẻ để lật ngược
              </div>
            </div>
          </div>
        </div>

        <!-- 4 Nút Đánh Giá SRS -->
        <div style="background: white; border-radius: var(--radius-xl); padding: 1.25rem; border: 1px solid var(--border-color); box-shadow: 0 4px 15px rgba(0,0,0,0.05);">
          <div style="text-align: center; font-size: 0.8125rem; color: var(--text-secondary); margin-bottom: 1rem; font-weight: 600;">
            Bạn nhớ từ này ở mức độ nào?
          </div>
          <div class="grid grid-cols-4 gap-3">
            <button type="button" class="btn btn-outline-danger btn-rate-card" data-rating="again" style="flex-direction: column; padding: 10px; font-weight: 700;">
              <span>Quên</span>
              <small style="font-size: 0.7rem; font-weight: normal;">+1 ngày</small>
            </button>
            <button type="button" class="btn btn-outline-warning btn-rate-card" data-rating="hard" style="flex-direction: column; padding: 10px; font-weight: 700;">
              <span>Khó</span>
              <small style="font-size: 0.7rem; font-weight: normal;">+2 ngày</small>
            </button>
            <button type="button" class="btn btn-outline-primary btn-rate-card" data-rating="good" style="flex-direction: column; padding: 10px; font-weight: 700;">
              <span>Được</span>
              <small style="font-size: 0.7rem; font-weight: normal;">+4 ngày</small>
            </button>
            <button type="button" class="btn btn-outline-success btn-rate-card" data-rating="easy" style="flex-direction: column; padding: 10px; font-weight: 700;">
              <span>Dễ</span>
              <small style="font-size: 0.7rem; font-weight: normal;">+7 ngày</small>
            </button>
          </div>
        </div>
      </div>
    `;
  }
}
