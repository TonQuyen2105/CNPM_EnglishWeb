/**
 * Hash-based Client-Side Router
 * Quản lý định tuyến SPA qua URL hash (#/path)
 */

export class Router {
  constructor(options = {}) {
    this.routes = [];
    this.beforeEachHooks = [];
    this.currentRoute = null;
    this.container = options.container || document.getElementById('app-main-content');

    window.addEventListener('hashchange', () => this._handleRouteChange());
  }

  /**
   * Đăng ký middleware chạy trước mỗi route
   * @param {Function} hookFn (toRoute, fromRoute, next)
   */
  beforeEach(hookFn) {
    this.beforeEachHooks.push(hookFn);
  }

  /**
   * Đăng ký một route
   * @param {string} path Ví dụ: '/dashboard', '/classes/:id'
   * @param {Function|Object} handler Function(params, query) hoặc Controller
   * @param {Object} meta Dữ liệu metadata (title, icon, roles, etc.)
   */
  register(path, handler, meta = {}) {
    const { regex, paramNames } = this._pathToRegex(path);
    this.routes.push({
      path,
      regex,
      paramNames,
      handler,
      meta
    });
    return this;
  }

  /**
   * Chuyển URL hash
   * @param {string} hash Ví dụ: '/classes/10'
   */
  navigate(hash) {
    const formatted = hash.startsWith('#') ? hash : `#${hash.startsWith('/') ? hash : '/' + hash}`;
    if (window.location.hash === formatted) {
      // Nếu hash đã trùng, hashchange sẽ không bắn -> thực thi luôn
      this._handleRouteChange();
    } else {
      window.location.hash = formatted;
    }
  }

  /**
   * Khởi chạy router
   */
  init(defaultRoute = '/dashboard') {
    if (!window.location.hash || window.location.hash === '#' || window.location.hash === '#/') {
      const formatted = defaultRoute.startsWith('#') ? defaultRoute : `#${defaultRoute.startsWith('/') ? defaultRoute : '/' + defaultRoute}`;
      window.location.hash = formatted;
    }
    // Luôn luôn kích hoạt render route ngay lập tức lúc khởi chạy, không phụ thuộc vào hashchange event
    this._handleRouteChange();
  }

  _pathToRegex(path) {
    const paramNames = [];
    const segments = path.split('/');
    const regexSegments = segments.map(seg => {
      if (seg.startsWith(':')) {
        paramNames.push(seg.slice(1));
        return '([^/]+)';
      }
      return seg.replace(/([.+*?^=!${}()|[\]/\\])/g, '\\$1');
    });

    const regexStr = regexSegments.join('/');
    return {
      regex: new RegExp(`^#?${regexStr}$`),
      paramNames
    };
  }

  _parseHash() {
    const raw = window.location.hash.slice(1) || '/';
    const [pathPart, queryPart] = raw.split('?');
    const path = pathPart.startsWith('/') ? pathPart : `/${pathPart}`;

    const query = {};
    if (queryPart) {
      const searchParams = new URLSearchParams(queryPart);
      for (const [key, value] of searchParams.entries()) {
        query[key] = value;
      }
    }

    return { path, query, raw };
  }

  async _handleRouteChange() {
    const { path, query, raw } = this._parseHash();

    // Tránh render lại nếu cùng một route vừa được vẽ ra đầy đủ
    if (this.currentRoute && 
        this.currentRoute.path === path && 
        JSON.stringify(this.currentRoute.query) === JSON.stringify(query) &&
        this.container && this.container.innerHTML.trim().length > 50) {
      return;
    }

    let matchedRoute = null;
    let params = {};

    for (const route of this.routes) {
      const match = path.match(route.regex);
      if (match) {
        matchedRoute = route;
        route.paramNames.forEach((name, index) => {
          params[name] = decodeURIComponent(match[index + 1]);
        });
        break;
      }
    }

    if (!matchedRoute) {
      console.warn(`Không tìm thấy route cho đường dẫn: ${path}`);
      if (this.routes.length > 0) {
        // Fallback về route đầu tiên hoặc hiển thị 404
        matchedRoute = this.routes[0];
      } else {
        return;
      }
    }

    const toRoute = { path, query, params, meta: matchedRoute.meta };
    const fromRoute = this.currentRoute;

    // Chạy các beforeEach hooks
    let proceed = true;
    for (const hook of this.beforeEachHooks) {
      const res = await new Promise(resolve => {
        hook(toRoute, fromRoute, (result = true) => resolve(result));
      });

      if (res === false) {
        proceed = false;
        break;
      } else if (typeof res === 'string') {
        this.navigate(res);
        return;
      }
    }

    if (!proceed) return;

    this.currentRoute = toRoute;

    // Cập nhật title trình duyệt nếu có
    if (matchedRoute.meta && matchedRoute.meta.title) {
      document.title = `${matchedRoute.meta.title} - EnglishLearning`;
    }

    // Thực thi handler
    try {
      if (matchedRoute.handler && typeof matchedRoute.handler.render === 'function') {
        await matchedRoute.handler.render(params, query, this.container);
      } else if (typeof matchedRoute.handler === 'function') {
        await matchedRoute.handler(params, query, this.container);
      } else if (matchedRoute.handler && typeof matchedRoute.handler.init === 'function') {
        await matchedRoute.handler.init(params, query, this.container);
      }
    } catch (err) {
      console.error(`Lỗi thực thi route ${path}:`, err);
    }
  }
}
