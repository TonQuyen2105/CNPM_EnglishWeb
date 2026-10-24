/**
 * SVG Charts Component - Vẽ biểu đồ trực quan bằng SVG & CSS thuần
 * Không phụ thuộc thư viện ngoài (Chart.js / Highcharts / D3)
 */

/**
 * Biểu đồ cột (Bar Chart)
 * @param {Object} options 
 * @param {Array<{label: string, value: number}>} options.data
 * @param {number} options.height
 * @param {string} options.color
 * @returns {string} SVG HTML string
 */
export function renderBarChart({ data = [], height = 240, color = 'var(--primary-600)' }) {
  if (!data || data.length === 0) return '<div class="text-muted text-center p-4">Không có dữ liệu biểu đồ</div>';

  const maxValue = Math.max(...data.map(d => d.value), 10);
  const chartHeight = height - 40; // trừ lề nhãn dưới
  const barWidth = 32;
  const gap = 24;
  const totalWidth = Math.max(data.length * (barWidth + gap) + 40, 360);

  const barsSvg = data.map((d, index) => {
    const barHeight = Math.round((d.value / maxValue) * chartHeight);
    const x = 30 + index * (barWidth + gap);
    const y = chartHeight - barHeight + 15;

    return `
      <g class="chart-bar-group" tabIndex="0">
        <!-- Background guide bar -->
        <rect x="${x}" y="15" width="${barWidth}" height="${chartHeight}" rx="6" fill="#F1F5F9" />
        <!-- Actual value bar with animation -->
        <rect x="${x}" y="${y}" width="${barWidth}" height="${barHeight}" rx="6" fill="${color}">
          <title>${d.label}: ${d.value.toLocaleString('vi-VN')}</title>
        </rect>
        <!-- Value text on top -->
        <text x="${x + barWidth / 2}" y="${Math.max(y - 6, 12)}" text-anchor="middle" font-size="10" font-weight="600" fill="var(--text-secondary)">
          ${d.value >= 1000 ? (d.value / 1000).toFixed(0) + 'k' : d.value}
        </text>
        <!-- Label below -->
        <text x="${x + barWidth / 2}" y="${height - 8}" text-anchor="middle" font-size="11" font-weight="500" fill="var(--text-secondary)">
          ${d.label}
        </text>
      </g>
    `;
  }).join('');

  return `
    <div style="width: 100%; overflow-x: auto;">
      <svg viewBox="0 0 ${totalWidth} ${height}" style="width: 100%; min-width: ${totalWidth}px; height: ${height}px;" preserveAspectRatio="xMidYMid meet">
        <!-- Horizontal grid line -->
        <line x1="15" y1="${chartHeight + 15}" x2="${totalWidth - 15}" y2="${chartHeight + 15}" stroke="var(--border-color)" stroke-width="1" />
        ${barsSvg}
      </svg>
    </div>
  `;
}

/**
 * Biểu đồ tròn dạng Donut (Donut Chart)
 * @param {Object} options 
 * @param {Array<{label: string, value: number, color: string}>} options.data
 * @param {number} options.size Kích thước đường kính (px)
 * @param {string} options.centerLabel Nhãn ở tâm
 * @returns {string} HTML string
 */
export function renderDonutChart({ data = [], size = 180, centerLabel = '' }) {
  if (!data || data.length === 0) return '<div class="text-muted text-center p-4">Không có dữ liệu biểu đồ</div>';

  const total = data.reduce((sum, d) => sum + d.value, 0) || 1;
  const strokeWidth = 24;
  const radius = (size - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  let accumulatedPercent = 0;

  const defaultColors = ['#2563EB', '#10B981', '#F59E0B', '#EF4444', '#0EA5E9', '#8B5CF6'];

  const segmentsSvg = data.map((d, index) => {
    const percent = d.value / total;
    const strokeDasharray = `${circumference * percent} ${circumference * (1 - percent)}`;
    const strokeDashoffset = -circumference * accumulatedPercent;
    accumulatedPercent += percent;
    const color = d.color || defaultColors[index % defaultColors.length];

    return `
      <circle 
        cx="${size / 2}" 
        cy="${size / 2}" 
        r="${radius}" 
        fill="transparent" 
        stroke="${color}" 
        stroke-width="${strokeWidth}" 
        stroke-dasharray="${strokeDasharray}" 
        stroke-dashoffset="${strokeDashoffset}"
        stroke-linecap="round"
        style="transition: stroke-dasharray 0.5s ease;"
      >
        <title>${d.label}: ${d.value} (${Math.round(percent * 100)}%)</title>
      </circle>
    `;
  }).join('');

  const legendHtml = data.map((d, index) => {
    const color = d.color || defaultColors[index % defaultColors.length];
    const pct = Math.round((d.value / total) * 100);
    return `
      <div style="display: flex; align-items: center; justify-content: space-between; font-size: 0.8125rem; margin-bottom: 6px;">
        <div style="display: flex; align-items: center; gap: 8px;">
          <span style="width: 10px; height: 10px; border-radius: 50%; background-color: ${color}; display: inline-block;"></span>
          <span style="color: var(--text-secondary);">${d.label}</span>
        </div>
        <strong style="color: var(--text-primary);">${pct}% (${d.value})</strong>
      </div>
    `;
  }).join('');

  return `
    <div style="display: flex; align-items: center; gap: 1.5rem; flex-wrap: wrap;">
      <div style="position: relative; width: ${size}px; height: ${size}px; flex-shrink: 0;">
        <svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" style="transform: rotate(-90deg);">
          <!-- Background track -->
          <circle cx="${size / 2}" cy="${size / 2}" r="${radius}" fill="transparent" stroke="#F1F5F9" stroke-width="${strokeWidth}" />
          ${segmentsSvg}
        </svg>
        ${centerLabel ? `
          <div style="position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center; pointer-events: none;">
            <span style="font-size: 1.25rem; font-weight: 800; color: var(--text-primary);">${total.toLocaleString('vi-VN')}</span>
            <span style="font-size: 0.75rem; color: var(--text-muted);">${centerLabel}</span>
          </div>
        ` : ''}
      </div>
      <div style="flex: 1; min-width: 160px;">
        ${legendHtml}
      </div>
    </div>
  `;
}

/**
 * Biểu đồ đường (Line / Area Chart)
 * @param {Object} options 
 * @param {Array<{label: string, value: number}>} options.data
 * @param {number} options.height
 * @param {string} options.color
 * @returns {string} SVG HTML
 */
export function renderLineChart({ data = [], height = 220, color = '#2563EB' }) {
  if (!data || data.length < 2) return '<div class="text-muted text-center p-4">Không đủ dữ liệu đường</div>';

  const maxValue = Math.max(...data.map(d => d.value), 10);
  const chartHeight = height - 40;
  const width = 450;
  const paddingX = 30;
  const availableWidth = width - paddingX * 2;
  const stepX = availableWidth / (data.length - 1);

  const points = data.map((d, index) => {
    const x = paddingX + index * stepX;
    const y = chartHeight - (d.value / maxValue) * chartHeight + 15;
    return { x, y, value: d.value, label: d.label };
  });

  const pathD = points.reduce((acc, pt, index) => {
    return `${acc} ${index === 0 ? 'M' : 'L'} ${pt.x} ${pt.y}`;
  }, '');

  const areaD = `${pathD} L ${points[points.length - 1].x} ${chartHeight + 15} L ${points[0].x} ${chartHeight + 15} Z`;

  const pointsSvg = points.map(pt => `
    <circle cx="${pt.x}" cy="${pt.y}" r="4.5" fill="#FFFFFF" stroke="${color}" stroke-width="2.5">
      <title>${pt.label}: ${pt.value}</title>
    </circle>
    <text x="${pt.x}" y="${height - 8}" text-anchor="middle" font-size="11" fill="var(--text-secondary)">
      ${pt.label}
    </text>
  `).join('');

  return `
    <div style="width: 100%; overflow-x: auto;">
      <svg viewBox="0 0 ${width} ${height}" style="width: 100%; min-width: 380px; height: ${height}px;" preserveAspectRatio="xMidYMid meet">
        <defs>
          <linearGradient id="areaGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stop-color="${color}" stop-opacity="0.25"/>
            <stop offset="100%" stop-color="${color}" stop-opacity="0.0"/>
          </linearGradient>
        </defs>
        <!-- Horizontal grid line -->
        <line x1="${paddingX}" y1="${chartHeight + 15}" x2="${width - paddingX}" y2="${chartHeight + 15}" stroke="var(--border-color)" stroke-width="1" />
        <!-- Area -->
        <path d="${areaD}" fill="url(#areaGradient)" />
        <!-- Line -->
        <path d="${pathD}" fill="none" stroke="${color}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
        <!-- Points & Labels -->
        ${pointsSvg}
      </svg>
    </div>
  `;
}
