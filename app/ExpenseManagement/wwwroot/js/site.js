// site.js – Client-side helpers for Expense Management

// Live filter for tables
function filterTable(inputId, tableId) {
    const input = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    if (!input || !table) return;

    input.addEventListener('input', function () {
        const term = this.value.toLowerCase();
        table.querySelectorAll('tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(term) ? '' : 'none';
        });
    });
}

// Auto-dismiss alert after 4 seconds
document.querySelectorAll('.alert--success').forEach(el => {
    setTimeout(() => { el.style.transition = 'opacity 0.5s'; el.style.opacity = '0'; setTimeout(() => el.remove(), 500); }, 4000);
});

// Format markdown-style text into HTML safely (for chat bubbles)
function formatChatMessage(text) {
    // 1. Escape HTML to prevent XSS
    let escaped = text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');

    // 2. Bold: **text** → <strong>text</strong>
    escaped = escaped.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');

    // 3. Numbered list lines
    const lines = escaped.split('\n');
    let html       = '';
    let inOl       = false;
    let inUl       = false;

    lines.forEach(line => {
        if (/^\d+\.\s/.test(line)) {
            if (!inOl) { if (inUl) { html += '</ul>'; inUl = false; } html += '<ol>'; inOl = true; }
            html += `<li>${line.replace(/^\d+\.\s/, '')}</li>`;
        } else if (/^[-*]\s/.test(line)) {
            if (!inUl) { if (inOl) { html += '</ol>'; inOl = false; } html += '<ul>'; inUl = true; }
            html += `<li>${line.replace(/^[-*]\s/, '')}</li>`;
        } else {
            if (inOl) { html += '</ol>'; inOl = false; }
            if (inUl) { html += '</ul>'; inUl = false; }
            html += line + '<br>';
        }
    });
    if (inOl) html += '</ol>';
    if (inUl) html += '</ul>';

    return html;
}

// Chat functionality
(function () {
    const form     = document.getElementById('chatForm');
    const input    = document.getElementById('chatInput');
    const messages = document.getElementById('chatMessages');
    if (!form) return;

    let history = [];

    function appendMessage(role, text) {
        const bubble = document.createElement('div');
        bubble.className = `chat-bubble chat-bubble--${role}`;

        const avatar  = document.createElement('div');
        avatar.className = 'chat-bubble__avatar';
        avatar.textContent = role === 'user' ? '👤' : '🤖';

        const content = document.createElement('div');
        content.className = 'chat-bubble__content';
        content.innerHTML = formatChatMessage(text);

        bubble.appendChild(avatar);
        bubble.appendChild(content);
        messages.appendChild(bubble);
        messages.scrollTop = messages.scrollHeight;
    }

    function showTyping() {
        const bubble = document.createElement('div');
        bubble.className = 'chat-bubble chat-bubble--assistant';
        bubble.id = 'typingIndicator';
        bubble.innerHTML = '<div class="chat-bubble__avatar">🤖</div><div class="chat-bubble__content"><div class="chat-typing"><span></span><span></span><span></span></div></div>';
        messages.appendChild(bubble);
        messages.scrollTop = messages.scrollHeight;
    }

    function removeTyping() {
        const t = document.getElementById('typingIndicator');
        if (t) t.remove();
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const text = input.value.trim();
        if (!text) return;

        appendMessage('user', text);
        history.push({ role: 'user', content: text });
        input.value = '';
        input.style.height = 'auto';
        showTyping();

        try {
            const resp = await fetch('/api/chat', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ message: text, history: history.slice(-10) })
            });
            const data = await resp.json();
            removeTyping();
            const msg = data.message || 'Sorry, I could not get a response.';
            appendMessage('assistant', msg);
            history.push({ role: 'assistant', content: msg });
        } catch (err) {
            removeTyping();
            appendMessage('assistant', '⚠️ Network error. Please try again.');
        }
    });

    // Send on Enter (Shift+Enter for newline)
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            form.dispatchEvent(new Event('submit'));
        }
    });

    // Auto-resize textarea
    input.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 120) + 'px';
    });
}());
