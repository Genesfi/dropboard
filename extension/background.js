const DROPBOARD_PORT = 28888;
const ADD_ENDPOINT = `http://127.0.0.1:${DROPBOARD_PORT}/add`;

// Create Context Menus
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "send-to-dropboard-img",
    title: "📌 Add Image to DropBoard",
    contexts: ["image"]
  });

  chrome.contextMenus.create({
    id: "send-to-dropboard-link",
    title: "📌 Send Link / Pin to DropBoard",
    contexts: ["link"]
  });

  chrome.contextMenus.create({
    id: "send-to-dropboard-page",
    title: "📌 Send Current Page to DropBoard",
    contexts: ["page"]
  });
});

async function sendToDropBoard(url, title = "") {
  try {
    const response = await fetch(ADD_ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url, title })
    });
    const data = await response.json();
    return data.success;
  } catch (err) {
    console.error("Failed to connect to DropBoard local server:", err);
    return false;
  }
}

// Handle Context Menu clicks
chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  let targetUrl = "";
  let title = tab ? tab.title : "";

  if (info.menuItemId === "send-to-dropboard-img" && info.srcUrl) {
    targetUrl = info.srcUrl;
    // Upgrade Pinterest thumbnail if applicable
    if (targetUrl.includes("pinimg.com/")) {
      targetUrl = targetUrl.replace(/\/(236x|474x|564x)\//, "/736x/");
    }
  } else if (info.menuItemId === "send-to-dropboard-link" && info.linkUrl) {
    targetUrl = info.linkUrl;
  } else if (info.menuItemId === "send-to-dropboard-page" && info.pageUrl) {
    targetUrl = info.pageUrl;
  }

  // Ask content script if it has a better high-res Pinterest image for the clicked element
  if (tab && tab.id) {
    try {
      const response = await chrome.tabs.sendMessage(tab.id, { type: "GET_LAST_CLICKED_IMAGE" });
      if (response && response.originalUrl) {
        targetUrl = response.originalUrl;
      }
    } catch (e) {
      // Content script may not be injected on this page
    }
  }

  if (targetUrl) {
    const ok = await sendToDropBoard(targetUrl, title);
    if (ok) {
      chrome.action.setBadgeText({ text: "✓", tabId: tab ? tab.id : undefined });
      chrome.action.setBadgeBackgroundColor({ color: "#10b981" });
      setTimeout(() => chrome.action.setBadgeText({ text: "" }), 2500);
    } else {
      chrome.action.setBadgeText({ text: "ERR", tabId: tab ? tab.id : undefined });
      chrome.action.setBadgeBackgroundColor({ color: "#ef4444" });
      setTimeout(() => chrome.action.setBadgeText({ text: "" }), 3000);
    }
  }
});

// Listen for messages from content scripts
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.type === "SEND_DIRECT_IMAGE" && request.url) {
    sendToDropBoard(request.url, request.title || "").then(ok => {
      sendResponse({ success: ok });
    });
    return true; // async response
  }
});
