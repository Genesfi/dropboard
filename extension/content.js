// DropBoard Content Script for Pinterest & Web Reference Capture

let lastRightClickedImageUrl = null;

function upgradePinterestUrl(url) {
  if (!url) return "";
  if (url.includes("pinimg.com/")) {
    return url.replace(/\/(236x|474x|564x)\//, "/736x/");
  }
  return url;
}

function findImageInElementOrAncestors(target) {
  if (!target) return null;

  // Direct image
  if (target.tagName === 'IMG' && target.src) {
    return upgradePinterestUrl(target.src);
  }

  // Look for image inside container (e.g. Pinterest pin wrapper)
  const pinContainer = target.closest('[data-test-id="pin"], [data-test-id="pinWrapper"], div[role="listitem"], .pin');
  if (pinContainer) {
    const img = pinContainer.querySelector('img');
    if (img) {
      const src = img.src || img.dataset.src || (img.srcset ? img.srcset.split(',')[0].trim().split(' ')[0] : null);
      if (src) return upgradePinterestUrl(src);
    }
  }

  // General search inside target
  if (target.querySelector) {
    const img = target.querySelector('img');
    if (img && img.src) return upgradePinterestUrl(img.src);
  }

  return null;
}

// Track right-click position to capture the underlying image
document.addEventListener('contextmenu', (e) => {
  const foundUrl = findImageInElementOrAncestors(e.target);
  if (foundUrl) {
    lastRightClickedImageUrl = foundUrl;
  }
}, true);

// Listen for query from background script
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.type === "GET_LAST_CLICKED_IMAGE") {
    sendResponse({ originalUrl: lastRightClickedImageUrl });
  }
});

// Add floating hover button on Pinterest pins
function injectPinterestButtons() {
  if (!window.location.hostname.includes('pinterest.')) return;

  const pins = document.querySelectorAll('[data-test-id="pin"]:not([data-dropboard-injected])');
  pins.forEach((pin) => {
    pin.setAttribute('data-dropboard-injected', 'true');

    const btn = document.createElement('button');
    btn.className = 'dropboard-pin-btn';
    btn.title = 'Add this image to DropBoard';
    btn.innerHTML = `
      <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.2">
        <rect x="3" y="3" width="18" height="18" rx="4"/>
        <path d="M12 8v8m-4-4h8"/>
      </svg>
      <span>Board</span>
    `;

    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      e.preventDefault();

      const img = pin.querySelector('img');
      if (img) {
        const src = img.src || img.dataset.src || (img.srcset ? img.srcset.split(',')[0].trim().split(' ')[0] : null);
        const originalUrl = upgradePinterestUrl(src);

        btn.innerHTML = `<span>Saving...</span>`;
        chrome.runtime.sendMessage({
          type: "SEND_DIRECT_IMAGE",
          url: originalUrl,
          title: document.title
        }, (response) => {
          if (response && response.success) {
            btn.innerHTML = `<span>✓ Added</span>`;
            btn.classList.add('success');
            setTimeout(() => {
              btn.innerHTML = `
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.2">
                  <rect x="3" y="3" width="18" height="18" rx="4"/>
                  <path d="M12 8v8m-4-4h8"/>
                </svg>
                <span>Board</span>
              `;
              btn.classList.remove('success');
            }, 2000);
          } else {
            btn.innerHTML = `<span>Offline</span>`;
            setTimeout(() => {
              btn.innerHTML = `<span>Board</span>`;
            }, 2000);
          }
        });
      }
    });

    pin.style.position = 'relative';
    pin.appendChild(btn);
  });
}

// Observe Pinterest feed scroll for lazy loaded pins
const observer = new MutationObserver(() => {
  injectPinterestButtons();
});

observer.observe(document.body, { childList: true, subtree: true });
injectPinterestButtons();
