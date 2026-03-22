window.adminChat = {
    connection: null,
    keyListeners: {},
    unreadCount: 0,
    init: function (dotNetRef) {
        // Reset if navigating back
        if (this.connection && this.connection.state === signalR.HubConnectionState.Disconnected) {
            console.log('AdminChat: Connection disconnected, resetting');
            this.connection = null;
        }
        
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            console.log('AdminChat: Connection already active');
            return;
        }
        
        console.log('AdminChat: Initializing new connection');
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chathub')
            .withAutomaticReconnect([0, 0, 1000, 3000, 5000, 10000])
            .build();
        
        this.connection.on('ReceiveMessage', function (fromUserId, user, message, isAdminMsg, time) {
            console.log('ReceiveMessage event:', fromUserId, user, message, isAdminMsg);
            dotNetRef.invokeMethodAsync('ReceiveFromHub', fromUserId, user, message, isAdminMsg, time);
        });
        
        this.connection.on('UserMessageReceived', function (fromUserId, user, message, time) {
            console.log('UserMessageReceived event:', fromUserId, user, message);
            window.adminChat.unreadCount++;
            
            // Update badge
            const badge = document.getElementById('chatUnreadBadge');
            if (badge) {
                badge.style.display = 'inline';
                badge.textContent = window.adminChat.unreadCount;
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
            .then(() => console.log('AdminChat: Connected'))
            .catch(function (err) { console.error('AdminChat error:', err.toString()); });
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

window.removeChatMenuHighlight = function() {
    const badge = document.getElementById('chatUnreadBadge');
    if (badge) {
        badge.style.display = 'none';
        window.adminChat.unreadCount = 0;
    }
    
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
        setTimeout(window.removeChatMenuHighlight, 100);
    });
} else {
    setTimeout(window.removeChatMenuHighlight, 100);
}
