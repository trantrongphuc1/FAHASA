(function(){
  if(window.App){ return; }
  const App = {};
  let notifConnection = null;
  let chatConnection = null;
  let countdowns = [];
  let countdownTimerId = null;
  let pollNotifIntervalId = null;

  function loadSignalR(cb){
    if(window.signalR){ cb(); return; }
    const s = document.createElement('script');
    s.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js';
    s.onload = cb; document.head.appendChild(s);
  }

  // Notification Hub
  App.initNotificationHub = function(options){
    const opts = options || {};
    const isLoggedIn = opts.isLoggedIn;
    const badge = document.getElementById('notifBadge');
    const panel = document.getElementById('notificationPanel');
    const listEl = document.getElementById('notificationList');
    if(!isLoggedIn){ return; }
    if(notifConnection){ return; }
    loadSignalR(() => {
      try {
        notifConnection = new signalR.HubConnectionBuilder().withUrl('/notificationhub').withAutomaticReconnect().build();
        notifConnection.on('ReceiveNotification', function(payload){
          App.refreshNotifCount();
          try {
            const msg = payload && payload.title ? payload.title : 'Bạn có thông báo mới';
            const note = document.createElement('div');
            note.style.position='fixed';note.style.bottom='90px';note.style.right='20px';
            note.style.background='#343a40';note.style.color='#fff';note.style.padding='12px 16px';
            note.style.borderRadius='10px';note.style.zIndex='99999';note.style.boxShadow='0 4px 12px rgba(0,0,0,0.3)';
            note.textContent = msg; document.body.appendChild(note);
            setTimeout(()=> note.remove(), 3000);
          }catch(_){ }
        });
        notifConnection.start().then(() => App.refreshNotifCount()).catch(()=>{});
        if(opts.enablePolling){
          pollNotifIntervalId = setInterval(App.refreshNotifCount, 60000);
        }
        // attach global handlers once
        if(!window.toggleNotificationPanel){
          window.toggleNotificationPanel = function(e){
            e.preventDefault();
            const isVisible = panel && panel.style.display === 'block';
            if(panel) panel.style.display = isVisible ? 'none' : 'block';
            if(!isVisible){ App.loadNotifications(); }
          };
          document.addEventListener('click', function(ev){
            if(!panel) return;
            const container = document.querySelector('.notification-container');
            if(container && !container.contains(ev.target)) panel.style.display='none';
          });
          window.markNotificationRead = async function(notifId, link){
            try{ await fetch('/Notification/MarkAsRead?id='+notifId,{method:'POST'}); await App.refreshNotifCount(); if(link) window.location.href=link; else if(panel) panel.style.display='none'; }catch(e){}
          };
          window.markAllNotificationsRead = async function(e){
            e.stopPropagation();
            try{ await fetch('/Notification/MarkAllAsRead',{method:'POST'}); await App.refreshNotifCount(); await App.loadNotifications(); }catch(e){}
          };
        }
      } catch(e){ /* ignore */ }
    });
  };

  App.loadNotifications = async function(){
    const listEl = document.getElementById('notificationList');
    if(!listEl){ return; }
    try{
      const res = await fetch('/Notification/GetNotifications');
      const data = await res.json();
      if(!data || !data.length){
        listEl.innerHTML = '<div style="padding:40px 20px;text-align:center;color:#999;"><i class="fas fa-bell-slash" style="font-size:3rem;margin-bottom:10px;opacity:0.3;"></i><p>Không có thông báo mới</p></div>';
        return;
      }
      listEl.innerHTML = data.map(n => {
        const bgColor = n.isRead ? '#f8f9fa' : '#fff';
        const fontWeight = n.isRead ? 'normal' : '700';
        const time = new Date(n.createdAt).toLocaleString('vi-VN');
        return `<div style="padding:15px 20px;border-bottom:1px solid #e0e0e0;background:${bgColor};cursor:pointer;" onclick="markNotificationRead(${n.notificationId}, '${n.link || ''}')">\n          <div style="font-weight:${fontWeight};margin-bottom:5px;color:#333;">${n.title || 'Thông báo'}</div>\n          <div style="font-size:0.9rem;color:#666;margin-bottom:5px;">${n.message || ''}</div>\n          <div style="font-size:0.8rem;color:#999;">${time}</div>\n        </div>`;
      }).join('');
    }catch(e){ listEl.innerHTML = '<div style="padding:20px;text-align:center;color:#dc3545;">Lỗi tải thông báo</div>'; }
  };

  App.refreshNotifCount = async function(){
    const badge = document.getElementById('notifBadge');
    try{
      const res = await fetch('/Notification/GetUnreadCount');
      const json = await res.json();
      const count = json && typeof json.count === 'number' ? json.count : 0;
      if(badge){ if(count>0){ badge.style.display='flex'; badge.textContent=count; } else { badge.style.display='none'; } }
    }catch(e){ }
  };

  // Chat Hub
  App.initChatHub = function(options){
    const opts = options || {};
    const userId = opts.userId;
    const userName = opts.userName;
    const isAdmin = !!opts.isAdmin;
    if(!userId){ return; }
    loadSignalR(() => {
      if(chatConnection){ return; }
      chatConnection = new signalR.HubConnectionBuilder().withUrl('/chathub').withAutomaticReconnect().build();
      chatConnection.on('ReceiveMessage', function(fromUserId, user, message, isAdminMsg, time){
        // For users, only show messages addressed to them
        if(!isAdmin && fromUserId === userId){
          App.renderChatMessage({ userName: user, message, isFromAdmin: isAdminMsg, sentAt: time, currentUserId: userId });
          // If message is from admin, refresh badge
          if(isAdminMsg){
            App.refreshChatBadge();
          }
        }
      });
      chatConnection.start().catch(err => console.error('Chat start error', err));
      App.loadChatMessages(opts);
      App._chatConfig = opts;
      // Initial badge refresh
      App.refreshChatBadge();
    });
  };

  App.sendChatMessage = function(message){
    const cfg = App._chatConfig || {}; if(!cfg.userId){ return; }
    const userId = cfg.userId, userName = cfg.userName, isAdmin = !!cfg.isAdmin;
    if(!message){ return; }

    // User side receives an echo from SignalR, so avoid optimistic render to prevent duplicate lines.
    if(isAdmin){
      const now = new Date();
      App.renderChatMessage({
        userName: userName,
        message: message,
        isFromAdmin: isAdmin,
        sentAt: now.toISOString(),
        currentUserId: userId
      });
    }
    
    if(!chatConnection || chatConnection.state !== signalR.HubConnectionState.Connected){
      // fallback
      fetch('/Chat/SendMessage',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({userId,userName,message,isAdmin})}).then(()=> App.loadChatMessages(cfg));
      return;
    }
    chatConnection.invoke('SendMessage', userId, userName, message, isAdmin).catch(e=>console.error('Send error', e));
  };

  App.loadChatMessages = function(options){
    const opts = options || App._chatConfig || {}; if(!opts.userId){ return; }
    fetch('/Chat/GetMessages').then(r=>r.json()).then(messages => {
      const container = document.getElementById('chatMessages'); if(!container){ return; }
      container.innerHTML='';
      messages.forEach(m => App.renderChatMessage(m));
    }).catch(()=>{});
  };

  App.renderChatMessage = function(msg){
    const container = document.getElementById('chatMessages'); if(!container){ return; }
    const isAdminMsg = msg.isFromAdmin;
    const div = document.createElement('div');
    div.className = 'message ' + (isAdminMsg ? 'admin' : 'user');
    let timeStr = '';
    if(msg.sentAt){
      if(typeof msg.sentAt === 'string' && /^\d{2}:\d{2}$/.test(msg.sentAt)){
        timeStr = msg.sentAt;
      } else {
        const date = new Date(msg.sentAt);
        timeStr = isNaN(date.getTime())
          ? String(msg.sentAt)
          : date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
      }
    }
    div.innerHTML = `<div class="message-bubble">${msg.message}</div><div class="message-time">${msg.userName} - ${timeStr}</div>`;
    container.appendChild(div); container.scrollTop = container.scrollHeight;
  };

  App.refreshChatBadge = async function(){
    const badge = document.getElementById('chatBadge');
    try{
      const res = await fetch('/Chat/GetUnreadCount');
      const json = await res.json();
      const count = json && typeof json.count === 'number' ? json.count : 0;
      if(badge){ if(count>0){ badge.style.display='flex'; badge.textContent=count; } else { badge.style.display='none'; } }
    }catch(e){ }
  };

  // Countdown Manager
  let visibilityListenerAdded = false;
  App.registerCountdown = function(config){
    // config: { endTime: Date, hoursEl, minutesEl, secondsEl, onExpire }
    countdowns.push(config);
    startCountdownLoop();
  };

  function startCountdownLoop(){
    if(countdownTimerId){ return; }
    countdownTimerId = setInterval(tickCountdowns, 1000);
    tickCountdowns();
    if(!visibilityListenerAdded){
      visibilityListenerAdded = true;
      document.addEventListener('visibilitychange', function(){
        if(document.hidden){ if(countdownTimerId){ clearInterval(countdownTimerId); countdownTimerId=null; } }
        else { startCountdownLoop(); }
      });
    }
  }

  function tickCountdowns(){
    const now = Date.now();
    countdowns = countdowns.filter(cd => {
      const diff = cd.endTime.getTime() - now;
      if(diff <= 0){
        if(cd.onExpire){ cd.onExpire(); }
        return false;
      }
      const hours = Math.floor(diff / (1000*60*60));
      const minutes = Math.floor((diff % (1000*60*60)) / (1000*60));
      const seconds = Math.floor((diff % (1000*60)) / 1000);
      if(cd.hoursEl) cd.hoursEl.textContent = String(hours).padStart(2,'0');
      if(cd.minutesEl) cd.minutesEl.textContent = String(minutes).padStart(2,'0');
      if(cd.secondsEl) cd.secondsEl.textContent = String(seconds).padStart(2,'0');
      return true;
    });
    if(countdowns.length === 0 && countdownTimerId){ clearInterval(countdownTimerId); countdownTimerId=null; }
  }

  // Debounce helper
  App.debounce = function(fn, ms){
    let t; return function(){ const args = arguments; clearTimeout(t); t = setTimeout(()=> fn.apply(this,args), ms); };
  };

  window.App = App;
})();
