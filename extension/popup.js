const statusDot = document.getElementById('status-dot');
const statusText = document.getElementById('status-text');
const btnCapture = document.getElementById('btn-capture-page');

async function checkConnection() {
  try {
    const res = await fetch('http://127.0.0.1:28888/ping');
    const data = await res.json();
    if (data.status === 'ready') {
      statusDot.classList.add('connected');
      statusText.textContent = 'Connected to DropBoard Desktop';
      return true;
    }
  } catch (e) {
    statusDot.classList.remove('connected');
    statusText.textContent = 'DropBoard Desktop is Closed';
  }
  return false;
}

checkConnection();

btnCapture.addEventListener('click', async () => {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (tab && tab.url) {
    btnCapture.textContent = 'Sending...';
    try {
      const res = await fetch('http://127.0.0.1:28888/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: tab.url, title: tab.title || '' })
      });
      const data = await res.json();
      if (data.success) {
        btnCapture.textContent = '✓ Sent to Board!';
        setTimeout(() => { btnCapture.textContent = 'Send Current Tab to Board'; }, 2000);
      } else {
        btnCapture.textContent = 'Failed';
      }
    } catch (e) {
      btnCapture.textContent = 'Desktop App Offline';
    }
  }
});
