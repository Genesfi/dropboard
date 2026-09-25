/**
 * DropBoard - Native Windows C++ & WebView2 Reference Board Engine
 */

// =============================================================================
// Native C++ Bridge Communication
// =============================================================================
const NativeBridge = {
  isAvailable() {
    return window.chrome && window.chrome.webview;
  },

  post(action, data = {}) {
    if (this.isAvailable()) {
      window.chrome.webview.postMessage({ action, ...data });
    } else {
      console.log(`[NativeBridge Mock] Action: ${action}`, data);
    }
  },

  init(onMessageCallback) {
    if (this.isAvailable()) {
      window.chrome.webview.addEventListener('message', (event) => {
        let msg = event.data;
        if (typeof msg === 'string') {
          try { msg = JSON.parse(msg); } catch (e) {}
        }
        if (onMessageCallback) onMessageCallback(msg);
      });
    }
  },

  minimize() { this.post('window_minimize'); },
  maximize() { this.post('window_maximize'); },
  close() { this.post('window_close'); },
  startDrag() { this.post('window_start_drag'); },
  startResize(edge) { this.post('window_start_resize', { edge }); },
  setAlwaysOnTop(enabled) { this.post('set_always_on_top', { value: enabled }); },
  setOpacity(val) { this.post('set_opacity', { value: val }); },
  downloadImage(url, id) { this.post('download_image', { url, id }); },
  copyFileToClipboard(filePath, imageData = '') { this.post('copy_file_to_clipboard', { filePath, imageData }); },
  revealInExplorer(filePath, imageData = '') { this.post('reveal_in_explorer', { filePath, imageData }); },
  sendToAE(filePath, imageData = '') { this.post('send_to_ae', { filePath, imageData }); },
  snapVideoFrame(cardId, clientX, clientY, width, height, sendToAe = true, imageData = '') {
    this.post('snap_video_frame', { cardId, clientX, clientY, width, height, sendToAe, imageData });
  },
  exportToAEComp(payload) { this.post('export_to_ae_comp', payload); },
  sendToPhotoshop(filePath, imageData = '') { this.post('send_to_photoshop', { filePath, imageData }); },
  sendToCustom(exePath, filePath, imageData = '') { this.post('send_to_custom', { exePath, filePath, imageData }); },
  openDefault(filePath, imageData = '') { this.post('open_default', { filePath, imageData }); },
  saveBoardDirect(data, filePath) { this.post('save_board_direct', { data, filePath }); },
  saveBoardDialog(data, name = '') { this.post('save_board_dialog', { data, name }); },
  loadBoardDirect(filePath) { this.post('load_board_direct', { filePath }); },
  loadBoardDialog() { this.post('load_board_dialog'); },
  loadSessionBoard() { this.post('load_session_board'); },
  clearImageCache() { this.post('clear_image_cache'); },
  openCacheFolder() { this.post('open_cache_folder'); },
  getSystemFonts() { this.post('get_system_fonts'); },
  openExternalUrl(url) { this.post('open_external_url', { url }); },
  registerFileAssociation() { this.post('register_file_association'); },
  appReady() { this.post('app_ready'); }
};

// =============================================================================
// Toast Notification Engine
// =============================================================================
const Toast = {
  container: document.getElementById('toast-container'),

  show(message, type = 'info', duration = 3200) {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    this.container.appendChild(toast);

    setTimeout(() => {
      toast.style.transition = 'all 0.25s ease';
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(20px)';
      setTimeout(() => toast.remove(), 260);
    }, duration);
  }
};

// =============================================================================
// Infinite Canvas Viewport & Coordinate Transformer
// =============================================================================
class CanvasEngine {
  constructor(viewportEl, worldEl, app = null) {
    this.viewport = viewportEl;
    this.world = worldEl;
    this.app = app;

    this.panX = window.innerWidth / 2;
    this.panY = window.innerHeight / 2;
    this.zoom = 1.0;

    this.isPanning = false;
    this.panStartX = 0;
    this.panStartY = 0;
    this.spacePressed = false;

    // Navigation & Touchpad Configuration
    this.navMode = 'macos'; // 'macos' | 'windows' | 'mouse'
    this.panSensitivity = 1.0;
    this.zoomSensitivity = 1.0;
    this.invertPan = false;

    try {
      const savedMode = localStorage.getItem('dropboard_nav_mode');
      if (savedMode) {
        if (savedMode === 'auto' || savedMode === 'touchpad') {
          this.navMode = 'macos';
        } else {
          this.navMode = savedMode;
        }
      }
      const savedPanSens = localStorage.getItem('dropboard_pan_sens');
      if (savedPanSens) this.panSensitivity = parseFloat(savedPanSens) || 1.0;
      const savedZoomSens = localStorage.getItem('dropboard_zoom_sens');
      if (savedZoomSens) this.zoomSensitivity = parseFloat(savedZoomSens) || 1.0;
      const savedInvert = localStorage.getItem('dropboard_invert_pan');
      if (savedInvert !== null) this.invertPan = savedInvert === 'true';
    } catch(e) {}

    this.initEvents();
    this.updateTransform();
  }

  screenToWorld(screenX, screenY) {
    const rect = this.viewport.getBoundingClientRect();
    const vx = screenX - rect.left;
    const vy = screenY - rect.top;
    return {
      x: (vx - this.panX) / this.zoom,
      y: (vy - this.panY) / this.zoom
    };
  }

  worldToScreen(worldX, worldY) {
    const rect = this.viewport.getBoundingClientRect();
    return {
      x: rect.left + this.panX + worldX * this.zoom,
      y: rect.top + this.panY + worldY * this.zoom
    };
  }

  updateTransform() {
    this.world.style.transform = `translate(${this.panX}px, ${this.panY}px) scale(${this.zoom})`;
    document.getElementById('zoom-text').textContent = `${Math.round(this.zoom * 100)}%`;
  }

  zoomAt(screenX, screenY, factor) {
    const rect = this.viewport.getBoundingClientRect();
    const mouseX = screenX - rect.left;
    const mouseY = screenY - rect.top;

    const newZoom = Math.min(Math.max(0.1, this.zoom * factor), 5.0);
    if (newZoom === this.zoom) return;

    this.panX = mouseX - (mouseX - this.panX) * (newZoom / this.zoom);
    this.panY = mouseY - (mouseY - this.panY) * (newZoom / this.zoom);
    this.zoom = newZoom;
    this.updateTransform();
  }

  resetView(centerX = 0, centerY = 0) {
    this.zoom = 1.0;
    this.panX = window.innerWidth / 2 - centerX;
    this.panY = window.innerHeight / 2 - centerY;
    this.updateTransform();
  }

  animateViewTo(targetPanX, targetPanY, targetZoom, duration = 280) {
    const startPanX = this.panX;
    const startPanY = this.panY;
    const startZoom = this.zoom;
    const startTime = performance.now();

    const animate = (time) => {
      const elapsed = time - startTime;
      const progress = Math.min(1, elapsed / duration);
      // Smooth cubic easing
      const ease = 1 - Math.pow(1 - progress, 3);

      this.panX = startPanX + (targetPanX - startPanX) * ease;
      this.panY = startPanY + (targetPanY - startPanY) * ease;
      this.zoom = startZoom + (targetZoom - startZoom) * ease;
      this.updateTransform();

      if (progress < 1) {
        requestAnimationFrame(animate);
      }
    };
    requestAnimationFrame(animate);
  }

  initEvents() {
    let wheelGestureTimeout = null;
    let isTrackpadGestureActive = false;

    const onWheelAction = (e) => {
      const navMode = this.navMode || 'macos';
      const zoomSens = this.zoomSensitivity || 1.0;
      const panSens = this.panSensitivity || 1.0;

      let cx = e.clientX;
      let cy = e.clientY;
      if (cx > window.innerWidth || cy > window.innerHeight) {
        const winLeft = window.screenLeft !== undefined ? window.screenLeft : (window.screenX || 0);
        const winTop = window.screenTop !== undefined ? window.screenTop : (window.screenY || 0);
        cx = cx - winLeft;
        cy = cy - winTop;
      }
      if (cx === undefined || isNaN(cx)) cx = window.innerWidth / 2;
      if (cy === undefined || isNaN(cy)) cy = window.innerHeight / 2;

      // 1. Pinch-to-zoom on Precision Touchpad OR Ctrl + Mouse Wheel
      if (e.ctrlKey) {
        let factor;
        if (Math.abs(e.deltaY) >= 60) {
          factor = e.deltaY < 0 ? 1.15 : 0.87;
        } else {
          factor = Math.exp(-e.deltaY * 0.006 * zoomSens);
        }
        this.zoomAt(cx, cy, factor);
        return;
      }

      // 2. PureRef / Classic Mouse Wheel Mode
      if (navMode === 'mouse') {
        const zoomFactor = e.deltaY < 0 ? 1.12 : 0.89;
        this.zoomAt(cx, cy, zoomFactor);
        return;
      }

      // 3. Touchpad Navigation Modes (macOS or Windows PC style)
      const hasHorizontalDelta = Math.abs(e.deltaX) > 0;
      const isFractionalDelta = (e.deltaY % 1 !== 0);
      const isDiscreteMouseWheel = (e.deltaMode !== 0) || (e.deltaX === 0 && Math.abs(e.deltaY) >= 30 && e.deltaY % 1 === 0);

      if (isDiscreteMouseWheel && !isTrackpadGestureActive && !hasHorizontalDelta && !isFractionalDelta) {
        const zoomFactor = e.deltaY < 0 ? 1.12 : 0.89;
        this.zoomAt(cx, cy, zoomFactor);
        return;
      }

      isTrackpadGestureActive = true;
      clearTimeout(wheelGestureTimeout);
      wheelGestureTimeout = setTimeout(() => {
        isTrackpadGestureActive = false;
      }, 250);

      let directionSign = (navMode === 'macos') ? 1 : -1;
      if (this.invertPan) {
        directionSign *= -1;
      }

      this.panX += directionSign * e.deltaX * panSens;
      this.panY += directionSign * e.deltaY * panSens;
      this.updateTransform();
    };

    this.viewport.addEventListener('wheel', (e) => {
      e.preventDefault();
      onWheelAction(e);
    }, { passive: false });

    // Multi-touch gestures for touchscreen displays / 2-in-1 laptops
    let initialTouchDist = null;
    let initialTouchMid = null;
    let initialPanX = 0;
    let initialPanY = 0;
    let initialZoom = 1.0;

    this.viewport.addEventListener('touchstart', (e) => {
      if (e.touches.length === 2) {
        e.preventDefault();
        const t1 = e.touches[0];
        const t2 = e.touches[1];
        initialTouchDist = Math.hypot(t2.clientX - t1.clientX, t2.clientY - t1.clientY);
        initialTouchMid = {
          x: (t1.clientX + t2.clientX) / 2,
          y: (t1.clientY + t2.clientY) / 2
        };
        initialPanX = this.panX;
        initialPanY = this.panY;
        initialZoom = this.zoom;
      }
    }, { passive: false });

    this.viewport.addEventListener('touchmove', (e) => {
      if (e.touches.length === 2 && initialTouchDist && initialTouchMid) {
        e.preventDefault();
        const t1 = e.touches[0];
        const t2 = e.touches[1];
        const currentDist = Math.hypot(t2.clientX - t1.clientX, t2.clientY - t1.clientY);
        const currentMid = {
          x: (t1.clientX + t2.clientX) / 2,
          y: (t1.clientY + t2.clientY) / 2
        };

        const zoomRatio = currentDist / initialTouchDist;
        const newZoom = Math.min(Math.max(0.1, initialZoom * zoomRatio), 5.0);

        const rect = this.viewport.getBoundingClientRect();
        const mouseX = initialTouchMid.x - rect.left;
        const mouseY = initialTouchMid.y - rect.top;

        this.panX = (currentMid.x - initialTouchMid.x) + (mouseX - (mouseX - initialPanX) * (newZoom / initialZoom));
        this.panY = (currentMid.y - initialTouchMid.y) + (mouseY - (mouseY - initialPanY) * (newZoom / initialZoom));
        this.zoom = newZoom;
        this.updateTransform();
      }
    }, { passive: false });

    const endTouch = () => {
      initialTouchDist = null;
      initialTouchMid = null;
    };
    this.viewport.addEventListener('touchend', endTouch);
    this.viewport.addEventListener('touchcancel', endTouch);

    // Keyboard Spacebar / Alt tracking for instant pan mode
    window.addEventListener('keydown', (e) => {
      if ((e.code === 'Space' || e.altKey) && e.target.tagName !== 'INPUT' && e.target.tagName !== 'TEXTAREA') {
        if (e.code === 'Space') {
          this.spacePressed = true;
          this.viewport.classList.add('panning');
        }
        document.body.classList.add('is-space-held');
      }
    });

    window.addEventListener('keyup', (e) => {
      if (e.code === 'Space') {
        this.spacePressed = false;
        if (!this.isPanning) {
          this.viewport.classList.remove('panning');
        }
      }
      if (!this.spacePressed && !e.altKey) {
        document.body.classList.remove('is-space-held');
      }
    });

    // Pan via Middle Click, Space + Left Click, Alt + Left Click, or Right-Click Drag (PureRef style)
    let hasMovedPan = false;
    let panStartClientX = 0;
    let panStartClientY = 0;
    let activePanButton = -1;

    this.startExternalPan = (button, clientX, clientY) => {
      this.isPanning = true;
      activePanButton = button;
      hasMovedPan = false;
      panStartClientX = clientX;
      panStartClientY = clientY;
      this.panStartX = clientX - this.panX;
      this.panStartY = clientY - this.panY;
      this.viewport.classList.add('panning-active');
      document.body.classList.add('is-canvas-panning');
    };

    this.viewport.addEventListener('mousedown', (e) => {
      // Don't pan if clicking UI controls outside canvas
      if (e.target.closest('.floating-dock') || e.target.closest('.titlebar') || e.target.closest('.modal-card') || e.target.closest('.context-menu') || e.target.closest('.blender-popup') || e.target.closest('.opacity-popover')) {
        return;
      }

      // Check if this is a Pan action
      const isMiddle = (e.button === 1);
      const isSpaceOrAlt = (e.button === 0 && (this.spacePressed || e.altKey));
      const isRight = (e.button === 2);

      if (isMiddle || isSpaceOrAlt || isRight) {
        this.isPanning = true;
        activePanButton = e.button;
        hasMovedPan = false;
        panStartClientX = e.clientX;
        panStartClientY = e.clientY;
        this.panStartX = e.clientX - this.panX;
        this.panStartY = e.clientY - this.panY;
        this.viewport.classList.add('panning-active');
        document.body.classList.add('is-canvas-panning');
        e.preventDefault();
        e.stopPropagation();
      }
    }, true); // Capture phase: catches pan input before any child card/group intercepts it!

    window.addEventListener('mousemove', (e) => {
      if (this.isPanning) {
        const dist = Math.hypot(e.clientX - panStartClientX, e.clientY - panStartClientY);
        if (dist > 3) {
          hasMovedPan = true;
        }
        this.panX = e.clientX - this.panStartX;
        this.panY = e.clientY - this.panStartY;
        this.updateTransform();
      }
    });

    window.addEventListener('mouseup', (e) => {
      if (this.isPanning) {
        this.isPanning = false;
        document.body.classList.remove('is-canvas-panning');
        this.viewport.classList.remove('panning-active');
        if (!this.spacePressed) {
          this.viewport.classList.remove('panning');
        }
      }
    });

    // Listen for pan and wheel messages forwarded from inside iframes (YouTube, web embeds)
    window.addEventListener('message', (e) => {
      if (!e.data || e.source === window) return;
      if (e.data.type === 'DROPBOARD_IFRAME_PAN_START') {
        const winLeft = window.screenLeft !== undefined ? window.screenLeft : (window.screenX || 0);
        const winTop = window.screenTop !== undefined ? window.screenTop : (window.screenY || 0);
        const clientX = e.data.clientX !== undefined ? (e.data.clientX - winLeft) : (window.innerWidth / 2);
        const clientY = e.data.clientY !== undefined ? (e.data.clientY - winTop) : (window.innerHeight / 2);
        this.startExternalPan(e.data.button, clientX, clientY);
      } else if (e.data.type === 'DROPBOARD_IFRAME_WHEEL') {
        const winLeft = window.screenLeft !== undefined ? window.screenLeft : (window.screenX || 0);
        const winTop = window.screenTop !== undefined ? window.screenTop : (window.screenY || 0);
        const clientX = e.data.clientX !== undefined ? (e.data.clientX - winLeft) : (window.innerWidth / 2);
        const clientY = e.data.clientY !== undefined ? (e.data.clientY - winTop) : (window.innerHeight / 2);
        onWheelAction({
          ...e.data,
          clientX,
          clientY
        });
      } else if (e.data.type === 'DROPBOARD_IFRAME_CLICK') {
        if (this.app) {
          const card = this.app.cards.find(c => 
            (c.ytIframe && c.ytIframe.contentWindow === e.source) || 
            (e.data.cardId && c.id === e.data.cardId)
          );
          if (card) {
            this.app.selectCard(card, false);
          }
        }
      }
    });

    window.addEventListener('blur', () => {
      if (this.isPanning) {
        this.isPanning = false;
        document.body.classList.remove('is-canvas-panning');
        this.viewport.classList.remove('panning-active');
        this.viewport.classList.remove('panning');
      }
      this.spacePressed = false;
      document.body.classList.remove('is-space-held');
    });

    // Suppress context menu if user was right-click dragging to pan (PureRef style!)
    window.addEventListener('contextmenu', (e) => {
      if (hasMovedPan && activePanButton === 2) {
        e.preventDefault();
        e.stopPropagation();
        hasMovedPan = false;
        activePanButton = -1;
      }
    }, true);
  }
}

// =============================================================================
// DropBoard App State & Reference Card Manager
// =============================================================================
class DropBoardManager {
  constructor() {
    this.cards = [];
    this.groups = [];
    this.selectedCard = null;
    this.selectedGroup = null;
    this.selectedNode = null;
    this.highestZ = 10;
    this.activeContextMenuCard = null;
    this.activeContextMenuGroup = null;

    // Undo / Redo History System
    this.undoStack = [];
    this.redoStack = [];
    this.preActionSnapshot = null;

    this.viewport = document.getElementById('viewport');
    this.world = document.getElementById('world');
    this.canvas = new CanvasEngine(this.viewport, this.world, this);
    this.selectedCards = new Set();
    this.selectedGroups = new Set();
    this.selectedNodes = new Set();
    this.autoSaveTimer = null;

    this.projectName = 'Untitled Project';
    this.currentFilePath = null;
    this.nodes = [];
    this.connections = [];
    this.systemFonts = [];
    this.arrangeGap = 32;
    try {
      const savedGap = localStorage.getItem('dropboard_arrange_gap');
      if (savedGap !== null) this.arrangeGap = parseInt(savedGap) || 0;
    } catch(e) {}
    this.autoSaveEnabled = true;
    try {
      this.autoSaveEnabled = localStorage.getItem('dropboard_autosave_enabled') !== 'false';
    } catch(e) {}
    this.dockPosition = 'top'; // 'top' | 'left' | 'bottom'
    this.dockStyle = 'auto'; // 'auto' | 'icon' | 'full'
    this.autoHideDock = false;
    try {
      const savedPos = localStorage.getItem('dropboard_dock_pos');
      if (savedPos) this.dockPosition = savedPos;
      const savedStyle = localStorage.getItem('dropboard_dock_style');
      if (savedStyle) this.dockStyle = savedStyle;
      const savedAutoHide = localStorage.getItem('dropboard_autohide_dock');
      if (savedAutoHide !== null) this.autoHideDock = (savedAutoHide === 'true');
    } catch(e) {}
    this.activeConnectingNode = null;
    this.connectorLayer = document.getElementById('connector-layer');
    this.currentMouseScreenX = window.innerWidth / 2;
    this.currentMouseScreenY = window.innerHeight / 2;
    this.lastCanvasClickWorldPos = { x: 0, y: 0 };

    this.initUIEvents();
    this.initDropAndPaste();
    this.initContextMenu();
    this.initMarqueeSelection();
    this.initNativeBridge();
    this.initBlenderSearch();
    this.initNewProjectModal();
    this.initSettingsModal();
    this.initAboutModal();
    this.updateDockLayout();
    this.loadAutoSave();
    NativeBridge.appReady();

    window.addEventListener('beforeunload', () => this.saveAutoSave());
  }

  setProjectName(name, commit = true) {
    if (!name || !name.trim()) name = 'Untitled Project';
    this.projectName = name.trim();
    const display = document.getElementById('project-name-display');
    if (display) {
      display.textContent = this.projectName;
    }
    document.title = `${this.projectName} - DropBoard STUDIO`;
    if (commit) {
      this.scheduleAutoSave();
    }
  }

  initNativeBridge() {
    NativeBridge.init((msg) => {
      if (msg.type === 'external_image_added') {
        const center = this.canvas.screenToWorld(window.innerWidth / 2, window.innerHeight / 2);
        const imageUrl = msg.localWebUrl || msg.url;
        this.addReferenceFromUrl(imageUrl, center.x, center.y, msg.title || (msg.isYouTube ? 'YouTube Ref' : 'Browser Capture'), {
          isYouTube: msg.isYouTube,
          youtubeId: msg.youtubeId,
          youtubeUrl: msg.youtubeUrl,
          originalUrl: msg.originalUrl,
          localPath: msg.localPath,
          localWebUrl: msg.localWebUrl,
          imageData: msg.imageData || null
        });
        Toast.show(msg.isYouTube ? 'Added YouTube Reference!' : 'Received reference from Browser Extension!', 'success');
      } else if (msg.type === 'download_completed') {
        const card = this.cards.find(c => c.id === msg.id);
        if (card && msg.success) {
          card.localPath = msg.localPath;
          if (msg.imageData) {
            card.imageData = msg.imageData;
          }
          if (msg.resolvedUrl) {
            card.url = msg.resolvedUrl;
          }
          if (msg.localWebUrl) {
            card.localWebUrl = msg.localWebUrl;
            const img = card.element ? card.element.querySelector('img') : null;
            if (img) {
              img.crossOrigin = 'anonymous';
              img.onload = () => {
                if (img.naturalWidth && img.naturalHeight) {
                  const maxInitialSize = 420;
                  let w = img.naturalWidth;
                  let h = img.naturalHeight;
                  if (w > maxInitialSize || h > maxInitialSize) {
                    const ratio = Math.min(maxInitialSize / w, maxInitialSize / h);
                    w = Math.round(w * ratio);
                    h = Math.round(h * ratio);
                  }
                  card.width = w;
                  card.height = h;
                  card.aspectRatio = w / h;
                  if (card.element) {
                    card.element.style.width = `${w}px`;
                    card.element.style.height = `${h}px`;
                  }
                  try {
                    const cvs = document.createElement('canvas');
                    cvs.width = w;
                    cvs.height = h;
                    const ctx = cvs.getContext('2d');
                    ctx.drawImage(img, 0, 0);
                    const isPng = msg.localPath && msg.localPath.toLowerCase().endsWith('.png');
                    card.imageData = cvs.toDataURL(isPng ? 'image/png' : 'image/jpeg', 0.90);
                  } catch(e) {}
                }
              };
              img.src = (msg.imageData && msg.imageData.startsWith('data:image/')) ? msg.imageData : msg.localWebUrl;
            }
          }
          if (card.tagEl) card.tagEl.textContent = 'Cached (HD)';
          Toast.show('Pinterest reference loaded!', 'success');
        } else if (card && !msg.success) {
          Toast.show('Could not resolve image from Pinterest URL', 'error');
          this.removeCard(card);
        }
      } else if (msg.type === 'software_result') {
        Toast.show(msg.message, msg.success ? 'success' : 'error');
      } else if (msg.type === 'clipboard_result') {
        Toast.show(msg.success ? 'Copied image file to clipboard!' : 'Failed to copy to clipboard', msg.success ? 'success' : 'error');
      } else if (msg.type === 'frame_snapped') {
        if (msg.success) {
          const ytCard = this.cards.find(c => c.id === msg.cardId);
          const newX = ytCard ? (ytCard.x + ytCard.width + 30) : 0;
          const newY = ytCard ? ytCard.y : 0;
          const src = msg.localWebUrl || msg.imageData;
          this.addReferenceFromUrl(src, newX, newY, `Snap Frame (${ytCard ? ytCard.sourceLabel : 'Video'})`, {
            localPath: msg.localPath,
            localWebUrl: msg.localWebUrl,
            imageData: msg.imageData
          });
          if (msg.sendToAe) {
            Toast.show(msg.aeSuccess ? '📸 Frame berhasil di-capture dan di-import ke After Effects!' : '📸 Frame di-capture! Mengirim ke After Effects...', 'success', 3500);
          } else {
            Toast.show('📸 Frame berhasil di-capture ke DropBoard!', 'success', 2500);
          }
        } else {
          Toast.show('Gagal mengambil snapshot video', 'error');
        }
      } else if (msg.type === 'board_saved') {
        if (msg.filePath) {
          this.currentFilePath = msg.filePath;
          try { localStorage.setItem('dropboard_last_file_path', msg.filePath); } catch(e) {}
          const fileName = msg.filePath.replace(/^.*[\\\/]/, '').replace(/\.dropboard$/i, '');
          this.setProjectName(fileName, false);
        }
        Toast.show(msg.direct ? `Saved changes to "${this.projectName}"` : `Project saved: "${this.projectName}"`, 'success');
      } else if (msg.type === 'board_loaded') {
        if (!msg.success) {
          console.warn('board_loaded failed:', msg.error);
          const saved = localStorage.getItem('dropboard_autosave_state');
          if (saved) {
            try {
              const parsed = JSON.parse(saved);
              this.deserialize(parsed);
            } catch(e) {}
          }
          return;
        }
        if (msg.filePath) {
          if (!msg.filePath.toLowerCase().endsWith('session.dropboard')) {
            this.currentFilePath = msg.filePath;
            try { localStorage.setItem('dropboard_last_file_path', msg.filePath); } catch(e) {}
            const fileName = msg.filePath.replace(/^.*[\\\/]/, '').replace(/\.dropboard$/i, '');
            this.setProjectName(fileName, false);
          }
        }
        this.deserialize(msg.content, msg.filePath);
        Toast.show(msg.direct ? `Restored: ${this.projectName}` : `Loaded: ${this.projectName}`, 'success');
      } else if (msg.type === 'system_fonts_list' && Array.isArray(msg.fonts)) {
        this.systemFonts = msg.fonts;
      } else if (msg.type === 'cache_cleared') {
        Toast.show(`Cleaned ${msg.count} cached image(s) from disk`, 'success');
      } else if (msg.type === 'file_assoc_registered') {
        Toast.show('DropBoard file icon (.dropboard) registered & updated in Windows Explorer!', 'success');
      }
    });
  }

  initUIEvents() {
    // Frameless titlebar dragging - supports dragging from logo, brand, titlebar background, or drag-region
    const titlebar = document.getElementById('titlebar');
    if (titlebar) {
      titlebar.addEventListener('mousedown', (e) => {
        if (e.button !== 0) return;
        // Do not drag if clicking on interactive controls:
        if (e.target.closest('.tb-btn') || 
            e.target.closest('.window-control') || 
            e.target.closest('.project-title-wrapper') || 
            e.target.closest('.opacity-popover') ||
            e.target.closest('input') ||
            e.target.closest('button')) {
          return;
        }
        NativeBridge.startDrag();
      });
    }

    const appBrand = document.getElementById('app-brand-btn');
    if (appBrand) {
      appBrand.addEventListener('mousedown', (e) => {
        if (e.button === 0) {
          NativeBridge.startDrag();
        }
      });
    }

    const dragRegion = document.getElementById('drag-region');
    if (dragRegion) {
      dragRegion.addEventListener('mousedown', (e) => {
        if (e.button === 0) NativeBridge.startDrag();
      });
    }

    // Project title inline editing
    const projWrapper = document.getElementById('project-title-wrapper');
    const projDisplay = document.getElementById('project-name-display');
    if (projWrapper && projDisplay) {
      projWrapper.addEventListener('click', (e) => {
        e.stopPropagation();
        if (projDisplay.contentEditable !== 'true') {
          this.recordPreState('Rename Project');
          projDisplay.contentEditable = 'true';
          projDisplay.focus();
          const range = document.createRange();
          range.selectNodeContents(projDisplay);
          const sel = window.getSelection();
          sel.removeAllRanges();
          sel.addRange(range);
        }
      });

      const finishProjectRename = () => {
        if (projDisplay.contentEditable === 'true') {
          projDisplay.contentEditable = 'false';
          const newName = projDisplay.textContent.trim() || 'Untitled Project';
          this.setProjectName(newName);
          this.commitHistory('Rename Project');
        }
      };

      projDisplay.addEventListener('blur', finishProjectRename);
      projDisplay.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          projDisplay.blur();
        } else if (e.key === 'Escape') {
          projDisplay.textContent = this.projectName;
          projDisplay.contentEditable = 'false';
        }
      });
    }

    // Native Window edge & corner resize handles
    document.querySelectorAll('.win-resize-edge, .win-resize-corner').forEach(el => {
      el.addEventListener('mousedown', (e) => {
        if (e.button === 0) {
          e.preventDefault();
          const edge = el.dataset.edge;
          NativeBridge.startResize(edge);
        }
      });
    });

    // Window controls
    document.getElementById('btn-min').addEventListener('click', () => NativeBridge.minimize());
    document.getElementById('btn-max').addEventListener('click', () => NativeBridge.maximize());
    document.getElementById('btn-close').addEventListener('click', () => {
      this.saveAutoSave();
      NativeBridge.close();
    });
    window.addEventListener('beforeunload', () => {
      this.saveAutoSave();
    });

    // Pin (Always on Top)
    const pinBtn = document.getElementById('btn-pin');
    let isPinned = false;
    pinBtn.addEventListener('click', () => {
      isPinned = !isPinned;
      NativeBridge.setAlwaysOnTop(isPinned);
      pinBtn.classList.toggle('active', isPinned);
      Toast.show(isPinned ? 'Always on Top: ON' : 'Always on Top: OFF', 'info');
    });

    // Opacity Controls & Dual-Mode Transparency (Canvas BG Glass vs Window Global)
    const opacityToggle = document.getElementById('btn-opacity-toggle');
    const opacityPopover = document.getElementById('opacity-popover');
    const opacitySlider = document.getElementById('opacity-slider');
    const opacityLabel = document.getElementById('opacity-label');
    const btnResetOpacity = document.getElementById('btn-reset-opacity');

    const canvasBgSlider = document.getElementById('canvas-bg-slider');
    const canvasBgLabel = document.getElementById('canvas-bg-opacity-label');
    const canvasPresetBtns = opacityPopover.querySelectorAll('.canvas-preset-btn');
    const winPresetBtns = opacityPopover.querySelectorAll('.win-preset-btn');

    let currentWinOpacity = 100;
    let currentCanvasBgOpacity = 100;

    const updateToggleBadge = () => {
      if (currentWinOpacity < 100) {
        opacityLabel.textContent = `${currentWinOpacity}%`;
      } else if (currentCanvasBgOpacity < 100) {
        opacityLabel.textContent = `${currentCanvasBgOpacity}% BG`;
      } else {
        opacityLabel.textContent = '100%';
      }
      opacityToggle.classList.toggle('is-translucent', currentWinOpacity < 100 || currentCanvasBgOpacity < 100);
    };

    this.setCanvasBgOpacity = (val, save = true) => {
      const clamped = Math.max(0, Math.min(100, Math.round(val)));
      currentCanvasBgOpacity = clamped;
      if (canvasBgSlider) canvasBgSlider.value = clamped;
      if (canvasBgLabel) canvasBgLabel.textContent = `${clamped}%`;

      // Apply per-pixel alpha transparency directly to CSS variables
      const alpha = clamped / 100;
      document.documentElement.style.setProperty('--canvas-bg-alpha', alpha.toFixed(2));
      document.body.classList.toggle('canvas-clear-glass', clamped === 0);

      // Update preset active classes
      canvasPresetBtns.forEach(btn => {
        const pVal = parseInt(btn.dataset.canvasVal, 10);
        btn.classList.toggle('active', pVal === clamped);
      });

      updateToggleBadge();
      if (save) {
        try { localStorage.setItem('dropboard_canvas_bg_opacity', clamped); } catch (e) {}
      }
    };

    this.setWindowOpacity = (val) => {
      const clamped = Math.max(30, Math.min(100, Math.round(val)));
      currentWinOpacity = clamped;
      if (opacitySlider) opacitySlider.value = clamped;
      NativeBridge.setOpacity(clamped / 100);

      winPresetBtns.forEach(btn => {
        const pVal = parseInt(btn.dataset.val, 10);
        btn.classList.toggle('active', pVal === clamped);
      });

      updateToggleBadge();
    };

    // Load saved canvas background opacity preference
    try {
      const savedCanvasBg = localStorage.getItem('dropboard_canvas_bg_opacity');
      if (savedCanvasBg !== null) {
        const parsed = parseInt(savedCanvasBg, 10);
        if (!isNaN(parsed)) {
          this.setCanvasBgOpacity(parsed, false);
        }
      }
    } catch (e) {}

    opacityToggle.addEventListener('click', (e) => {
      e.stopPropagation();
      opacityPopover.classList.toggle('show');
    });

    opacityPopover.addEventListener('click', (e) => {
      e.stopPropagation();
    });

    // Double-click % button instantly resets both to 100% solid
    opacityToggle.addEventListener('dblclick', (e) => {
      e.stopPropagation();
      this.setWindowOpacity(100);
      this.setCanvasBgOpacity(100);
      Toast.show('Canvas & Window Opacity Reset to 100%', 'success');
    });

    if (btnResetOpacity) {
      btnResetOpacity.addEventListener('click', (e) => {
        e.stopPropagation();
        this.setWindowOpacity(100);
        Toast.show('Window Opacity Reset to 100%', 'success');
      });
    }

    canvasPresetBtns.forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const val = parseInt(btn.dataset.canvasVal, 10);
        this.setCanvasBgOpacity(val);
      });
    });

    if (canvasBgSlider) {
      canvasBgSlider.addEventListener('input', (e) => {
        const val = parseInt(e.target.value, 10);
        this.setCanvasBgOpacity(val);
      });
    }

    winPresetBtns.forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const val = parseInt(btn.dataset.val, 10);
        this.setWindowOpacity(val);
      });
    });

    if (opacitySlider) {
      opacitySlider.addEventListener('input', (e) => {
        const val = parseInt(e.target.value, 10);
        this.setWindowOpacity(val);
      });
    }

    document.addEventListener('click', (e) => {
      if (!opacityPopover.contains(e.target) && !opacityToggle.contains(e.target) && e.target !== opacityToggle) {
        opacityPopover.classList.remove('show');
      }
    });

    // HUD Zoom Buttons
    document.getElementById('btn-zoom-in').addEventListener('click', () => {
      this.canvas.zoomAt(window.innerWidth / 2, window.innerHeight / 2, 1.2);
    });
    document.getElementById('btn-zoom-out').addEventListener('click', () => {
      this.canvas.zoomAt(window.innerWidth / 2, window.innerHeight / 2, 0.83);
    });
    document.getElementById('btn-zoom-reset').addEventListener('click', () => {
      this.fitAllToView();
    });

    // Dock Buttons
    const btnAutoGrid = document.getElementById('btn-auto-grid');
    if (btnAutoGrid) btnAutoGrid.addEventListener('click', () => this.autoArrangeGrid());
    
    const btnPipeline = document.getElementById('btn-pipeline-arrange');
    if (btnPipeline) btnPipeline.addEventListener('click', () => this.autoArrangePipeline());

    // Auto-Arrange Gap & Spacing Controls
    const btnGapSettings = document.getElementById('btn-gap-settings');
    const gapPopover = document.getElementById('gap-popover');
    const gapSlider = document.getElementById('gap-slider');
    const gapLabel = document.getElementById('gap-btn-label');
    const gapBadge = document.getElementById('gap-value-display');

    const updateGapUI = (val) => {
      this.arrangeGap = Math.max(0, Math.min(100, parseInt(val) || 0));
      if (gapSlider) gapSlider.value = this.arrangeGap;
      const text = this.arrangeGap === 0 ? 'No Gap' : `${this.arrangeGap}px`;
      if (gapLabel) gapLabel.textContent = `Gap: ${text}`;
      if (gapBadge) gapBadge.textContent = text;
      try { localStorage.setItem('dropboard_arrange_gap', this.arrangeGap); } catch (e) {}

      if (gapPopover) {
        gapPopover.querySelectorAll('.gap-preset-btn').forEach(b => {
          b.classList.toggle('active', parseInt(b.dataset.gap) === this.arrangeGap);
        });
      }
    };

    updateGapUI(this.arrangeGap);

    if (btnGapSettings && gapPopover) {
      btnGapSettings.addEventListener('click', (e) => {
        e.stopPropagation();
        gapPopover.classList.toggle('show');
      });

      if (gapSlider) {
        gapSlider.addEventListener('input', (e) => {
          updateGapUI(e.target.value);
        });
      }

      gapPopover.querySelectorAll('.gap-preset-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
          e.stopPropagation();
          const targetGap = parseInt(btn.dataset.gap);
          updateGapUI(targetGap);
          gapPopover.classList.remove('show');
          Toast.show(`Arrange gap set to ${targetGap === 0 ? 'No Gap (0px)' : targetGap + 'px'}`, 'info', 1600);
        });
      });

      window.addEventListener('click', () => {
        gapPopover.classList.remove('show');
      });
    }

    // Request installed system fonts from native host and browser
    NativeBridge.getSystemFonts();
    if ('queryLocalFonts' in window) {
      window.queryLocalFonts().then(fonts => {
        if (Array.isArray(fonts) && fonts.length > 0) {
          const names = fonts.map(f => f.family || f.fullName).filter(Boolean);
          this.systemFonts = Array.from(new Set([...(this.systemFonts || []), ...names]));
        }
      }).catch(() => {});
    }
    
    const btnAddGroup = document.getElementById('btn-add-group');
    if (btnAddGroup) {
      btnAddGroup.addEventListener('click', () => {
        this.recordPreState('Create Group');
        const next = this.getNextSceneInfo();
        const center = this.canvas.screenToWorld(window.innerWidth / 2, window.innerHeight / 2);
        const bounds = { x: center.x - 230, y: center.y - 190, w: 460, h: 380 };
        
        // Auto-group selected cards or ungrouped cards sitting right where the group is placed
        const cardsToGroup = this.selectedCards && this.selectedCards.size > 0
          ? Array.from(this.selectedCards)
          : this.cards.filter(c => !c.groupId && (
              (c.x + c.width / 2) >= bounds.x && (c.x + c.width / 2) <= (bounds.x + bounds.w) &&
              (c.y + c.height / 2) >= bounds.y && (c.y + c.height / 2) <= (bounds.y + bounds.h)
            ));

        const grp = this.createGroup(next.title, bounds.x, bounds.y, bounds.w, bounds.h, next.color);
        if (cardsToGroup.length > 0) {
          cardsToGroup.forEach(c => {
            c.groupId = grp.id;
            this.updateCardGroupBadge(c);
          });
          this.updateGroupCounts();
          this.tidyGroup(grp, true);
        }
        this.commitHistory('Create Group');
        Toast.show(`Created ${next.title}`, 'success');
      });
    }

    // Creative Production Node Dropdown (+ Node)
    const btnAddNode = document.getElementById('btn-add-node');
    const nodeDropdown = document.getElementById('node-dropdown-menu');
    if (btnAddNode && nodeDropdown) {
      btnAddNode.addEventListener('click', (e) => {
        e.stopPropagation();
        nodeDropdown.classList.toggle('show');
      });

      nodeDropdown.querySelectorAll('.node-menu-item').forEach(item => {
        item.addEventListener('click', (e) => {
          e.stopPropagation();
          const type = item.dataset.nodeType || 'note';
          nodeDropdown.classList.remove('show');
          this.recordPreState('Add Production Node');
          const center = this.canvas.screenToWorld(window.innerWidth / 2, window.innerHeight / 2);
          const offset = (this.nodes.length % 5) * 24;
          this.createNode(type, Math.round(center.x - 130 + offset), Math.round(center.y - 80 + offset));
          this.commitHistory('Add Production Node');
          Toast.show(`Added ${item.querySelector('.node-menu-title').textContent} Node`, 'success');
        });
      });

      window.addEventListener('click', () => {
        nodeDropdown.classList.remove('show');
      });
    }

    document.getElementById('btn-clear-canvas').addEventListener('click', () => this.clearAll());
    
    // Project Save, Save As & Load
    const btnSave = document.getElementById('btn-save-board');
    if (btnSave) btnSave.addEventListener('click', () => this.saveProject(false));
    const btnSaveAs = document.getElementById('btn-save-as-board');
    if (btnSaveAs) btnSaveAs.addEventListener('click', () => this.saveProject(true));
    const btnLoad = document.getElementById('btn-load-board');
    if (btnLoad) btnLoad.addEventListener('click', () => NativeBridge.loadBoardDialog());

    // Dock Position Quick Switcher (Top Bar <-> Left Sidebar)
    const btnDockPosToggle = document.getElementById('btn-dock-pos-toggle');
    if (btnDockPosToggle) {
      btnDockPosToggle.addEventListener('click', () => {
        const nextPos = this.dockPosition === 'left' ? 'top' : 'left';
        this.setDockPosition(nextPos);
      });
    }

    // Dock Auto-Hide Pin / Unpin Quick Toggle
    const btnDockPin = document.getElementById('btn-dock-pin');
    if (btnDockPin) {
      btnDockPin.addEventListener('click', () => {
        this.setAutoHideDock(!this.autoHideDock);
      });
    }

    // Auto-Hide Dock Hover Trigger Zone & Leave Handling
    const dockHoverZone = document.getElementById('dock-hover-zone');
    const floatingDock = document.getElementById('floating-dock');
    if (dockHoverZone && floatingDock) {
      dockHoverZone.addEventListener('mouseenter', () => {
        if (this.autoHideDock) {
          floatingDock.classList.add('is-revealed');
        }
      });
      floatingDock.addEventListener('mouseleave', () => {
        if (this.autoHideDock) {
          floatingDock.classList.remove('is-revealed');
        }
      });
    }

    // Undo & Redo Dock buttons
    const btnUndo = document.getElementById('btn-undo');
    if (btnUndo) btnUndo.addEventListener('click', () => this.undo());
    const btnRedo = document.getElementById('btn-redo');
    if (btnRedo) btnRedo.addEventListener('click', () => this.redo());

    // URL Modal
    const urlModal = document.getElementById('url-modal');
    const urlInput = document.getElementById('url-input');
    const btnAddUrl = document.getElementById('btn-add-url');
    const btnCloseModal = document.getElementById('btn-close-modal');
    const btnSubmitUrl = document.getElementById('btn-submit-url');

    if (btnAddUrl) {
      btnAddUrl.addEventListener('click', () => {
        urlModal.classList.add('show');
        urlInput.value = '';
        setTimeout(() => urlInput.focus(), 100);
      });
    }

    if (btnCloseModal) {
      btnCloseModal.addEventListener('click', () => {
        urlModal.classList.remove('show');
      });
    }

    if (urlModal) {
      urlModal.addEventListener('click', (e) => {
        if (e.target === urlModal) {
          urlModal.classList.remove('show');
        }
      });
    }

    const submitUrlHandler = () => {
      const val = urlInput.value.trim();
      if (val) {
        urlModal.classList.remove('show');
        const pos = this.canvas.screenToWorld(window.innerWidth / 2, window.innerHeight / 2);
        this.addReferenceFromUrl(val, pos.x, pos.y, val.includes('pinterest') ? 'Pinterest' : 'Web');
      }
    };

    if (btnSubmitUrl) btnSubmitUrl.addEventListener('click', submitUrlHandler);
    if (urlInput) {
      urlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') submitUrlHandler();
        if (e.key === 'Escape') urlModal.classList.remove('show');
      });
    }

    // Quick Dock Integrations
    document.getElementById('btn-quick-ae').addEventListener('click', () => {
      this.exportSelectionToAfterEffects();
    });

    document.getElementById('btn-quick-ps').addEventListener('click', () => {
      const c = this.selectedCard;
      if (c && (c.localPath || c.imageData)) {
        NativeBridge.sendToPhotoshop(c.localPath || '', c.imageData || '');
      } else {
        Toast.show('Select a reference card first', 'error');
      }
    });

    document.getElementById('btn-quick-copy').addEventListener('click', () => {
      const c = this.selectedCard;
      if (c && (c.localPath || c.imageData)) {
        NativeBridge.copyFileToClipboard(c.localPath || '', c.imageData || '');
      } else {
        Toast.show('Select a reference card first', 'error');
      }
    });

    // Add file fallback
    const fileInput = document.getElementById('file-input');
    document.getElementById('btn-add-file').addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', (e) => {
      Array.from(e.target.files).forEach((file) => this.handleLocalFile(file));
      fileInput.value = '';
    });

    // Global Keyboard Shortcuts (Undo, Redo, Delete)
    window.addEventListener('keydown', (e) => {
      // Don't trigger shortcuts when user is typing in notes or input fields
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) {
        return;
      }

      // Undo: Ctrl+Z
      // Redo: Ctrl+Shift+Z or Ctrl+Y
      if (e.ctrlKey || e.metaKey) {
        // Opacity Reset shortcut: Ctrl+Alt+0 or Ctrl+Shift+O
        if ((e.altKey && (e.key === '0' || e.code === 'Digit0')) || (e.shiftKey && (e.key === 'o' || e.key === 'O'))) {
          e.preventDefault();
          this.setWindowOpacity(100);
          Toast.show('Window Opacity Reset to 100%', 'success');
          return;
        }

        if (e.key === 'z' || e.key === 'Z') {
          e.preventDefault();
          if (e.shiftKey) {
            this.redo();
          } else {
            this.undo();
          }
          return;
        } else if (e.key === 'y' || e.key === 'Y') {
          e.preventDefault();
          this.redo();
          return;
        }
      }

      // Duplicate: Ctrl+D
      if ((e.ctrlKey || e.metaKey) && (e.key === 'd' || e.key === 'D')) {
        e.preventDefault();
        this.duplicateSelection();
        return;
      }

      // Select All: Ctrl+A
      if ((e.ctrlKey || e.metaKey) && (e.key === 'a' || e.key === 'A')) {
        e.preventDefault();
        this.selectMultiple({
          cards: this.cards,
          groups: this.groups,
          nodes: this.nodes
        });
        const total = this.cards.length + this.groups.length + this.nodes.length;
        Toast.show(`Selected all ${total} items`, 'info');
        return;
      }

      // Delete selected reference card(s), group(s), or node(s)
      if (e.key === 'Delete' || e.key === 'Backspace') {
        const hasCards = this.selectedCards.size > 0 || this.selectedCard;
        const hasGroups = this.selectedGroups.size > 0 || this.selectedGroup;
        const hasNodes = this.selectedNodes.size > 0 || this.selectedNode;

        if (hasCards || hasGroups || hasNodes) {
          this.recordPreState('Delete Selection');
          const cardsToDelete = this.selectedCards.size > 0 ? Array.from(this.selectedCards) : (this.selectedCard ? [this.selectedCard] : []);
          const groupsToDelete = this.selectedGroups.size > 0 ? Array.from(this.selectedGroups) : (this.selectedGroup ? [this.selectedGroup] : []);
          const nodesToDelete = this.selectedNodes.size > 0 ? Array.from(this.selectedNodes) : (this.selectedNode ? [this.selectedNode] : []);

          nodesToDelete.forEach(n => this.removeNode(n));
          groupsToDelete.forEach(g => {
            this.cards.filter(c => c.groupId === g.id).forEach(c => {
              c.groupId = null;
              this.updateCardGroupBadge(c);
            });
            if (g.element) g.element.remove();
            this.groups = this.groups.filter(grp => grp.id !== g.id);
          });
          cardsToDelete.forEach(c => {
            if (c.element) c.element.remove();
            this.cards = this.cards.filter(card => card.id !== c.id);
          });

          this.clearSelection();
          this.updateCardCount();
          this.updateGroupCounts();
          this.renderConnections();
          this.commitHistory('Delete Selection');
          const totalDel = cardsToDelete.length + groupsToDelete.length + nodesToDelete.length;
          Toast.show(`Deleted ${totalDel} selected item(s)`, 'info');
          return;
        }
      }

      // Blender-style Quick Add: Ctrl+Space or Shift+A
      if ((e.ctrlKey && e.code === 'Space') || (e.shiftKey && e.code === 'KeyA')) {
        const tag = document.activeElement ? document.activeElement.tagName : '';
        const isEditing = tag === 'INPUT' || tag === 'TEXTAREA' || (document.activeElement && document.activeElement.isContentEditable);
        if (!isEditing || (e.ctrlKey && e.code === 'Space')) {
          e.preventDefault();
          this.openBlenderSearch(this.currentMouseScreenX, this.currentMouseScreenY);
          return;
        }
      }

      // New Project: Ctrl+N
      if ((e.ctrlKey || e.metaKey) && (e.key === 'n' || e.key === 'N')) {
        e.preventDefault();
        if (this.openNewProjectModal) this.openNewProjectModal();
        return;
      }

      // Save Project: Ctrl+S (Direct Save) / Save As: Ctrl+Shift+S
      if ((e.ctrlKey || e.metaKey) && (e.key === 's' || e.key === 'S')) {
        e.preventDefault();
        this.saveProject(e.shiftKey);
        return;
      }

      // Open Project: Ctrl+O
      if ((e.ctrlKey || e.metaKey) && (e.key === 'o' || e.key === 'O')) {
        e.preventDefault();
        NativeBridge.loadBoardDialog();
        return;
      }

      // Escape to close modals or deselect active items
      if (e.key === 'Escape') {
        const urlModal = document.getElementById('url-modal');
        if (urlModal && urlModal.classList.contains('show')) {
          urlModal.classList.remove('show');
          return;
        }
        const settingsModal = document.getElementById('settings-modal');
        if (settingsModal && (settingsModal.classList.contains('show') || settingsModal.style.display !== 'none')) {
          settingsModal.classList.remove('show');
          settingsModal.style.display = 'none';
          return;
        }
        const newProjModal = document.getElementById('new-project-modal');
        if (newProjModal && (newProjModal.classList.contains('show') || newProjModal.style.display !== 'none')) {
          newProjModal.classList.remove('show');
          newProjModal.style.display = 'none';
          return;
        }
        const popup = document.getElementById('blender-search-popup');
        if (popup && popup.classList.contains('show')) {
          popup.classList.remove('show');
          return;
        }
        this.clearSelection();
      }
    });

    window.addEventListener('mousemove', (e) => {
      this.currentMouseScreenX = e.clientX;
      this.currentMouseScreenY = e.clientY;
    });
  }

  // ===========================================================================
  // Smart Drag & Drop + Pinterest URL Resolution
  // ===========================================================================
  initDropAndPaste() {
    const vp = this.viewport;

    vp.addEventListener('dragover', (e) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = 'copy';
    });

    vp.addEventListener('drop', (e) => {
      e.preventDefault();
      const dropWorldPos = this.canvas.screenToWorld(e.clientX, e.clientY);

      // Check if dragging files from Windows Explorer
      if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
        Array.from(e.dataTransfer.files).forEach((file, index) => {
          this.handleLocalFile(file, dropWorldPos.x + index * 40, dropWorldPos.y + index * 40);
        });
        return;
      }

      // Check HTML snippet (Pinterest and web browsers often provide <img> or <a> in text/html)
      const html = e.dataTransfer.getData('text/html');
      if (html) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const img = doc.querySelector('img');
        if (img) {
          const imgSrc = img.src || img.dataset.src || (img.srcset ? img.srcset.split(',')[0].trim().split(' ')[0] : null);
          if (imgSrc) {
            const highResUrl = this.resolvePinterestHighRes(imgSrc);
            this.addReferenceFromUrl(highResUrl, dropWorldPos.x, dropWorldPos.y, 'Pinterest');
            return;
          }
        }
        const link = doc.querySelector('a');
        if (link && link.href && (link.href.includes('pinterest.com/pin/') || link.href.includes('pin.it/'))) {
          this.addReferenceFromUrl(link.href, dropWorldPos.x, dropWorldPos.y, 'Pinterest Pin');
          return;
        }
      }

      // Check URI List / Plain Text URL
      const uri = e.dataTransfer.getData('text/uri-list') || e.dataTransfer.getData('text/plain');
      if (uri && (uri.startsWith('http://') || uri.startsWith('https://') || uri.startsWith('data:image/'))) {
        const highResUrl = this.resolvePinterestHighRes(uri);
        this.addReferenceFromUrl(highResUrl, dropWorldPos.x, dropWorldPos.y, uri.includes('pinterest') ? 'Pinterest' : 'Web');
      }
    });

    // Clipboard Paste (Ctrl+V)
    window.addEventListener('paste', (e) => {
      const items = e.clipboardData.items;
      for (let i = 0; i < items.length; i++) {
        if (items[i].type.indexOf('image') !== -1) {
          const blob = items[i].getAsFile();
          const shouldSendAE = !!this.autoSendNextPasteToAE;
          this.autoSendNextPasteToAE = false;

          this.handleLocalFile(blob, 0, 0, (dataUrl, nativePath) => {
            if (shouldSendAE) {
              NativeBridge.sendToAE(nativePath || '', dataUrl || '');
              Toast.show('📸 Snapshot video berhasil dikirim ke After Effects!', 'success', 3500);
            }
          });

          if (!shouldSendAE) {
            Toast.show('Pasted image from clipboard', 'success');
          }
          return;
        }
      }

      const text = e.clipboardData.getData('text');
      if (text && (text.startsWith('http://') || text.startsWith('https://'))) {
        const highResUrl = this.resolvePinterestHighRes(text);
        this.addReferenceFromUrl(highResUrl, 0, 0, text.includes('pinterest') ? 'Pinterest' : 'Pasted URL');
      }
    });
  }

  /**
   * Upgrades Pinterest thumbnail URLs to 736x high-resolution previews:
   * e.g., i.pinimg.com/236x/... or /474x/... -> i.pinimg.com/736x/...
   */
  resolvePinterestHighRes(url) {
    if (url.includes('pinimg.com/')) {
      return url.replace(/\/(236x|474x|564x)\//, '/736x/');
    }
    return url;
  }

  handleLocalFile(file, worldX = 0, worldY = 0, onDoneCallback = null) {
    if (!file.type.startsWith('image/')) return;

    const nativePath = file.path || '';
    const reader = new FileReader();
    reader.onload = (e) => {
      const dataUrl = e.target.result;
      this.addReferenceFromUrl(dataUrl, worldX, worldY, file.name || 'Local File', {
        localPath: nativePath
      });
      if (onDoneCallback) onDoneCallback(dataUrl, nativePath);
    };
    reader.readAsDataURL(file);
  }

  // ===========================================================================
  // Reference Card Creation & Manipulation
  // ===========================================================================
  addReferenceFromUrl(url, x = 0, y = 0, sourceLabel = 'Web', extraMeta = {}) {
    const id = 'ref_' + Date.now() + '_' + Math.random().toString(36).substr(2, 6);

    // Check if it's a YouTube link
    const ytMatch = url.match(/(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?|shorts)\/|.*[?&]v=)|youtu\.be\/)([^"&?\/\s]{11})/i);
    const isYouTube = !!extraMeta.isYouTube || !!ytMatch;
    const ytVideoId = extraMeta.youtubeId || (ytMatch ? ytMatch[1] : null);
    const ytVideoUrl = extraMeta.youtubeUrl || (ytVideoId ? `https://www.youtube.com/watch?v=${ytVideoId}` : '');
    let targetImgUrl = url;

    if (isYouTube && ytVideoId) {
      targetImgUrl = (extraMeta.url && extraMeta.url.includes('img.youtube.com')) 
        ? extraMeta.url 
        : `https://img.youtube.com/vi/${ytVideoId}/maxresdefault.jpg`;
      sourceLabel = 'YouTube Ref';
    }

    // If it is a Pinterest Pin link (web page), resolve via C++ native bridge first!
    if (!isYouTube && (url.includes('pinterest.com/pin/') || url.includes('pin.it/'))) {
      Toast.show('Resolving Pinterest Pin...', 'info');

      const card = {
        id,
        url,
        localPath: '',
        isYouTube: false,
        x: x || (Math.random() * 200 - 100),
        y: y || (Math.random() * 200 - 100),
        width: 320,
        height: 380,
        aspectRatio: 320 / 380,
        zIndex: ++this.highestZ,
        sourceLabel: 'Pinterest Pin',
        element: null
      };

      const placeholderImg = new Image();
      placeholderImg.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="320" height="380" viewBox="0 0 320 380"><rect width="320" height="380" rx="8" fill="%23171b24"/><text x="50%" y="50%" fill="%2360a5fa" font-family="sans-serif" font-size="13" text-anchor="middle">Loading Pinterest Pin...</text></svg>';
      this.createCardDOM(card, placeholderImg);
      this.cards.push(card);
      this.selectCard(card);
      this.updateCardCount();

      NativeBridge.downloadImage(url, id);
      return;
    }

    // If localWebUrl is already available from C++ cache, prioritize it!
    if (extraMeta.localWebUrl) {
      targetImgUrl = extraMeta.localWebUrl;
    }

    const img = new Image();
    if (targetImgUrl.includes('dropboard-cache.local')) {
      img.crossOrigin = 'anonymous';
    }
    img.src = targetImgUrl;

    img.onload = () => {
      let width = img.naturalWidth || 320;
      let height = img.naturalHeight || 240;

      // Scale down oversized images for initial viewport comfort
      const maxInitialSize = 420;
      if (width > maxInitialSize || height > maxInitialSize) {
        const ratio = Math.min(maxInitialSize / width, maxInitialSize / height);
        width = Math.round(width * ratio);
        height = Math.round(height * ratio);
      }

      const card = {
        id,
        url: targetImgUrl,
        imageData: targetImgUrl.startsWith('data:image/') ? targetImgUrl : (extraMeta.imageData || null),
        localPath: extraMeta.localPath || '',
        localWebUrl: extraMeta.localWebUrl || '',
        isYouTube,
        youtubeId: ytVideoId,
        youtubeUrl: ytVideoUrl,
        originalUrl: extraMeta.originalUrl || url,
        crop: { top: 0, right: 0, bottom: 0, left: 0 },
        x: x || (Math.random() * 200 - 100),
        y: y || (Math.random() * 200 - 100),
        width,
        height,
        aspectRatio: width / height,
        zIndex: ++this.highestZ,
        sourceLabel,
        element: null
      };

      this.createCardDOM(card, img);
      this.cards.push(card);
      this.selectCard(card);
      this.updateCardCount();

      // Pre-extract Base64 imageData immediately when image loads so saving is instant
      if (!card.imageData && !card.isYouTube) {
        try {
          const cvs = document.createElement('canvas');
          cvs.width = width;
          cvs.height = height;
          const ctx = cvs.getContext('2d');
          ctx.drawImage(img, 0, 0);
          const isPng = (targetImgUrl && targetImgUrl.toLowerCase().endsWith('.png')) || (card.localPath && card.localPath.toLowerCase().endsWith('.png'));
          card.imageData = cvs.toDataURL(isPng ? 'image/png' : 'image/jpeg', 0.90);
        } catch(e) {}
      }

      // Trigger native download to cache folder if not already cached
      if (!card.localPath && (targetImgUrl.startsWith('http://') || targetImgUrl.startsWith('https://')) && !targetImgUrl.includes('dropboard-cache.local')) {
        NativeBridge.downloadImage(targetImgUrl, id);
      }
    };

    img.onerror = () => {
      // If web URL failed and we have local cached file, fallback to it
      if (extraMeta.localWebUrl && img.src !== extraMeta.localWebUrl) {
        img.src = extraMeta.localWebUrl;
        targetImgUrl = extraMeta.localWebUrl;
        return;
      }
      // If YouTube maxresdefault thumbnail fails, fallback to hqdefault
      if (isYouTube && ytVideoId && !img.dataset.fallbackTried) {
        img.dataset.fallbackTried = 'true';
        img.src = `https://img.youtube.com/vi/${ytVideoId}/hqdefault.jpg`;
        targetImgUrl = img.src;
        return;
      }
      Toast.show('Failed to load image from URL', 'error');
    };
  }

  createCardDOM(card, imgElement) {
    card.baseWidth = card.baseWidth || card.width;
    card.baseHeight = card.baseHeight || card.height;
    card.baseX = card.baseX !== undefined ? card.baseX : card.x;
    card.baseY = card.baseY !== undefined ? card.baseY : card.y;

    const cardEl = document.createElement('div');
    cardEl.className = 'ref-card';
    cardEl.id = card.id;
    cardEl.style.width = `${card.width}px`;
    cardEl.style.height = `${card.height}px`;
    cardEl.style.transform = `translate(${card.x}px, ${card.y}px)`;
    cardEl.style.zIndex = card.zIndex;

    // Image Media Container
    const mediaContainer = document.createElement('div');
    mediaContainer.className = 'card-media-container';
    mediaContainer.appendChild(imgElement);
    cardEl.appendChild(mediaContainer);
    card.mediaContainer = mediaContainer;

    // Attached Group Badge
    this.updateCardGroupBadge(card, cardEl);

    // YouTube Badge
    if (card.isYouTube) {
      const ytBadge = document.createElement('div');
      ytBadge.className = 'card-yt-badge';
      ytBadge.innerHTML = `
        <svg viewBox="0 0 24 24" width="12" height="12" fill="#ff0033"><path d="M19.615 3.184c-3.604-.246-11.631-.245-15.23 0-3.897.266-4.356 2.62-4.385 8.816.029 6.185.484 8.549 4.385 8.816 3.6.245 11.626.246 15.23 0 3.897-.266 4.356-2.62 4.385-8.816zm-10.615 12.816v-8l8 3.993-8 4.007z"/></svg>
        <span>YouTube</span>
      `;
      cardEl.appendChild(ytBadge);
    }

    // YouTube Center Play Icon Button & Header Bar
    if (card.isYouTube) {
      const centerPlay = document.createElement('div');
      centerPlay.className = 'card-yt-center-play';
      centerPlay.title = 'Play in DropBoard';
      centerPlay.innerHTML = '<svg viewBox="0 0 24 24"><polygon points="6 4 20 12 6 20 6 4"/></svg>';
      centerPlay.addEventListener('click', (e) => {
        e.stopPropagation();
        this.toggleYouTubePlayback(card);
      });
      mediaContainer.appendChild(centerPlay);
      card.centerPlayBtn = centerPlay;

      const topDragBar = document.createElement('div');
      topDragBar.className = 'card-top-drag-bar';
      topDragBar.title = 'Drag to move video card';
      cardEl.appendChild(topDragBar);
    }

    // Overlay Quick Action Buttons (Unified Card Toolbar)
    const overlay = document.createElement('div');
    overlay.className = 'card-overlay';
    card.overlay = overlay;

    const dragGrip = document.createElement('div');
    dragGrip.className = 'card-drag-grip';
    dragGrip.title = 'Drag to move reference card';
    dragGrip.innerHTML = `
      <svg viewBox="0 0 24 24" width="11" height="11" fill="currentColor"><circle cx="8" cy="6" r="1.5"/><circle cx="15" cy="6" r="1.5"/><circle cx="8" cy="12" r="1.5"/><circle cx="15" cy="12" r="1.5"/><circle cx="8" cy="18" r="1.5"/><circle cx="15" cy="18" r="1.5"/></svg>
      <span>Move</span>
    `;
    overlay.appendChild(dragGrip);

    if (card.isYouTube) {
      const btnPlay = document.createElement('button');
      btnPlay.className = 'card-action-btn btn-yt-play';
      btnPlay.title = 'Play/Stop Video in DropBoard';
      btnPlay.innerHTML = '▶ Play';
      btnPlay.addEventListener('click', (e) => {
        e.stopPropagation();
        this.toggleYouTubePlayback(card);
      });
      overlay.appendChild(btnPlay);
      card.btnPlayOverlay = btnPlay;

      const btnSnapAe = document.createElement('button');
      btnSnapAe.className = 'card-action-btn btn-yt-snap-ae';
      btnSnapAe.title = 'Screenshot clean video frame directly to After Effects';
      btnSnapAe.innerHTML = '<svg viewBox="0 0 24 24" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:2px;vertical-align:-1px;"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/><circle cx="12" cy="13" r="4"/></svg>Snap to Ae';
      btnSnapAe.addEventListener('click', (e) => {
        e.stopPropagation();
        this.snapYouTubeFrameToAE(card);
      });
      overlay.appendChild(btnSnapAe);
      card.btnSnapAe = btnSnapAe;

      const btnBrowser = document.createElement('button');
      btnBrowser.className = 'card-action-btn btn-yt-ext';
      btnBrowser.title = 'Open in External Browser';
      btnBrowser.innerHTML = '↗ Browser';
      btnBrowser.addEventListener('click', (e) => {
        e.stopPropagation();
        if (card.youtubeUrl) {
          NativeBridge.openDefault(card.youtubeUrl);
        }
      });
      overlay.appendChild(btnBrowser);
    }

    const btnAE = document.createElement('button');
    btnAE.className = 'card-action-btn btn-ae-quick';
    btnAE.title = 'Send to Adobe After Effects';
    btnAE.innerHTML = '<strong>Ae</strong> Import';
    btnAE.addEventListener('click', (e) => {
      e.stopPropagation();
      const selCards = Array.from(this.selectedCards || []);
      if (this.selectedCard && !selCards.includes(this.selectedCard)) selCards.push(this.selectedCard);
      
      if (selCards.length > 1) {
        this.exportSelectionToAfterEffects();
      } else {
        if (card.localPath || card.imageData) {
          NativeBridge.sendToAE(card.localPath || '', card.imageData || this.getCardImageData(card) || '');
        } else {
          Toast.show('Caching high-res image...', 'info');
        }
      }
    });

    const btnCrop = document.createElement('button');
    btnCrop.className = 'card-action-btn crop-action-btn';
    btnCrop.title = 'Crop / Mask Reference';
    btnCrop.innerHTML = '<svg viewBox="0 0 24 24" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:3px;vertical-align:-1px;"><circle cx="6" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><line x1="20" y1="4" x2="8.12" y2="15.88"/><line x1="14.47" y1="14.48" x2="20" y2="20"/><line x1="8.12" y1="8.12" x2="12" y2="12"/></svg>Crop';
    btnCrop.addEventListener('click', (e) => {
      e.stopPropagation();
      this.startCropCard(card);
    });

    const btnCopy = document.createElement('button');
    btnCopy.className = 'card-action-btn copy-action-btn';
    btnCopy.title = 'Copy Image File';
    btnCopy.innerHTML = 'Copy';
    btnCopy.addEventListener('click', (e) => {
      e.stopPropagation();
      if (card.localPath) {
        NativeBridge.copyFileToClipboard(card.localPath);
      } else {
        Toast.show('Image still downloading...', 'info');
      }
    });

    const btnDel = document.createElement('button');
    btnDel.className = 'card-action-btn';
    btnDel.title = 'Delete or Close';
    btnDel.innerHTML = '✕';
    btnDel.addEventListener('click', (e) => {
      e.stopPropagation();
      if (card.isPlayingYouTube) {
        this.stopYouTubeCard(card);
      } else {
        this.removeCard(card);
      }
    });

    overlay.appendChild(btnAE);
    overlay.appendChild(btnCrop);
    overlay.appendChild(btnCopy);
    overlay.appendChild(btnDel);
    cardEl.appendChild(overlay);

    // Double click on YouTube card toggles inline playback
    if (card.isYouTube) {
      cardEl.addEventListener('dblclick', (e) => {
        if (e.target.closest('.card-overlay')) return;
        e.stopPropagation();
        this.toggleYouTubePlayback(card);
      });
    }

    // Tag
    const tagEl = document.createElement('div');
    tagEl.className = 'card-tag';
    tagEl.textContent = card.sourceLabel;
    cardEl.appendChild(tagEl);
    card.tagEl = tagEl;

    // Resize Handles
    ['nw', 'ne', 'sw', 'se'].forEach((dir) => {
      const handle = document.createElement('div');
      handle.className = `resize-handle handle-${dir}`;
      handle.dataset.dir = dir;
      cardEl.appendChild(handle);
      this.initResizeHandle(handle, card);
    });

    // Card Dragging
    this.initCardDrag(cardEl, card);

    // Context menu trigger
    cardEl.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.showContextMenu(e.clientX, e.clientY, card);
    });

    card.element = cardEl;
    this.world.appendChild(cardEl);
    this.applyCropTransform(card);
  }

  toggleYouTubePlayback(card) {
    if (!card || !card.isYouTube) return;
    if (card.isPlayingYouTube) {
      this.stopYouTubeCard(card);
    } else {
      this.playYouTubeCard(card);
    }
  }

  playYouTubeCard(card) {
    if (!card || !card.youtubeId || !card.mediaContainer) return;
    card.isPlayingYouTube = true;

    if (card.element) {
      card.element.classList.add('is-yt-playing');
    }

    if (card.btnPlayOverlay) {
      card.btnPlayOverlay.innerHTML = '⏹ Stop';
      card.btnPlayOverlay.classList.add('is-playing');
      card.btnPlayOverlay.title = 'Stop Video Player';
    }

    // Hide static thumbnail image
    const img = card.mediaContainer.querySelector('img');
    if (img) img.style.display = 'none';

    // Remove any previous iframe instance
    const oldIframe = card.mediaContainer.querySelector('.card-yt-iframe');
    if (oldIframe) {
      oldIframe.src = 'about:blank';
      oldIframe.remove();
    }

    // Create YouTube embed iframe
    const iframe = document.createElement('iframe');
    iframe.className = 'card-yt-iframe';
    iframe.src = `https://www.youtube-nocookie.com/embed/${card.youtubeId}?autoplay=1&enablejsapi=1#cardId=${card.id}`;
    iframe.title = 'YouTube Video Player';
    iframe.setAttribute('allow', 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share');
    iframe.setAttribute('referrerpolicy', 'strict-origin-when-cross-origin');
    iframe.setAttribute('allowfullscreen', 'true');
    card.mediaContainer.appendChild(iframe);
    card.ytIframe = iframe;

    // Auto-hide toolbar overlay on inactivity when playing (like media players)
    if (card.overlay) {
      const showOverlay = () => {
        if (!card.overlay) return;
        card.overlay.classList.add('is-visible');
        clearTimeout(card.overlayHideTimer);
        card.overlayHideTimer = setTimeout(() => {
          if (card.overlay && !card.overlay.matches(':hover')) {
            card.overlay.classList.remove('is-visible');
          }
        }, 2200);
      };

      if (!card.overlayMouseMoveBound) {
        card.overlayMouseMoveBound = true;
        card.element.addEventListener('mousemove', showOverlay);
        card.element.addEventListener('mouseleave', () => {
          clearTimeout(card.overlayHideTimer);
          if (card.overlay && !card.overlay.matches(':hover')) {
            card.overlay.classList.remove('is-visible');
          }
        });
      }

      showOverlay();
    }

    Toast.show('Playing YouTube in DropBoard', 'info');
  }

  stopYouTubeCard(card) {
    if (!card) return;
    card.isPlayingYouTube = false;

    if (card.overlay) {
      clearTimeout(card.overlayHideTimer);
      card.overlay.classList.remove('is-visible');
      card.overlay.style.opacity = '';
      card.overlay.style.pointerEvents = '';
    }

    if (card.element) {
      card.element.classList.remove('is-yt-playing');
    }

    if (card.btnPlayOverlay) {
      card.btnPlayOverlay.innerHTML = '▶ Play';
      card.btnPlayOverlay.classList.remove('is-playing');
      card.btnPlayOverlay.title = 'Play in DropBoard';
    }

    if (card.mediaContainer) {
      const iframes = card.mediaContainer.querySelectorAll('.card-yt-iframe');
      iframes.forEach(f => {
        f.src = 'about:blank';
        f.remove();
      });
      const img = card.mediaContainer.querySelector('img');
      if (img) img.style.display = '';
    }
    card.ytIframe = null;
  }

  snapYouTubeFrameToAE(card) {
    if (!card || !card.element) return;
    const mediaEl = card.mediaContainer || card.element;
    const rect = mediaEl.getBoundingClientRect();

    // 1. Temporarily hide toolbar overlay and bring card to front
    const prevZIndex = card.element.style.zIndex;
    card.element.style.zIndex = '9999';
    if (card.overlay) {
      card.overlay.classList.remove('is-visible');
      card.overlay.style.opacity = '0';
      card.overlay.style.pointerEvents = 'none';
    }

    let handled = false;
    const restoreUI = () => {
      card.element.style.zIndex = prevZIndex || '';
      if (card.overlay) {
        card.overlay.style.opacity = '';
        card.overlay.style.pointerEvents = '';
      }
    };

    // 2. Listen for clean direct video canvas frame from injected script in YouTube iframe
    const onFrameMessage = (e) => {
      if (e.data && e.data.type === 'DROPBOARD_YT_FRAME_RESULT' && e.data.cardId === card.id) {
        window.removeEventListener('message', onFrameMessage);
        clearTimeout(fallbackTimer);
        handled = true;
        restoreUI();
        NativeBridge.snapVideoFrame(
          card.id,
          Math.round(rect.left),
          Math.round(rect.top),
          Math.round(rect.width),
          Math.round(rect.height),
          true,
          e.data.dataUrl
        );
      }
    };
    window.addEventListener('message', onFrameMessage);

    // 3. Request clean video canvas frame from the YouTube iframe
    if (card.ytIframe && card.ytIframe.contentWindow) {
      try {
        card.ytIframe.contentWindow.postMessage({
          type: 'DROPBOARD_CAPTURE_YT_FRAME',
          cardId: card.id,
          sendToAe: true
        }, '*');
      } catch(err) {}
    }

    // 4. Fallback: if iframe does not reply within 180ms, capture screen cleanly without header
    const fallbackTimer = setTimeout(() => {
      if (handled) return;
      window.removeEventListener('message', onFrameMessage);
      NativeBridge.snapVideoFrame(
        card.id,
        Math.round(rect.left),
        Math.round(rect.top),
        Math.round(rect.width),
        Math.round(rect.height),
        true,
        ''
      );
      setTimeout(restoreUI, 250);
    }, 180);

    Toast.show('📸 Mengambil clean snapshot video ke After Effects...', 'info', 1800);
  }

  updateCardGroupBadge(card, cardEl = card.element) {
    if (!cardEl) return;
    // Remove legacy floating circle badge if any
    const legacyBadge = cardEl.querySelector('.card-group-pill');
    if (legacyBadge) legacyBadge.remove();

    const tagEl = cardEl.querySelector('.card-tag') || card.tagEl;
    if (!tagEl) return;

    if (card.groupId) {
      const group = this.groups.find(g => g.id === card.groupId);
      if (group) {
        tagEl.classList.add('is-grouped');
        tagEl.style.setProperty('--group-color', group.color || '#3b82f6');
        tagEl.title = `Group: ${group.title} (Click ✕ to unlock)`;
        tagEl.innerHTML = `
          <span class="card-tag-text">${card.sourceLabel || 'Reference'}</span>
          <span class="card-tag-detach" title="Unlock from ${group.title}">✕</span>
        `;
        const btnDetach = tagEl.querySelector('.card-tag-detach');
        if (btnDetach) {
          btnDetach.onclick = (e) => {
            e.stopPropagation();
            this.recordPreState('Detach from Group');
            const prevGroup = this.groups.find(g => g.id === card.groupId);
            card.groupId = null;
            this.updateCardGroupBadge(card);
            this.updateGroupCounts();
            if (prevGroup) this.tidyGroup(prevGroup, true);
            this.commitHistory('Detach from Group');
            Toast.show(`Unlocked from ${group.title}`, 'info');
          };
        }
        return;
      }
    }

    tagEl.classList.remove('is-grouped');
    tagEl.style.removeProperty('--group-color');
    tagEl.title = card.sourceLabel || '';
    tagEl.textContent = card.sourceLabel || 'Reference';
  }

  initCardDrag(cardEl, card) {
    let isDragging = false;
    let startMouseX = 0;
    let startMouseY = 0;
    let initialCardX = 0;
    let initialCardY = 0;
    let currentHoverGroup = null;

    cardEl.addEventListener('mousedown', (e) => {
      if (e.target.classList.contains('resize-handle') || 
          e.target.closest('button') || 
          e.target.closest('input') || 
          e.target.closest('a') || 
          e.target.closest('.card-group-pill') || 
          e.target.closest('.crop-overlay-editor') || 
          e.button !== 0 ||
          this.canvas.spacePressed || e.altKey || this.canvas.isPanning) {
        return;
      }

      const isMulti = (e.shiftKey || e.ctrlKey || e.metaKey);
      if (isMulti) {
        this.selectCard(card, true);
      } else if (!this.selectedCards.has(card)) {
        this.selectCard(card, false);
      }

      startMouseX = e.clientX;
      startMouseY = e.clientY;
      initialCardX = card.x;
      initialCardY = card.y;
      e.stopPropagation();

      let isDragging = false;
      let hasStartedMoving = false;
      let dragRafPending = false;
      let lastMoveEvt = null;

      let movingCards = null;
      let startPositions = null;
      let movingGroups = null;
      let startGroupPositions = null;
      let movingNodes = null;
      let startNodePositions = null;

      const initDragState = () => {
        hasStartedMoving = true;
        isDragging = true;
        this.recordPreState('Move Reference');

        const iframes = document.querySelectorAll('.card-yt-iframe');
        iframes.forEach(f => f.style.pointerEvents = 'none');
        document.body.classList.add('is-dragging-card');

        movingCards = this.selectedCards.has(card) && this.selectedCards.size > 1
          ? Array.from(this.selectedCards)
          : [card];

        startPositions = movingCards.map(c => {
          if (c.element) c.element.classList.add('is-dragging-card');
          return {
            card: c,
            x: c.x,
            y: c.y,
            baseX: c.baseX !== undefined ? c.baseX : c.x,
            baseY: c.baseY !== undefined ? c.baseY : c.y
          };
        });

        movingGroups = Array.from(this.selectedGroups || []);
        startGroupPositions = movingGroups.map(g => ({
          group: g,
          x: g.x,
          y: g.y,
          memberCards: this.cards.filter(c => c.groupId === g.id && !movingCards.includes(c)).map(mc => ({
            card: mc,
            x: mc.x,
            y: mc.y,
            baseX: mc.baseX !== undefined ? mc.baseX : mc.x,
            baseY: mc.baseY !== undefined ? mc.baseY : mc.y
          }))
        }));

        movingNodes = Array.from(this.selectedNodes || []);
        startNodePositions = movingNodes.map(n => ({
          node: n,
          x: n.x,
          y: n.y
        }));
      };

      const onMouseMove = (moveEvent) => {
        lastMoveEvt = moveEvent;

        if (!hasStartedMoving) {
          const dist = Math.hypot(moveEvent.clientX - startMouseX, moveEvent.clientY - startMouseY);
          if (dist < 4) return;
          initDragState();
        }

        if (dragRafPending) return;
        dragRafPending = true;

        requestAnimationFrame(() => {
          dragRafPending = false;
          if (!hasStartedMoving || !lastMoveEvt) return;

          const dx = (lastMoveEvt.clientX - startMouseX) / this.canvas.zoom;
          const dy = (lastMoveEvt.clientY - startMouseY) / this.canvas.zoom;

          // Move all selected cards together
          startPositions.forEach(item => {
            item.card.x = item.x + dx;
            item.card.y = item.y + dy;
            if (item.card.baseX !== undefined) item.card.baseX = item.baseX + dx;
            if (item.card.baseY !== undefined) item.card.baseY = item.baseY + dy;
            item.card.element.style.transform = `translate(${item.card.x}px, ${item.card.y}px)`;
          });

          // Move any selected groups together
          startGroupPositions.forEach(item => {
            item.group.x = item.x + dx;
            item.group.y = item.y + dy;
            if (item.group.element) item.group.element.style.transform = `translate(${item.group.x}px, ${item.group.y}px)`;
            item.memberCards.forEach(mc => {
              mc.card.x = mc.x + dx;
              mc.card.y = mc.y + dy;
              if (mc.card.baseX !== undefined) mc.card.baseX = mc.baseX + dx;
              if (mc.card.baseY !== undefined) mc.card.baseY = mc.baseY + dy;
              if (mc.card.element) mc.card.element.style.transform = `translate(${mc.card.x}px, ${mc.card.y}px)`;
            });
          });

          // Move any selected nodes together
          startNodePositions.forEach(item => {
            item.node.x = item.x + dx;
            item.node.y = item.y + dy;
            if (item.node.element) item.node.element.style.transform = `translate(${item.node.x}px, ${item.node.y}px)`;
          });

          this.renderConnectionsThrottled();

          // Check if hovering over any group frame (based on primary dragged card)
          const cx = card.x + card.width / 2;
          const cy = card.y + card.height / 2;
          const hoverGroup = this.groups.find(g => 
            cx >= g.x && cx <= (g.x + g.width) &&
            cy >= g.y && cy <= (g.y + g.height)
          );

          if (hoverGroup !== currentHoverGroup) {
            if (currentHoverGroup && currentHoverGroup.element) {
              currentHoverGroup.element.classList.remove('is-drop-target');
            }
            currentHoverGroup = hoverGroup;
            if (currentHoverGroup && currentHoverGroup.element) {
              currentHoverGroup.element.classList.add('is-drop-target');
            }
          }
        });
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);

        document.body.classList.remove('is-dragging-card');
        document.querySelectorAll('.card-yt-iframe').forEach(f => f.style.pointerEvents = 'auto');

        if (!hasStartedMoving) {
          // Instant selection / click - zero history snapshot, zero delay!
          return;
        }

        if (startPositions) {
          startPositions.forEach(item => {
            if (item.card.element) item.card.element.classList.remove('is-dragging-card');
          });
        }

        if (currentHoverGroup && currentHoverGroup.element) {
          currentHoverGroup.element.classList.remove('is-drop-target');
        }

        const cx = card.x + card.width / 2;
        const cy = card.y + card.height / 2;
        const targetGroup = this.groups.find(g => 
          cx >= g.x && cx <= (g.x + g.width) &&
          cy >= g.y && cy <= (g.y + g.height)
        );

        if (targetGroup) {
          movingCards.forEach(c => {
            c.groupId = targetGroup.id;
            this.updateCardGroupBadge(c);
          });
          this.updateGroupCounts();
          Toast.show(`Locked ${movingCards.length} ref(s) to ${targetGroup.title}`, 'success');
          // Auto-tidy group contents cleanly!
          this.tidyGroup(targetGroup, true);
        } else if (movingCards.some(c => c.groupId)) {
          // Dragged out of group onto empty canvas
          const prevGroups = new Set();
          movingCards.forEach(c => {
            if (c.groupId) {
              const pg = this.groups.find(g => g.id === c.groupId);
              if (pg) prevGroups.add(pg);
              c.groupId = null;
              this.updateCardGroupBadge(c);
            }
          });
          this.updateGroupCounts();
          prevGroups.forEach(pg => this.tidyGroup(pg, true));
          Toast.show(`Detached from group`, 'info');
        }

        if (card.baseX !== undefined) {
          card.baseX += (card.x - initialCardX);
          card.baseY += (card.y - initialCardY);
        }

        currentHoverGroup = null;
        this.commitHistory('Move Reference', true);
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  initResizeHandle(handle, card) {
    handle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      this.recordPreState('Resize Reference');

      // Disable iframe pointer events during resize so mousemove is never swallowed
      const iframes = document.querySelectorAll('.card-yt-iframe');
      iframes.forEach(f => f.style.pointerEvents = 'none');

      const dir = handle.dataset.dir;
      const startMouseX = e.clientX;
      const startMouseY = e.clientY;
      const startWidth = card.width;
      const startHeight = card.height;
      const startX = card.x;
      const startY = card.y;
      const startBaseWidth = card.baseWidth || card.width;
      const startBaseHeight = card.baseHeight || card.height;

      const onMouseMove = (moveEvent) => {
        const dx = (moveEvent.clientX - startMouseX) / this.canvas.zoom;
        const dy = (moveEvent.clientY - startMouseY) / this.canvas.zoom;

        let newWidth = startWidth;
        let newHeight = startHeight;
        let newX = startX;
        let newY = startY;

        if (dir.includes('e')) {
          newWidth = Math.max(60, startWidth + dx);
          newHeight = newWidth / card.aspectRatio;
        } else if (dir.includes('w')) {
          newWidth = Math.max(60, startWidth - dx);
          newHeight = newWidth / card.aspectRatio;
          newX = startX + (startWidth - newWidth);
        }

        card.width = Math.round(newWidth);
        card.height = Math.round(newHeight);
        card.x = Math.round(newX);
        card.y = Math.round(newY);

        card.element.style.width = `${card.width}px`;
        card.element.style.height = `${card.height}px`;
        card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

        if (card.isCropped && card.crop) {
          const factor = newWidth / startWidth;
          card.baseWidth = Math.round(startBaseWidth * factor);
          card.baseHeight = Math.round(startBaseHeight * factor);
          card.baseX = Math.round(card.x - card.baseWidth * ((card.crop.left || 0) / 100));
          card.baseY = Math.round(card.y - card.baseHeight * ((card.crop.top || 0) / 100));

          const img = card.mediaContainer ? card.mediaContainer.querySelector('img') : null;
          if (img) {
            img.style.width = `${card.baseWidth}px`;
            img.style.height = `${card.baseHeight}px`;
            img.style.left = `-${Math.round(card.baseWidth * ((card.crop.left || 0) / 100))}px`;
            img.style.top = `-${Math.round(card.baseHeight * ((card.crop.top || 0) / 100))}px`;
          }
        }
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        document.querySelectorAll('.card-yt-iframe').forEach(f => f.style.pointerEvents = 'auto');
        if (card.groupId) {
          const group = this.groups.find(g => g.id === card.groupId);
          if (group) {
            const bottom = card.y + card.height + 20;
            const right = card.x + card.width + 16;
            if (bottom > group.y + group.height) {
              group.height = bottom - group.y;
              group.element.style.height = `${group.height}px`;
            }
            if (right > group.x + group.width) {
              group.width = right - group.x;
              group.element.style.width = `${group.width}px`;
            }
          }
        }
        this.commitHistory('Resize Reference');
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  selectCard(card, multi = false) {
    if (!card) {
      this.clearSelection();
      return;
    }

    if (multi) {
      if (this.selectedCards.has(card)) {
        this.selectedCards.delete(card);
        if (card.element) card.element.classList.remove('selected');
        this.selectedCard = Array.from(this.selectedCards).pop() || null;
      } else {
        this.selectedCards.add(card);
        if (card.element) card.element.classList.add('selected');
        this.selectedCard = card;
        card.zIndex = ++this.highestZ;
        if (card.element) card.element.style.zIndex = card.zIndex;
      }
    } else {
      this.clearSelection();
      this.selectedCards.add(card);
      this.selectedCard = card;
      if (card.element) {
        card.element.classList.add('selected');
        card.zIndex = ++this.highestZ;
        card.element.style.zIndex = card.zIndex;
      }
    }
  }

  selectGroup(group, multi = false) {
    if (!group) return;

    if (multi) {
      if (this.selectedGroups.has(group)) {
        this.selectedGroups.delete(group);
        if (group.element) group.element.classList.remove('selected');
        this.selectedGroup = Array.from(this.selectedGroups).pop() || null;
      } else {
        this.selectedGroups.add(group);
        if (group.element) group.element.classList.add('selected');
        this.selectedGroup = group;
      }
    } else {
      this.clearSelection();
      this.selectedGroups.add(group);
      this.selectedGroup = group;
      if (group.element) group.element.classList.add('selected');
    }
  }

  selectNode(node, multi = false) {
    if (!node) return;

    if (multi) {
      if (this.selectedNodes.has(node)) {
        this.selectedNodes.delete(node);
        if (node.element) node.element.classList.remove('selected');
        this.selectedNode = Array.from(this.selectedNodes).pop() || null;
      } else {
        this.selectedNodes.add(node);
        if (node.element) node.element.classList.add('selected');
        this.selectedNode = node;
      }
    } else {
      this.clearSelection();
      this.selectedNodes.add(node);
      this.selectedNode = node;
      if (node.element) node.element.classList.add('selected');
    }
  }

  clearSelection() {
    this.selectedCards.forEach(c => {
      if (c.element) c.element.classList.remove('selected');
    });
    this.selectedCards.clear();
    this.selectedCard = null;

    if (this.selectedGroups) {
      this.selectedGroups.forEach(g => {
        if (g.element) g.element.classList.remove('selected');
      });
      this.selectedGroups.clear();
      this.selectedGroup = null;
    }

    if (this.selectedNodes) {
      this.selectedNodes.forEach(n => {
        if (n.element) n.element.classList.remove('selected');
      });
      this.selectedNodes.clear();
      this.selectedNode = null;
    }
  }

  selectMultiple(arg) {
    this.clearSelection();
    let cards = [];
    let groups = [];
    let nodes = [];

    if (Array.isArray(arg)) {
      cards = arg;
    } else if (arg && typeof arg === 'object') {
      cards = arg.cards || [];
      groups = arg.groups || [];
      nodes = arg.nodes || [];
    }

    cards.forEach(c => {
      this.selectedCards.add(c);
      if (c.element) {
        c.element.classList.add('selected');
        c.zIndex = ++this.highestZ;
        c.element.style.zIndex = c.zIndex;
      }
    });
    this.selectedCard = cards[cards.length - 1] || null;

    groups.forEach(g => {
      this.selectedGroups.add(g);
      if (g.element) g.element.classList.add('selected');
    });
    this.selectedGroup = groups[groups.length - 1] || null;

    nodes.forEach(n => {
      this.selectedNodes.add(n);
      if (n.element) n.element.classList.add('selected');
    });
    this.selectedNode = nodes[nodes.length - 1] || null;
  }

  cloneCard(srcCard, newX, newY, newGroupId = undefined) {
    const id = 'ref_' + Date.now() + '_' + Math.random().toString(36).substr(2, 6);
    const card = {
      id,
      url: srcCard.url,
      imageData: srcCard.imageData || this.getCardImageData(srcCard) || undefined,
      localPath: srcCard.localPath || '',
      localWebUrl: srcCard.localWebUrl || '',
      isYouTube: !!srcCard.isYouTube,
      youtubeId: srcCard.youtubeId || null,
      youtubeUrl: srcCard.youtubeUrl || '',
      originalUrl: srcCard.originalUrl || '',
      x: newX !== undefined ? newX : srcCard.x + 30,
      y: newY !== undefined ? newY : srcCard.y + 30,
      width: srcCard.width,
      height: srcCard.height,
      baseWidth: srcCard.baseWidth || srcCard.width,
      baseHeight: srcCard.baseHeight || srcCard.height,
      aspectRatio: srcCard.aspectRatio,
      zIndex: ++this.highestZ,
      sourceLabel: srcCard.sourceLabel || 'Web',
      groupId: newGroupId !== undefined ? newGroupId : srcCard.groupId,
      crop: srcCard.crop ? { ...srcCard.crop } : null,
      element: null
    };

    const img = new Image();
    img.src = card.localWebUrl || card.url;
    this.createCardDOM(card, img);
    this.cards.push(card);
    if (card.groupId) this.updateCardGroupBadge(card);
    return card;
  }

  cloneNode(srcNode, newX, newY) {
    const customData = {
      title: `${srcNode.title} (Copy)`,
      color: srcNode.color,
      content: srcNode.content,
      mode: srcNode.mode,
      items: srcNode.items ? JSON.parse(JSON.stringify(srcNode.items)) : [],
      tags: srcNode.tags ? [...srcNode.tags] : [],
      fontFamily: srcNode.fontFamily,
      fontSize: srcNode.fontSize,
      activeVfx: srcNode.activeVfx ? [...srcNode.activeVfx] : [],
      deadline: srcNode.deadline,
      status: srcNode.status,
      width: srcNode.width
    };
    const node = this.createNode(
      srcNode.type,
      newX !== undefined ? newX : srcNode.x + 30,
      newY !== undefined ? newY : srcNode.y + 30,
      customData
    );
    return node;
  }

  cloneGroup(srcGroup, newX, newY) {
    const targetX = newX !== undefined ? newX : srcGroup.x + 40;
    const targetY = newY !== undefined ? newY : srcGroup.y + 40;
    const newGroup = this.createGroup(
      `${srcGroup.title} (Copy)`,
      targetX,
      targetY,
      srcGroup.width,
      srcGroup.height,
      srcGroup.color,
      srcGroup.notes || ''
    );

    // Clone all member cards of the source group into the new group
    const memberCards = this.cards.filter(c => c.groupId === srcGroup.id);
    const clonedCards = [];
    memberCards.forEach(c => {
      const offsetX = c.x - srcGroup.x;
      const offsetY = c.y - srcGroup.y;
      const clonedCard = this.cloneCard(c, newGroup.x + offsetX, newGroup.y + offsetY, newGroup.id);
      clonedCards.push(clonedCard);
    });

    this.updateGroupCounts();
    this.tidyGroup(newGroup, true);
    return { group: newGroup, cards: clonedCards };
  }

  duplicateSelection() {
    const hasCards = this.selectedCards.size > 0;
    const hasGroups = this.selectedGroups && this.selectedGroups.size > 0;
    const hasNodes = this.selectedNodes && this.selectedNodes.size > 0;
    if (!hasCards && !hasGroups && !hasNodes) return;

    this.recordPreState('Duplicate Selection');

    const idMap = new Map();
    const newSelectedCards = [];
    const newSelectedGroups = [];
    const newSelectedNodes = [];
    const duplicatedGroupIds = new Set();

    // 1. Duplicate Selected Groups (along with their member cards)
    if (hasGroups) {
      this.selectedGroups.forEach(grp => {
        duplicatedGroupIds.add(grp.id);
        const origMemberCards = this.cards.filter(c => c.groupId === grp.id);
        const res = this.cloneGroup(grp);
        idMap.set(grp.id, res.group.id);

        res.cards.forEach((clonedC, idx) => {
          if (origMemberCards[idx]) {
            idMap.set(origMemberCards[idx].id, clonedC.id);
          }
        });
        newSelectedGroups.push(res.group);
      });
    }

    // 2. Duplicate Selected Cards (exclude cards whose parent group was already duplicated)
    if (hasCards) {
      this.selectedCards.forEach(card => {
        if (card.groupId && duplicatedGroupIds.has(card.groupId)) {
          return;
        }
        const clonedCard = this.cloneCard(card);
        idMap.set(card.id, clonedCard.id);
        newSelectedCards.push(clonedCard);
      });
    }

    // 3. Duplicate Selected Nodes
    if (hasNodes) {
      this.selectedNodes.forEach(node => {
        const clonedNode = this.cloneNode(node);
        idMap.set(node.id, clonedNode.id);
        newSelectedNodes.push(clonedNode);
      });
    }

    // 4. Replicate cable connections between any duplicated items!
    this.connections.forEach(conn => {
      const newFrom = idMap.get(conn.fromNodeId);
      const newTo = idMap.get(conn.toTargetId);
      if (newFrom && newTo) {
        this.connections.push({
          fromNodeId: newFrom,
          toTargetId: newTo,
          targetType: conn.targetType,
          side: conn.side
        });
      }
    });

    // 5. Select newly created duplicates
    this.clearSelection();
    newSelectedCards.forEach(c => {
      this.selectedCards.add(c);
      if (c.element) c.element.classList.add('selected');
    });
    newSelectedGroups.forEach(g => {
      this.selectedGroups.add(g);
      if (g.element) g.element.classList.add('selected');
    });
    newSelectedNodes.forEach(n => {
      this.selectedNodes.add(n);
      if (n.element) n.element.classList.add('selected');
    });

    this.updateConnectedFontEffects();
    this.updateCardCount();
    this.updateGroupCounts();
    this.renderConnections();
    this.commitHistory('Duplicate Selection');
    this.scheduleAutoSave();

    const total = newSelectedCards.length + newSelectedGroups.length + newSelectedNodes.length;
    Toast.show(`Duplicated ${total} item(s) with cable connections (Ctrl+D)`, 'success');
  }

  initMarqueeSelection() {
    const selectionBox = document.getElementById('selection-box');
    if (!selectionBox) return;
    let isMarquee = false;
    let startX = 0, startY = 0;
    let marqueeMode = 'all'; // 'all' or 'cards_only'
    let targetGroup = null;

    this.viewport.addEventListener('mousedown', (e) => {
      // Only left click
      if (e.button !== 0 || this.canvas.spacePressed || e.altKey) return;

      // Ignore interactive controls
      if (e.target.closest('.floating-dock') || 
          e.target.closest('.canvas-hud') ||
          e.target.closest('.context-menu') ||
          e.target.closest('.titlebar') ||
          e.target.closest('.group-actions') ||
          e.target.closest('.group-notes-textarea') ||
          e.target.closest('.group-resize-handle') ||
          e.target.closest('.prod-node-pin') ||
          e.target.closest('.prod-node-header') ||
          e.target.closest('.ref-card')) {
        return;
      }

      // Check if starting inside a group frame
      const insideGroup = e.target.closest('.mv-group-frame');
      const insideGroupHeader = e.target.closest('.group-header');

      // If clicking directly on group header, that's group dragging/selecting (handled in groupEl)
      if (insideGroupHeader) return;

      const isShift = e.shiftKey;
      if (isShift) {
        // Shift + Drag: Marquee to select ONLY photos/cards from ANYWHERE (inside group, outside group, across groups!)
        marqueeMode = 'cards_only';
        targetGroup = null;
      } else if (insideGroup) {
        // Inside a group frame WITHOUT Shift: allow standard click/drag to move the group directly from the middle!
        return;
      } else {
        // Started on empty canvas without Shift -> Universal Selection (Cards, Groups, Nodes)
        marqueeMode = 'all';
        targetGroup = null;
      }

      startX = e.clientX;
      startY = e.clientY;
      isMarquee = false;
      const isAdditive = (e.shiftKey || e.ctrlKey || e.metaKey);
      const initialSelectedCards = isAdditive ? new Set(this.selectedCards) : new Set();
      const initialSelectedNodes = isAdditive ? new Set(this.selectedNodes) : new Set();
      const initialSelectedGroups = isAdditive ? new Set(this.selectedGroups) : new Set();

      const onMouseMove = (moveEvt) => {
        const dx = moveEvt.clientX - startX;
        const dy = moveEvt.clientY - startY;
        const dist = Math.hypot(dx, dy);

        if (dist > 6) {
          isMarquee = true;
          const left = Math.min(startX, moveEvt.clientX);
          const top = Math.min(startY, moveEvt.clientY);
          const width = Math.abs(dx);
          const height = Math.abs(dy);

          selectionBox.style.display = 'block';
          selectionBox.style.left = `${left}px`;
          selectionBox.style.top = `${top}px`;
          selectionBox.style.width = `${width}px`;
          selectionBox.style.height = `${height}px`;

          const p1 = this.canvas.screenToWorld(left, top);
          const p2 = this.canvas.screenToWorld(left + width, top + height);

          if (marqueeMode === 'cards_only') {
            // ONLY select cards (photos) from ANYWHERE intersecting the box, never groups or nodes!
            const matchedCards = this.cards.filter(c => 
              c.x + c.width >= p1.x && c.x <= p2.x &&
              c.y + c.height >= p1.y && c.y <= p2.y
            );

            this.clearSelection();
            if (isAdditive) {
              initialSelectedCards.forEach(c => {
                this.selectedCards.add(c);
                if (c.element) c.element.classList.add('selected');
              });
            }
            matchedCards.forEach(c => {
              this.selectedCards.add(c);
              if (c.element) c.element.classList.add('selected');
            });
            this.selectedCard = matchedCards[matchedCards.length - 1] || (isAdditive ? Array.from(initialSelectedCards).pop() : null);
            this.selectedGroup = null;
            this.selectedNode = null;
          } else {
            // Universal selection: Cards, Groups, and Nodes!
            const matchedCards = this.cards.filter(c => 
              c.x + c.width >= p1.x && c.x <= p2.x &&
              c.y + c.height >= p1.y && c.y <= p2.y
            );

            const matchedNodes = this.nodes.filter(n => {
              const nh = n.element ? n.element.offsetHeight : 180;
              return (n.x + n.width >= p1.x && n.x <= p2.x &&
                      n.y + nh >= p1.y && n.y <= p2.y);
            });

            const matchedGroups = this.groups.filter(g => {
              const overlapsX = (g.x + g.width >= p1.x && g.x <= p2.x);
              const overlapsY = (g.y + g.height >= p1.y && g.y <= p2.y);
              if (!overlapsX || !overlapsY) return false;
              const headerY2 = g.y + 42;
              const headerInBox = (headerY2 >= p1.y && g.y <= p2.y);
              return headerInBox || (p2.x - p1.x > 80 && p2.y - p1.y > 80);
            });

            this.clearSelection();
            if (isAdditive) {
              initialSelectedCards.forEach(c => {
                this.selectedCards.add(c);
                if (c.element) c.element.classList.add('selected');
              });
              initialSelectedNodes.forEach(n => {
                this.selectedNodes.add(n);
                if (n.element) n.element.classList.add('selected');
              });
              initialSelectedGroups.forEach(g => {
                this.selectedGroups.add(g);
                if (g.element) g.element.classList.add('selected');
              });
            }

            matchedCards.forEach(c => {
              this.selectedCards.add(c);
              if (c.element) c.element.classList.add('selected');
            });
            this.selectedCard = matchedCards[matchedCards.length - 1] || (isAdditive ? Array.from(initialSelectedCards).pop() : null);

            matchedNodes.forEach(n => {
              this.selectedNodes.add(n);
              if (n.element) n.element.classList.add('selected');
            });
            this.selectedNode = matchedNodes[matchedNodes.length - 1] || (isAdditive ? Array.from(initialSelectedNodes).pop() : null);

            matchedGroups.forEach(g => {
              this.selectedGroups.add(g);
              if (g.element) g.element.classList.add('selected');
            });
            this.selectedGroup = matchedGroups[matchedGroups.length - 1] || (isAdditive ? Array.from(initialSelectedGroups).pop() : null);
          }
        }
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);

        if (isMarquee) {
          selectionBox.style.display = 'none';
          isMarquee = false;
          if (marqueeMode === 'cards_only') {
            this.selectedGroups.clear();
            this.selectedNodes.clear();
            this.selectedGroup = null;
            this.selectedNode = null;
          }
        } else {
          // Simple click on empty canvas deselects all
          if (!insideGroup) {
            this.clearSelection();
          }
        }
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  removeCard(card) {
    if (!card) return;
    this.stopYouTubeCard(card);
    this.recordPreState('Delete Reference');
    if (card.element) card.element.remove();
    this.cards = this.cards.filter(c => c.id !== card.id);
    this.selectedCards.delete(card);
    if (this.selectedCard === card) this.selectedCard = null;
    this.updateCardCount();
    this.updateGroupCounts();
    this.commitHistory('Delete Reference');
  }

  clearAll(clearAutoSave = true) {
    this.recordPreState('Clear Canvas');
    this.cards.forEach(c => {
      this.stopYouTubeCard(c);
      if (c.element) c.element.remove();
    });
    this.cards = [];
    this.nodes.forEach(n => {
      if (n.element) n.element.remove();
    });
    this.nodes = [];
    this.connections = [];
    if (this.connectorLayer) this.connectorLayer.innerHTML = '';
    this.clearSelection();
    this.updateCardCount();
    this.updateGroupCounts();
    if (clearAutoSave) {
      try { localStorage.removeItem('dropboard_autosave_state'); } catch(e){}
    }
    this.commitHistory('Clear Canvas');
    Toast.show('Canvas cleared', 'info');
  }

  getNodeGroupId(nodeId) {
    if (!nodeId) return null;
    // Direct connection to group
    for (const c of this.connections) {
      if (c.fromNodeId === nodeId) {
        if (this.groups.some(g => g.id === c.toTargetId)) return c.toTargetId;
        const cd = this.cards.find(x => x.id === c.toTargetId);
        if (cd && cd.groupId) return cd.groupId;
      }
    }
    // BFS for chained nodes (e.g. Font -> Note -> Note -> Scene)
    const visited = new Set([nodeId]);
    const queue = [nodeId];
    while (queue.length > 0) {
      const curr = queue.shift();
      for (const c of this.connections) {
        if (c.fromNodeId === curr && !visited.has(c.toTargetId)) {
          if (this.groups.some(g => g.id === c.toTargetId)) return c.toTargetId;
          const cd = this.cards.find(x => x.id === c.toTargetId);
          if (cd && cd.groupId) return cd.groupId;
          visited.add(c.toTargetId);
          queue.push(c.toTargetId);
        }
        if (c.toTargetId === curr && !visited.has(c.fromNodeId)) {
          if (this.groups.some(g => g.id === c.fromNodeId)) return c.fromNodeId;
          visited.add(c.fromNodeId);
          queue.push(c.fromNodeId);
        }
      }
    }
    return null;
  }

  getAccurateNodeHeight(node) {
    if (node.element) {
      if (node.element.offsetHeight > 50) return node.element.offsetHeight;
      const rect = node.element.getBoundingClientRect();
      const zoom = (this.canvas && this.canvas.zoom > 0) ? this.canvas.zoom : 1;
      if (rect && rect.height > 50) return Math.round(rect.height / zoom);
    }
    let h = 220;
    if (node.type === 'font') {
      const tagCount = (node.items && Array.isArray(node.items)) ? node.items.length : 0;
      h = 280 + tagCount * 26;
    } else if (node.type === 'schedule' || node.type === 'plan') {
      h = 220;
    } else if (node.type === 'vfx') {
      h = 240;
    } else if (node.type === 'note') {
      const tagCount = (node.tags && Array.isArray(node.tags)) ? node.tags.length : 0;
      h = 230 + Math.ceil(tagCount / 3) * 26;
    }
    return Math.max(h, 180);
  }

  getAccurateNodeWidth(node) {
    if (node.element) {
      if (node.element.offsetWidth > 60) return node.element.offsetWidth;
      const rect = node.element.getBoundingClientRect();
      const zoom = (this.canvas && this.canvas.zoom > 0) ? this.canvas.zoom : 1;
      if (rect && rect.width > 60) return Math.round(rect.width / zoom);
    }
    if (node.width && node.width > 60) return Math.max(node.width, node.type === 'font' ? 295 : 60);
    if (node.type === 'font') return 295;
    if (node.type === 'schedule' || node.type === 'plan') return 285;
    return 270;
  }

  assignNodeLayers(nodes, connections, targetGroupId = null) {
    const layers = new Map();
    const directNodes = new Set();

    if (targetGroupId) {
      nodes.forEach(n => {
        for (const c of connections) {
          if (c.fromNodeId === n.id) {
            if (c.toTargetId === targetGroupId) directNodes.add(n.id);
            const card = this.cards.find(cd => cd.id === c.toTargetId);
            if (card && card.groupId === targetGroupId) directNodes.add(n.id);
          }
        }
      });
    }

    if (directNodes.size === 0 && nodes.length > 0) {
      // For standalone clusters, pick sink nodes (nodes with 0 outgoing connections inside this cluster)
      const outgoingCount = new Map();
      nodes.forEach(n => outgoingCount.set(n.id, 0));
      for (const c of connections) {
        if (outgoingCount.has(c.fromNodeId) && nodes.some(n => n.id === c.toTargetId)) {
          outgoingCount.set(c.fromNodeId, outgoingCount.get(c.fromNodeId) + 1);
        }
      }
      nodes.forEach(n => {
        if (outgoingCount.get(n.id) === 0) directNodes.add(n.id);
      });
      if (directNodes.size === 0) directNodes.add(nodes[0].id);
    }

    directNodes.forEach(id => layers.set(id, 1));

    // Propagate backwards: fromNode -> toNode: fromNode is upstream, so layer = toNode.layer + 1
    let changed = true;
    let iterations = 0;
    while (changed && iterations < 12) {
      changed = false;
      iterations++;
      for (const c of connections) {
        const fromNode = nodes.find(n => n.id === c.fromNodeId);
        const toNode = nodes.find(n => n.id === c.toTargetId);

        if (fromNode && toNode && layers.has(toNode.id)) {
          const desired = layers.get(toNode.id) + 1;
          if (!layers.has(fromNode.id) || layers.get(fromNode.id) < desired) {
            layers.set(fromNode.id, desired);
            changed = true;
          }
        }
      }
      for (const c of connections) {
        const fromNode = nodes.find(n => n.id === c.fromNodeId);
        const toNode = nodes.find(n => n.id === c.toTargetId);
        if (fromNode && toNode && layers.has(fromNode.id) && !layers.has(toNode.id)) {
          layers.set(toNode.id, Math.max(1, layers.get(fromNode.id) - 1));
          changed = true;
        }
      }
    }

    nodes.forEach(n => {
      if (!layers.has(n.id)) layers.set(n.id, 1);
    });

    return layers;
  }

  autoArrangeGrid() {
    if (this.cards.length === 0 && this.groups.length === 0 && this.nodes.length === 0) return;

    const gap = this.arrangeGap !== undefined ? this.arrangeGap : 32;
    const nodeGapY = Math.max(16, gap);
    const colGapX = Math.max(24, gap + 14);
    const groupGapX = Math.max(36, gap + 24);
    const groupGapY = Math.max(48, gap + 28);

    // Identify selected items
    const selCards = new Set(this.selectedCards || []);
    if (this.selectedCard) selCards.add(this.selectedCard);

    const selGroups = new Set(this.selectedGroups || []);
    if (this.selectedGroup) selGroups.add(this.selectedGroup);

    const selNodes = new Set(this.selectedNodes || []);
    if (this.selectedNode) selNodes.add(this.selectedNode);

    const totalSelected = selCards.size + selGroups.size + selNodes.size;

    // Single item selection handling
    if (totalSelected === 1) {
      if (selGroups.size === 1) {
        const grp = Array.from(selGroups)[0];
        this.tidyGroup(grp, true);
        this.renderConnections();
        Toast.show(`Tidied group "${grp.title}". Tip: Select 2+ items to arrange selection, or deselect all to arrange entire board.`, 'info');
        return;
      }
      Toast.show('Select 2 or more items to arrange selection, or deselect all to arrange entire board.', 'info');
      return;
    }

    const isSelectionMode = totalSelected >= 2;

    if (isSelectionMode) {
      this.recordPreState('Auto-Arrange Grid (Selection)');

      const targetGroups = this.groups.filter(g => selGroups.has(g));
      const targetNodes = this.nodes.filter(n => selNodes.has(n));
      const targetCards = this.cards.filter(c => selCards.has(c));
      const targetLooseCards = targetCards.filter(c => !c.groupId);

      // Special case: Only member cards inside group(s) were selected
      if (targetGroups.length === 0 && targetNodes.length === 0 && targetLooseCards.length === 0 && targetCards.length > 0) {
        const groupsToTidy = new Set();
        targetCards.forEach(c => {
          if (c.groupId) {
            const g = this.groups.find(grp => grp.id === c.groupId);
            if (g) groupsToTidy.add(g);
          }
        });
        groupsToTidy.forEach(g => this.tidyGroup(g, true));
        this.renderConnections();
        this.commitHistory('Auto-Arrange Grid (Selection)');
        Toast.show(`Tidied cards inside ${groupsToTidy.size} group(s)`, 'success');
        return;
      }

      // Calculate anchor bounding box (minX, minY) of selected canvas items
      let minX = Infinity;
      let minY = Infinity;
      targetGroups.forEach(g => {
        minX = Math.min(minX, g.x);
        minY = Math.min(minY, g.y);
      });
      targetNodes.forEach(n => {
        minX = Math.min(minX, n.x);
        minY = Math.min(minY, n.y);
      });
      targetLooseCards.forEach(c => {
        minX = Math.min(minX, c.x);
        minY = Math.min(minY, c.y);
      });

      if (!isFinite(minX)) minX = 0;
      if (!isFinite(minY)) minY = 0;

      // Special case: Only loose cards selected
      if (targetGroups.length === 0 && targetNodes.length === 0 && targetLooseCards.length >= 2) {
        const numCards = targetLooseCards.length;
        const cols = Math.min(numCards, Math.max(2, Math.ceil(Math.sqrt(numCards))));
        let curX = minX;
        let curY = minY;
        let rowMaxH = 0;
        let colIdx = 0;

        targetLooseCards.forEach(card => {
          card.x = curX;
          card.y = curY;
          if (card.baseX !== undefined) card.baseX = curX;
          if (card.baseY !== undefined) card.baseY = curY;
          if (card.element) card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

          rowMaxH = Math.max(rowMaxH, card.height);
          curX += card.width + gap;
          colIdx++;

          if (colIdx >= cols) {
            colIdx = 0;
            curX = minX;
            curY += rowMaxH + gap;
            rowMaxH = 0;
          }
        });

        this.renderConnections();
        this.commitHistory('Auto-Arrange Grid (Selection)');
        Toast.show(`Auto-arranged ${numCards} selected references in grid`, 'success');
        return;
      }

      // Special case: Only nodes selected
      if (targetGroups.length === 0 && targetLooseCards.length === 0 && targetNodes.length >= 2) {
        const layers = this.assignNodeLayers(targetNodes, this.connections, null);
        const layerBuckets = new Map();
        targetNodes.forEach(n => {
          const l = layers.get(n.id) || 1;
          if (!layerBuckets.has(l)) layerBuckets.set(l, []);
          layerBuckets.get(l).push(n);
        });

        const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);
        let curX = minX;

        sortedLayers.forEach(l => {
          const nodesInLayer = layerBuckets.get(l);
          let layerMaxW = 0;
          let nY = minY;

          nodesInLayer.forEach(n => {
            n.x = curX;
            n.y = nY;
            if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
            const nW = this.getAccurateNodeWidth(n);
            const nH = this.getAccurateNodeHeight(n);
            layerMaxW = Math.max(layerMaxW, nW);
            nY += nH + nodeGapY;
          });

          curX += layerMaxW + colGapX;
        });

        this.renderConnections();
        this.commitHistory('Auto-Arrange Grid (Selection)');
        Toast.show(`Auto-arranged ${targetNodes.length} selected nodes in grid`, 'success');
        return;
      }

      // General case: Selected Groups (plus any selected nodes & loose cards)
      targetGroups.forEach(group => {
        this.tidyGroup(group, true);
      });

      const positionedNodeIds = new Set();
      const groupNodeMap = new Map();
      targetGroups.forEach(g => groupNodeMap.set(g.id, []));

      targetNodes.forEach(n => {
        const gid = this.getNodeGroupId(n.id);
        if (gid && groupNodeMap.has(gid)) {
          groupNodeMap.get(gid).push(n);
        }
      });

      const standaloneNodes = targetNodes.filter(n => {
        const gid = this.getNodeGroupId(n.id);
        return !gid || !groupNodeMap.has(gid);
      });

      let currentY = minY;

      // Standalone selected nodes
      if (standaloneNodes.length > 0) {
        const clusterLayers = this.assignNodeLayers(standaloneNodes, this.connections, null);
        const layerBuckets = new Map();
        standaloneNodes.forEach(n => {
          const l = clusterLayers.get(n.id) || 1;
          if (!layerBuckets.has(l)) layerBuckets.set(l, []);
          layerBuckets.get(l).push(n);
        });

        const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);
        let sX = minX;
        let sRowMaxH = 0;

        sortedLayers.forEach(l => {
          const nodesInLayer = layerBuckets.get(l);
          let layerMaxW = 0;
          let nY = currentY;

          nodesInLayer.forEach(n => {
            n.x = sX;
            n.y = nY;
            if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
            positionedNodeIds.add(n.id);
            const nW = this.getAccurateNodeWidth(n);
            const nH = this.getAccurateNodeHeight(n);
            layerMaxW = Math.max(layerMaxW, nW);
            nY += nH + nodeGapY;
          });

          sRowMaxH = Math.max(sRowMaxH, nY - currentY);
          sX += layerMaxW + colGapX;
        });

        currentY += sRowMaxH + groupGapY;
      }

      // Selected groups
      if (targetGroups.length > 0) {
        const sortedGroups = [...targetGroups].sort((a, b) => {
          return a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' });
        });

        const numGroups = sortedGroups.length;
        let groupCols = numGroups <= 2 ? numGroups : (numGroups <= 4 ? numGroups : (numGroups <= 6 ? 3 : 4));

        let curX = minX;
        let rowStartY = currentY;
        let groupRowMaxH = 0;
        let groupColIdx = 0;

        sortedGroups.forEach(group => {
          const connectedNodes = groupNodeMap.get(group.id) || [];
          let nodeAreaH = 0;

          if (connectedNodes.length > 0) {
            const layers = this.assignNodeLayers(connectedNodes, this.connections, group.id);
            const layerBuckets = new Map();
            connectedNodes.forEach(n => {
              const l = layers.get(n.id) || 1;
              if (!layerBuckets.has(l)) layerBuckets.set(l, []);
              layerBuckets.get(l).push(n);
            });

            const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);

            sortedLayers.forEach(l => {
              const nodesInLayer = layerBuckets.get(l);
              let layerMaxW = 0;
              let nY = rowStartY;

              nodesInLayer.forEach(n => {
                n.x = curX;
                n.y = nY;
                if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
                positionedNodeIds.add(n.id);
                const nW = this.getAccurateNodeWidth(n);
                const nH = this.getAccurateNodeHeight(n);
                layerMaxW = Math.max(layerMaxW, nW);
                nY += nH + nodeGapY;
              });

              nodeAreaH = Math.max(nodeAreaH, nY - rowStartY);
              curX += layerMaxW + colGapX;
            });
          }

          // Place group to the right of its nodes
          const memberCards = this.cards.filter(c => c.groupId === group.id);
          const dx = curX - group.x;
          const dy = rowStartY - group.y;
          group.x = curX;
          group.y = rowStartY;
          group.element.style.transform = `translate(${group.x}px, ${group.y}px)`;

          memberCards.forEach(card => {
            card.x += dx;
            card.y += dy;
            if (card.baseX !== undefined) card.baseX += dx;
            if (card.baseY !== undefined) card.baseY += dy;
            card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;
          });

          const thisUnitH = Math.max(group.height, nodeAreaH);
          groupRowMaxH = Math.max(groupRowMaxH, thisUnitH);

          curX += group.width + groupGapX;
          groupColIdx++;

          if (groupColIdx >= groupCols) {
            groupColIdx = 0;
            curX = minX;
            rowStartY += groupRowMaxH + groupGapY;
            groupRowMaxH = 0;
          }
        });

        if (groupColIdx > 0) {
          rowStartY += groupRowMaxH + groupGapY;
          curX = minX;
        }
        currentY = rowStartY;
      }

      // Position selected loose cards
      if (targetLooseCards.length > 0) {
        let cardX = minX;
        let cardRowMaxH = 0;
        const maxCardsWidth = minX + 3200;

        targetLooseCards.forEach(card => {
          if (cardX > minX && cardX + card.width > maxCardsWidth) {
            cardX = minX;
            currentY += cardRowMaxH + gap;
            cardRowMaxH = 0;
          }
          card.x = cardX;
          card.y = currentY;
          if (card.baseX !== undefined) card.baseX = cardX;
          if (card.baseY !== undefined) card.baseY = cardY;
          if (card.element) card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

          cardRowMaxH = Math.max(cardRowMaxH, card.height);
          cardX += card.width + gap;
        });
        currentY += cardRowMaxH + gap;
      }

      this.renderConnections();
      this.commitHistory('Auto-Arrange Grid (Selection)');
      const totalMoved = targetGroups.length + targetNodes.length + targetLooseCards.length;
      Toast.show(`Auto-arranged ${totalMoved} selected item(s) in grid`, 'success');
      return;
    }

    // ==========================================
    // ALL MODE: Arrange entire board (default)
    // ==========================================
    this.recordPreState('Auto-Arrange Grid');

    // 1. Tidy all groups first
    this.groups.forEach(group => {
      this.tidyGroup(group, true);
    });

    const positionedNodeIds = new Set();

    // Map which nodes belong to which group
    const groupNodeMap = new Map();
    this.groups.forEach(g => groupNodeMap.set(g.id, []));

    this.nodes.forEach(n => {
      const gid = this.getNodeGroupId(n.id);
      if (gid && groupNodeMap.has(gid)) {
        groupNodeMap.get(gid).push(n);
      }
    });

    // Standalone nodes (nodes not connected to any Scene group)
    const standaloneNodes = this.nodes.filter(n => {
      const gid = this.getNodeGroupId(n.id);
      return !gid || !groupNodeMap.has(gid);
    });

    let currentY = 0;

    // 2. Position Standalone / Overview Nodes at the top in a clean horizontal dashboard row
    if (standaloneNodes.length > 0) {
      const visitedStandalone = new Set();
      let sX = 0;
      let sRowMaxH = 0;
      const maxHeaderWidth = 3200;

      standaloneNodes.forEach(node => {
        if (visitedStandalone.has(node.id)) return;

        const cluster = [];
        const queue = [node.id];
        visitedStandalone.add(node.id);

        while (queue.length > 0) {
          const currId = queue.shift();
          const currNode = standaloneNodes.find(n => n.id === currId);
          if (currNode) cluster.push(currNode);

          for (const c of this.connections) {
            if (c.fromNodeId === currId && !visitedStandalone.has(c.toTargetId)) {
              if (standaloneNodes.some(n => n.id === c.toTargetId)) {
                visitedStandalone.add(c.toTargetId);
                queue.push(c.toTargetId);
              }
            }
            if (c.toTargetId === currId && !visitedStandalone.has(c.fromNodeId)) {
              if (standaloneNodes.some(n => n.id === c.fromNodeId)) {
                visitedStandalone.add(c.fromNodeId);
                queue.push(c.fromNodeId);
              }
            }
          }
        }

        if (cluster.length > 1) {
          const clusterLayers = this.assignNodeLayers(cluster, this.connections, null);
          const layerBuckets = new Map();
          cluster.forEach(n => {
            const l = clusterLayers.get(n.id) || 1;
            if (!layerBuckets.has(l)) layerBuckets.set(l, []);
            layerBuckets.get(l).push(n);
          });
          const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);
          let clusterH = 0;

          sortedLayers.forEach(l => {
            const nodesInLayer = layerBuckets.get(l);
            let layerMaxW = 0;
            let nY = currentY;

            nodesInLayer.forEach(n => {
              n.x = sX;
              n.y = nY;
              if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
              positionedNodeIds.add(n.id);
              const nW = this.getAccurateNodeWidth(n);
              const nH = this.getAccurateNodeHeight(n);
              layerMaxW = Math.max(layerMaxW, nW);
              nY += nH + nodeGapY;
            });

            clusterH = Math.max(clusterH, nY - currentY);
            sX += layerMaxW + colGapX;
          });

          sRowMaxH = Math.max(sRowMaxH, clusterH);
          if (sX > maxHeaderWidth) {
            sX = 0;
            currentY += sRowMaxH + groupGapY;
            sRowMaxH = 0;
          }
        } else {
          const n = cluster[0];
          const nW = this.getAccurateNodeWidth(n);
          const nH = this.getAccurateNodeHeight(n);
          if (sX > 0 && sX + nW > maxHeaderWidth) {
            sX = 0;
            currentY += sRowMaxH + groupGapY;
            sRowMaxH = 0;
          }
          n.x = sX;
          n.y = currentY;
          if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
          positionedNodeIds.add(n.id);
          sX += nW + colGapX;
          sRowMaxH = Math.max(sRowMaxH, nH);
        }
      });

      currentY += sRowMaxH + groupGapY * 1.5;
    }

    // 3. Position Groups in an Expansive Widescreen Grid (Up to 4 columns across)
    if (this.groups.length > 0) {
      const sortedGroups = [...this.groups].sort((a, b) => {
        return a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' });
      });

      const numGroups = sortedGroups.length;
      let groupCols = 4;
      if (numGroups <= 2) groupCols = numGroups;
      else if (numGroups <= 4) groupCols = numGroups;
      else if (numGroups <= 6) groupCols = 3;
      else if (numGroups <= 8) groupCols = 4;
      else if (numGroups <= 12) groupCols = 4;
      else if (numGroups <= 16) groupCols = 5;
      else groupCols = Math.min(6, Math.ceil(Math.sqrt(numGroups * 2.2)));

      let curX = 0;
      let rowStartY = currentY;
      let groupRowMaxH = 0;
      let groupColIdx = 0;

      sortedGroups.forEach((group) => {
        const connectedNodes = groupNodeMap.get(group.id) || [];
        let nodeAreaH = 0;

        // Lay out connected nodes in layer columns to the left of the scene
        if (connectedNodes.length > 0) {
          const layers = this.assignNodeLayers(connectedNodes, this.connections, group.id);
          const layerBuckets = new Map();
          connectedNodes.forEach(n => {
            const l = layers.get(n.id) || 1;
            if (!layerBuckets.has(l)) layerBuckets.set(l, []);
            layerBuckets.get(l).push(n);
          });

          const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);

          sortedLayers.forEach(l => {
            const nodesInLayer = layerBuckets.get(l);
            let layerMaxW = 0;
            let nY = rowStartY;

            nodesInLayer.forEach(n => {
              n.x = curX;
              n.y = nY;
              if (n.element) n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
              positionedNodeIds.add(n.id);
              const nW = this.getAccurateNodeWidth(n);
              const nH = this.getAccurateNodeHeight(n);
              layerMaxW = Math.max(layerMaxW, nW);
              nY += nH + nodeGapY;
            });

            nodeAreaH = Math.max(nodeAreaH, nY - rowStartY);
            curX += layerMaxW + colGapX;
          });
        }

        // Place group to the right of its nodes
        const memberCards = this.cards.filter(c => c.groupId === group.id);
        const dx = curX - group.x;
        const dy = rowStartY - group.y;
        group.x = curX;
        group.y = rowStartY;
        group.element.style.transform = `translate(${group.x}px, ${group.y}px)`;

        memberCards.forEach(card => {
          card.x += dx;
          card.y += dy;
          if (card.baseX !== undefined) card.baseX += dx;
          if (card.baseY !== undefined) card.baseY += dy;
          card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;
        });

        const thisUnitH = Math.max(group.height, nodeAreaH);
        groupRowMaxH = Math.max(groupRowMaxH, thisUnitH);

        curX += group.width + groupGapX;
        groupColIdx++;

        if (groupColIdx >= groupCols) {
          groupColIdx = 0;
          curX = 0;
          rowStartY += groupRowMaxH + groupGapY;
          groupRowMaxH = 0;
        }
      });

      if (groupColIdx > 0) {
        rowStartY += groupRowMaxH + groupGapY;
        curX = 0;
      }
      currentY = rowStartY;
    }

    // 4. Position Ungrouped Loose Cards
    const ungroupedCards = this.cards.filter(c => !c.groupId);
    if (ungroupedCards.length > 0) {
      let cardX = 0;
      let cardRowMaxH = 0;
      const maxCardsWidth = 3200;

      ungroupedCards.forEach(card => {
        if (cardX > 0 && cardX + card.width > maxCardsWidth) {
          cardX = 0;
          currentY += cardRowMaxH + gap;
          cardRowMaxH = 0;
        }
        card.x = cardX;
        card.y = currentY;
        if (card.baseX !== undefined) card.baseX = cardX;
        if (card.baseY !== undefined) card.baseY = currentY;
        if (card.element) card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

        cardRowMaxH = Math.max(cardRowMaxH, card.height);
        cardX += card.width + gap;
      });
      currentY += cardRowMaxH + gap;
    }

    this.renderConnections();
    this.commitHistory('Auto-Arrange Grid');
    setTimeout(() => this.fitAllToView(), 120);
    Toast.show(`Auto-arranged widescreen grid (Gap: ${gap === 0 ? 'No Gap' : gap + 'px'})`, 'success');
  }

  autoArrangePipeline() {
    if (this.cards.length === 0 && this.groups.length === 0 && this.nodes.length === 0) return;

    const gap = this.arrangeGap !== undefined ? this.arrangeGap : 32;
    const nodeGapY = Math.max(16, gap);
    const colGapX = Math.max(24, gap + 14);
    const sceneGapX = Math.max(48, gap + 32);
    const sceneGapY = Math.max(48, gap + 32);

    // Identify selected items
    const selCards = new Set(this.selectedCards || []);
    if (this.selectedCard) selCards.add(this.selectedCard);

    const selGroups = new Set(this.selectedGroups || []);
    if (this.selectedGroup) selGroups.add(this.selectedGroup);

    const selNodes = new Set(this.selectedNodes || []);
    if (this.selectedNode) selNodes.add(this.selectedNode);

    const totalSelected = selCards.size + selGroups.size + selNodes.size;

    // Single item selection handling
    if (totalSelected === 1) {
      if (selGroups.size === 1) {
        const grp = Array.from(selGroups)[0];
        this.tidyGroup(grp, true);
        this.renderConnections();
        Toast.show(`Tidied group "${grp.title}". Tip: Select 2+ items to arrange selection, or deselect all to arrange entire board.`, 'info');
        return;
      }
      Toast.show('Select 2 or more items to arrange selection, or deselect all to arrange entire board.', 'info');
      return;
    }

    const isSelectionMode = totalSelected >= 2;

    if (isSelectionMode) {
      this.recordPreState('Storyboard Pipeline Layout (Selection)');

      const targetGroups = this.groups.filter(g => selGroups.has(g));
      const targetNodes = this.nodes.filter(n => selNodes.has(n));
      const targetCards = this.cards.filter(c => selCards.has(c));
      const targetLooseCards = targetCards.filter(c => !c.groupId);

      // Special case: Only member cards inside group(s) were selected
      if (targetGroups.length === 0 && targetNodes.length === 0 && targetLooseCards.length === 0 && targetCards.length > 0) {
        const groupsToTidy = new Set();
        targetCards.forEach(c => {
          if (c.groupId) {
            const g = this.groups.find(grp => grp.id === c.groupId);
            if (g) groupsToTidy.add(g);
          }
        });
        groupsToTidy.forEach(g => this.tidyGroup(g, true));
        this.renderConnections();
        this.commitHistory('Storyboard Pipeline Layout (Selection)');
        Toast.show(`Tidied cards inside ${groupsToTidy.size} group(s)`, 'success');
        return;
      }

      // Calculate anchor bounding box (minX, minY) of selected canvas items
      let minX = Infinity;
      let minY = Infinity;
      targetGroups.forEach(g => {
        minX = Math.min(minX, g.x);
        minY = Math.min(minY, g.y);
      });
      targetNodes.forEach(n => {
        minX = Math.min(minX, n.x);
        minY = Math.min(minY, n.y);
      });
      targetLooseCards.forEach(c => {
        minX = Math.min(minX, c.x);
        minY = Math.min(minY, c.y);
      });

      if (!isFinite(minX)) minX = 0;
      if (!isFinite(minY)) minY = 0;

      // Special case: Only loose cards selected
      if (targetGroups.length === 0 && targetNodes.length === 0 && targetLooseCards.length >= 2) {
        let curX = minX;
        let curY = minY;
        let rowH = 0;
        const maxCardsW = minX + 3200;

        targetLooseCards.forEach(c => {
          if (curX > minX && curX + c.width > maxCardsW) {
            curX = minX;
            curY += rowH + 24;
            rowH = 0;
          }
          c.x = curX;
          c.y = curY;
          if (c.baseX !== undefined) c.baseX = curX;
          if (c.baseY !== undefined) c.baseY = curY;
          if (c.element) c.element.style.transform = `translate(${c.x}px, ${c.y}px)`;
          rowH = Math.max(rowH, c.height);
          curX += c.width + 24;
        });

        this.commitHistory('Storyboard Pipeline Layout (Selection)');
        Toast.show(`Storyboard Pipeline arranged for ${targetLooseCards.length} selected reference(s)!`, 'success');
        return;
      }

      // Special case: Only nodes selected
      if (targetGroups.length === 0 && targetLooseCards.length === 0 && targetNodes.length >= 2) {
        const layers = this.assignNodeLayers(targetNodes, this.connections, null);
        const layerBuckets = new Map();
        targetNodes.forEach(n => {
          const l = layers.get(n.id) || 1;
          if (!layerBuckets.has(l)) layerBuckets.set(l, []);
          layerBuckets.get(l).push(n);
        });

        const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);
        let curX = minX;

        sortedLayers.forEach(l => {
          const nodesInLayer = layerBuckets.get(l);
          let layerMaxW = 0;
          let nY = minY;

          nodesInLayer.forEach(node => {
            node.x = curX;
            node.y = nY;
            if (node.element) node.element.style.transform = `translate(${node.x}px, ${node.y}px)`;
            const nodeW = this.getAccurateNodeWidth(node);
            const nodeH = this.getAccurateNodeHeight(node);
            layerMaxW = Math.max(layerMaxW, nodeW);
            nY += nodeH + nodeGapY;
          });

          curX += layerMaxW + colGapX;
        });

        this.renderConnections();
        this.commitHistory('Storyboard Pipeline Layout (Selection)');
        Toast.show(`Storyboard Pipeline arranged for ${targetNodes.length} selected node(s)!`, 'success');
        return;
      }

      // General case: Selected groups (plus any selected nodes & loose cards)
      targetGroups.forEach(group => {
        this.tidyGroup(group, true);
      });

      const sortedGroups = [...targetGroups].sort((a, b) => {
        return a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' });
      });

      const positionedNodeIds = new Set();
      const groupNodeMap = new Map();
      sortedGroups.forEach(g => groupNodeMap.set(g.id, []));

      targetNodes.forEach(n => {
        const gid = this.getNodeGroupId(n.id);
        if (gid && groupNodeMap.has(gid)) {
          groupNodeMap.get(gid).push(n);
        }
      });

      const standaloneNodes = targetNodes.filter(n => {
        const gid = this.getNodeGroupId(n.id);
        return !gid || !groupNodeMap.has(gid);
      });

      let currentY = minY;

      // Standalone selected nodes
      if (standaloneNodes.length > 0) {
        let sX = minX;
        let sRowMaxH = 0;
        const maxHeaderWidth = minX + 3200;

        standaloneNodes.forEach(node => {
          const w = this.getAccurateNodeWidth(node);
          const h = this.getAccurateNodeHeight(node);
          if (sX > minX && sX + w > maxHeaderWidth) {
            sX = minX;
            currentY += sRowMaxH + nodeGapY;
            sRowMaxH = 0;
          }
          node.x = sX;
          node.y = currentY;
          if (node.element) node.element.style.transform = `translate(${node.x}px, ${node.y}px)`;
          positionedNodeIds.add(node.id);
          sX += w + colGapX;
          sRowMaxH = Math.max(sRowMaxH, h);
        });

        currentY += sRowMaxH + sceneGapY * 1.5;
      }

      // Sequential Pipeline Flow for selected groups
      if (sortedGroups.length > 0) {
        const pipelineCols = Math.min(sortedGroups.length, 3);
        let curX = minX;
        let rowStartY = currentY;
        let stageRowMaxH = 0;
        let stageColIdx = 0;

        sortedGroups.forEach(group => {
          const connectedNodes = groupNodeMap.get(group.id) || [];
          let nodeAreaH = 0;

          if (connectedNodes.length > 0) {
            const layers = this.assignNodeLayers(connectedNodes, this.connections, group.id);
            const layerBuckets = new Map();
            connectedNodes.forEach(n => {
              const l = layers.get(n.id) || 1;
              if (!layerBuckets.has(l)) layerBuckets.set(l, []);
              layerBuckets.get(l).push(n);
            });

            const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);

            sortedLayers.forEach(l => {
              const nodesInLayer = layerBuckets.get(l);
              let layerMaxW = 0;
              let nY = rowStartY;

              nodesInLayer.forEach(node => {
                node.x = curX;
                node.y = nY;
                if (node.element) node.element.style.transform = `translate(${node.x}px, ${node.y}px)`;
                positionedNodeIds.add(node.id);
                const nodeW = this.getAccurateNodeWidth(node);
                const nodeH = this.getAccurateNodeHeight(node);
                layerMaxW = Math.max(layerMaxW, nodeW);
                nY += nodeH + nodeGapY;
              });

              nodeAreaH = Math.max(nodeAreaH, nY - rowStartY);
              curX += layerMaxW + colGapX;
            });
          }

          // Group placed to the right of its nodes
          const dx = curX - group.x;
          const dy = rowStartY - group.y;
          group.x = curX;
          group.y = rowStartY;
          group.element.style.transform = `translate(${group.x}px, ${group.y}px)`;

          const memberCards = this.cards.filter(c => c.groupId === group.id);
          memberCards.forEach(card => {
            card.x += dx;
            card.y += dy;
            if (card.baseX !== undefined) card.baseX += dx;
            if (card.baseY !== undefined) card.baseY += dy;
            card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;
          });

          const thisStageH = Math.max(group.height, nodeAreaH);
          stageRowMaxH = Math.max(stageRowMaxH, thisStageH);

          curX += group.width + sceneGapX;
          stageColIdx++;

          if (stageColIdx >= pipelineCols) {
            stageColIdx = 0;
            curX = minX;
            rowStartY += stageRowMaxH + sceneGapY;
            stageRowMaxH = 0;
          }
        });

        if (stageColIdx > 0) {
          rowStartY += stageRowMaxH + sceneGapY;
          curX = minX;
        }
        currentY = rowStartY;
      }

      // Standalone Loose Cards in selection
      if (targetLooseCards.length > 0) {
        let looseX = minX;
        let looseRowH = 0;
        const maxCardsW = minX + 3200;
        targetLooseCards.forEach(c => {
          if (looseX > minX && looseX + c.width > maxCardsW) {
            looseX = minX;
            currentY += looseRowH + 24;
            looseRowH = 0;
          }
          c.x = looseX;
          c.y = currentY;
          if (c.baseX !== undefined) c.baseX = c.x;
          if (c.baseY !== undefined) c.baseY = c.y;
          if (c.element) c.element.style.transform = `translate(${c.x}px, ${c.y}px)`;
          looseRowH = Math.max(looseRowH, c.height);
          looseX += c.width + 24;
        });
        currentY += looseRowH + 24;
      }

      this.renderConnections();
      this.commitHistory('Storyboard Pipeline Layout (Selection)');
      const totalMoved = targetGroups.length + targetNodes.length + targetLooseCards.length;
      Toast.show(`Storyboard Pipeline arranged for ${totalMoved} selected item(s)!`, 'success');
      return;
    }

    // ==========================================
    // ALL MODE: Arrange entire board (default)
    // ==========================================
    this.recordPreState('Storyboard Pipeline Layout');

    // 1. Tidy all groups first
    this.groups.forEach(group => {
      this.tidyGroup(group, true);
    });

    // 2. Sort groups naturally (Scene 01, Scene 02, etc.)
    const sortedGroups = [...this.groups].sort((a, b) => {
      return a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' });
    });

    const positionedNodeIds = new Set();
    const groupNodeMap = new Map();
    sortedGroups.forEach(g => groupNodeMap.set(g.id, []));

    this.nodes.forEach(n => {
      const gid = this.getNodeGroupId(n.id);
      if (gid && groupNodeMap.has(gid)) {
        groupNodeMap.get(gid).push(n);
      }
    });

    const standaloneNodes = this.nodes.filter(n => {
      const gid = this.getNodeGroupId(n.id);
      return !gid || !groupNodeMap.has(gid);
    });

    let currentY = 0;

    // 3. Standalone / Global Overview Header (placed strictly at top, never overlapping scenes)
    if (standaloneNodes.length > 0) {
      let sX = 0;
      let sRowMaxH = 0;
      const maxHeaderWidth = 3200;

      standaloneNodes.forEach(node => {
        const w = this.getAccurateNodeWidth(node);
        const h = this.getAccurateNodeHeight(node);
        if (sX > 0 && sX + w > maxHeaderWidth) {
          sX = 0;
          currentY += sRowMaxH + nodeGapY;
          sRowMaxH = 0;
        }
        node.x = sX;
        node.y = currentY;
        if (node.element) node.element.style.transform = `translate(${node.x}px, ${node.y}px)`;
        positionedNodeIds.add(node.id);
        sX += w + colGapX;
        sRowMaxH = Math.max(sRowMaxH, h);
      });

      currentY += sRowMaxH + sceneGapY * 1.5;
    }

    // 4. Sequential Pipeline Flow (Widescreen 3-Scene stage rows, wrapping cleanly)
    if (sortedGroups.length > 0) {
      const pipelineCols = Math.min(sortedGroups.length, 3);
      let curX = 0;
      let rowStartY = currentY;
      let stageRowMaxH = 0;
      let stageColIdx = 0;

      sortedGroups.forEach(group => {
        const connectedNodes = groupNodeMap.get(group.id) || [];
        let nodeAreaH = 0;

        if (connectedNodes.length > 0) {
          const layers = this.assignNodeLayers(connectedNodes, this.connections, group.id);
          const layerBuckets = new Map();
          connectedNodes.forEach(n => {
            const l = layers.get(n.id) || 1;
            if (!layerBuckets.has(l)) layerBuckets.set(l, []);
            layerBuckets.get(l).push(n);
          });

          const sortedLayers = Array.from(layerBuckets.keys()).sort((a, b) => b - a);

          sortedLayers.forEach(l => {
            const nodesInLayer = layerBuckets.get(l);
            let layerMaxW = 0;
            let nY = rowStartY;

            nodesInLayer.forEach(node => {
              node.x = curX;
              node.y = nY;
              if (node.element) node.element.style.transform = `translate(${node.x}px, ${node.y}px)`;
              positionedNodeIds.add(node.id);
              const nodeW = this.getAccurateNodeWidth(node);
              const nodeH = this.getAccurateNodeHeight(node);
              layerMaxW = Math.max(layerMaxW, nodeW);
              nY += nodeH + nodeGapY;
            });

            nodeAreaH = Math.max(nodeAreaH, nY - rowStartY);
            curX += layerMaxW + colGapX;
          });
        }

        // Group placed to the right of its nodes
        const dx = curX - group.x;
        const dy = rowStartY - group.y;
        group.x = curX;
        group.y = rowStartY;
        group.element.style.transform = `translate(${group.x}px, ${group.y}px)`;

        const memberCards = this.cards.filter(c => c.groupId === group.id);
        memberCards.forEach(card => {
          card.x += dx;
          card.y += dy;
          if (card.baseX !== undefined) card.baseX += dx;
          if (card.baseY !== undefined) card.baseY += dy;
          card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;
        });

        const thisStageH = Math.max(group.height, nodeAreaH);
        stageRowMaxH = Math.max(stageRowMaxH, thisStageH);

        curX += group.width + sceneGapX;
        stageColIdx++;

        if (stageColIdx >= pipelineCols) {
          stageColIdx = 0;
          curX = 0;
          rowStartY += stageRowMaxH + sceneGapY;
          stageRowMaxH = 0;
        }
      });

      if (stageColIdx > 0) {
        rowStartY += stageRowMaxH + sceneGapY;
        curX = 0;
      }
      currentY = rowStartY;
    }

    // 5. Standalone Loose Cards
    const looseCards = this.cards.filter(c => !c.groupId);
    if (looseCards.length > 0) {
      let looseX = 0;
      let looseRowH = 0;
      const maxCardsW = 3200;
      looseCards.forEach(c => {
        if (looseX > 0 && looseX + c.width > maxCardsW) {
          looseX = 0;
          currentY += looseRowH + 24;
          looseRowH = 0;
        }
        c.x = looseX;
        c.y = currentY;
        if (c.baseX !== undefined) c.baseX = c.x;
        if (c.baseY !== undefined) c.baseY = c.y;
        if (c.element) c.element.style.transform = `translate(${c.x}px, ${c.y}px)`;
        looseRowH = Math.max(looseRowH, c.height);
        looseX += c.width + 24;
      });
      currentY += looseRowH + 24;
    }

    this.renderConnections();
    this.commitHistory('Storyboard Pipeline Layout');
    setTimeout(() => this.fitAllToView(), 120);
    Toast.show('Storyboard Pipeline arranged!', 'success');
  }

  fitAllToView() {
    if (this.cards.length === 0 && this.groups.length === 0 && this.nodes.length === 0) {
      this.canvas.resetView();
      return;
    }

    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    this.cards.forEach(c => {
      minX = Math.min(minX, c.x);
      minY = Math.min(minY, c.y);
      maxX = Math.max(maxX, c.x + c.width);
      maxY = Math.max(maxY, c.y + c.height);
    });

    this.groups.forEach(g => {
      minX = Math.min(minX, g.x);
      minY = Math.min(minY, g.y);
      maxX = Math.max(maxX, g.x + g.width);
      maxY = Math.max(maxY, g.y + g.height);
    });

    this.nodes.forEach(n => {
      minX = Math.min(minX, n.x);
      minY = Math.min(minY, n.y);
      maxX = Math.max(maxX, n.x + n.width);
      maxY = Math.max(maxY, n.y + (n.element ? n.element.offsetHeight : 140));
    });

    const boardWidth = maxX - minX;
    const boardHeight = maxY - minY;
    const padding = 100;

    const scaleX = (window.innerWidth - padding * 2) / boardWidth;
    const scaleY = (window.innerHeight - padding * 2) / boardHeight;
    const targetZoom = Math.min(1.2, Math.max(0.15, Math.min(scaleX, scaleY)));

    this.canvas.zoom = targetZoom;
    this.canvas.panX = window.innerWidth / 2 - (minX + boardWidth / 2) * targetZoom;
    this.canvas.panY = window.innerHeight / 2 - (minY + boardHeight / 2) * targetZoom;
    this.canvas.updateTransform();
  }

  updateCardCount() {
    const count = this.cards.length;
    document.getElementById('ref-counter').textContent = `${count} Reference${count === 1 ? '' : 's'}`;
    const emptyState = document.getElementById('empty-state');
    if (emptyState) {
      const hasContent = this.cards.length > 0 || this.groups.length > 0 || this.nodes.length > 0;
      emptyState.classList.toggle('hidden', hasContent);
    }
  }

  extractNodeInfoForExport(nodes) {
    let fontText = '';
    let fontFamily = '';
    let sampleText = '';
    let notesText = '';
    let vfxText = '';

    nodes.forEach(n => {
      if (n.type === 'typography' || n.type === 'font') {
        const family = (n.fontFamily || 'Sans-Serif').trim();
        const userContent = (n.content || '').trim();
        const sample = (userContent && userContent !== 'DropBoard Studio 2026' && userContent !== 'Typography Sample Text') 
          ? userContent 
          : ((n.title && n.title !== 'Typography') ? n.title.trim() : family);
        if (!fontFamily) fontFamily = family;
        if (!sampleText) sampleText = sample;
        fontText += `• ${family}: "${sample}"\n`;
      } else if (n.type === 'note' || n.type === 'script') {
        const title = n.title ? `[${n.title}]` : '[Note]';
        if (n.mode === 'checklist' && Array.isArray(n.items) && n.items.length > 0) {
          const list = n.items
            .filter(it => it && it.text && it.text.trim())
            .map(it => `  [${it.done ? 'x' : ' '}] ${it.text.trim()}`)
            .join('\n');
          if (list) notesText += `${title}\n${list}\n\n`;
        } else {
          const text = (n.content || n.text || '').trim();
          if (text) notesText += `${title}\n${text}\n\n`;
        }
      } else if (n.type === 'vfx') {
        const title = n.title ? `[${n.title}]: ` : '';
        const tags = (n.tags && n.tags.length > 0) ? n.tags.join(', ') : (n.content || n.text || '');
        if (tags) vfxText += `* ${title}${tags}\n`;
      } else {
        const title = n.title || n.type;
        const content = (n.content || n.text || '').trim();
        if (content) notesText += `[${title}]: ${content}\n\n`;
      }
    });

    return {
      fontFamily: fontFamily.trim(),
      sampleText: sampleText.trim(),
      fontText: fontText.trim(),
      notesText: notesText.trim(),
      vfxText: vfxText.trim()
    };
  }

  exportSelectionToAfterEffects(preferredGroup = null) {
    const selCards = Array.from(this.selectedCards || []);
    if (this.selectedCard && !selCards.includes(this.selectedCard)) selCards.push(this.selectedCard);

    const selGroups = Array.from(this.selectedGroups || []);
    if (preferredGroup && !selGroups.includes(preferredGroup)) selGroups.push(preferredGroup);
    else if (this.selectedGroup && !selGroups.includes(this.selectedGroup)) selGroups.push(this.selectedGroup);

    const selNodes = Array.from(this.selectedNodes || []);
    if (this.selectedNode && !selNodes.includes(this.selectedNode)) selNodes.push(this.selectedNode);

    // 1. Group Reference Comp Mode:
    // Only if preferredGroup was specifically chosen, OR only a group frame was selected without card selection
    let targetGroup = preferredGroup;
    if (!targetGroup && selGroups.length > 0 && selCards.length === 0) {
      targetGroup = selGroups[0];
    }

    if (targetGroup) {
      let groupCards = this.cards.filter(c => c.groupId === targetGroup.id);

      if (groupCards.length === 0) {
        Toast.show(`Group "${targetGroup.title}" has no reference images to export`, 'info');
        return;
      }

      const connectedNodes = this.nodes.filter(n => this.getNodeGroupId(n.id) === targetGroup.id);
      selNodes.forEach(n => {
        if (!connectedNodes.includes(n)) connectedNodes.push(n);
      });

      const nodeInfo = this.extractNodeInfoForExport(connectedNodes);

      const extractCardExportData = (c) => {
        let imgData = '';
        const imgEl = c.element ? c.element.querySelector('img') : null;
        if (imgEl && imgEl.complete && imgEl.naturalWidth > 0) {
          try {
            const cvs = document.createElement('canvas');
            cvs.width = imgEl.naturalWidth;
            cvs.height = imgEl.naturalHeight;
            const ctx = cvs.getContext('2d');
            ctx.drawImage(imgEl, 0, 0);
            const isPng = (c.url && c.url.toLowerCase().endsWith('.png')) || (c.localPath && c.localPath.toLowerCase().endsWith('.png'));
            imgData = cvs.toDataURL(isPng ? 'image/png' : 'image/jpeg', 0.92);
          } catch(e) {}
        }
        if (!imgData) {
          imgData = c.imageData || this.getCardImageData(c) || '';
        }
        return imgData;
      };

      const items = groupCards.map(c => ({
        filePath: c.localPath || '',
        imageData: extractCardExportData(c),
        relX: c.x - targetGroup.x,
        relY: c.y - targetGroup.y,
        width: c.width || 300,
        height: c.height || 200
      }));

      Toast.show(`Exporting "${targetGroup.title}" Reference Comp to After Effects...`, 'info');
      NativeBridge.exportToAEComp({
        mode: 'group_comp',
        compName: targetGroup.title,
        compWidth: 1920,
        compHeight: 1080,
        fontFamily: nodeInfo.fontFamily,
        sampleText: nodeInfo.sampleText,
        notesText: nodeInfo.notesText,
        fontText: nodeInfo.fontText,
        vfxText: nodeInfo.vfxText,
        items
      });
      return;
    }

    // 2. Loose Photos Mode (user selected photos directly, or selected multiple cards):
    if (selCards.length > 0) {
      const extractCardExportData = (c) => {
        let imgData = '';
        const imgEl = c.element ? c.element.querySelector('img') : null;
        if (imgEl && imgEl.complete && imgEl.naturalWidth > 0) {
          try {
            const cvs = document.createElement('canvas');
            cvs.width = imgEl.naturalWidth;
            cvs.height = imgEl.naturalHeight;
            const ctx = cvs.getContext('2d');
            ctx.drawImage(imgEl, 0, 0);
            const isPng = (c.url && c.url.toLowerCase().endsWith('.png')) || (c.localPath && c.localPath.toLowerCase().endsWith('.png'));
            imgData = cvs.toDataURL(isPng ? 'image/png' : 'image/jpeg', 0.92);
          } catch(e) {}
        }
        if (!imgData) {
          imgData = c.imageData || this.getCardImageData(c) || '';
        }
        return imgData;
      };

      const items = selCards.map(c => ({
        filePath: c.localPath || '',
        imageData: extractCardExportData(c),
        relX: c.x,
        relY: c.y,
        width: c.width || 300,
        height: c.height || 200
      }));

      Toast.show(`Importing ${items.length} reference photo(s) into After Effects...`, 'info');
      NativeBridge.exportToAEComp({
        mode: 'loose_photos',
        items
      });
      return;
    }

    Toast.show('Select reference photos or a Scene Group to export to After Effects', 'info');
  }

  // ===========================================================================
  // Custom Context Menu
  // ===========================================================================
  initContextMenu() {
    const cm = document.getElementById('context-menu');
    const grpCm = document.getElementById('group-context-menu');
    const canvasCm = document.getElementById('canvas-context-menu');

    document.addEventListener('click', () => {
      cm.classList.remove('show');
      if (grpCm) grpCm.classList.remove('show');
      if (canvasCm) canvasCm.classList.remove('show');
    });

    if (canvasCm) {
      canvasCm.querySelectorAll('button[data-canvas-action]').forEach(btn => {
        btn.addEventListener('click', (e) => {
          e.stopPropagation();
          canvasCm.classList.remove('show');
          const act = btn.dataset.canvasAction;
          const pos = this.lastCanvasClickWorldPos || this.canvas.screenToWorld(window.innerWidth / 2, window.innerHeight / 2);
          this.executeQuickAction(act, pos.x, pos.y);
        });
      });
    }

    // Viewport Right-Click -> Canvas Context Menu
    this.viewport.addEventListener('contextmenu', (e) => {
      if (e.target.closest('.ref-card') || e.target.closest('.mv-group-frame') || e.target.closest('.prod-node') || e.target.closest('.floating-dock') || e.target.closest('.canvas-hud') || e.target.closest('.titlebar')) {
        return;
      }
      e.preventDefault();
      e.stopPropagation();
      this.showCanvasContextMenu(e.clientX, e.clientY);
    });

    // Zoom to Reference Card
    const btnZoomCard = document.getElementById('cm-zoom-card');
    if (btnZoomCard) {
      btnZoomCard.addEventListener('click', () => {
        if (this.activeContextMenuCard) {
          this.zoomToCard(this.activeContextMenuCard);
        }
      });
    }

    // Group Context Menu Listeners
    if (grpCm) {
      const btnGrpZoom = document.getElementById('cm-grp-zoom');
      if (btnGrpZoom) {
        btnGrpZoom.addEventListener('click', () => {
          if (this.activeContextMenuGroup) {
            this.zoomToGroup(this.activeContextMenuGroup);
          }
        });
      }

      const btnGrpTidy = document.getElementById('cm-grp-tidy');
      if (btnGrpTidy) {
        btnGrpTidy.addEventListener('click', () => {
          if (this.activeContextMenuGroup) {
            this.recordPreState('Tidy Group');
            this.tidyGroup(this.activeContextMenuGroup, true);
            this.commitHistory('Tidy Group');
            Toast.show(`Tidied references in ${this.activeContextMenuGroup.title}`, 'success');
          }
        });
      }

      const btnGrpExportAe = document.getElementById('cm-grp-export-ae');
      if (btnGrpExportAe) {
        btnGrpExportAe.addEventListener('click', () => {
          if (this.activeContextMenuGroup) {
            this.exportSelectionToAfterEffects(this.activeContextMenuGroup);
          }
        });
      }

      const btnGrpRename = document.getElementById('cm-grp-rename');
      if (btnGrpRename) {
        btnGrpRename.addEventListener('click', () => {
          if (this.activeContextMenuGroup && this.activeContextMenuGroup.element) {
            const titleEl = this.activeContextMenuGroup.element.querySelector('.group-title');
            if (titleEl) {
              const dblClickEvt = new MouseEvent('dblclick', { bubbles: true, cancelable: true });
              titleEl.dispatchEvent(dblClickEvt);
            }
          }
        });
      }

      const btnGrpDelete = document.getElementById('cm-grp-delete');
      if (btnGrpDelete) {
        btnGrpDelete.addEventListener('click', () => {
          if (this.activeContextMenuGroup && this.activeContextMenuGroup.element) {
            const btnDel = this.activeContextMenuGroup.element.querySelector('.group-btn');
            if (btnDel) btnDel.click();
          }
        });
      }
    }

    document.getElementById('cm-send-ae').addEventListener('click', () => {
      const selCards = Array.from(this.selectedCards || []);
      if (this.selectedCard && !selCards.includes(this.selectedCard)) selCards.push(this.selectedCard);
      const targetCard = this.activeContextMenuCard || this.selectedCard;

      if (selCards.length > 1) {
        this.exportSelectionToAfterEffects();
      } else if (targetCard && (targetCard.localPath || targetCard.imageData)) {
        NativeBridge.sendToAE(targetCard.localPath || '', targetCard.imageData || this.getCardImageData(targetCard) || '');
      } else {
        Toast.show('Select a reference card first', 'info');
      }
    });

    document.getElementById('cm-send-ps').addEventListener('click', () => {
      const c = this.activeContextMenuCard;
      if (c && (c.localPath || c.imageData)) {
        NativeBridge.sendToPhotoshop(c.localPath || '', c.imageData || '');
      }
    });

    document.getElementById('cm-copy-file').addEventListener('click', () => {
      const c = this.activeContextMenuCard;
      if (c && (c.localPath || c.imageData)) {
        NativeBridge.copyFileToClipboard(c.localPath || '', c.imageData || '');
      }
    });

    const cmPlayYt = document.getElementById('cm-play-yt');
    if (cmPlayYt) {
      cmPlayYt.addEventListener('click', () => {
        if (this.activeContextMenuCard) {
          this.toggleYouTubePlayback(this.activeContextMenuCard);
        }
      });
    }

    document.getElementById('cm-open-yt').addEventListener('click', () => {
      if (this.activeContextMenuCard && this.activeContextMenuCard.youtubeUrl) {
        NativeBridge.openDefault(this.activeContextMenuCard.youtubeUrl);
      }
    });

    document.getElementById('cm-copy-yt-link').addEventListener('click', () => {
      if (this.activeContextMenuCard && this.activeContextMenuCard.youtubeUrl) {
        navigator.clipboard.writeText(this.activeContextMenuCard.youtubeUrl);
        Toast.show('Copied YouTube link to clipboard!', 'success');
      }
    });

    document.getElementById('cm-crop-card').addEventListener('click', () => {
      if (this.activeContextMenuCard) {
        this.startCropCard(this.activeContextMenuCard);
      }
    });

    const btnDetachGroup = document.getElementById('cm-detach-group');
    if (btnDetachGroup) {
      btnDetachGroup.addEventListener('click', () => {
        if (this.activeContextMenuCard && this.activeContextMenuCard.groupId) {
          this.recordPreState('Detach from Group');
          const group = this.groups.find(g => g.id === this.activeContextMenuCard.groupId);
          this.activeContextMenuCard.groupId = null;
          this.updateCardGroupBadge(this.activeContextMenuCard);
        }
      });
    }

    document.getElementById('cm-reveal-explorer').addEventListener('click', () => {
      const c = this.activeContextMenuCard;
      if (c && (c.localPath || c.imageData)) {
        NativeBridge.revealInExplorer(c.localPath || '', c.imageData || '');
      } else {
        NativeBridge.openCacheFolder();
        Toast.show('Opened Image Cache folder', 'info');
      }
    });

    const btnMultiGrid = document.getElementById('cm-multi-grid');
    if (btnMultiGrid) {
      btnMultiGrid.addEventListener('click', () => {
        cm.classList.remove('show');
        this.autoArrangeGrid();
      });
    }

    const btnMultiPipeline = document.getElementById('cm-multi-pipeline');
    if (btnMultiPipeline) {
      btnMultiPipeline.addEventListener('click', () => {
        cm.classList.remove('show');
        this.autoArrangePipeline();
      });
    }

    const btnMultiGroup = document.getElementById('cm-multi-group');
    if (btnMultiGroup) {
      btnMultiGroup.addEventListener('click', () => {
        cm.classList.remove('show');
        const selCards = Array.from(this.selectedCards || []);
        if (selCards.length > 0) {
          this.recordPreState('Group Selection');
          let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
          selCards.forEach(c => {
            minX = Math.min(minX, c.x);
            minY = Math.min(minY, c.y);
            maxX = Math.max(maxX, c.x + c.width);
            maxY = Math.max(maxY, c.y + c.height);
          });
          const next = this.getNextSceneInfo();
          const pad = 24;
          const grp = this.createGroup(next.title, minX - pad, minY - 36 - pad, Math.max(260, maxX - minX + pad * 2), Math.max(200, maxY - minY + 36 + pad * 2), next.color);
          selCards.forEach(c => {
            c.groupId = grp.id;
            this.updateCardGroupBadge(c);
          });
          this.updateGroupCounts();
          this.tidyGroup(grp, true);
          this.commitHistory('Group Selection');
          Toast.show(`Grouped ${selCards.length} references into ${grp.title}`, 'success');
        }
      });
    }

    document.getElementById('cm-bring-front').addEventListener('click', () => {
      const selCards = Array.from(this.selectedCards || []);
      if (selCards.length > 1) {
        selCards.forEach(c => {
          c.zIndex = ++this.highestZ;
          if (c.element) c.element.style.zIndex = c.zIndex;
        });
      } else if (this.activeContextMenuCard) {
        this.activeContextMenuCard.zIndex = ++this.highestZ;
        this.activeContextMenuCard.element.style.zIndex = this.activeContextMenuCard.zIndex;
      }
    });

    document.getElementById('cm-reset-scale').addEventListener('click', () => {
      const selCards = Array.from(this.selectedCards || []);
      const targets = selCards.length > 1 ? selCards : (this.activeContextMenuCard ? [this.activeContextMenuCard] : []);
      targets.forEach(c => {
        const img = c.element ? c.element.querySelector('img') : null;
        if (img && img.naturalWidth) {
          c.width = img.naturalWidth;
          c.height = img.naturalHeight;
          c.element.style.width = `${img.naturalWidth}px`;
          c.element.style.height = `${img.naturalHeight}px`;
        }
      });
      if (targets.length > 0) Toast.show(`Reset 1:1 Scale for ${targets.length} reference(s)`, 'info');
    });

    document.getElementById('cm-delete').addEventListener('click', () => {
      const selCards = Array.from(this.selectedCards || []);
      if (selCards.length > 1) {
        this.recordPreState('Delete References');
        selCards.forEach(c => this.removeCard(c));
        this.commitHistory('Delete References');
        Toast.show(`Deleted ${selCards.length} references`, 'info');
      } else if (this.activeContextMenuCard) {
        this.removeCard(this.activeContextMenuCard);
      }
    });
  }

  showContextMenu(screenX, screenY, card) {
    this.activeContextMenuCard = card;
    const cm = document.getElementById('context-menu');
    const grpCm = document.getElementById('group-context-menu');
    const canvasCm = document.getElementById('canvas-context-menu');
    if (grpCm) grpCm.classList.remove('show');
    if (canvasCm) canvasCm.classList.remove('show');

    const selCards = Array.from(this.selectedCards || []);
    const isMulti = selCards.length > 1 && selCards.includes(card);
    const multiSec = document.getElementById('cm-multi-section');
    if (multiSec) {
      multiSec.style.display = isMulti ? 'block' : 'none';
    }

    const titleEl = document.getElementById('cm-ref-title');
    if (titleEl) {
      titleEl.textContent = isMulti 
        ? `${selCards.length} References Selected` 
        : (card.sourceLabel || (card.isYouTube ? 'YouTube Ref' : 'Reference'));
    }

    const aeBtnText = document.querySelector('#cm-send-ae strong');
    if (aeBtnText) {
      aeBtnText.textContent = isMulti ? `Send to After Effects (${selCards.length})` : 'Send to After Effects';
    }

    const ytItems = cm.querySelectorAll('.cm-yt-item');
    ytItems.forEach(el => {
      el.style.display = card.isYouTube ? 'flex' : 'none';
    });
    const cmPlayYtText = document.getElementById('cm-play-yt-text');
    if (cmPlayYtText) {
      cmPlayYtText.textContent = card.isPlayingYouTube ? 'Stop Video Player' : 'Play in DropBoard';
    }

    const detachItem = document.getElementById('cm-detach-group');
    if (detachItem) {
      if (card.groupId) {
        detachItem.style.display = 'flex';
        const group = this.groups.find(g => g.id === card.groupId);
        document.getElementById('cm-detach-group-text').textContent = `Unlock from ${group ? group.title : 'Group'}`;
      } else {
        detachItem.style.display = 'none';
      }
    }

    // Show first to measure actual rendered height
    cm.classList.add('show');
    const menuW = cm.offsetWidth || 230;
    const menuH = cm.offsetHeight || 420;

    let posX = screenX;
    let posY = screenY;

    // Flip or clamp horizontally
    if (posX + menuW > window.innerWidth - 12) {
      posX = Math.max(12, window.innerWidth - menuW - 12);
    }

    // Flip or clamp vertically: If cursor is near bottom, position menu ABOVE cursor
    if (posY + menuH > window.innerHeight - 12) {
      if (screenY - menuH >= 12) {
        posY = screenY - menuH;
      } else {
        posY = Math.max(12, window.innerHeight - menuH - 12);
      }
    }

    cm.style.left = `${Math.round(posX)}px`;
    cm.style.top = `${Math.round(posY)}px`;
  }

  showGroupContextMenu(screenX, screenY, group) {
    this.activeContextMenuGroup = group;
    const cm = document.getElementById('context-menu');
    if (cm) cm.classList.remove('show');
    const canvasCm = document.getElementById('canvas-context-menu');
    if (canvasCm) canvasCm.classList.remove('show');

    const grpCm = document.getElementById('group-context-menu');
    if (grpCm) {
      document.getElementById('cm-grp-title').textContent = group.title || 'Scene Group';
      grpCm.classList.add('show');
      const menuW = grpCm.offsetWidth || 220;
      const menuH = grpCm.offsetHeight || 200;

      let posX = screenX;
      let posY = screenY;

      if (posX + menuW > window.innerWidth - 12) {
        posX = Math.max(12, window.innerWidth - menuW - 12);
      }
      if (posY + menuH > window.innerHeight - 12) {
        if (screenY - menuH >= 12) {
          posY = screenY - menuH;
        } else {
          posY = Math.max(12, window.innerHeight - menuH - 12);
        }
      }

      grpCm.style.left = `${Math.round(posX)}px`;
      grpCm.style.top = `${Math.round(posY)}px`;
    }
  }

  showCanvasContextMenu(screenX, screenY) {
    const cm = document.getElementById('context-menu');
    if (cm) cm.classList.remove('show');
    const grpCm = document.getElementById('group-context-menu');
    if (grpCm) grpCm.classList.remove('show');

    this.lastCanvasClickWorldPos = this.canvas.screenToWorld(screenX, screenY);
    const canvasCm = document.getElementById('canvas-context-menu');
    if (canvasCm) {
      canvasCm.classList.add('show');
      const menuW = canvasCm.offsetWidth || 220;
      const menuH = canvasCm.offsetHeight || 250;

      let posX = screenX;
      let posY = screenY;

      if (posX + menuW > window.innerWidth - 12) {
        posX = Math.max(12, window.innerWidth - menuW - 12);
      }
      if (posY + menuH > window.innerHeight - 12) {
        if (screenY - menuH >= 12) {
          posY = screenY - menuH;
        } else {
          posY = Math.max(12, window.innerHeight - menuH - 12);
        }
      }

      canvasCm.style.left = `${Math.round(posX)}px`;
      canvasCm.style.top = `${Math.round(posY)}px`;
    }
  }

  executeQuickAction(act, worldX, worldY) {
    if (act === 'note') {
      this.recordPreState('Add Note');
      this.createNode('note', worldX - 130, worldY - 60);
      this.commitHistory('Add Note');
      Toast.show('Added Note Node', 'success');
    } else if (act === 'group') {
      this.recordPreState('Add Scene Group');
      const next = this.getNextSceneInfo();
      this.createGroup(next.title, worldX - 230, worldY - 180, 460, 380, next.color);
      this.commitHistory('Add Scene Group');
      Toast.show(`Created ${next.title}`, 'success');
    } else if (act === 'font') {
      this.recordPreState('Add Typography Node');
      this.createNode('font', worldX - 130, worldY - 60);
      this.commitHistory('Add Typography Node');
      Toast.show('Added Typography Node', 'success');
    } else if (act === 'vfx') {
      this.recordPreState('Add VFX Node');
      this.createNode('vfx', worldX - 130, worldY - 60);
      this.commitHistory('Add VFX Node');
      Toast.show('Added VFX Spec Node', 'success');
    } else if (act === 'plan') {
      this.recordPreState('Add Plan Node');
      this.createNode('plan', worldX - 130, worldY - 60);
      this.commitHistory('Add Plan Node');
      Toast.show('Added Plan Node', 'success');
    } else if (act === 'paste') {
      navigator.clipboard.read().then(items => {
        for (const item of items) {
          const imageType = item.types.find(t => t.startsWith('image/'));
          if (imageType) {
            item.getType(imageType).then(blob => {
              this.handleLocalFile(blob, worldX, worldY);
              Toast.show('Pasted image to canvas', 'success');
            });
            return;
          }
        }
        Toast.show('No image in clipboard', 'info');
      }).catch(() => {
        Toast.show('Press Ctrl+V to paste', 'info');
      });
    } else if (act === 'grid') {
      this.autoArrangeGrid();
    } else if (act === 'pipeline') {
      this.autoArrangePipeline();
    } else if (act === 'file') {
      document.getElementById('file-input')?.click();
    } else if (act === 'url') {
      const urlModal = document.getElementById('url-modal');
      if (urlModal) {
        urlModal.classList.add('show');
        setTimeout(() => document.getElementById('url-input')?.focus(), 50);
      }
    } else if (act === 'fit') {
      this.fitAllToView();
    }
  }

  initBlenderSearch() {
    const popup = document.getElementById('blender-search-popup');
    const input = document.getElementById('blender-search-input');
    const list = document.getElementById('blender-popup-list');
    if (!popup || !input || !list) return;

    const items = [
      {
        id: 'note',
        title: 'Lyric / Script Note',
        desc: 'Customizable text, checklist, mood',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#38bdf8" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>',
        action: (pos) => this.executeQuickAction('note', pos.x, pos.y)
      },
      {
        id: 'chk',
        title: 'Checklist / Task Note',
        desc: 'Interactive to-do list for scenes',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#38bdf8" stroke-width="2"><polyline points="9 11 12 14 22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>',
        action: (pos) => {
          this.recordPreState('Add Checklist Note');
          this.createNode('note', pos.x - 130, pos.y - 60, { mode: 'checklist', title: 'Scene Checklist' });
          this.commitHistory('Add Checklist Note');
          Toast.show('Added Checklist Note', 'success');
        }
      },
      {
        id: 'group',
        title: 'MV Scene Group Frame',
        desc: 'Organize references in storyboard scene',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#3b82f6" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="3" stroke-dasharray="3 2"/><path d="M8 8h8v3H8z"/></svg>',
        action: (pos) => this.executeQuickAction('group', pos.x, pos.y)
      },
      {
        id: 'font',
        title: 'Typography / Font Node',
        desc: 'Font name, size & typography spec',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#a855f7" stroke-width="2"><polyline points="4 7 4 4 20 4 20 7"/><line x1="9" y1="20" x2="15" y2="20"/><line x1="12" y1="4" x2="12" y2="20"/></svg>',
        action: (pos) => this.executeQuickAction('font', pos.x, pos.y)
      },
      {
        id: 'vfx',
        title: 'VFX / Filter Spec Node',
        desc: 'Visual effects, glow, transitions',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#10b981" stroke-width="2"><path d="m12 3-1.9 5.8a2 2 0 0 1-1.3 1.3L3 12l5.8 1.9a2 2 0 0 1 1.3 1.3L12 21l1.9-5.8a2 2 0 0 1 1.3-1.3L21 12l-5.8-1.9a2 2 0 0 1-1.3-1.3L12 3z"/></svg>',
        action: (pos) => this.executeQuickAction('vfx', pos.x, pos.y)
      },
      {
        id: 'plan',
        title: 'Schedule / Deadline Node',
        desc: 'Timeline, status & target date',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="#f43f5e" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>',
        action: (pos) => this.executeQuickAction('plan', pos.x, pos.y)
      },
      {
        id: 'file',
        title: 'Add Image from File',
        desc: 'Choose image file from computer',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14m-7-7h14"/></svg>',
        action: () => document.getElementById('file-input').click()
      },
      {
        id: 'url',
        title: 'Add Reference from Web / URL',
        desc: 'YouTube, Pinterest, or image link',
        icon: '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/></svg>',
        action: () => document.getElementById('btn-add-url').click()
      }
    ];

    let selectedIndex = 0;
    let filteredItems = [...items];

    const renderList = () => {
      list.innerHTML = '';
      if (filteredItems.length === 0) {
        list.innerHTML = '<div style="padding:10px;text-align:center;font-size:11px;color:var(--text-muted);">No matching actions</div>';
        return;
      }
      filteredItems.forEach((item, idx) => {
        const el = document.createElement('div');
        el.className = `blender-item ${idx === selectedIndex ? 'active' : ''}`;
        el.innerHTML = `
          <div class="blender-item-icon">${item.icon}</div>
          <div class="blender-item-text">
            <span class="blender-item-title">${item.title}</span>
            <span class="blender-item-desc">${item.desc}</span>
          </div>
        `;
        el.addEventListener('click', () => {
          popup.classList.remove('show');
          const pos = this.canvas.screenToWorld(this.blenderSpawnScreenX, this.blenderSpawnScreenY);
          item.action(pos);
        });
        list.appendChild(el);
      });
    };

    input.addEventListener('input', () => {
      const q = input.value.trim().toLowerCase();
      filteredItems = items.filter(it => it.title.toLowerCase().includes(q) || it.desc.toLowerCase().includes(q));
      selectedIndex = 0;
      renderList();
    });

    input.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        selectedIndex = (selectedIndex + 1) % Math.max(1, filteredItems.length);
        renderList();
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        selectedIndex = (selectedIndex - 1 + filteredItems.length) % Math.max(1, filteredItems.length);
        renderList();
      } else if (e.key === 'Enter') {
        e.preventDefault();
        if (filteredItems[selectedIndex]) {
          popup.classList.remove('show');
          const pos = this.canvas.screenToWorld(this.blenderSpawnScreenX, this.blenderSpawnScreenY);
          filteredItems[selectedIndex].action(pos);
        }
      } else if (e.key === 'Escape') {
        popup.classList.remove('show');
      }
    });

    document.addEventListener('click', (e) => {
      if (!popup.contains(e.target)) {
        popup.classList.remove('show');
      }
    });

    this.openNodePalette = (screenX, screenY) => {
      this.blenderSpawnScreenX = screenX || (window.innerWidth / 2);
      this.blenderSpawnScreenY = screenY || (window.innerHeight / 2);
      popup.style.left = `${Math.min(this.blenderSpawnScreenX, window.innerWidth - 290)}px`;
      popup.style.top = `${Math.min(this.blenderSpawnScreenY, window.innerHeight - 300)}px`;
      popup.classList.add('show');
      input.value = '';
      filteredItems = [...items];
      selectedIndex = 0;
      renderList();
      setTimeout(() => input.focus(), 50);
    };
    this.openBlenderSearch = this.openNodePalette;
  }

  zoomToElement(x, y, width, height, padding = 100) {
    const scaleX = (window.innerWidth - padding * 2) / Math.max(10, width);
    const scaleY = (window.innerHeight - padding * 2) / Math.max(10, height);
    const targetZoom = Math.min(2.0, Math.max(0.2, Math.min(scaleX, scaleY)));

    const targetPanX = window.innerWidth / 2 - (x + width / 2) * targetZoom;
    const targetPanY = window.innerHeight / 2 - (y + height / 2) * targetZoom;

    this.canvas.animateViewTo(targetPanX, targetPanY, targetZoom);
  }

  zoomToCard(card) {
    if (!card) return;
    this.selectCard(card);
    this.zoomToElement(card.x, card.y, card.width, card.height, 120);
    Toast.show(`Zoomed to ${card.sourceLabel || 'Reference'}`, 'info');
  }

  zoomToGroup(group) {
    if (!group) return;
    this.zoomToElement(group.x, group.y, group.width, group.height, 80);
    Toast.show(`Zoomed to ${group.title}`, 'info');
  }

  // ===========================================================================
  // Non-Destructive Crop & Mask System
  // ===========================================================================
  applyCropTransform(card) {
    if (!card || !card.element) return;

    if (!card.baseWidth) {
      card.baseWidth = card.width;
      card.baseHeight = card.height;
      card.baseX = card.x;
      card.baseY = card.y;
    }

    const { top = 0, right = 0, bottom = 0, left = 0 } = card.crop || {};
    const isCropped = (top > 0 || right > 0 || bottom > 0 || left > 0);
    card.isCropped = isCropped;

    if (isCropped) {
      const visW_pct = Math.max(0.05, (100 - left - right) / 100);
      const visH_pct = Math.max(0.05, (100 - top - bottom) / 100);

      card.width = Math.max(40, Math.round(card.baseWidth * visW_pct));
      card.height = Math.max(40, Math.round(card.baseHeight * visH_pct));
      card.aspectRatio = card.width / card.height;
      card.x = Math.round(card.baseX + card.baseWidth * (left / 100));
      card.y = Math.round(card.baseY + card.baseHeight * (top / 100));

      card.element.style.width = `${card.width}px`;
      card.element.style.height = `${card.height}px`;
      card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

      if (card.mediaContainer) {
        card.mediaContainer.style.clipPath = 'none';
        card.mediaContainer.style.overflow = 'hidden';
        card.mediaContainer.style.position = 'relative';
        const img = card.mediaContainer.querySelector('img');
        if (img) {
          img.style.position = 'absolute';
          img.style.width = `${card.baseWidth}px`;
          img.style.height = `${card.baseHeight}px`;
          img.style.maxWidth = 'none';
          img.style.maxHeight = 'none';
          img.style.left = `-${Math.round(card.baseWidth * (left / 100))}px`;
          img.style.top = `-${Math.round(card.baseHeight * (top / 100))}px`;
        }
      }
    } else {
      card.width = card.baseWidth;
      card.height = card.baseHeight;
      card.aspectRatio = card.width / card.height;
      card.x = card.baseX;
      card.y = card.baseY;

      card.element.style.width = `${card.width}px`;
      card.element.style.height = `${card.height}px`;
      card.element.style.transform = `translate(${card.x}px, ${card.y}px)`;

      if (card.mediaContainer) {
        card.mediaContainer.style.clipPath = 'none';
        card.mediaContainer.style.overflow = 'visible';
        const img = card.mediaContainer.querySelector('img');
        if (img) {
          img.style.position = 'static';
          img.style.width = '100%';
          img.style.height = '100%';
          img.style.maxWidth = '100%';
          img.style.maxHeight = '100%';
          img.style.left = '0';
          img.style.top = '0';
        }
      }
    }
  }

  prepareCardForCropEditing(card) {
    if (!card || !card.element) return;
    if (!card.baseWidth) {
      card.baseWidth = card.width;
      card.baseHeight = card.height;
      card.baseX = card.x;
      card.baseY = card.y;
    }

    // Expand back to uncropped rectangle so full image and dim masks are visible
    card.element.style.width = `${card.baseWidth}px`;
    card.element.style.height = `${card.baseHeight}px`;
    card.element.style.transform = `translate(${card.baseX}px, ${card.baseY}px)`;

    if (card.mediaContainer) {
      card.mediaContainer.style.overflow = 'visible';
      card.mediaContainer.style.clipPath = 'none';
      const img = card.mediaContainer.querySelector('img');
      if (img) {
        img.style.position = 'static';
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.left = '0';
        img.style.top = '0';
      }
    }
  }

  startCropCard(card) {
    if (!card || !card.element) return;
    if (card.element.classList.contains('is-cropping')) return;

    card.element.classList.add('is-cropping');
    this.recordPreState('Crop Reference');

    if (!card.crop) {
      card.crop = { top: 0, right: 0, bottom: 0, left: 0 };
    }

    // Expand to full image so crop editing is clear and intuitive
    this.prepareCardForCropEditing(card);

    const editor = document.createElement('div');
    editor.className = 'crop-overlay-editor';

    // 4 Dark Dim Masks (regions being cropped out)
    const maskTop = document.createElement('div');
    maskTop.className = 'crop-dim-mask';
    const maskBottom = document.createElement('div');
    maskBottom.className = 'crop-dim-mask';
    const maskLeft = document.createElement('div');
    maskLeft.className = 'crop-dim-mask';
    const maskRight = document.createElement('div');
    maskRight.className = 'crop-dim-mask';

    editor.appendChild(maskTop);
    editor.appendChild(maskBottom);
    editor.appendChild(maskLeft);
    editor.appendChild(maskRight);

    // Active Yellow Dashed Crop Box
    const activeBox = document.createElement('div');
    activeBox.className = 'crop-active-box';
    editor.appendChild(activeBox);

    const updateCropLayout = () => {
      const { top, right, bottom, left } = card.crop;

      // Update active dashed box
      activeBox.style.top = `${top}%`;
      activeBox.style.bottom = `${bottom}%`;
      activeBox.style.left = `${left}%`;
      activeBox.style.right = `${right}%`;

      // Update 4 dim masks around active box
      maskTop.style.top = '0';
      maskTop.style.left = '0';
      maskTop.style.right = '0';
      maskTop.style.height = `${top}%`;

      maskBottom.style.bottom = '0';
      maskBottom.style.left = '0';
      maskBottom.style.right = '0';
      maskBottom.style.height = `${bottom}%`;

      maskLeft.style.top = `${top}%`;
      maskLeft.style.bottom = `${bottom}%`;
      maskLeft.style.left = '0';
      maskLeft.style.width = `${left}%`;

      maskRight.style.top = `${top}%`;
      maskRight.style.bottom = `${bottom}%`;
      maskRight.style.right = '0';
      maskRight.style.width = `${right}%`;
    };

    // 4 Draggable edge bars
    ['top', 'bottom', 'left', 'right'].forEach(side => {
      const b = document.createElement('div');
      b.className = `crop-drag-border crop-border-${side}`;
      b.dataset.side = side;
      activeBox.appendChild(b);
      this.initCropDragHandle(b, side, card, updateCropLayout);
    });

    // 4 Corner handles
    ['nw', 'ne', 'sw', 'se'].forEach(corner => {
      const c = document.createElement('div');
      c.className = `crop-corner-handle crop-corner-${corner}`;
      c.dataset.corner = corner;
      activeBox.appendChild(c);
      this.initCropCornerHandle(c, corner, card, updateCropLayout);
    });

    updateCropLayout();

    // Floating Pill: Done & Reset (Always visible at bottom center!)
    const pill = document.createElement('div');
    pill.className = 'crop-controls-pill';

    const btnDone = document.createElement('button');
    btnDone.className = 'crop-btn-done';
    btnDone.textContent = '✓ Done (Enter)';

    const btnReset = document.createElement('button');
    btnReset.className = 'crop-btn-reset';
    btnReset.textContent = '↺ Reset (Esc)';

    const finishCrop = () => {
      window.removeEventListener('keydown', onCropKeyDown);
      card.element.classList.remove('is-cropping');
      editor.remove();
      this.applyCropTransform(card);
      this.commitHistory('Crop Reference');
      Toast.show('Crop applied (Fit to crop)', 'success');
    };

    const resetCrop = () => {
      card.crop = { top: 0, right: 0, bottom: 0, left: 0 };
      this.applyCropTransform(card);
      window.removeEventListener('keydown', onCropKeyDown);
      card.element.classList.remove('is-cropping');
      editor.remove();
      this.commitHistory('Reset Crop');
      Toast.show('Crop reset to full image', 'info');
    };

    const onCropKeyDown = (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        finishCrop();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        resetCrop();
      }
    };

    btnDone.addEventListener('click', (e) => {
      e.stopPropagation();
      finishCrop();
    });

    btnReset.addEventListener('click', (e) => {
      e.stopPropagation();
      resetCrop();
    });

    window.addEventListener('keydown', onCropKeyDown);

    pill.appendChild(btnDone);
    pill.appendChild(btnReset);
    editor.appendChild(pill);
    card.element.appendChild(editor);

    Toast.show('Drag yellow borders/corners to crop. Press Enter when done.', 'info');
  }

  initCropDragHandle(handle, side, card, onUpdate) {
    handle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      const startMouseX = e.clientX;
      const startMouseY = e.clientY;
      const initialCrop = { ...card.crop };

      const onMouseMove = (moveEvt) => {
        const dx = (moveEvt.clientX - startMouseX) / this.canvas.zoom;
        const dy = (moveEvt.clientY - startMouseY) / this.canvas.zoom;

        if (side === 'top') {
          const maxAllowed = 85 - initialCrop.bottom;
          card.crop.top = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.top + (dy / card.height) * 100)));
        } else if (side === 'bottom') {
          const maxAllowed = 85 - initialCrop.top;
          card.crop.bottom = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.bottom - (dy / card.height) * 100)));
        } else if (side === 'left') {
          const maxAllowed = 85 - initialCrop.right;
          card.crop.left = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.left + (dx / card.width) * 100)));
        } else if (side === 'right') {
          const maxAllowed = 85 - initialCrop.left;
          card.crop.right = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.right - (dx / card.width) * 100)));
        }
        onUpdate();
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  initCropCornerHandle(handle, corner, card, onUpdate) {
    handle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      const startMouseX = e.clientX;
      const startMouseY = e.clientY;
      const initialCrop = { ...card.crop };

      const onMouseMove = (moveEvt) => {
        const dx = (moveEvt.clientX - startMouseX) / this.canvas.zoom;
        const dy = (moveEvt.clientY - startMouseY) / this.canvas.zoom;

        if (corner.includes('n')) {
          const maxAllowed = 85 - initialCrop.bottom;
          card.crop.top = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.top + (dy / card.height) * 100)));
        }
        if (corner.includes('s')) {
          const maxAllowed = 85 - initialCrop.top;
          card.crop.bottom = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.bottom - (dy / card.height) * 100)));
        }
        if (corner.includes('w')) {
          const maxAllowed = 85 - initialCrop.right;
          card.crop.left = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.left + (dx / card.width) * 100)));
        }
        if (corner.includes('e')) {
          const maxAllowed = 85 - initialCrop.left;
          card.crop.right = Math.round(Math.max(0, Math.min(maxAllowed, initialCrop.right - (dx / card.width) * 100)));
        }
        onUpdate();
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  // ===========================================================================
  // MV Storyboard Group Frame & Notes System
  // ===========================================================================
  getNextSceneInfo() {
    const palette = ['#3b82f6', '#8b5cf6', '#ec4899', '#10b981', '#f59e0b'];
    let maxSceneNum = 0;

    this.groups.forEach(g => {
      const match = (g.title || '').match(/Scene\s*(\d+)/i);
      if (match) {
        const num = parseInt(match[1], 10);
        if (!isNaN(num) && num > maxSceneNum) {
          maxSceneNum = num;
        }
      }
    });

    const nextNum = maxSceneNum > 0 ? maxSceneNum + 1 : (this.groups.length + 1);
    const paddedNum = nextNum < 10 ? `0${nextNum}` : `${nextNum}`;
    const title = `Scene ${paddedNum}`;
    const colorIndex = (nextNum - 1) % palette.length;
    const color = palette[colorIndex];

    return { title, color, sceneNumber: nextNum };
  }

  setCardSizeAndPosition(card, x, y, width, height) {
    if (!card || !card.element) return;

    const { top = 0, right = 0, bottom = 0, left = 0 } = card.crop || {};
    const isCropped = (top > 0 || right > 0 || bottom > 0 || left > 0);
    const wasCropped = card.isCropped;
    card.isCropped = isCropped;

    if (isCropped) {
      const visW_pct = Math.max(0.05, (100 - left - right) / 100);
      const visH_pct = Math.max(0.05, (100 - top - bottom) / 100);

      card.baseWidth = Math.round(width / visW_pct);
      card.baseHeight = Math.round(height / visH_pct);
      card.width = width;
      card.height = height;
      card.aspectRatio = width / height;

      card.baseX = Math.round(x - card.baseWidth * (left / 100));
      card.baseY = Math.round(y - card.baseHeight * (top / 100));
      card.x = x;
      card.y = y;

      this.applyCropTransform(card);
    } else {
      card.baseWidth = width;
      card.baseHeight = height;
      card.baseX = x;
      card.baseY = y;
      card.width = width;
      card.height = height;
      card.aspectRatio = width / height;
      card.x = x;
      card.y = y;

      card.element.style.width = `${width}px`;
      card.element.style.height = `${height}px`;
      card.element.style.transform = `translate(${x}px, ${y}px)`;

      if (wasCropped && card.mediaContainer) {
        card.mediaContainer.style.clipPath = 'none';
        card.mediaContainer.style.overflow = 'visible';
        const img = card.mediaContainer.querySelector('img');
        if (img) {
          img.style.position = 'static';
          img.style.width = '100%';
          img.style.height = '100%';
          img.style.maxWidth = '100%';
          img.style.maxHeight = '100%';
          img.style.left = '0';
          img.style.top = '0';
        }
      }
    }
  }

  calculateGroupContentHeight(group, targetWidth = group.width) {
    if (!group) return 180;
    const memberCards = this.cards.filter(c => c.groupId === group.id);
    const headerH = 38;
    const notesEl = group.element ? group.element.querySelector('.group-notes-container') : null;
    const notesH = (notesEl && notesEl.clientHeight) ? notesEl.clientHeight : (group.notes ? 64 : 0);
    const topOffset = headerH + notesH + 14;
    const paddingBottom = 16;
    const gap = 12;

    if (memberCards.length === 0) {
      return Math.max(160, topOffset + 90);
    }

    const paddingX = 14;
    const availW = Math.max(100, targetWidth - paddingX * 2);
    let cols = 1;
    if (memberCards.length === 1) {
      cols = 1;
    } else if (memberCards.length === 2) {
      cols = availW >= 320 ? 2 : 1;
    } else {
      cols = Math.max(1, Math.min(memberCards.length, Math.floor((availW + gap) / 160)));
      if (cols < 2 && availW >= 280) cols = 2;
    }

    if (cols === 1 && memberCards.length === 1) {
      const targetW = Math.min(availW, 360);
      const ar = memberCards[0].aspectRatio || (memberCards[0].width / memberCards[0].height) || 1;
      const targetH = Math.max(36, Math.round(targetW / ar));
      return topOffset + targetH + paddingBottom;
    }

    const colWidth = Math.max(60, Math.floor((availW - (cols - 1) * gap) / cols));
    const colHeights = new Array(cols).fill(topOffset);

    memberCards.forEach((card) => {
      let targetCol = 0;
      let minH = colHeights[0];
      for (let c = 1; c < cols; c++) {
        if (colHeights[c] < minH) {
          minH = colHeights[c];
          targetCol = c;
        }
      }
      const ar = card.aspectRatio || (card.width / card.height) || 1;
      const targetH = Math.max(36, Math.round(colWidth / ar));
      colHeights[targetCol] += targetH + gap;
    });

    return Math.max(...colHeights) - gap + paddingBottom;
  }

  tidyGroup(group, autoExpandHeight = true) {
    if (!group || !group.element) return;
    const memberCards = this.cards.filter(c => c.groupId === group.id);
    if (memberCards.length === 0) return;

    // Sort member cards by current layout position (row-first, then col) for intuitive drag reordering
    memberCards.sort((a, b) => {
      const rowA = Math.round(a.y / 50);
      const rowB = Math.round(b.y / 50);
      if (rowA !== rowB) return rowA - rowB;
      return a.x - b.x;
    });

    const isNoGap = this.arrangeGap === 0;
    const paddingX = isNoGap ? 6 : 14;
    const paddingBottom = isNoGap ? 8 : 16;
    const gap = isNoGap ? 0 : Math.min(24, Math.max(4, Math.round((this.arrangeGap !== undefined ? this.arrangeGap : 32) * 0.4)));

    const headerH = 38;
    const notesEl = group.element.querySelector('.group-notes-container');
    const notesH = (notesEl && notesEl.clientHeight) ? notesEl.clientHeight : (group.notes ? 64 : 0);
    const topOffset = headerH + notesH + 14;

    const availW = Math.max(100, group.width - paddingX * 2);

    // Determine optimal columns
    let cols = 1;
    if (memberCards.length === 1) {
      cols = 1;
    } else if (memberCards.length === 2) {
      cols = availW >= 420 ? 2 : 1;
    } else {
      cols = Math.max(1, Math.min(memberCards.length, Math.floor((availW + gap) / 200)));
      if (cols < 2 && availW >= 420) cols = 2;
    }

    if (cols === 1 && memberCards.length === 1) {
      const targetW = Math.min(availW, 360);
      const ar = memberCards[0].aspectRatio || (memberCards[0].width / memberCards[0].height) || 1;
      const targetH = Math.max(36, Math.round(targetW / ar));
      const posX = Math.round(group.x + paddingX);
      const posY = Math.round(group.y + topOffset);
      this.setCardSizeAndPosition(memberCards[0], posX, posY, targetW, targetH);

      const neededH = topOffset + targetH + paddingBottom;
      if (autoExpandHeight || group.height < neededH) {
        group.height = Math.max(group.height, neededH);
        group.element.style.height = `${group.height}px`;
      }
      return;
    }

    const colWidth = Math.max(60, Math.floor((availW - (cols - 1) * gap) / cols));
    const colHeights = new Array(cols).fill(topOffset);

    memberCards.forEach((card) => {
      // Put in column with the lowest current height
      let targetCol = 0;
      let minH = colHeights[0];
      for (let c = 1; c < cols; c++) {
        if (colHeights[c] < minH) {
          minH = colHeights[c];
          targetCol = c;
        }
      }

      const posX = Math.round(group.x + paddingX + targetCol * (colWidth + gap));
      const posY = Math.round(group.y + colHeights[targetCol]);

      // Calculate proportional height keeping aspect ratio
      const ar = card.aspectRatio || (card.width / card.height) || 1;
      const targetW = colWidth;
      const targetH = Math.max(36, Math.round(colWidth / ar));

      this.setCardSizeAndPosition(card, posX, posY, targetW, targetH);

      colHeights[targetCol] += targetH + gap;
    });

    const neededH = Math.max(...colHeights) - gap + paddingBottom;
    if (autoExpandHeight || group.height < neededH) {
      group.height = Math.max(group.height, neededH);
      group.element.style.height = `${group.height}px`;
    }
  }

  createGroup(title = null, x = 0, y = 0, width = 460, height = 400, color = null, notes = '', customId = null) {
    if (!title || !color) {
      const info = this.getNextSceneInfo();
      title = title || info.title;
      color = color || info.color;
    }

    const id = customId || ('grp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 6));
    const group = {
      id,
      title,
      notes,
      x,
      y,
      width,
      height,
      color,
      element: null
    };

    const groupEl = document.createElement('div');
    groupEl.className = 'mv-group-frame';
    groupEl.id = id;
    groupEl.style.transform = `translate(${x}px, ${y}px)`;
    groupEl.style.width = `${width}px`;
    groupEl.style.height = `${height}px`;
    groupEl.style.setProperty('--group-color', color);

    // Header
    const header = document.createElement('div');
    header.className = 'group-header';

    const titleWrapper = document.createElement('div');
    titleWrapper.className = 'group-title-wrapper';

    const colorDot = document.createElement('div');
    colorDot.className = 'group-color-dot';

    const titleText = document.createElement('span');
    titleText.className = 'group-title';
    titleText.contentEditable = 'false';
    titleText.spellcheck = false;
    titleText.textContent = title;
    titleText.title = 'Double-click to rename title';

    // Double-click to rename
    titleText.addEventListener('dblclick', (e) => {
      e.stopPropagation();
      this.recordPreState('Rename Group');
      titleText.contentEditable = 'true';
      titleText.focus();
      const range = document.createRange();
      range.selectNodeContents(titleText);
      const sel = window.getSelection();
      sel.removeAllRanges();
      sel.addRange(range);
    });

    const finishTitleEdit = () => {
      if (titleText.contentEditable === 'true') {
        titleText.contentEditable = 'false';
        const newTitle = titleText.textContent.trim() || 'Untitled Scene';
        titleText.textContent = newTitle;
        group.title = newTitle;
        this.cards.filter(c => c.groupId === group.id).forEach(c => this.updateCardGroupBadge(c));
        this.commitHistory('Rename Group');
      }
    };

    titleText.addEventListener('blur', finishTitleEdit);
    titleText.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        titleText.blur();
      } else if (e.key === 'Escape') {
        titleText.textContent = group.title;
        titleText.contentEditable = 'false';
      }
    });

    const countBadge = document.createElement('span');
    countBadge.className = 'group-count-badge';
    countBadge.id = `count_${id}`;
    countBadge.textContent = '0 refs';

    titleWrapper.appendChild(colorDot);
    titleWrapper.appendChild(titleText);
    titleWrapper.appendChild(countBadge);
    header.appendChild(titleWrapper);

    // Actions
    const actions = document.createElement('div');
    actions.className = 'group-actions';

    // Tidy Button (Auto-Tidy References inside Group)
    const btnTidy = document.createElement('button');
    btnTidy.className = 'btn-tidy-group';
    btnTidy.innerHTML = `
      <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M4 6h16M4 12h16M4 18h12"/>
      </svg>
      <span>Tidy</span>
    `;
    btnTidy.title = 'Auto-Tidy References inside Group';
    btnTidy.addEventListener('click', (e) => {
      e.stopPropagation();
      this.recordPreState('Tidy Group');
      this.tidyGroup(group, true);
      this.commitHistory('Tidy Group');
      Toast.show(`Tidied references in ${group.title}`, 'success');
    });
    actions.appendChild(btnTidy);

    // Color picker options
    const picker = document.createElement('div');
    picker.className = 'group-color-picker';
    const palette = ['#3b82f6', '#8b5cf6', '#ec4899', '#10b981', '#f59e0b'];
    palette.forEach(c => {
      const dot = document.createElement('div');
      dot.className = 'color-option';
      dot.style.backgroundColor = c;
      dot.addEventListener('click', (e) => {
        e.stopPropagation();
        this.recordPreState('Change Group Color');
        group.color = c;
        groupEl.style.setProperty('--group-color', c);
        this.cards.filter(card => card.groupId === group.id).forEach(card => this.updateCardGroupBadge(card));
        this.commitHistory('Change Group Color');
      });
      picker.appendChild(dot);
    });
    actions.appendChild(picker);

    // Delete group button
    const btnDel = document.createElement('button');
    btnDel.className = 'group-btn';
    btnDel.innerHTML = '✕';
    btnDel.title = 'Delete Group Frame (Keeps cards on canvas)';
    btnDel.addEventListener('click', (e) => {
      e.stopPropagation();
      this.recordPreState('Delete Group');
      // Unlock all member cards
      this.cards.filter(c => c.groupId === group.id).forEach(c => {
        c.groupId = null;
        this.updateCardGroupBadge(c);
      });
      groupEl.remove();
      this.groups = this.groups.filter(g => g.id !== group.id);
      this.updateGroupCounts();
      this.updateCardCount();
      this.commitHistory('Delete Group');
      Toast.show('Removed Group Frame', 'info');
    });
    actions.appendChild(btnDel);
    header.appendChild(actions);

    groupEl.appendChild(header);

    // Notes Container
    const notesContainer = document.createElement('div');
    notesContainer.className = 'group-notes-container';

    const textarea = document.createElement('textarea');
    textarea.className = 'group-notes-textarea';
    textarea.placeholder = 'Write scene notes, mood, visual ideas, or camera details...';
    textarea.rows = 2;
    textarea.value = notes;
    textarea.addEventListener('input', () => {
      group.notes = textarea.value;
      this.scheduleAutoSave();
    });
    notesContainer.appendChild(textarea);
    groupEl.appendChild(notesContainer);

    // Content drop area with empty hint
    const contentArea = document.createElement('div');
    contentArea.className = 'group-content-area';

    const emptyHint = document.createElement('div');
    emptyHint.className = 'group-empty-hint';
    emptyHint.id = `hint_${id}`;
    emptyHint.innerHTML = 'Drop references here to lock into group<br><span style="opacity:0.6;font-size:10px;">Drag panel or header to move</span>';
    contentArea.appendChild(emptyHint);

    groupEl.appendChild(contentArea);

    // Resize Handle (bottom-right) - snaps & reflows cards neatly!
    const resizeHandle = document.createElement('div');
    resizeHandle.className = 'group-resize-handle';
    resizeHandle.innerHTML = '⋰';
    resizeHandle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      this.recordPreState('Resize Group');
      const startMouseX = e.clientX;
      const startMouseY = e.clientY;
      const startW = group.width;
      const startH = group.height;

      const onMouseMove = (moveEvt) => {
        const dx = (moveEvt.clientX - startMouseX) / this.canvas.zoom;
        const dy = (moveEvt.clientY - startMouseY) / this.canvas.zoom;
        
        // Keep group width wide enough so header elements (title, badges, buttons) never cramp or wrap
        const minW = 340;
        group.width = Math.max(minW, startW + dx);
        
        // Dynamically clamp height so it never cuts through member reference cards
        const minContentH = this.calculateGroupContentHeight(group, group.width);
        group.height = Math.max(minContentH, startH + dy);
        
        groupEl.style.width = `${group.width}px`;
        groupEl.style.height = `${group.height}px`;

        // Responsive card reflow & snap during group resize
        this.tidyGroup(group, false);
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        // Final tidy pass with auto-fit height
        this.tidyGroup(group, true);
        this.commitHistory('Resize Group');
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
    groupEl.appendChild(resizeHandle);

    // Whole-Panel Group Dragging & Selection
    groupEl.addEventListener('mousedown', (e) => {
      if (e.button !== 0 ||
          this.canvas.spacePressed || e.altKey || this.canvas.isPanning ||
          e.target.closest('.group-actions') || 
          e.target.closest('.group-notes-textarea') || 
          e.target.closest('.group-resize-handle') ||
          (e.target === titleText && titleText.contentEditable === 'true')) {
        return;
      }

      // If user holds Shift inside group body (not header), let it bubble to viewport for Shift-Marquee photo selection!
      if (e.shiftKey && !e.target.closest('.group-header')) {
        return;
      }

      e.stopPropagation();

      const isMulti = (e.shiftKey || e.ctrlKey || e.metaKey);
      if (isMulti) {
        this.selectGroup(group, true);
      } else if (!this.selectedGroups.has(group)) {
        this.selectGroup(group, false);
      }

      const startMouseX = e.clientX;
      const startMouseY = e.clientY;

      let isDragging = false;
      let hasStartedMoving = false;
      let dragRafPending = false;
      let lastMoveEvt = null;

      let movingGroups = null;
      let startGroupPositions = null;
      let movingNodes = null;
      let startNodePositions = null;
      let movingLooseCards = null;

      const initGroupDragState = () => {
        hasStartedMoving = true;
        isDragging = true;
        this.recordPreState('Move Group');
        groupEl.classList.add('is-dragging-group');

        movingGroups = this.selectedGroups.has(group) && this.selectedGroups.size > 1
          ? Array.from(this.selectedGroups)
          : [group];

        startGroupPositions = movingGroups.map(g => ({
          group: g,
          startX: g.x,
          startY: g.y,
          memberCards: this.cards.filter(c => c.groupId === g.id).map(c => ({
            card: c,
            x: c.x,
            y: c.y,
            baseX: c.baseX !== undefined ? c.baseX : c.x,
            baseY: c.baseY !== undefined ? c.baseY : c.y
          }))
        }));

        movingNodes = Array.from(this.selectedNodes || []);
        startNodePositions = movingNodes.map(n => ({
          node: n,
          x: n.x,
          y: n.y
        }));

        movingLooseCards = Array.from(this.selectedCards || []).filter(c => 
          !movingGroups.some(g => g.id === c.groupId)
        ).map(c => ({
          card: c,
          x: c.x,
          y: c.y,
          baseX: c.baseX !== undefined ? c.baseX : c.x,
          baseY: c.baseY !== undefined ? c.baseY : c.y
        }));
      };

      const onMouseMove = (moveEvt) => {
        lastMoveEvt = moveEvt;

        if (!hasStartedMoving) {
          const dist = Math.hypot(moveEvt.clientX - startMouseX, moveEvt.clientY - startMouseY);
          if (dist < 4) return;
          initGroupDragState();
        }

        if (dragRafPending) return;
        dragRafPending = true;

        requestAnimationFrame(() => {
          dragRafPending = false;
          if (!hasStartedMoving || !lastMoveEvt) return;

          const dx = (lastMoveEvt.clientX - startMouseX) / this.canvas.zoom;
          const dy = (lastMoveEvt.clientY - startMouseY) / this.canvas.zoom;

          // Move all selected groups and their member cards
          startGroupPositions.forEach(item => {
            item.group.x = item.startX + dx;
            item.group.y = item.startY + dy;
            if (item.group.element) item.group.element.style.transform = `translate(${item.group.x}px, ${item.group.y}px)`;

            item.memberCards.forEach(mc => {
              mc.card.x = mc.x + dx;
              mc.card.y = mc.y + dy;
              if (mc.card.baseX !== undefined) mc.card.baseX = mc.baseX + dx;
              if (mc.card.baseY !== undefined) mc.card.baseY = mc.baseY + dy;
              if (mc.card.element) mc.card.element.style.transform = `translate(${mc.card.x}px, ${mc.card.y}px)`;
            });
          });

          // Move any selected nodes
          startNodePositions.forEach(item => {
            item.node.x = item.x + dx;
            item.node.y = item.y + dy;
            if (item.node.element) item.node.element.style.transform = `translate(${item.node.x}px, ${item.node.y}px)`;
          });

          // Move any loose selected cards
          movingLooseCards.forEach(item => {
            item.card.x = item.x + dx;
            item.card.y = item.y + dy;
            if (item.card.baseX !== undefined) item.card.baseX = item.baseX + dx;
            if (item.card.baseY !== undefined) item.card.baseY = item.baseY + dy;
            if (item.card.element) item.card.element.style.transform = `translate(${item.card.x}px, ${item.card.y}px)`;
          });

          this.renderConnectionsThrottled();
        });
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);

        if (!hasStartedMoving) {
          // Instant click / selection without delay!
          return;
        }

        groupEl.classList.remove('is-dragging-group');
        this.commitHistory('Move Group', true);
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });

    // Group Context Menu (Right-click anywhere on group frame)
    groupEl.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.showGroupContextMenu(e.clientX, e.clientY, group);
    });

    group.element = groupEl;
    this.groups.push(group);
    // Prepend to world so group frames stay beneath reference cards
    this.world.insertBefore(groupEl, this.world.firstChild);
    this.updateGroupCounts();
    this.updateCardCount();
    return group;
  }

  updateGroupCounts() {
    this.groups.forEach(g => {
      const count = this.cards.filter(c => c.groupId === g.id).length;
      const badge = document.getElementById(`count_${g.id}`);
      if (badge) {
        badge.textContent = `${count} ${count === 1 ? 'ref' : 'refs'}`;
      }
      const hint = document.getElementById(`hint_${g.id}`);
      if (hint) {
        hint.style.display = count > 0 ? 'none' : 'block';
      }
    });
  }

  // ===========================================================================
  // Creative Production Nodes (Lyrics, Fonts, VFX, Deadline/Plan) & Connectors
  // ===========================================================================
  getAvailableFonts() {
    const defaultFonts = [
      'Plus Jakarta Sans', 'Inter', 'Segoe UI', 'Roboto', 'Montserrat', 'Poppins', 'Arial',
      'Bebas Neue', 'Impact', 'Oswald', 'Anton', 'Cinzel', 'Trajan Pro',
      'Times New Roman', 'Georgia', 'Playfair Display', 'Garamond', 'Merriweather',
      'JetBrains Mono', 'Consolas', 'Fira Code', 'Courier New', 'Cascadia Code',
      'Comic Sans MS', 'Trebuchet MS', 'Verdana', 'Tahoma', 'Pacifico', 'Dancing Script',
      'Century Gothic', 'Franklin Gothic Medium', 'Trebuchet MS', 'Palatino Linotype'
    ];
    if (this.systemFonts && this.systemFonts.length > 0) {
      const set = new Set([...this.systemFonts, ...defaultFonts]);
      return Array.from(set).sort((a, b) => a.localeCompare(b));
    }
    return defaultFonts;
  }

  createNode(type = 'note', x = 0, y = 0, customData = {}) {
    const id = customData.id || ('node_' + Date.now() + '_' + Math.random().toString(36).substr(2, 6));
    
    // Type definitions with clean vector SVGs (No emojis!)
    const typeMeta = {
      note: {
        label: 'Note',
        icon: '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>',
        color: '#38bdf8',
        defaultTitle: 'Note'
      },
      font: {
        label: 'Font',
        icon: '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2"><polyline points="4 7 4 4 20 4 20 7"/><line x1="9" y1="20" x2="15" y2="20"/><line x1="12" y1="4" x2="12" y2="20"/></svg>',
        color: '#a855f7',
        defaultTitle: 'Typography'
      },
      vfx: {
        label: 'Effect',
        icon: '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2"><path d="m12 3-1.9 5.8a2 2 0 0 1-1.3 1.3L3 12l5.8 1.9a2 2 0 0 1 1.3 1.3L12 21l1.9-5.8a2 2 0 0 1 1.3-1.3L21 12l-5.8-1.9a2 2 0 0 1-1.3-1.3L12 3z"/></svg>',
        color: '#10b981',
        defaultTitle: 'Visual Effects'
      },
      plan: {
        label: 'Schedule',
        icon: '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>',
        color: '#f43f5e',
        defaultTitle: 'Deadline'
      }
    };
    const meta = typeMeta[type] || typeMeta.note;

    const node = {
      id,
      type,
      x,
      y,
      width: customData.width ? customData.width : (type === 'font' ? 295 : (type === 'note' || type === 'plan' ? 280 : 260)),
      title: customData.title || meta.defaultTitle,
      color: customData.color || meta.color,
      content: customData.content !== undefined ? customData.content : '',
      mode: customData.mode || 'text', // 'text' | 'checklist'
      items: customData.items ? [...customData.items] : [],
      tags: customData.tags ? [...customData.tags] : [],
      fontFamily: customData.fontFamily || 'Plus Jakarta Sans',
      fontSize: customData.fontSize || (type === 'font' ? '24px' : 13),
      activeVfx: customData.activeVfx || ['Glow'],
      deadline: customData.deadline || new Date(Date.now() + 3 * 86400000).toISOString().split('T')[0],
      status: customData.status || 'in_progress', // 'todo' | 'in_progress' | 'done'
      element: null
    };

    const nodeEl = document.createElement('div');
    nodeEl.className = 'prod-node';
    nodeEl.id = id;
    nodeEl.dataset.type = type;
    nodeEl.style.transform = `translate(${x}px, ${y}px)`;
    nodeEl.style.width = `${node.width}px`;
    nodeEl.style.setProperty('--node-color', node.color);

    // Header
    const header = document.createElement('div');
    header.className = 'prod-node-header';

    const typeBadge = document.createElement('div');
    typeBadge.className = 'prod-node-type-badge';
    typeBadge.innerHTML = `<span style="display:inline-flex;align-items:center;">${meta.icon}</span><span>${meta.label}</span>`;

    const titleInput = document.createElement('span');
    titleInput.className = 'prod-node-title';
    titleInput.contentEditable = 'true';
    titleInput.spellcheck = false;
    titleInput.textContent = node.title;
    titleInput.addEventListener('blur', () => {
      node.title = titleInput.textContent.trim() || meta.defaultTitle;
      this.renderConnections();
      this.scheduleAutoSave();
    });
    titleInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { e.preventDefault(); titleInput.blur(); }
    });

    const btnDel = document.createElement('button');
    btnDel.className = 'prod-node-btn-del';
    btnDel.innerHTML = '✕';
    btnDel.title = 'Delete Node';
    btnDel.addEventListener('click', (e) => {
      e.stopPropagation();
      this.recordPreState('Delete Node');
      this.removeNode(node);
      this.commitHistory('Delete Node');
    });

    header.appendChild(typeBadge);
    header.appendChild(titleInput);
    header.appendChild(btnDel);
    nodeEl.appendChild(header);

    // Left & Right Connector Pins (for wiring)
    const pinLeft = document.createElement('div');
    pinLeft.className = 'prod-node-pin pin-left';
    pinLeft.title = 'Drag wire to connect to card or group';
    this.initPinDrag(pinLeft, node, 'left');

    const pinRight = document.createElement('div');
    pinRight.className = 'prod-node-pin pin-right';
    pinRight.title = 'Drag wire to connect to card or group';
    this.initPinDrag(pinRight, node, 'right');

    nodeEl.appendChild(pinLeft);
    nodeEl.appendChild(pinRight);

    // Content Body
    const body = document.createElement('div');
    body.className = 'prod-node-body';

    if (type === 'note') {
      // Customizable Note Toolbar: Mode Toggle (Text vs Checklist) + Accent Color Swatches
      const toolbar = document.createElement('div');
      toolbar.className = 'node-note-toolbar';

      const modesDiv = document.createElement('div');
      modesDiv.className = 'node-note-modes';
      const btnModeText = document.createElement('button');
      btnModeText.className = `node-mode-btn ${node.mode === 'text' ? 'active' : ''}`;
      btnModeText.textContent = 'Text';
      const btnModeChk = document.createElement('button');
      btnModeChk.className = `node-mode-btn ${node.mode === 'checklist' ? 'active' : ''}`;
      btnModeChk.textContent = 'Checklist';

      modesDiv.appendChild(btnModeText);
      modesDiv.appendChild(btnModeChk);
      toolbar.appendChild(modesDiv);

      const swatchesDiv = document.createElement('div');
      swatchesDiv.className = 'node-color-swatches';
      const notePalette = ['#38bdf8', '#a855f7', '#10b981', '#f59e0b', '#f43f5e', '#64748b'];
      notePalette.forEach(c => {
        const swatch = document.createElement('div');
        swatch.className = 'node-color-swatch';
        swatch.style.backgroundColor = c;
        swatch.title = 'Change note accent color';
        swatch.addEventListener('click', (e) => {
          e.stopPropagation();
          node.color = c;
          nodeEl.style.setProperty('--node-color', c);
          this.renderConnections();
          this.scheduleAutoSave();
        });
        swatchesDiv.appendChild(swatch);
      });
      toolbar.appendChild(swatchesDiv);

      // Font Size Controls (A- / A+ and scrub slider like After Effects)
      const fontDiv = document.createElement('div');
      fontDiv.className = 'node-font-controls';
      node.fontSize = node.fontSize ? parseInt(node.fontSize) : 13;
      
      const btnFontMinus = document.createElement('button');
      btnFontMinus.className = 'node-font-btn';
      btnFontMinus.textContent = 'A-';
      btnFontMinus.title = 'Smaller Font (Ctrl+Click to reset)';

      const fontLabel = document.createElement('span');
      fontLabel.className = 'node-font-label';
      fontLabel.textContent = `${node.fontSize}px`;
      fontLabel.title = 'Drag left/right to adjust font size (like After Effects)';

      const btnFontPlus = document.createElement('button');
      btnFontPlus.className = 'node-font-btn';
      btnFontPlus.textContent = 'A+';
      btnFontPlus.title = 'Larger Font';

      const applyFontSize = (delta) => {
        node.fontSize = Math.max(10, Math.min(48, node.fontSize + delta));
        fontLabel.textContent = `${node.fontSize}px`;
        textarea.style.fontSize = `${node.fontSize}px`;
        autoResizeTextarea();
        this.renderConnections();
        this.scheduleAutoSave();
      };

      btnFontMinus.addEventListener('click', (e) => {
        e.stopPropagation();
        if (e.ctrlKey) {
          node.fontSize = 13;
          fontLabel.textContent = '13px';
          textarea.style.fontSize = '13px';
          autoResizeTextarea();
          this.renderConnections();
          this.scheduleAutoSave();
        } else {
          applyFontSize(-1);
        }
      });
      btnFontPlus.addEventListener('click', (e) => {
        e.stopPropagation();
        applyFontSize(1);
      });

      // AE-style mouse-drag scrubbing on fontLabel
      fontLabel.addEventListener('mousedown', (e) => {
        if (e.button !== 0) return;
        e.stopPropagation();
        e.preventDefault();
        const startX = e.clientX;
        const initialSize = node.fontSize;
        this.recordPreState('Resize Font');

        const onScrubMove = (moveEvt) => {
          const diff = Math.round((moveEvt.clientX - startX) / 5);
          const newSize = Math.max(10, Math.min(48, initialSize + diff));
          if (newSize !== node.fontSize) {
            node.fontSize = newSize;
            fontLabel.textContent = `${node.fontSize}px`;
            textarea.style.fontSize = `${node.fontSize}px`;
            autoResizeTextarea();
            this.renderConnections();
          }
        };

        const onScrubUp = () => {
          window.removeEventListener('mousemove', onScrubMove);
          window.removeEventListener('mouseup', onScrubUp);
          this.commitHistory('Resize Font');
          this.scheduleAutoSave();
        };

        window.addEventListener('mousemove', onScrubMove);
        window.addEventListener('mouseup', onScrubUp);
      });

      fontDiv.appendChild(btnFontMinus);
      fontDiv.appendChild(fontLabel);
      fontDiv.appendChild(btnFontPlus);
      toolbar.appendChild(fontDiv);
      body.appendChild(toolbar);

      // Text container
      const textContainer = document.createElement('div');
      textContainer.style.display = node.mode === 'text' ? 'block' : 'none';
      const textarea = document.createElement('textarea');
      textarea.className = 'node-textarea';
      textarea.value = node.content;
      textarea.placeholder = 'Type note or reference text here...';
      textarea.style.fontSize = `${node.fontSize}px`;

      const autoResizeTextarea = () => {
        textarea.style.height = 'auto';
        const minH = node.customTextareaHeight || 52;
        textarea.style.height = `${Math.max(minH, textarea.scrollHeight)}px`;
      };

      textarea.addEventListener('input', () => {
        node.content = textarea.value;
        autoResizeTextarea();
        this.renderConnections();
        this.scheduleAutoSave();
      });

      textarea.addEventListener('paste', () => setTimeout(autoResizeTextarea, 0));
      setTimeout(autoResizeTextarea, 0);

      textContainer.appendChild(textarea);
      body.appendChild(textContainer);

      // Checklist container
      const chkContainer = document.createElement('div');
      chkContainer.style.display = node.mode === 'checklist' ? 'flex' : 'none';
      chkContainer.style.flexDirection = 'column';
      chkContainer.style.gap = '4px';

      const chkList = document.createElement('div');
      chkList.className = 'node-checklist-container';

      const renderChecklist = () => {
        chkList.innerHTML = '';
        node.items.forEach((item, index) => {
          const itemEl = document.createElement('div');
          itemEl.className = `node-checklist-item ${item.done ? 'is-done' : ''}`;

          const chk = document.createElement('input');
          chk.type = 'checkbox';
          chk.className = 'node-chk-box';
          chk.checked = !!item.done;
          chk.addEventListener('change', () => {
            item.done = chk.checked;
            itemEl.classList.toggle('is-done', item.done);
            this.scheduleAutoSave();
          });

          const label = document.createElement('input');
          label.type = 'text';
          label.className = 'node-chk-label';
          label.value = item.text;
          label.addEventListener('input', () => {
            item.text = label.value;
            this.scheduleAutoSave();
          });

          const delBtn = document.createElement('button');
          delBtn.className = 'node-chk-del';
          delBtn.textContent = '✕';
          delBtn.title = 'Remove item';
          delBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            node.items.splice(index, 1);
            renderChecklist();
            this.scheduleAutoSave();
          });

          itemEl.appendChild(chk);
          itemEl.appendChild(label);
          itemEl.appendChild(delBtn);
          chkList.appendChild(itemEl);
        });
      };
      renderChecklist();
      chkContainer.appendChild(chkList);

      const addRow = document.createElement('div');
      addRow.className = 'node-chk-add-row';
      const addInput = document.createElement('input');
      addInput.type = 'text';
      addInput.className = 'node-chk-input';
      addInput.placeholder = '+ Add task or checklist item...';
      const btnAddChk = document.createElement('button');
      btnAddChk.className = 'node-chk-btn-add';
      btnAddChk.textContent = '+ Add';

      const handleAddChk = () => {
        const val = addInput.value.trim();
        if (val) {
          node.items.push({ id: Date.now(), text: val, done: false });
          addInput.value = '';
          renderChecklist();
          this.scheduleAutoSave();
        }
      };
      btnAddChk.addEventListener('click', handleAddChk);
      addInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') handleAddChk();
      });
      addRow.appendChild(addInput);
      addRow.appendChild(btnAddChk);
      chkContainer.appendChild(addRow);
      body.appendChild(chkContainer);

      // Mode toggle listeners
      btnModeText.addEventListener('click', (e) => {
        e.stopPropagation();
        node.mode = 'text';
        btnModeText.classList.add('active');
        btnModeChk.classList.remove('active');
        textContainer.style.display = 'block';
        chkContainer.style.display = 'none';
        setTimeout(autoResizeTextarea, 0);
        this.scheduleAutoSave();
      });
      btnModeChk.addEventListener('click', (e) => {
        e.stopPropagation();
        node.mode = 'checklist';
        btnModeChk.classList.add('active');
        btnModeText.classList.remove('active');
        textContainer.style.display = 'none';
        chkContainer.style.display = 'flex';
        renderChecklist();
        this.scheduleAutoSave();
      });

      // Customizable Tags Row
      const tagsContainer = document.createElement('div');
      tagsContainer.className = 'node-custom-tags';

      const renderTags = () => {
        tagsContainer.innerHTML = '';
        node.tags.forEach((tag, idx) => {
          const tagEl = document.createElement('span');
          tagEl.className = 'node-custom-tag';
          tagEl.innerHTML = `<span>${tag}</span><span class="node-custom-tag-del">✕</span>`;
          tagEl.querySelector('.node-custom-tag-del').addEventListener('click', (e) => {
            e.stopPropagation();
            node.tags.splice(idx, 1);
            renderTags();
            this.scheduleAutoSave();
          });
          tagsContainer.appendChild(tagEl);
        });

        const btnAddTag = document.createElement('button');
        btnAddTag.className = 'node-add-tag-btn';
        btnAddTag.textContent = '+ Tag';

        const inlineTagInput = document.createElement('input');
        inlineTagInput.type = 'text';
        inlineTagInput.className = 'node-inline-tag-input';
        inlineTagInput.placeholder = '#tag...';

        const commitTag = () => {
          const val = inlineTagInput.value.trim();
          if (val) {
            const clean = (val.startsWith('#') ? val : '#' + val).trim();
            if (!node.tags.includes(clean)) {
              node.tags.push(clean);
              this.scheduleAutoSave();
            }
            inlineTagInput.value = '';
            renderTags();
          }
        };

        inlineTagInput.addEventListener('keydown', (e) => {
          if (e.key === 'Enter') commitTag();
          if (e.key === 'Escape') {
            inlineTagInput.value = '';
            inlineTagInput.blur();
          }
        });

        btnAddTag.addEventListener('click', (e) => {
          e.stopPropagation();
          inlineTagInput.focus();
        });

        tagsContainer.appendChild(btnAddTag);
        tagsContainer.appendChild(inlineTagInput);
      };
      renderTags();
      body.appendChild(tagsContainer);

      // Manual Node Corner Resize Handles (All 4 Corners)
      ['nw', 'ne', 'sw', 'se'].forEach(dir => {
        const handle = document.createElement('div');
        handle.className = `node-resize-handle node-handle-${dir}`;
        handle.dataset.dir = dir;
        handle.title = `Drag to resize note (${dir.toUpperCase()})`;
        this.initNodeResize(handle, node, nodeEl, dir, autoResizeTextarea);
        nodeEl.appendChild(handle);
      });

    } else if (type === 'font') {
      const row1 = document.createElement('div');
      row1.className = 'node-field-row';
      row1.innerHTML = `<span class="node-field-label">Font</span>`;

      const picker = document.createElement('div');
      picker.className = 'node-font-picker';

      const inputWrap = document.createElement('div');
      inputWrap.className = 'node-font-input-wrapper';

      const fontInput = document.createElement('input');
      fontInput.className = 'node-input-text';
      fontInput.type = 'text';
      fontInput.value = node.fontFamily;
      fontInput.placeholder = 'Search fonts...';
      fontInput.setAttribute('autocomplete', 'off');
      fontInput.setAttribute('spellcheck', 'false');

      const btnDropdown = document.createElement('button');
      btnDropdown.className = 'node-font-btn-dropdown';
      btnDropdown.textContent = '▼';
      btnDropdown.title = 'Browse installed system fonts (Use ↑ / ↓ to cycle)';

      inputWrap.appendChild(fontInput);
      inputWrap.appendChild(btnDropdown);
      picker.appendChild(inputWrap);

      const dropdownList = document.createElement('div');
      dropdownList.className = 'node-font-dropdown';
      dropdownList.addEventListener('wheel', (e) => {
        e.stopPropagation();
      }, { passive: false });
      picker.appendChild(dropdownList);

      row1.appendChild(picker);
      body.appendChild(row1);

      const preview = document.createElement('div');
      preview.className = 'node-font-preview';
      preview.contentEditable = 'true';
      preview.spellcheck = false;
      preview.title = 'Click to customize sample text';
      preview.style.fontFamily = `"${node.fontFamily}", sans-serif`;
      preview.textContent = node.content || 'Typography Sample Text';
      preview.addEventListener('input', () => {
        node.content = preview.textContent;
        this.scheduleAutoSave();
      });
      preview.addEventListener('keydown', (e) => {
        e.stopPropagation();
      });
      body.appendChild(preview);

      let currentFilteredFonts = [];
      let renderedCount = 0;
      const BATCH_SIZE = 35;

      const updateFont = (fontName, closeDropdown = false) => {
        node.fontFamily = fontName;
        fontInput.value = fontName;
        preview.style.fontFamily = `"${fontName}", sans-serif`;
        if (closeDropdown) {
          dropdownList.classList.remove('show');
        }
        // Update active highlight on rendered items
        const items = dropdownList.querySelectorAll('.node-font-dropdown-item');
        items.forEach(it => {
          it.classList.toggle('active', it.dataset.font === fontName);
        });
        this.updateConnectedFontEffects();
        this.scheduleAutoSave();
      };

      const appendFontBatch = () => {
        if (!currentFilteredFonts || currentFilteredFonts.length === 0) return;
        const nextBatch = currentFilteredFonts.slice(renderedCount, renderedCount + BATCH_SIZE);
        if (nextBatch.length === 0) return;

        const frag = document.createDocumentFragment();
        nextBatch.forEach((font, i) => {
          const itemIdx = renderedCount + i;
          const item = document.createElement('div');
          item.className = `node-font-dropdown-item ${font === node.fontFamily ? 'active' : ''}`;
          item.dataset.index = itemIdx;
          item.dataset.font = font;
          const isSys = this.systemFonts && this.systemFonts.includes(font);
          item.innerHTML = `
            <span class="node-font-item-name" style="font-family: '${font}', sans-serif;">${font}</span>
            <span class="node-font-item-badge">${isSys ? 'System' : 'Standard'}</span>
          `;
          item.addEventListener('mousedown', (e) => {
            e.stopPropagation();
            e.preventDefault();
            this.recordPreState('Change Font');
            updateFont(font, true);
            this.commitHistory('Change Font');
          });
          frag.appendChild(item);
        });
        dropdownList.appendChild(frag);
        renderedCount += nextBatch.length;
      };

      const renderDropdown = (query = '') => {
        dropdownList.innerHTML = '';
        renderedCount = 0;
        const allFonts = this.getAvailableFonts();
        currentFilteredFonts = query
          ? allFonts.filter(f => f.toLowerCase().includes(query.toLowerCase()))
          : allFonts;

        if (currentFilteredFonts.length === 0) {
          const empty = document.createElement('div');
          empty.style.padding = '8px';
          empty.style.fontSize = '10px';
          empty.style.color = 'var(--text-muted)';
          empty.style.textAlign = 'center';
          empty.textContent = `No font found matching "${query}". Hit Enter to use anyway.`;
          dropdownList.appendChild(empty);
        } else {
          // Render initial lightweight batch for 0ms instantaneous load
          appendFontBatch();
        }
      };

      // Infinite scroll listener for progressive loading
      dropdownList.addEventListener('scroll', () => {
        if (dropdownList.scrollTop + dropdownList.clientHeight >= dropdownList.scrollHeight - 60) {
          appendFontBatch();
        }
      });

      const stepFont = (direction) => {
        if (!currentFilteredFonts || currentFilteredFonts.length === 0) {
          currentFilteredFonts = this.getAvailableFonts();
        }
        if (currentFilteredFonts.length === 0) return;

        let curIdx = currentFilteredFonts.indexOf(node.fontFamily);
        let nextIdx = curIdx + direction;
        if (curIdx === -1) {
          nextIdx = direction > 0 ? 0 : currentFilteredFonts.length - 1;
        } else if (nextIdx >= currentFilteredFonts.length) {
          nextIdx = 0;
        } else if (nextIdx < 0) {
          nextIdx = currentFilteredFonts.length - 1;
        }

        const nextFont = currentFilteredFonts[nextIdx];

        // Ensure item batch is rendered so scroll into view works
        while (renderedCount <= nextIdx && renderedCount < currentFilteredFonts.length) {
          appendFontBatch();
        }

        if (!dropdownList.classList.contains('show')) {
          dropdownList.classList.add('show');
        }

        updateFont(nextFont, false);

        const targetEl = dropdownList.querySelector(`.node-font-dropdown-item[data-index="${nextIdx}"]`);
        if (targetEl) {
          targetEl.scrollIntoView({ block: 'nearest' });
        }
      };

      fontInput.addEventListener('focus', () => {
        fontInput.select();
        renderDropdown(fontInput.value);
        dropdownList.classList.add('show');
        const curIdx = currentFilteredFonts.indexOf(node.fontFamily);
        if (curIdx >= 0) {
          while (renderedCount <= curIdx && renderedCount < currentFilteredFonts.length) {
            appendFontBatch();
          }
          const activeEl = dropdownList.querySelector(`.node-font-dropdown-item[data-index="${curIdx}"]`);
          if (activeEl) activeEl.scrollIntoView({ block: 'nearest' });
        }
      });

      fontInput.addEventListener('input', () => {
        node.fontFamily = fontInput.value || 'sans-serif';
        preview.style.fontFamily = `"${node.fontFamily}", sans-serif`;
        renderDropdown(fontInput.value);
        dropdownList.classList.add('show');
        this.updateConnectedFontEffects();
        this.scheduleAutoSave();
      });

      fontInput.addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown') {
          e.preventDefault();
          stepFont(1);
        } else if (e.key === 'ArrowUp') {
          e.preventDefault();
          stepFont(-1);
        } else if (e.key === 'Enter') {
          e.preventDefault();
          dropdownList.classList.remove('show');
          fontInput.blur();
          this.commitHistory('Change Font');
        } else if (e.key === 'Escape') {
          dropdownList.classList.remove('show');
        }
      });

      btnDropdown.addEventListener('click', (e) => {
        e.stopPropagation();
        if (dropdownList.classList.contains('show')) {
          dropdownList.classList.remove('show');
        } else {
          renderDropdown('');
          dropdownList.classList.add('show');
          fontInput.focus();
          const curIdx = currentFilteredFonts.indexOf(node.fontFamily);
          if (curIdx >= 0) {
            while (renderedCount <= curIdx && renderedCount < currentFilteredFonts.length) {
              appendFontBatch();
            }
            const activeEl = dropdownList.querySelector(`.node-font-dropdown-item[data-index="${curIdx}"]`);
            if (activeEl) activeEl.scrollIntoView({ block: 'nearest' });
          }
        }
      });

      window.addEventListener('click', (e) => {
        if (!picker.contains(e.target)) {
          dropdownList.classList.remove('show');
        }
      });
    } else if (type === 'vfx') {
      const tagList = document.createElement('div');
      tagList.className = 'node-tag-list';
      const availableVfx = ['Glow', 'Halation', 'Grain', 'Chromatic Aberration', 'Motion Blur', 'Lens Flare', 'Glitch', 'Color Grading'];
      availableVfx.forEach(tag => {
        const tagEl = document.createElement('span');
        tagEl.className = `node-vfx-tag ${node.activeVfx.includes(tag) ? 'active' : ''}`;
        tagEl.textContent = tag;
        tagEl.addEventListener('click', (e) => {
          e.stopPropagation();
          if (node.activeVfx.includes(tag)) {
            node.activeVfx = node.activeVfx.filter(t => t !== tag);
            tagEl.classList.remove('active');
          } else {
            node.activeVfx.push(tag);
            tagEl.classList.add('active');
          }
          this.scheduleAutoSave();
        });
        tagList.appendChild(tagEl);
      });
      body.appendChild(tagList);

      const vfxNotes = document.createElement('textarea');
      vfxNotes.className = 'node-textarea';
      vfxNotes.style.minHeight = '48px';
      vfxNotes.value = node.content;
      vfxNotes.placeholder = 'Additional effect notes & timing...';
      vfxNotes.addEventListener('input', () => {
        node.content = vfxNotes.value;
        this.scheduleAutoSave();
      });
      body.appendChild(vfxNotes);
    } else if (type === 'plan') {
      const daysBadge = document.createElement('span');
      daysBadge.className = 'node-days-left-badge';

      const updateDaysLeft = () => {
        if (node.status === 'done') {
          daysBadge.className = 'node-days-left-badge status-done';
          daysBadge.textContent = 'Done ✓';
          return;
        }
        if (!node.deadline) {
          daysBadge.textContent = '';
          return;
        }
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const parts = node.deadline.split('-');
        if (parts.length !== 3) {
          daysBadge.textContent = '';
          return;
        }
        const year = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10) - 1;
        const day = parseInt(parts[2], 10);

        if (isNaN(year) || isNaN(month) || isNaN(day) || year < 1900 || year > 2150) {
          daysBadge.className = 'node-days-left-badge status-urgent';
          daysBadge.textContent = 'Set Year';
          return;
        }

        const target = new Date(year, month, day);
        target.setHours(0, 0, 0, 0);
        
        const diffTime = target.getTime() - today.getTime();
        const diffDays = Math.round(diffTime / (1000 * 60 * 60 * 24));

        if (diffDays > 3) {
          daysBadge.className = 'node-days-left-badge status-ok';
          daysBadge.textContent = `${diffDays} days left`;
        } else if (diffDays > 1) {
          daysBadge.className = 'node-days-left-badge status-urgent';
          daysBadge.textContent = `${diffDays} days left`;
        } else if (diffDays === 1) {
          daysBadge.className = 'node-days-left-badge status-urgent';
          daysBadge.textContent = 'Tomorrow';
        } else if (diffDays === 0) {
          daysBadge.className = 'node-days-left-badge status-today';
          daysBadge.textContent = 'Due Today!';
        } else {
          daysBadge.className = 'node-days-left-badge status-overdue';
          daysBadge.textContent = `${Math.abs(diffDays)}d overdue`;
        }
      };

      const statusRow = document.createElement('div');
      statusRow.className = 'node-status-row';
      const statuses = [
        { key: 'todo', label: 'To Do', cls: 'active-todo' },
        { key: 'in_progress', label: 'In Progress', cls: 'active-prog' },
        { key: 'done', label: 'Done', cls: 'active-done' }
      ];
      statuses.forEach(st => {
        const btn = document.createElement('div');
        btn.className = `node-status-btn ${node.status === st.key ? st.cls : ''}`;
        btn.textContent = st.label;
        btn.addEventListener('click', (e) => {
          e.stopPropagation();
          node.status = st.key;
          statusRow.querySelectorAll('.node-status-btn').forEach((b, idx) => {
            b.className = `node-status-btn ${statuses[idx].key === node.status ? statuses[idx].cls : ''}`;
          });
          updateDaysLeft();
          this.scheduleAutoSave();
        });
        statusRow.appendChild(btn);
      });
      body.appendChild(statusRow);

      const dateRow = document.createElement('div');
      dateRow.className = 'node-field-row node-deadline-row';
      dateRow.innerHTML = `<span class="node-field-label">Target</span>`;
      const dateInput = document.createElement('input');
      dateInput.type = 'date';
      dateInput.className = 'node-deadline-input';
      dateInput.value = node.deadline;

      dateInput.addEventListener('click', () => {
        try {
          if (typeof dateInput.showPicker === 'function') {
            dateInput.showPicker();
          }
        } catch(e) {}
      });

      updateDaysLeft();

      dateInput.addEventListener('change', () => {
        node.deadline = dateInput.value;
        updateDaysLeft();
        this.scheduleAutoSave();
      });
      dateRow.appendChild(dateInput);
      dateRow.appendChild(daysBadge);
      body.appendChild(dateRow);

      // Quick Date Presets Row (+3d, +1w, +2w, Calendar)
      const presetsRow = document.createElement('div');
      presetsRow.className = 'node-deadline-presets';

      const setDateByDays = (days) => {
        const d = new Date();
        d.setDate(d.getDate() + days);
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const dt = String(d.getDate()).padStart(2, '0');
        const formatted = `${y}-${m}-${dt}`;
        node.deadline = formatted;
        dateInput.value = formatted;
        updateDaysLeft();
        this.scheduleAutoSave();
        Toast.show(`Target set to ${d.toLocaleDateString()}`, 'info', 1600);
      };

      const btnPlus3 = document.createElement('button');
      btnPlus3.className = 'node-date-preset-btn';
      btnPlus3.textContent = '+3d';
      btnPlus3.title = 'Set deadline 3 days from today';
      btnPlus3.addEventListener('click', (e) => { e.stopPropagation(); setDateByDays(3); });

      const btnPlus7 = document.createElement('button');
      btnPlus7.className = 'node-date-preset-btn';
      btnPlus7.textContent = '+1w';
      btnPlus7.title = 'Set deadline 1 week from today';
      btnPlus7.addEventListener('click', (e) => { e.stopPropagation(); setDateByDays(7); });

      const btnPlus14 = document.createElement('button');
      btnPlus14.className = 'node-date-preset-btn';
      btnPlus14.textContent = '+2w';
      btnPlus14.title = 'Set deadline 2 weeks from today';
      btnPlus14.addEventListener('click', (e) => { e.stopPropagation(); setDateByDays(14); });

      const btnCalendar = document.createElement('button');
      btnCalendar.className = 'node-date-preset-btn btn-picker';
      btnCalendar.innerHTML = '<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg><span>Pick</span>';
      btnCalendar.title = 'Open Visual Calendar Picker';
      btnCalendar.addEventListener('click', (e) => {
        e.stopPropagation();
        try {
          if (typeof dateInput.showPicker === 'function') {
            dateInput.showPicker();
          } else {
            dateInput.focus();
          }
        } catch(e) {
          dateInput.focus();
        }
      });

      presetsRow.appendChild(btnPlus3);
      presetsRow.appendChild(btnPlus7);
      presetsRow.appendChild(btnPlus14);
      presetsRow.appendChild(btnCalendar);

      body.appendChild(presetsRow);
    }

    nodeEl.appendChild(body);

    // Connected Target Badges List Footer
    const connList = document.createElement('div');
    connList.className = 'node-connections-list';
    connList.id = `conn_list_${node.id}`;
    connList.style.display = 'none';
    nodeEl.appendChild(connList);

    // Drag behavior
    this.initNodeDrag(nodeEl, node);

    node.element = nodeEl;
    this.nodes.push(node);
    this.world.appendChild(nodeEl);
    this.renderConnections();
    this.updateCardCount();
    return node;
  }

  initNodeDrag(nodeEl, node) {
    nodeEl.addEventListener('mousedown', (e) => {
      if (e.button !== 0 ||
          this.canvas.spacePressed || e.altKey || this.canvas.isPanning ||
          e.target.closest('.prod-node-pin') ||
          e.target.closest('.node-textarea') ||
          e.target.closest('.node-input-text') ||
          e.target.closest('.node-deadline-input') ||
          e.target.closest('.node-vfx-tag') ||
          e.target.closest('.node-status-btn') ||
          e.target.closest('.prod-node-btn-del') ||
          (e.target.classList.contains('prod-node-title') && document.activeElement === e.target)) {
        return;
      }
      e.stopPropagation();

      const isMulti = (e.shiftKey || e.ctrlKey || e.metaKey);
      if (isMulti) {
        this.selectNode(node, true);
      } else if (!this.selectedNodes.has(node)) {
        this.selectNode(node, false);
      }

      const startMouseX = e.clientX;
      const startMouseY = e.clientY;

      let isDragging = false;
      let hasStartedMoving = false;
      let dragRafPending = false;
      let lastMoveEvt = null;

      let movingNodes = null;
      let startNodePositions = null;
      let movingGroups = null;
      let startGroupPositions = null;
      let movingLooseCards = null;

      const initNodeDragState = () => {
        hasStartedMoving = true;
        isDragging = true;
        this.recordPreState('Move Node');
        nodeEl.classList.add('is-dragging');

        movingNodes = this.selectedNodes.has(node) && this.selectedNodes.size > 1
          ? Array.from(this.selectedNodes)
          : [node];

        startNodePositions = movingNodes.map(n => ({
          node: n,
          startX: n.x,
          startY: n.y
        }));

        movingGroups = Array.from(this.selectedGroups || []);
        startGroupPositions = movingGroups.map(g => ({
          group: g,
          startX: g.x,
          startY: g.y,
          memberCards: this.cards.filter(c => c.groupId === g.id).map(c => ({
            card: c,
            x: c.x,
            y: c.y,
            baseX: c.baseX !== undefined ? c.baseX : c.x,
            baseY: c.baseY !== undefined ? c.baseY : c.y
          }))
        }));

        movingLooseCards = Array.from(this.selectedCards || []).filter(c => 
          !movingGroups.some(g => g.id === c.groupId)
        ).map(c => ({
          card: c,
          x: c.x,
          y: c.y,
          baseX: c.baseX !== undefined ? c.baseX : c.x,
          baseY: c.baseY !== undefined ? c.baseY : c.y
        }));
      };

      const onMouseMove = (moveEvt) => {
        lastMoveEvt = moveEvt;

        if (!hasStartedMoving) {
          const dist = Math.hypot(moveEvt.clientX - startMouseX, moveEvt.clientY - startMouseY);
          if (dist < 4) return;
          initNodeDragState();
        }

        if (dragRafPending) return;
        dragRafPending = true;

        requestAnimationFrame(() => {
          dragRafPending = false;
          if (!hasStartedMoving || !lastMoveEvt) return;

          const dx = (lastMoveEvt.clientX - startMouseX) / this.canvas.zoom;
          const dy = (lastMoveEvt.clientY - startMouseY) / this.canvas.zoom;

          startNodePositions.forEach(item => {
            item.node.x = Math.round(item.startX + dx);
            item.node.y = Math.round(item.startY + dy);
            if (item.node.element) item.node.element.style.transform = `translate(${item.node.x}px, ${item.node.y}px)`;
          });

          startGroupPositions.forEach(item => {
            item.group.x = item.startX + dx;
            item.group.y = item.startY + dy;
            if (item.group.element) item.group.element.style.transform = `translate(${item.group.x}px, ${item.group.y}px)`;

            item.memberCards.forEach(mc => {
              mc.card.x = mc.x + dx;
              mc.card.y = mc.y + dy;
              if (mc.card.baseX !== undefined) mc.card.baseX = mc.baseX + dx;
              if (mc.card.baseY !== undefined) mc.card.baseY = mc.baseY + dy;
              if (mc.card.element) mc.card.element.style.transform = `translate(${mc.card.x}px, ${mc.card.y}px)`;
            });
          });

          movingLooseCards.forEach(item => {
            item.card.x = item.x + dx;
            item.card.y = item.y + dy;
            if (item.card.baseX !== undefined) item.card.baseX = item.baseX + dx;
            if (item.card.baseY !== undefined) item.card.baseY = item.baseY + dy;
            if (item.card.element) item.card.element.style.transform = `translate(${item.card.x}px, ${item.card.y}px)`;
          });

          this.renderConnectionsThrottled();
        });
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);

        if (!hasStartedMoving) {
          // Instant click / selection without delay!
          return;
        }

        nodeEl.classList.remove('is-dragging');
        this.commitHistory('Move Node', true);
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  removeNode(node) {
    this.recordPreState('Delete Node');
    if (node.element) node.element.remove();
    this.nodes = this.nodes.filter(n => n.id !== node.id);
    this.connections = this.connections.filter(c => c.fromNodeId !== node.id && c.toTargetId !== node.id);
    this.renderConnections();
    this.updateCardCount();
    this.commitHistory('Delete Node');
    Toast.show(`Removed ${node.title}`, 'info');
  }

  initNodeResize(handle, node, nodeEl, dir, onResizeCallback) {
    handle.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      this.recordPreState('Resize Node');

      const startMouseX = e.clientX;
      const startMouseY = e.clientY;
      const startWidth = node.width || 285;
      const startHeight = node.height || nodeEl.offsetHeight || 180;
      const startX = node.x || 0;
      const startY = node.y || 0;
      const startFontSize = node.fontSize ? parseInt(node.fontSize) : 13;
      const textarea = nodeEl.querySelector('.node-textarea');
      const startTextareaH = textarea ? textarea.offsetHeight : 60;

      const onMouseMove = (moveEvent) => {
        const dx = (moveEvent.clientX - startMouseX) / this.canvas.zoom;
        const dy = (moveEvent.clientY - startMouseY) / this.canvas.zoom;
        const minW = (node.type === 'note') ? 120 : 160;
        const minH = (node.type === 'note') ? 50 : 80;

        let newWidth = startWidth;
        let newHeight = startHeight;
        let newX = startX;
        let newY = startY;

        // Horizontal stretching
        if (dir.includes('e')) {
          newWidth = Math.max(minW, Math.min(1600, Math.round(startWidth + dx)));
        } else if (dir.includes('w')) {
          newWidth = Math.max(minW, Math.min(1600, Math.round(startWidth - dx)));
          newX = startX + (startWidth - newWidth);
        }

        // Vertical stretching
        if (dir.includes('s')) {
          newHeight = Math.max(minH, Math.min(1800, Math.round(startHeight + dy)));
        } else if (dir.includes('n')) {
          newHeight = Math.max(minH, Math.min(1800, Math.round(startHeight - dy)));
          newY = startY + (startHeight - newHeight);
        }

        node.width = newWidth;
        node.height = newHeight;
        node.x = Math.round(newX);
        node.y = Math.round(newY);

        nodeEl.style.width = `${newWidth}px`;
        nodeEl.style.minHeight = `${newHeight}px`;
        nodeEl.style.transform = `translate(${node.x}px, ${node.y}px)`;

        // Proportional Font Size Scaling & Vertical Expansion (like Text Tool in After Effects)
        if (node.type === 'note') {
          const scaleX = newWidth / startWidth;
          const scaleY = newHeight / startHeight;
          const scale = (scaleX * 0.65) + (scaleY * 0.35);
          const newFontSize = Math.max(8.5, Math.min(72, Math.round(startFontSize * scale)));
          node.fontSize = newFontSize;

          const fontLabel = nodeEl.querySelector('.node-font-label');
          if (fontLabel) fontLabel.textContent = `${newFontSize}px`;

          if (textarea) {
            textarea.style.fontSize = `${newFontSize}px`;
            const dHeight = newHeight - startHeight;
            const newTaHeight = Math.max(28, Math.round(startTextareaH + dHeight));
            node.customTextareaHeight = newTaHeight;
            textarea.style.height = `${newTaHeight}px`;
          }
        }

        if (onResizeCallback) onResizeCallback();
        this.renderConnections();
      };

      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        this.commitHistory('Resize Node');
        this.scheduleAutoSave();
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  initPinDrag(pinEl, node, side) {
    pinEl.addEventListener('mousedown', (e) => {
      e.stopPropagation();
      e.preventDefault();
      
      document.body.classList.add('is-connecting-wire');
      const iframes = document.querySelectorAll('.card-yt-iframe');
      iframes.forEach(f => f.style.pointerEvents = 'none');

      const startPos = this.getNodePinPosition(node, side);
      let ghostWire = document.getElementById('connector-ghost-wire');
      if (!ghostWire) {
        ghostWire = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        ghostWire.id = 'connector-ghost-wire';
        ghostWire.setAttribute('class', 'connector-ghost-wire');
        if (this.connectorLayer) this.connectorLayer.appendChild(ghostWire);
      }

      let activeHoverTargetEl = null;

      const onMouseMove = (moveEvt) => {
        const mouseWorld = this.canvas.screenToWorld(moveEvt.clientX, moveEvt.clientY);
        const p1 = startPos;
        const p2 = mouseWorld;
        const dx = Math.max(20, Math.abs(p2.x - p1.x) * 0.5);
        const d = `M ${p1.x} ${p1.y} C ${p1.x + (side === 'left' ? -dx : dx)} ${p1.y}, ${p2.x + (side === 'left' ? dx : -dx)} ${p2.y}, ${p2.x} ${p2.y}`;
        ghostWire.setAttribute('d', d);

        // Find candidate hover target (with generous 12px margin)
        let newHoverEl = null;
        const hoverCard = this.cards.find(c => 
          mouseWorld.x >= (c.x - 12) && mouseWorld.x <= (c.x + c.width + 12) &&
          mouseWorld.y >= (c.y - 12) && mouseWorld.y <= (c.y + c.height + 12)
        );
        if (hoverCard && hoverCard.element) {
          newHoverEl = hoverCard.element;
        } else {
          const hoverGrp = this.groups.find(g => 
            mouseWorld.x >= (g.x - 8) && mouseWorld.x <= (g.x + g.width + 8) &&
            mouseWorld.y >= (g.y - 8) && mouseWorld.y <= (g.y + g.height + 8)
          );
          if (hoverGrp && hoverGrp.element) {
            newHoverEl = hoverGrp.element;
          }
        }

        if (activeHoverTargetEl !== newHoverEl) {
          if (activeHoverTargetEl) activeHoverTargetEl.classList.remove('pin-hover-target');
          activeHoverTargetEl = newHoverEl;
          if (activeHoverTargetEl) activeHoverTargetEl.classList.add('pin-hover-target');
        }
      };

      const onMouseUp = (upEvt) => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);

        document.body.classList.remove('is-connecting-wire');
        document.querySelectorAll('.card-yt-iframe').forEach(f => f.style.pointerEvents = 'auto');

        if (ghostWire) ghostWire.remove();
        if (activeHoverTargetEl) {
          activeHoverTargetEl.classList.remove('pin-hover-target');
          activeHoverTargetEl = null;
        }

        const mouseWorld = this.canvas.screenToWorld(upEvt.clientX, upEvt.clientY);
        
        // Check if dropped on a card (with 12px margin)
        const targetCard = this.cards.find(c => 
          mouseWorld.x >= (c.x - 12) && mouseWorld.x <= (c.x + c.width + 12) &&
          mouseWorld.y >= (c.y - 12) && mouseWorld.y <= (c.y + c.height + 12)
        );
        if (targetCard) {
          this.recordPreState('Connect Node');
          this.addConnection(node.id, targetCard.id, 'card', side);
          this.commitHistory('Connect Node');
          Toast.show(`Connected to ${targetCard.sourceLabel || 'Reference'}`, 'success');
          return;
        }

        // Check if dropped on a group frame
        const targetGroup = this.groups.find(g => 
          mouseWorld.x >= g.x && mouseWorld.x <= (g.x + g.width) &&
          mouseWorld.y >= g.y && mouseWorld.y <= (g.y + g.height)
        );
        if (targetGroup) {
          this.recordPreState('Connect Node');
          this.addConnection(node.id, targetGroup.id, 'group', side);
          this.commitHistory('Connect Node');
          Toast.show(`Connected to ${targetGroup.title}`, 'success');
          return;
        }

        // Check if dropped on another node
        const targetNode = this.nodes.find(n => 
          n.id !== node.id &&
          mouseWorld.x >= n.x && mouseWorld.x <= (n.x + n.width) &&
          mouseWorld.y >= n.y && mouseWorld.y <= (n.y + (n.element ? n.element.offsetHeight : 150))
        );
        if (targetNode) {
          this.recordPreState('Connect Node');
          this.addConnection(node.id, targetNode.id, 'node', side);
          this.commitHistory('Connect Node');
          Toast.show(`Connected to ${targetNode.title}`, 'success');
        }
      };

      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    });
  }

  getNodePinPosition(node, side = 'right') {
    const h = node.element ? node.element.offsetHeight : 120;
    return {
      x: side === 'left' ? node.x : (node.x + node.width),
      y: node.y + h / 2
    };
  }

  getTargetAnchorPosition(targetId, targetType, fromPos = null) {
    if (targetType === 'card') {
      const card = this.cards.find(c => c.id === targetId);
      if (card) {
        const cardLeft = card.x;
        const cardRight = card.x + card.width;
        const cardMidY = card.y + card.height / 2;
        if (!fromPos || fromPos.x <= cardLeft) {
          return { x: cardLeft, y: cardMidY, side: 'left' };
        } else if (fromPos.x >= cardRight) {
          return { x: cardRight, y: cardMidY, side: 'right' };
        } else {
          return { x: card.x + card.width / 2, y: card.y + card.height / 2, side: 'center' };
        }
      }
    } else if (targetType === 'group') {
      const grp = this.groups.find(g => g.id === targetId);
      if (grp) {
        const grpLeft = grp.x;
        const grpRight = grp.x + grp.width;
        const grpAnchorY = grp.y + 24;
        if (!fromPos || fromPos.x <= grpLeft) {
          return { x: grpLeft, y: grpAnchorY, side: 'left' };
        } else if (fromPos.x >= grpRight) {
          return { x: grpRight, y: grpAnchorY, side: 'right' };
        } else {
          return { x: grp.x + grp.width / 2, y: grp.y + 24, side: 'center' };
        }
      }
    } else if (targetType === 'node') {
      const targetNode = this.nodes.find(n => n.id === targetId);
      if (targetNode) {
        const side = (!fromPos || fromPos.x <= targetNode.x) ? 'left' : 'right';
        const pos = this.getNodePinPosition(targetNode, side);
        return { ...pos, side };
      }
    }
    return null;
  }

  addConnection(fromNodeId, toTargetId, targetType = 'card', side = 'right') {
    const exists = this.connections.some(c => c.fromNodeId === fromNodeId && c.toTargetId === toTargetId);
    if (!exists) {
      this.connections.push({ fromNodeId, toTargetId, targetType, side });
      this.renderConnections();
    }
  }

  renderConnectionsThrottled() {
    if (this.connectionsRafPending) return;
    if (!this.connections || this.connections.length === 0) return;
    this.connectionsRafPending = true;
    requestAnimationFrame(() => {
      this.connectionsRafPending = false;
      this.renderConnections();
    });
  }

  renderConnections() {
    if (!this.connectorLayer) {
      this.connectorLayer = document.getElementById('connector-layer');
    }
    if (!this.connectorLayer) return;
    this.connectorLayer.style.zIndex = '1';

    if (!this.connections || this.connections.length === 0) {
      if (this.connectorLayer) {
        this.connectorLayer.innerHTML = '';
      }
      // Purge all connection pills from cards and nodes
      this.cards.forEach(card => {
        if (card.element) {
          card.element.querySelectorAll('.card-node-link-pill').forEach(p => p.remove());
        }
      });
      this.nodes.forEach(node => {
        const connList = node.element ? node.element.querySelector('.node-connections-list') : null;
        if (connList) {
          connList.innerHTML = '';
          connList.style.display = 'none';
        }
      });
      this.updateConnectedFontEffects();
      return;
    }

    // Filter valid connections
    this.connections = this.connections.filter(conn => {
      const node = this.nodes.find(n => n.id === conn.fromNodeId);
      const targetPos = this.getTargetAnchorPosition(conn.toTargetId, conn.targetType);
      return !!node && !!targetPos;
    });

    // Clear and redraw SVG paths
    this.connectorLayer.innerHTML = '';

    this.connections.forEach((conn, index) => {
      const node = this.nodes.find(n => n.id === conn.fromNodeId);
      const rawTargetPos = this.getTargetAnchorPosition(conn.toTargetId, conn.targetType);
      if (!rawTargetPos) return;

      // Smart pin side selection: exit from the side of the node that faces the target
      let exitSide = conn.side || 'right';
      if (rawTargetPos.x >= (node.x + (node.width || 260))) {
        exitSide = 'right';
      } else if (rawTargetPos.x <= node.x) {
        exitSide = 'left';
      }

      const start = this.getNodePinPosition(node, exitSide);
      const end = this.getTargetAnchorPosition(conn.toTargetId, conn.targetType, start);
      if (!start || !end) return;

      const dx = Math.max(35, Math.abs(end.x - start.x) * 0.45);
      const p1x = start.x + (exitSide === 'left' ? -dx : dx);
      const p2x = end.x + (end.side === 'left' ? -dx : (end.side === 'right' ? dx : 0));
      const d = `M ${start.x} ${start.y} C ${p1x} ${start.y}, ${p2x} ${end.y}, ${end.x} ${end.y}`;

      const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('class', 'connector-wire');
      path.setAttribute('d', d);

      const typeMeta = { note: '#38bdf8', font: '#a855f7', vfx: '#10b981', plan: '#f43f5e' };
      const wireColor = node.color || typeMeta[node.type] || '#38bdf8';
      path.setAttribute('stroke', wireColor);
      path.setAttribute('stroke-width', '3.5');
      path.setAttribute('fill', 'none');
      path.setAttribute('stroke-linecap', 'round');
      path.style.setProperty('--wire-color', wireColor);
      path.setAttribute('title', 'Click to remove connection wire');

      path.addEventListener('click', (e) => {
        e.stopPropagation();
        this.recordPreState('Remove Connection');
        this.connections.splice(index, 1);
        this.renderConnections();
        this.commitHistory('Remove Connection');
        Toast.show('Connection wire removed', 'info');
      });

      this.connectorLayer.appendChild(path);

      // Terminal dots
      const dotStart = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
      dotStart.setAttribute('cx', start.x);
      dotStart.setAttribute('cy', start.y);
      dotStart.setAttribute('r', '4.5');
      dotStart.setAttribute('fill', wireColor);
      this.connectorLayer.appendChild(dotStart);

      const dotEnd = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
      dotEnd.setAttribute('cx', end.x);
      dotEnd.setAttribute('cy', end.y);
      dotEnd.setAttribute('r', '4.5');
      dotEnd.setAttribute('fill', wireColor);
      this.connectorLayer.appendChild(dotEnd);
    });

    // Update footer pills on each node
    this.nodes.forEach(node => {
      const connList = node.element ? node.element.querySelector('.node-connections-list') : null;
      if (!connList) return;
      connList.innerHTML = '';
      const myConns = this.connections.filter(c => c.fromNodeId === node.id);
      if (myConns.length === 0) {
        connList.style.display = 'none';
      } else {
        connList.style.display = 'flex';
        myConns.forEach(c => {
          let label = 'Target';
          if (c.targetType === 'card') {
            const card = this.cards.find(cd => cd.id === c.toTargetId);
            label = card ? (card.sourceLabel || 'Reference') : 'Card';
          } else if (c.targetType === 'group') {
            const grp = this.groups.find(g => g.id === c.toTargetId);
            label = grp ? grp.title : 'Group';
          } else if (c.targetType === 'node') {
            const targetNode = this.nodes.find(n => n.id === c.toTargetId);
            label = targetNode ? targetNode.title : 'Node';
          }
          const pill = document.createElement('span');
          pill.className = 'node-conn-pill';
          pill.style.borderColor = node.color || '#38bdf8';
          pill.innerHTML = `
            <svg viewBox="0 0 24 24" width="9" height="9" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
              <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
            </svg>
            <span>${label}</span>
            <button class="btn-conn-del" title="Disconnect">✕</button>
          `;
          pill.querySelector('.btn-conn-del').addEventListener('click', (e) => {
            e.stopPropagation();
            this.recordPreState('Remove Connection');
            this.connections = this.connections.filter(item => item !== c);
            this.renderConnections();
            this.commitHistory('Remove Connection');
          });
          connList.appendChild(pill);
        });
      }
    });

    // Update link pills on connected cards
    this.cards.forEach(card => {
      const activeLink = this.connections.find(c => c.toTargetId === card.id);
      const existingPills = card.element ? Array.from(card.element.querySelectorAll('.card-node-link-pill')) : [];
      if (activeLink) {
        const parentNode = this.nodes.find(n => n.id === activeLink.fromNodeId);
        if (parentNode && card.element) {
          let pill = existingPills[0];
          if (!pill) {
            pill = document.createElement('div');
            pill.className = 'card-node-link-pill';
            card.element.appendChild(pill);
          }
          pill.style.setProperty('--link-color', parentNode.color || '#38bdf8');
          pill.innerHTML = `<span>●</span><span>${parentNode.title}</span>`;
          // Remove any extra duplicate pills
          for (let i = 1; i < existingPills.length; i++) {
            existingPills[i].remove();
          }
        } else {
          existingPills.forEach(p => p.remove());
        }
      } else {
        existingPills.forEach(p => p.remove());
      }
    });

    // Live update dynamic typography on any connected note nodes!
    this.updateConnectedFontEffects();
  }

  updateConnectedFontEffects() {
    const fontNodes = this.nodes.filter(n => n.type === 'font');
    const noteNodes = this.nodes.filter(n => n.type === 'note');

    noteNodes.forEach(note => {
      const fontConn = this.connections.find(c => {
        return (fontNodes.some(fn => fn.id === c.fromNodeId) && c.toTargetId === note.id) ||
               (c.fromNodeId === note.id && fontNodes.some(fn => fn.id === c.toTargetId));
      });

      let appliedFont = '';
      let sourceFontNode = null;
      if (fontConn) {
        sourceFontNode = fontNodes.find(fn => fn.id === fontConn.fromNodeId || fn.id === fontConn.toTargetId);
        if (sourceFontNode && sourceFontNode.fontFamily) {
          appliedFont = sourceFontNode.fontFamily;
        }
      }

      if (note.element) {
        const textarea = note.element.querySelector('.node-textarea');
        if (textarea) {
          textarea.style.fontFamily = appliedFont ? `"${appliedFont}", sans-serif` : '';
        }
        const chkLabels = note.element.querySelectorAll('.node-chk-label');
        chkLabels.forEach(lbl => {
          lbl.style.fontFamily = appliedFont ? `"${appliedFont}", sans-serif` : '';
        });

        let badge = note.element.querySelector('.node-active-font-badge');
        if (sourceFontNode && appliedFont) {
          if (!badge) {
            badge = document.createElement('span');
            badge.className = 'node-active-font-badge';
            const typeBadge = note.element.querySelector('.prod-node-type-badge');
            if (typeBadge) {
              typeBadge.insertAdjacentElement('afterend', badge);
            }
          }
          badge.textContent = appliedFont;
          badge.title = `Font driven by: ${sourceFontNode.title} (${appliedFont})`;
          badge.style.display = 'inline-block';
        } else if (badge) {
          badge.style.display = 'none';
        }
      }
    });
  }

  // ===========================================================================
  // History & Undo / Redo Engine
  // ===========================================================================
  getSnapshot() {
    return {
      cards: this.cards.map(c => ({
        id: c.id,
        url: c.url,
        localPath: c.localPath || '',
        localWebUrl: c.localWebUrl || '',
        isYouTube: !!c.isYouTube,
        youtubeId: c.youtubeId || null,
        youtubeUrl: c.youtubeUrl || null,
        originalUrl: c.originalUrl || null,
        crop: c.crop ? { ...c.crop } : { top: 0, right: 0, bottom: 0, left: 0 },
        baseWidth: c.baseWidth || c.width,
        baseHeight: c.baseHeight || c.height,
        baseX: c.baseX !== undefined ? c.baseX : c.x,
        baseY: c.baseY !== undefined ? c.baseY : c.y,
        x: c.x,
        y: c.y,
        width: c.width,
        height: c.height,
        aspectRatio: c.aspectRatio,
        zIndex: c.zIndex,
        sourceLabel: c.sourceLabel,
        groupId: c.groupId || null
      })),
      groups: this.groups.map(g => ({
        id: g.id,
        title: g.title,
        notes: g.notes || '',
        x: g.x,
        y: g.y,
        width: g.width,
        height: g.height,
        color: g.color
      })),
      projectName: this.projectName,
      arrangeGap: this.arrangeGap !== undefined ? this.arrangeGap : 32,
      nodes: this.nodes.map(n => ({
        id: n.id,
        type: n.type,
        x: n.x,
        y: n.y,
        width: n.width,
        title: n.title,
        color: n.color || null,
        content: n.content,
        mode: n.mode || 'text',
        items: n.items ? [...n.items] : [],
        tags: n.tags ? [...n.tags] : [],
        fontFamily: n.fontFamily,
        fontSize: n.fontSize,
        activeVfx: n.activeVfx ? [...n.activeVfx] : [],
        deadline: n.deadline,
        status: n.status
      })),
      connections: this.connections.map(c => ({
        fromNodeId: c.fromNodeId,
        toTargetId: c.toTargetId,
        targetType: c.targetType,
        side: c.side
      }))
    };
  }

  recordPreState(description = 'Action') {
    if (!this.preActionSnapshot) {
      this.preActionSnapshot = {
        description,
        snapshot: this.getSnapshot()
      };
    }
  }

  commitHistory(actionName, forceChanged = false) {
    if (this.preActionSnapshot) {
      let hasChanged = forceChanged;
      if (!hasChanged) {
        const current = this.getSnapshot();
        hasChanged = (JSON.stringify(current) !== JSON.stringify(this.preActionSnapshot.snapshot));
      }
      if (hasChanged) {
        this.undoStack.push({
          description: actionName || this.preActionSnapshot.description,
          snapshot: this.preActionSnapshot.snapshot
        });
        if (this.undoStack.length > 50) this.undoStack.shift();
        this.redoStack = [];
      }
      this.preActionSnapshot = null;
      this.scheduleAutoSave();
    }
  }

  undo() {
    if (this.undoStack.length === 0) {
      Toast.show('Nothing to undo', 'info');
      return;
    }
    const entry = this.undoStack.pop();
    this.redoStack.push({
      description: entry.description,
      snapshot: this.getSnapshot()
    });
    this.applySnapshot(entry.snapshot);
    Toast.show(`Undo: ${entry.description}`, 'info');
  }

  redo() {
    if (this.redoStack.length === 0) {
      Toast.show('Nothing to redo', 'info');
      return;
    }
    const entry = this.redoStack.pop();
    this.undoStack.push({
      description: entry.description,
      snapshot: this.getSnapshot()
    });
    this.applySnapshot(entry.snapshot);
    Toast.show(`Redo: ${entry.description}`, 'info');
  }

  applySnapshot(snapshot) {
    if (!snapshot || !snapshot.cards || !snapshot.groups) return;

    // 1. Reconcile Groups
    const currentGroupMap = new Map(this.groups.map(g => [g.id, g]));
    const targetGroupIds = new Set(snapshot.groups.map(g => g.id));

    this.groups = this.groups.filter(g => {
      if (!targetGroupIds.has(g.id)) {
        if (g.element) g.element.remove();
        return false;
      }
      return true;
    });

    snapshot.groups.forEach(snapG => {
      let g = currentGroupMap.get(snapG.id);
      if (g) {
        g.title = snapG.title;
        g.notes = snapG.notes;
        g.x = snapG.x;
        g.y = snapG.y;
        g.width = snapG.width;
        g.height = snapG.height;
        g.color = snapG.color;
        if (g.element) {
          g.element.style.transform = `translate(${g.x}px, ${g.y}px)`;
          g.element.style.width = `${g.width}px`;
          g.element.style.height = `${g.height}px`;
          g.element.style.setProperty('--group-color', g.color);
          const t = g.element.querySelector('.group-title');
          if (t && t.textContent !== g.title) t.textContent = g.title;
          const ta = g.element.querySelector('.group-notes-textarea');
          if (ta && ta.value !== g.notes) ta.value = g.notes;
        }
      } else {
        this.createGroup(snapG.title, snapG.x, snapG.y, snapG.width, snapG.height, snapG.color, snapG.notes, snapG.id);
      }
    });

    // 2. Reconcile Cards
    const currentCardMap = new Map(this.cards.map(c => [c.id, c]));
    const targetCardIds = new Set(snapshot.cards.map(c => c.id));

    this.cards = this.cards.filter(c => {
      if (!targetCardIds.has(c.id)) {
        if (c.element) c.element.remove();
        return false;
      }
      return true;
    });

    snapshot.cards.forEach(snapC => {
      let c = currentCardMap.get(snapC.id);
      if (c) {
        c.baseWidth = snapC.baseWidth || snapC.width;
        c.baseHeight = snapC.baseHeight || snapC.height;
        c.baseX = snapC.baseX !== undefined ? snapC.baseX : snapC.x;
        c.baseY = snapC.baseY !== undefined ? snapC.baseY : snapC.y;
        c.x = snapC.x;
        c.y = snapC.y;
        c.width = snapC.width;
        c.height = snapC.height;
        c.crop = { ...snapC.crop };
        c.groupId = snapC.groupId || null;
        c.zIndex = snapC.zIndex || c.zIndex;

        if (c.element) {
          c.element.style.zIndex = c.zIndex;
          c.element.style.transform = `translate(${c.x}px, ${c.y}px)`;
          c.element.style.width = `${c.width}px`;
          c.element.style.height = `${c.height}px`;
          this.applyCropTransform(c);
          this.updateCardGroupBadge(c);
        }
      } else {
        const img = new Image();
        let src = snapC.url;
        if (snapC.localPath) {
          const slash = snapC.localPath.lastIndexOf('\\');
          const fname = slash >= 0 ? snapC.localPath.substring(slash + 1) : snapC.localPath;
          src = 'https://dropboard-cache.local/' + fname;
        }
        img.src = src || snapC.url;
        img.onload = () => {
          const card = {
            id: snapC.id,
            url: snapC.url,
            localPath: snapC.localPath || '',
            localWebUrl: snapC.localWebUrl || '',
            isYouTube: !!snapC.isYouTube,
            youtubeId: snapC.youtubeId || null,
            youtubeUrl: snapC.youtubeUrl || null,
            originalUrl: snapC.originalUrl || null,
            crop: snapC.crop ? { ...snapC.crop } : { top: 0, right: 0, bottom: 0, left: 0 },
            baseWidth: snapC.baseWidth || snapC.width,
            baseHeight: snapC.baseHeight || snapC.height,
            baseX: snapC.baseX !== undefined ? snapC.baseX : snapC.x,
            baseY: snapC.baseY !== undefined ? snapC.baseY : snapC.y,
            x: snapC.x,
            y: snapC.y,
            width: snapC.width,
            height: snapC.height,
            aspectRatio: snapC.aspectRatio || (snapC.width / snapC.height),
            zIndex: snapC.zIndex || ++this.highestZ,
            sourceLabel: snapC.sourceLabel || (snapC.isYouTube ? 'YouTube Ref' : 'Reference'),
            groupId: snapC.groupId || null,
            element: null
          };
          this.createCardDOM(card, img);
          this.cards.push(card);
          this.updateCardCount();
          this.updateGroupCounts();
        };
      }
    });

    this.updateCardCount();
    this.updateGroupCounts();

    if (snapshot.projectName) {
      this.setProjectName(snapshot.projectName, false);
    }

    if (snapshot.arrangeGap !== undefined) {
      this.arrangeGap = snapshot.arrangeGap;
      const gapBadge = document.getElementById('gap-value-display');
      const gapLabel = document.getElementById('gap-btn-label');
      const gapSlider = document.getElementById('gap-slider');
      const text = this.arrangeGap === 0 ? 'No Gap' : `${this.arrangeGap}px`;
      if (gapBadge) gapBadge.textContent = text;
      if (gapLabel) gapLabel.textContent = `Gap: ${text}`;
      if (gapSlider) gapSlider.value = this.arrangeGap;
    }

    // 3. Reconcile Production Nodes
    if (snapshot.nodes && Array.isArray(snapshot.nodes)) {
      const currentNodeMap = new Map(this.nodes.map(n => [n.id, n]));
      const targetNodeIds = new Set(snapshot.nodes.map(n => n.id));

      this.nodes = this.nodes.filter(n => {
        if (!targetNodeIds.has(n.id)) {
          if (n.element) n.element.remove();
          return false;
        }
        return true;
      });

      snapshot.nodes.forEach(snapN => {
        let n = currentNodeMap.get(snapN.id);
        if (n) {
          n.x = snapN.x;
          n.y = snapN.y;
          n.width = snapN.width;
          n.title = snapN.title;
          n.content = snapN.content;
          n.fontFamily = snapN.fontFamily;
          n.fontSize = snapN.fontSize;
          n.activeVfx = snapN.activeVfx ? [...snapN.activeVfx] : [];
          n.deadline = snapN.deadline;
          n.status = snapN.status;
          if (n.element) {
            n.element.style.transform = `translate(${n.x}px, ${n.y}px)`;
            n.element.style.width = `${n.width}px`;
            const titleEl = n.element.querySelector('.prod-node-title');
            if (titleEl && titleEl.textContent !== n.title) titleEl.textContent = n.title;
            const ta = n.element.querySelector('.node-textarea');
            if (ta && ta.value !== n.content) ta.value = n.content;
            const fontIn = n.element.querySelector('.node-input-text');
            if (fontIn && fontIn.value !== n.fontFamily) fontIn.value = n.fontFamily;
            const fp = n.element.querySelector('.node-font-preview');
            if (fp) fp.style.fontFamily = `"${n.fontFamily}", sans-serif`;
            const dateIn = n.element.querySelector('.node-deadline-input');
            if (dateIn && dateIn.value !== n.deadline) dateIn.value = n.deadline;
          }
        } else {
          this.createNode(snapN.type, snapN.x, snapN.y, snapN);
        }
      });
    }

    if (snapshot.connections && Array.isArray(snapshot.connections)) {
      this.connections = [...snapshot.connections];
      setTimeout(() => this.renderConnections(), 50);
    }
  }

  // ===========================================================================
  // Serialization (Save & Load Board)
  // ===========================================================================
  getCardImageData(card) {
    if (card.isYouTube) return null;
    if (card.imageData && typeof card.imageData === 'string' && card.imageData.startsWith('data:image/')) {
      return card.imageData;
    }
    const img = card.element ? card.element.querySelector('img') : null;
    if (img && img.complete && img.naturalWidth > 0) {
      try {
        const cvs = document.createElement('canvas');
        cvs.width = img.naturalWidth;
        cvs.height = img.naturalHeight;
        const ctx = cvs.getContext('2d');
        ctx.drawImage(img, 0, 0);
        const isPng = (card.url && card.url.toLowerCase().endsWith('.png')) || (card.localPath && card.localPath.toLowerCase().endsWith('.png'));
        const format = isPng ? 'image/png' : 'image/jpeg';
        const dataUrl = cvs.toDataURL(format, 0.90);
        card.imageData = dataUrl;
        return dataUrl;
      } catch (e) {
        console.warn('Canvas export skipped for card:', card.id, e);
      }
    }
    return card.imageData || null;
  }

  serialize(embedImages = true) {
    return {
      version: 3,
      name: this.projectName || 'Untitled Project',
      arrangeGap: this.arrangeGap !== undefined ? this.arrangeGap : 32,
      created: new Date().toISOString(),
      cards: this.cards.map(c => ({
        id: c.id,
        url: c.url,
        imageData: embedImages ? this.getCardImageData(c) : undefined,
        localPath: c.localPath,
        isYouTube: !!c.isYouTube,
        youtubeId: c.youtubeId || null,
        youtubeUrl: c.youtubeUrl || null,
        originalUrl: c.originalUrl || null,
        crop: c.crop || { top: 0, right: 0, bottom: 0, left: 0 },
        baseWidth: c.baseWidth || c.width,
        baseHeight: c.baseHeight || c.height,
        baseX: c.baseX !== undefined ? c.baseX : c.x,
        baseY: c.baseY !== undefined ? c.baseY : c.y,
        x: c.x,
        y: c.y,
        width: c.width,
        height: c.height,
        aspectRatio: c.aspectRatio,
        sourceLabel: c.sourceLabel,
        groupId: c.groupId || null
      })),
      groups: this.groups.map(g => ({
        id: g.id,
        title: g.title,
        notes: g.notes,
        x: g.x,
        y: g.y,
        width: g.width,
        height: g.height,
        color: g.color
      })),
      nodes: this.nodes.map(n => ({
        id: n.id,
        type: n.type,
        x: n.x,
        y: n.y,
        width: n.width,
        title: n.title,
        color: n.color || null,
        content: n.content,
        mode: n.mode || 'text',
        items: n.items ? [...n.items] : [],
        tags: n.tags ? [...n.tags] : [],
        fontFamily: n.fontFamily,
        fontSize: n.fontSize,
        activeVfx: n.activeVfx ? [...n.activeVfx] : [],
        deadline: n.deadline,
        status: n.status
      })),
      connections: this.connections.map(c => ({
        fromNodeId: c.fromNodeId,
        toTargetId: c.toTargetId,
        targetType: c.targetType,
        side: c.side
      }))
    };
  }

  deserialize(jsonStr, filePath = null) {
    try {
      const data = typeof jsonStr === 'string' ? JSON.parse(jsonStr) : jsonStr;
      if (!data.cards) return;

      if (data.name) {
        this.setProjectName(data.name, false);
      } else if (filePath) {
        const fileName = filePath.replace(/^.*[\\\/]/, '').replace(/\.dropboard$/i, '');
        this.setProjectName(fileName, false);
      }

      if (data.arrangeGap !== undefined) {
        this.arrangeGap = data.arrangeGap;
        const gapBadge = document.getElementById('gap-value-display');
        const gapLabel = document.getElementById('gap-btn-label');
        const gapSlider = document.getElementById('gap-slider');
        const text = this.arrangeGap === 0 ? 'No Gap' : `${this.arrangeGap}px`;
        if (gapBadge) gapBadge.textContent = text;
        if (gapLabel) gapLabel.textContent = `Gap: ${text}`;
        if (gapSlider) gapSlider.value = this.arrangeGap;
      }

      this.clearAll(false);
      this.groups.forEach(g => {
        if (g.element) g.element.remove();
      });
      this.groups = [];

      this.nodes.forEach(n => {
        if (n.element) n.element.remove();
      });
      this.nodes = [];
      this.connections = [];

      // Restore Groups
      if (data.groups && Array.isArray(data.groups)) {
        data.groups.forEach(g => {
          this.createGroup(g.title, g.x, g.y, g.width, g.height, g.color, g.notes || '', g.id);
        });
      }

      // Restore Nodes
      if (data.nodes && Array.isArray(data.nodes)) {
        data.nodes.forEach(n => {
          this.createNode(n.type, n.x, n.y, n);
        });
      }

      // Restore Connections
      if (data.connections && Array.isArray(data.connections)) {
        this.connections = [...data.connections];
        setTimeout(() => this.renderConnections(), 200);
      }

      // Restore Cards
      const totalCards = (data.cards && Array.isArray(data.cards)) ? data.cards.length : 0;
      let loadedCardsCount = 0;
      const onCardFinished = () => {
        loadedCardsCount++;
        if (loadedCardsCount >= totalCards) {
          // Re-tidy all groups once all member cards have rendered to guarantee layout integrity
          this.groups.forEach(g => {
            const members = this.cards.filter(c => c.groupId === g.id);
            if (members.length > 0) {
              this.tidyGroup(g, false);
            }
          });
          setTimeout(() => this.fitAllToView(), 150);
        }
      };

      if (totalCards === 0) {
        setTimeout(() => this.fitAllToView(), 150);
      } else {
        data.cards.forEach(c => {
          const img = new Image();
          let src = null;
          // 1. If embedded imageData is present, use it directly (100% portable across computers / wiped cache)
          if (c.imageData && typeof c.imageData === 'string' && c.imageData.startsWith('data:image/')) {
            src = c.imageData;
          } else if (c.localPath) {
            const slash = c.localPath.lastIndexOf('\\');
            const fname = slash >= 0 ? c.localPath.substring(slash + 1) : c.localPath;
            src = 'https://dropboard-cache.local/' + fname;
            img.crossOrigin = 'anonymous';
          } else {
            src = c.url;
          }
          // Attach load and error listeners BEFORE setting src to guarantee no events are missed on fast data URLs
          img.onload = () => {
            const card = {
              id: c.id,
              url: c.url,
              imageData: c.imageData || (src && src.startsWith('data:image/') ? src : null),
              localPath: c.localPath || '',
              localWebUrl: c.localWebUrl || '',
              isYouTube: !!c.isYouTube,
              youtubeId: c.youtubeId || null,
              youtubeUrl: c.youtubeUrl || null,
              originalUrl: c.originalUrl || null,
              crop: c.crop || { top: 0, right: 0, bottom: 0, left: 0 },
              baseWidth: c.baseWidth || c.width,
              baseHeight: c.baseHeight || c.height,
              baseX: c.baseX !== undefined ? c.baseX : c.x,
              baseY: c.baseY !== undefined ? c.baseY : c.y,
              x: c.x,
              y: c.y,
              width: c.width,
              height: c.height,
              aspectRatio: c.aspectRatio || (c.width / c.height),
              zIndex: ++this.highestZ,
              sourceLabel: c.sourceLabel || (c.isYouTube ? 'YouTube Ref' : 'Reference'),
              groupId: c.groupId || null,
              element: null
            };
            this.createCardDOM(card, img);
            this.cards.push(card);
            this.updateCardCount();
            this.updateGroupCounts();

            // Pre-cache base64 imageData if not already set
            if (!card.imageData && !card.isYouTube) {
              try {
                const cvs = document.createElement('canvas');
                cvs.width = img.naturalWidth;
                cvs.height = img.naturalHeight;
                const ctx = cvs.getContext('2d');
                ctx.drawImage(img, 0, 0);
                const isPng = (card.url && card.url.toLowerCase().endsWith('.png')) || (card.localPath && card.localPath.toLowerCase().endsWith('.png'));
                card.imageData = cvs.toDataURL(isPng ? 'image/png' : 'image/jpeg', 0.90);
              } catch(e) {}
            }

            onCardFinished();
          };

          img.onerror = () => {
            // Fallback 1: If local path or url failed, but embedded imageData exists
            if (c.imageData && img.src !== c.imageData) {
              img.src = c.imageData;
              return;
            }
            // Fallback 2: If cache file was removed, but originalUrl exists (Pinterest, Google, Bing, Web)
            if (c.originalUrl && !img.dataset.originalUrlTried && (c.originalUrl.startsWith('http://') || c.originalUrl.startsWith('https://') || c.originalUrl.startsWith('data:image/'))) {
              img.dataset.originalUrlTried = 'true';
              img.src = c.originalUrl;
              if (c.originalUrl.startsWith('http://') || c.originalUrl.startsWith('https://')) {
                NativeBridge.downloadImage(c.originalUrl, c.id);
              }
              return;
            }
            // Fallback 3: YouTube thumbnail fallback
            if (c.isYouTube && c.youtubeId && !img.dataset.fallbackTried) {
              img.dataset.fallbackTried = 'true';
              img.src = `https://img.youtube.com/vi/${c.youtubeId}/hqdefault.jpg`;
              return;
            }
            // Fallback 4: Even if image file was deleted from disk and no imageData exists,
            // keep the card on board with a placeholder so groups, notes, and connections are preserved!
            const fallbackCard = {
              id: c.id,
              url: c.url,
              imageData: null,
              localPath: c.localPath || '',
              localWebUrl: c.localWebUrl || '',
              isYouTube: !!c.isYouTube,
              youtubeId: c.youtubeId || null,
              youtubeUrl: c.youtubeUrl || null,
              originalUrl: c.originalUrl || null,
              crop: c.crop || { top: 0, right: 0, bottom: 0, left: 0 },
              baseWidth: c.baseWidth || c.width || 320,
              baseHeight: c.baseHeight || c.height || 240,
              baseX: c.baseX !== undefined ? c.baseX : c.x,
              baseY: c.baseY !== undefined ? c.baseY : c.y,
              x: c.x,
              y: c.y,
              width: c.width || 320,
              height: c.height || 240,
              aspectRatio: c.aspectRatio || ((c.width || 320) / (c.height || 240)),
              zIndex: ++this.highestZ,
              sourceLabel: c.sourceLabel || (c.isYouTube ? 'YouTube Ref' : 'Reference'),
              groupId: c.groupId || null,
              element: null
            };
            const placeholderImg = new Image();
            placeholderImg.src = `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="${fallbackCard.width}" height="${fallbackCard.height}" viewBox="0 0 ${fallbackCard.width} ${fallbackCard.height}"><rect width="100%" height="100%" rx="8" fill="%23171b24" stroke="%23334155"/><text x="50%" y="45%" fill="%23ef4444" font-family="sans-serif" font-size="12" font-weight="600" text-anchor="middle">Image File Missing</text><text x="50%" y="60%" fill="%2364748b" font-family="sans-serif" font-size="11" text-anchor="middle">(Cache was removed)</text></svg>`;
            placeholderImg.onload = () => {
              this.createCardDOM(fallbackCard, placeholderImg);
              this.cards.push(fallbackCard);
              this.updateCardCount();
              this.updateGroupCounts();
              onCardFinished();
            };
          };

          img.src = src || c.url;
          if (img.complete && img.naturalWidth > 0) {
            img.onload();
          }
        });
      }
    } catch (err) {
      Toast.show('Failed to parse board file', 'error');
    }
  }

  clearAll(commit = true) {
    if (commit) {
      this.recordPreState('Clear All');
    }
    this.cards.forEach(c => {
      if (c.element) c.element.remove();
    });
    this.cards = [];
    this.groups.forEach(g => {
      if (g.element) g.element.remove();
    });
    this.groups = [];
    this.nodes.forEach(n => {
      if (n.element) n.element.remove();
    });
    this.nodes = [];
    this.connections = [];
    this.selectedCards.clear();
    this.selectedGroups.clear();
    this.selectedNodes.clear();
    this.selectedCard = null;
    this.selectedGroup = null;
    this.selectedNode = null;
    this.renderConnections();
    this.updateCardCount();
    this.updateGroupCounts();
    if (commit) {
      this.commitHistory('Clear All');
      this.scheduleAutoSave();
    }
  }

  createNewProject(title = 'Untitled Project') {
    this.currentFilePath = null;
    try {
      localStorage.removeItem('dropboard_last_file_path');
      localStorage.removeItem('dropboard_autosave_state');
    } catch(e) {}
    this.clearAll(false);
    this.undoStack = [];
    this.redoStack = [];
    this.setProjectName(title || 'Untitled Project', false);
    // Reset canvas pan & zoom
    this.canvas.panX = window.innerWidth / 2;
    this.canvas.panY = window.innerHeight / 2;
    this.canvas.zoom = 1.0;
    this.canvas.updateTransform();
    Toast.show(`Created new project: "${this.projectName}"`, 'success');
  }

  saveProject(saveAs = false) {
    const data = JSON.stringify(this.serialize(true), null, 2);
    if (!saveAs && this.currentFilePath) {
      // Direct fast save (overwrites existing file without prompt)
      NativeBridge.saveBoardDirect(data, this.currentFilePath);
    } else {
      // Save As (prompts file explorer)
      NativeBridge.saveBoardDialog(data, this.projectName);
    }
  }

  initNewProjectModal() {
    const modal = document.getElementById('new-project-modal');
    const nameInput = document.getElementById('new-project-name-input');
    const btnCancel = document.getElementById('btn-cancel-new-proj');
    const btnClose = document.getElementById('btn-close-new-modal');
    const btnConfirm = document.getElementById('btn-confirm-new-proj');
    const btnSaveFirst = document.getElementById('btn-save-then-new');
    const btnDock = document.getElementById('btn-new-project');

    this.openNewProjectModal = () => {
      if (nameInput) nameInput.value = 'Untitled Project';
      if (modal) {
        modal.classList.add('show');
        modal.style.display = 'flex';
      }
      setTimeout(() => { if (nameInput) nameInput.select(); }, 50);
    };

    const closeModal = () => {
      if (modal) {
        modal.classList.remove('show');
        modal.style.display = 'none';
      }
    };

    if (btnDock) {
      btnDock.addEventListener('click', (e) => {
        e.stopPropagation();
        this.openNewProjectModal();
      });
    }

    if (btnCancel) btnCancel.addEventListener('click', closeModal);
    if (btnClose) btnClose.addEventListener('click', closeModal);
    if (modal) {
      modal.addEventListener('click', (e) => {
        if (e.target === modal) closeModal();
      });
    }

    if (btnConfirm) {
      btnConfirm.addEventListener('click', () => {
        const val = nameInput ? nameInput.value.trim() : '';
        this.createNewProject(val || 'Untitled Project');
        closeModal();
      });
    }

    if (btnSaveFirst) {
      btnSaveFirst.addEventListener('click', () => {
        const currentData = JSON.stringify(this.serialize(true), null, 2);
        NativeBridge.saveBoardDialog(currentData, this.projectName);
        const val = nameInput ? nameInput.value.trim() : '';
        this.createNewProject(val || 'Untitled Project');
        closeModal();
      });
    }

    if (nameInput) {
      nameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          const val = nameInput.value.trim();
          this.createNewProject(val || 'Untitled Project');
          closeModal();
        }
      });
    }
  }

  initSettingsModal() {
    const modal = document.getElementById('settings-modal');
    const btnTb = document.getElementById('btn-settings-tb');
    const btnDock = document.getElementById('btn-settings-dock');
    const btnClose = document.getElementById('btn-close-settings-modal');
    const btnCloseFooter = document.getElementById('btn-close-settings-footer');
    const toggleAutoSave = document.getElementById('settings-toggle-autosave');
    const autoSaveStatus = document.getElementById('settings-autosave-status');
    const btnClearCache = document.getElementById('btn-clear-cache-storage');
    const btnSaveNow = document.getElementById('settings-btn-save-now');
    const selectDockPos = document.getElementById('settings-select-dock-pos');
    const selectDockStyle = document.getElementById('settings-select-dock-style');

    const gridDockPos = document.getElementById('grid-dock-pos');
    const gridDockStyle = document.getElementById('grid-dock-style');
    const btnOpenCacheFolder = document.getElementById('btn-open-cache-folder');

    // Navigation & Touchpad Controls
    const gridNavMode = document.getElementById('grid-nav-mode');
    const sliderPanSens = document.getElementById('settings-pan-sensitivity');
    const labelPanSens = document.getElementById('pan-sens-label');
    const sliderZoomSens = document.getElementById('settings-zoom-sensitivity');
    const labelZoomSens = document.getElementById('zoom-sens-label');
    const toggleInvertPan = document.getElementById('settings-toggle-invert-pan');

    const tabBtns = modal ? modal.querySelectorAll('.settings-tab-btn') : [];
    const tabPanes = modal ? modal.querySelectorAll('.settings-tab-pane') : [];

    const syncVisualSelectors = () => {
      if (gridDockPos) {
        gridDockPos.querySelectorAll('.visual-select-card').forEach(card => {
          card.classList.toggle('active', card.dataset.value === this.dockPosition);
        });
      }
      if (gridDockStyle) {
        gridDockStyle.querySelectorAll('.visual-select-card').forEach(card => {
          card.classList.toggle('active', card.dataset.value === this.dockStyle);
        });
      }
      if (gridNavMode) {
        gridNavMode.querySelectorAll('.visual-select-card').forEach(card => {
          card.classList.toggle('active', card.dataset.value === this.canvas.navMode);
        });
      }
    };

    const updateStats = () => {
      const statRefs = document.getElementById('stat-cards-count');
      const statGroups = document.getElementById('stat-groups-count');
      const statNodes = document.getElementById('stat-nodes-count');
      if (statRefs) statRefs.textContent = this.cards.length;
      if (statGroups) statGroups.textContent = this.groups.length;
      if (statNodes) statNodes.textContent = this.nodes.length;
    };

    const openModal = () => {
      if (toggleAutoSave) {
        toggleAutoSave.checked = this.autoSaveEnabled;
      }
      if (autoSaveStatus) {
        autoSaveStatus.textContent = this.autoSaveEnabled ? 'Status: Auto-Save Active (saving changes automatically)' : 'Status: Auto-Save Paused (manual save only)';
        autoSaveStatus.style.color = this.autoSaveEnabled ? '#34d399' : '#f87171';
      }
      if (selectDockPos) {
        selectDockPos.value = this.dockPosition;
      }
      if (selectDockStyle) {
        selectDockStyle.value = this.dockStyle;
      }
      if (sliderPanSens) {
        sliderPanSens.value = this.canvas.panSensitivity;
        if (labelPanSens) labelPanSens.textContent = `${this.canvas.panSensitivity.toFixed(1)}x`;
      }
      if (sliderZoomSens) {
        sliderZoomSens.value = this.canvas.zoomSensitivity;
        if (labelZoomSens) labelZoomSens.textContent = `${this.canvas.zoomSensitivity.toFixed(1)}x`;
      }
      if (toggleInvertPan) {
        toggleInvertPan.checked = this.canvas.invertPan;
      }
      const toggleAutoHide = document.getElementById('settings-toggle-autohide-dock');
      if (toggleAutoHide) {
        toggleAutoHide.checked = this.autoHideDock;
      }
      syncVisualSelectors();
      updateStats();
      if (modal) {
        modal.classList.add('show');
        modal.style.display = 'flex';
      }
    };

    const closeModal = () => {
      if (modal) {
        modal.classList.remove('show');
        modal.style.display = 'none';
      }
    };

    if (btnTb) btnTb.addEventListener('click', openModal);
    if (btnDock) btnDock.addEventListener('click', openModal);
    if (btnClose) btnClose.addEventListener('click', closeModal);
    if (btnCloseFooter) btnCloseFooter.addEventListener('click', closeModal);
    if (modal) {
      modal.addEventListener('click', (e) => {
        if (e.target === modal) closeModal();
      });
    }

    tabBtns.forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        const targetTab = btn.getAttribute('data-tab');
        tabBtns.forEach(b => b.classList.remove('active'));
        tabPanes.forEach(p => p.classList.remove('active'));
        btn.classList.add('active');
        const targetPane = document.getElementById(targetTab);
        if (targetPane) {
          targetPane.classList.add('active');
        }
      });
    });

    if (gridDockPos) {
      gridDockPos.querySelectorAll('.visual-select-card').forEach(card => {
        card.addEventListener('click', () => {
          const val = card.dataset.value;
          this.setDockPosition(val);
          syncVisualSelectors();
          Toast.show(`Toolbar: ${card.querySelector('strong').textContent}`, 'info');
        });
      });
    }

    if (gridDockStyle) {
      gridDockStyle.querySelectorAll('.visual-select-card').forEach(card => {
        card.addEventListener('click', () => {
          const val = card.dataset.value;
          this.setDockStyle(val);
          syncVisualSelectors();
          Toast.show(`Mode: ${card.querySelector('strong').textContent}`, 'info');
        });
      });
    }

    if (gridNavMode) {
      gridNavMode.querySelectorAll('.visual-select-card').forEach(card => {
        card.addEventListener('click', () => {
          const val = card.dataset.value;
          this.canvas.navMode = val;
          localStorage.setItem('dropboard_nav_mode', val);
          syncVisualSelectors();
          Toast.show(`Navigation: ${card.querySelector('strong').textContent}`, 'info');
        });
      });
    }

    if (sliderPanSens) {
      sliderPanSens.addEventListener('input', (e) => {
        const val = parseFloat(e.target.value) || 1.0;
        this.canvas.panSensitivity = val;
        if (labelPanSens) labelPanSens.textContent = `${val.toFixed(1)}x`;
        localStorage.setItem('dropboard_pan_sens', val.toString());
      });
    }

    if (sliderZoomSens) {
      sliderZoomSens.addEventListener('input', (e) => {
        const val = parseFloat(e.target.value) || 1.0;
        this.canvas.zoomSensitivity = val;
        if (labelZoomSens) labelZoomSens.textContent = `${val.toFixed(1)}x`;
        localStorage.setItem('dropboard_zoom_sens', val.toString());
      });
    }

    if (toggleInvertPan) {
      toggleInvertPan.addEventListener('change', (e) => {
        this.canvas.invertPan = e.target.checked;
        localStorage.setItem('dropboard_invert_pan', e.target.checked.toString());
        Toast.show(e.target.checked ? 'Touchpad Direction Inverted' : 'Touchpad Direction Normal', 'info');
      });
    }

    if (btnOpenCacheFolder) {
      btnOpenCacheFolder.addEventListener('click', () => {
        NativeBridge.openCacheFolder();
        Toast.show('Opened Image Cache folder in Explorer', 'info');
      });
    }

    if (selectDockPos) {
      selectDockPos.addEventListener('change', (e) => {
        this.setDockPosition(e.target.value);
        syncVisualSelectors();
      });
    }

    if (selectDockStyle) {
      selectDockStyle.addEventListener('change', (e) => {
        this.setDockStyle(e.target.value);
        syncVisualSelectors();
      });
    }

    const toggleAutoHide = document.getElementById('settings-toggle-autohide-dock');
    if (toggleAutoHide) {
      toggleAutoHide.addEventListener('change', (e) => {
        this.setAutoHideDock(e.target.checked);
      });
    }

    if (toggleAutoSave) {
      toggleAutoSave.addEventListener('change', () => {
        this.autoSaveEnabled = toggleAutoSave.checked;
        try {
          localStorage.setItem('dropboard_autosave_enabled', this.autoSaveEnabled ? 'true' : 'false');
        } catch(e) {}
        if (autoSaveStatus) {
          autoSaveStatus.textContent = this.autoSaveEnabled ? 'Status: Auto-Save Active (saving changes automatically)' : 'Status: Auto-Save Paused (manual save only)';
          autoSaveStatus.style.color = this.autoSaveEnabled ? '#34d399' : '#f87171';
        }
        Toast.show(this.autoSaveEnabled ? 'Auto-Save Enabled' : 'Auto-Save Disabled (Manual Save Only)', 'info');
      });
    }

    if (btnClearCache) {
      btnClearCache.addEventListener('click', () => {
        this.cards.forEach(c => {
          if (!c.imageData && !c.isYouTube) {
            this.getCardImageData(c);
          }
        });
        this.saveAutoSave();
        NativeBridge.clearImageCache();
      });
    }

    if (btnSaveNow) {
      btnSaveNow.addEventListener('click', () => {
        const currentData = JSON.stringify(this.serialize(true), null, 2);
        NativeBridge.saveBoardDialog(currentData, this.projectName);
      });
    }
  }

  initAboutModal() {
    const modal = document.getElementById('about-modal');
    const btnTb = document.getElementById('btn-about-tb');
    const btnBrand = document.getElementById('app-brand-btn');
    const btnCm = document.getElementById('cm-open-about');
    const btnClose = document.getElementById('btn-close-about-modal');
    const btnCloseFooter = document.getElementById('btn-close-about-footer');

    const openAbout = () => {
      if (modal) {
        modal.classList.add('show');
        modal.style.display = 'flex';
      }
    };

    const closeAbout = () => {
      if (modal) {
        modal.classList.remove('show');
        setTimeout(() => {
          if (!modal.classList.contains('show')) modal.style.display = 'none';
        }, 150);
      }
    };

    if (btnTb) btnTb.addEventListener('click', openAbout);
    if (btnBrand) btnBrand.addEventListener('click', openAbout);
    if (btnCm) btnCm.addEventListener('click', openAbout);
    if (btnClose) btnClose.addEventListener('click', closeAbout);
    if (btnCloseFooter) btnCloseFooter.addEventListener('click', closeAbout);

    if (modal) {
      modal.addEventListener('click', (e) => {
        if (e.target === modal) closeAbout();
      });
    }

    // Keyboard shortcut F1 -> open about
    window.addEventListener('keydown', (e) => {
      if (e.key === 'F1') {
        e.preventDefault();
        openAbout();
      }
    });

    // Copy Email handler (supports all .btn-copy-email-action elements)
    document.querySelectorAll('.btn-copy-email-action').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        const email = 'gnmigi@gmail.com';
        try {
          if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(email);
          } else {
            const ta = document.createElement('textarea');
            ta.value = email;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
          }
          Toast.show(`Email copied: ${email}`, 'success');
        } catch (err) {
          Toast.show(`Email: ${email}`, 'info');
        }
      });
    });

    // Visit X button (@migi_gn)
    document.querySelectorAll('.btn-open-x').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        NativeBridge.openExternalUrl('https://x.com/migi_gn');
      });
    });

    // File association registration button
    document.querySelectorAll('.btn-register-assoc-action').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        NativeBridge.registerFileAssociation();
        Toast.show('Refreshing .dropboard file association & icon...', 'info');
      });
    });
  }

  updateDockLayout() {
    const dock = document.getElementById('floating-dock');
    const posLabel = document.getElementById('dock-pos-label');
    if (!dock) return;
    dock.classList.remove('dock-top', 'dock-left', 'dock-bottom', 'dock-style-auto', 'dock-style-icon', 'dock-style-full');
    dock.classList.add(`dock-${this.dockPosition}`);
    dock.classList.add(`dock-style-${this.dockStyle}`);
    document.body.classList.remove('dock-pos-top', 'dock-pos-left', 'dock-pos-bottom');
    document.body.classList.add(`dock-pos-${this.dockPosition}`);
    if (posLabel) {
      posLabel.textContent = this.dockPosition === 'left' ? 'Top Bar' : 'Sidebar';
    }
    this.setAutoHideDock(this.autoHideDock, false);
  }

  setAutoHideDock(enabled, showToast = true) {
    this.autoHideDock = !!enabled;
    try { localStorage.setItem('dropboard_autohide_dock', this.autoHideDock ? 'true' : 'false'); } catch(e) {}
    document.body.classList.toggle('has-autohide-dock', this.autoHideDock);
    document.body.classList.remove('dock-pos-top', 'dock-pos-left', 'dock-pos-bottom');
    document.body.classList.add(`dock-pos-${this.dockPosition}`);

    const btnPin = document.getElementById('btn-dock-pin');
    const pinLabel = document.getElementById('dock-pin-label');
    if (btnPin) {
      btnPin.classList.toggle('is-pinned', !this.autoHideDock);
      btnPin.classList.toggle('is-autohide', this.autoHideDock);
      btnPin.title = this.autoHideDock 
        ? 'Toolbar: Auto-Hide on Hover (Click to Pin always visible)' 
        : 'Toolbar: Always Pinned (Click to enable Auto-Hide)';
    }
    if (pinLabel) {
      pinLabel.textContent = this.autoHideDock ? 'Float' : 'Pin';
    }

    const toggle = document.getElementById('settings-toggle-autohide-dock');
    if (toggle) toggle.checked = this.autoHideDock;

    if (showToast) {
      const hint = this.dockPosition === 'left' ? 'Hover left edge to reveal' : (this.dockPosition === 'bottom' ? 'Hover bottom edge to reveal' : 'Hover top edge to reveal');
      Toast.show(this.autoHideDock ? `Toolbar set to Auto-Hide (${hint})` : 'Toolbar pinned always visible', 'info', 1800);
    }
  }

  setDockPosition(pos) {
    this.dockPosition = pos;
    try { localStorage.setItem('dropboard_dock_pos', pos); } catch(e) {}
    this.updateDockLayout();
    const labels = { top: 'Top Bar (Horizontal)', left: 'Left Sidebar (Vertical)', bottom: 'Bottom Bar' };
    Toast.show(`Toolbar position: ${labels[pos] || pos}`, 'info', 1800);
  }

  setDockStyle(style) {
    this.dockStyle = style;
    try { localStorage.setItem('dropboard_dock_style', style); } catch(e) {}
    this.updateDockLayout();
  }

  // ===========================================================================
  // Automatic Session Persistence & Crash Recovery
  // ===========================================================================
  scheduleAutoSave() {
    if (!this.autoSaveEnabled) return;
    clearTimeout(this.autoSaveTimer);
    this.autoSaveTimer = setTimeout(() => {
      this.saveAutoSave();
    }, 1200);
  }

  saveAutoSave() {
    if (!this.autoSaveEnabled) return;
    try {
      if (this.cards.length === 0 && this.groups.length === 0 && this.nodes.length === 0) {
        localStorage.removeItem('dropboard_autosave_state');
        return;
      }
      this.cards.forEach(c => {
        if (!c.imageData && !c.isYouTube) {
          this.getCardImageData(c);
        }
      });
      const data = this.serialize(true);
      if (this.currentFilePath) {
        data.currentFilePath = this.currentFilePath;
        try { localStorage.setItem('dropboard_last_file_path', this.currentFilePath); } catch(e) {}
        try {
          const fullData = JSON.stringify(data);
          NativeBridge.saveBoardDirect(fullData, this.currentFilePath);
        } catch(e) {}
      } else {
        try {
          const fullData = JSON.stringify(data);
          NativeBridge.saveBoardDirect(fullData, '');
        } catch(e) {}
      }
      try {
        const lightData = this.serialize(false);
        if (this.currentFilePath) lightData.currentFilePath = this.currentFilePath;
        localStorage.setItem('dropboard_autosave_state', JSON.stringify(lightData));
      } catch(e) {}

      const counter = document.getElementById('ref-counter');
      if (counter && !counter.textContent.includes('• Saved')) {
        const orig = counter.textContent;
        counter.textContent = `${orig} • Saved`;
        setTimeout(() => {
          if (counter) counter.textContent = counter.textContent.replace(' • Saved', '');
        }, 1200);
      }
    } catch (e) {
      console.warn('Auto-save error:', e);
    }
  }

  loadAutoSave() {
    try {
      const lastPath = localStorage.getItem('dropboard_last_file_path');
      if (lastPath) {
        NativeBridge.loadBoardDirect(lastPath);
        return;
      }
      NativeBridge.loadSessionBoard();
    } catch (e) {
      console.warn('Failed to load auto-save:', e);
    }
  }
}

// Instantiate DropBoard on DOM Ready
window.addEventListener('DOMContentLoaded', () => {
  window.dropBoard = new DropBoardManager();
});
