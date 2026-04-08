window.adminChat = {
    connection: null,
    dotNetRef: null,
    keyListeners: {},
    unreadCount: 0,
    badgePollId: null,
    ensureSignalR: function () {
        if (window.signalR) {
            return Promise.resolve();
        }

        return new Promise((resolve, reject) => {
            const existing = document.querySelector('script[data-admin-signalr="1"]');
            if (existing) {
                existing.addEventListener('load', () => resolve(), { once: true });
                existing.addEventListener('error', () => reject(new Error('Cannot load SignalR script')), { once: true });
                return;
            }

            const s = document.createElement('script');
            s.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js';
            s.async = true;
            s.dataset.adminSignalr = '1';
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Cannot load SignalR script'));
            document.head.appendChild(s);
        });
    },
    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;

        this.ensureSignalR().then(() => {
        // Always reset old connection when entering chat page to avoid stale callbacks after navigation.
        if (this.connection) {
            try {
                this.connection.stop();
            } catch (_) { }
            this.connection = null;
        }
        
        console.log('AdminChat: Initializing new connection');
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chathub')
            .withAutomaticReconnect([0, 0, 1000, 3000, 5000, 10000])
            .build();
        
        this.connection.on('ReceiveMessage', function (fromUserId, user, message, isAdminMsg, time) {
            console.log('ReceiveMessage event:', fromUserId, user, message, isAdminMsg);
            if (!window.adminChat.dotNetRef) return;
            window.adminChat.dotNetRef.invokeMethodAsync('ReceiveFromHub', fromUserId, user, message, isAdminMsg, time)
                .catch(() => { });
        });
        
        this.connection.on('UserMessageReceived', function (fromUserId, user, message, time) {
            console.log('UserMessageReceived event:', fromUserId, user, message);
            window.adminChat.unreadCount++;
            
            // Update badge
            const badge = document.getElementById('chatUnreadBadge');
            if (badge) {
                badge.style.display = 'inline';
            }
            
            // Highlight chat menu item with red dot
            const chatMenuItem = document.querySelector('a[href="/admin/chat"]');
            if (chatMenuItem) {
                chatMenuItem.classList.add('has-new-message');
                // Add red dot if not exists
                let dot = chatMenuItem.querySelector('.new-message-dot');
                if (!dot) {
                    dot = document.createElement('span');
                    dot.className = 'new-message-dot';
                    dot.style.cssText = 'position:absolute; right:0px; top:0px; width:8px; height:8px; background:red; border-radius:50%;';
                    chatMenuItem.style.position = 'relative';
                    chatMenuItem.appendChild(dot);
                }
            }
        });
        
        this.connection.onreconnecting((error) => {
            console.log('Reconnecting...', error);
        });
        
        this.connection.onreconnected((connectionId) => {
            console.log('Reconnected with ID:', connectionId);
        });
        
        this.connection.start()
            .then(() => {
                console.log('AdminChat: Connected');
                window.adminChat.refreshUnreadBadge();
                if (window.adminChat.badgePollId) {
                    clearInterval(window.adminChat.badgePollId);
                }
                window.adminChat.badgePollId = setInterval(() => {
                    window.adminChat.refreshUnreadBadge();
                }, 10000);
            })
            .catch(function (err) { console.error('AdminChat error:', err.toString()); });
        }).catch(err => {
            console.error('AdminChat init failed:', err);
        });
    },
    refreshUnreadBadge: async function () {
        try {
            const res = await fetch('/Chat/GetAdminUnreadCount');
            if (!res.ok) return;
            const data = await res.json();
            const count = data && typeof data.count === 'number' ? data.count : 0;

            const badge = document.getElementById('chatUnreadBadge');
            if (badge) {
                if (count > 0) {
                    badge.style.display = 'inline';
                } else {
                    badge.style.display = 'none';
                }
            }

            const chatMenuItem = document.querySelector('a[href="/admin/chat"]');
            if (chatMenuItem) {
                if (count > 0) {
                    chatMenuItem.classList.add('has-new-message');
                } else {
                    chatMenuItem.classList.remove('has-new-message');
                    const dot = chatMenuItem.querySelector('.new-message-dot');
                    if (dot) dot.remove();
                }
            }
        } catch (_) { }
    },
    setupAdminKeySend: function (dotNetRef, textareaId) {
        var el = document.getElementById(textareaId);
        if (!el) {
            console.warn('AdminChat: Element not found:', textareaId);
            return;
        }
        var handler = function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                console.log('Enter pressed, invoking SendReply');
                dotNetRef.invokeMethodAsync('OnAdminEnterPressed');
            }
        };
        el.addEventListener('keydown', handler);
        this.keyListeners[textareaId] = handler;
        console.log('AdminChat: Key handler attached to', textareaId);
    },
    stopAdminKeySend: function (textareaId) {
        var el = document.getElementById(textareaId);
        var h = this.keyListeners[textareaId];
        if (el && h) {
            el.removeEventListener('keydown', h);
        }
        delete this.keyListeners[textareaId];
    },
    stop: function () {
        this.dotNetRef = null;
        if (this.badgePollId) {
            clearInterval(this.badgePollId);
            this.badgePollId = null;
        }
        if (this.connection) {
            console.log('AdminChat: Stopping connection');
            this.connection.stop().catch(err => console.error('Stop error:', err));
            this.connection = null;
        }
    }
};

window.initAdminSignalR = function (dotNetRef) {
    window.adminChat.init(dotNetRef);
};

window.stopAdminSignalR = function () {
    window.adminChat.stop();
};

window.adminChat.scrollMessagesToBottom = function (containerId) {
    const id = containerId || 'admin-messages-list';
    const el = document.getElementById(id);
    if (!el) return;

    const doScroll = () => {
        el.style.scrollBehavior = 'auto';
        el.scrollTop = el.scrollHeight;
        el.scrollTop = el.scrollHeight;
    };

    doScroll();
    requestAnimationFrame(doScroll);
    setTimeout(doScroll, 20);
    setTimeout(doScroll, 80);
    setTimeout(doScroll, 180);
};

window.removeChatMenuHighlight = function() {
    const badge = document.getElementById('chatUnreadBadge');
    if (badge) {
        badge.style.display = 'none';
    }
    window.adminChat.unreadCount = 0;
    
    const chatMenuItem = document.querySelector('a[href="/admin/chat"]');
    if (chatMenuItem) {
        chatMenuItem.classList.remove('has-new-message');
        const dot = chatMenuItem.querySelector('.new-message-dot');
        if (dot) {
            dot.remove();
        }
    }
};

// Auto-clear when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        // Keep unread indicator active on all admin pages, not only /admin/chat.
        if (window.location.pathname.startsWith('/admin')) {
            window.adminChat.init(null);

            const chatMenuItem = document.querySelector('a[href="/admin/chat"]');
            if (chatMenuItem) {
                chatMenuItem.addEventListener('click', function(){
                    window.removeChatMenuHighlight();
                });
            }
        }
        if (window.location.pathname === '/admin/chat') {
            setTimeout(window.removeChatMenuHighlight, 100);
        }
    });
} else {
    if (window.location.pathname.startsWith('/admin')) {
        window.adminChat.init(null);

        const chatMenuItem = document.querySelector('a[href="/admin/chat"]');
        if (chatMenuItem) {
            chatMenuItem.addEventListener('click', function(){
                window.removeChatMenuHighlight();
            });
        }
    }
    if (window.location.pathname === '/admin/chat') {
        setTimeout(window.removeChatMenuHighlight, 100);
    }
}
