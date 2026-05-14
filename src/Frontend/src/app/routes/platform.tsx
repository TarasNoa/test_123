import { createSignal, onMount, onCleanup, For } from 'solid-js';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { ServerSidebar } from '../../widgets/ServerSidebar';
import { ChannelList } from '../../widgets/ChannelList';
import { ChatArea } from '../../widgets/ChatArea';
import { VoicePanel } from '../../widgets/VoicePanel';
import { CallScheduler } from '../../widgets/CallScheduler';
import { TaskBoard } from '../../widgets/TaskBoard';
import { CodeSnippetShare } from '../../widgets/CodeSnippetShare';
import { UserPresence } from '../../widgets/UserPresence';

export default function Platform() {
  const [servers, setServers] = createSignal([]);
  const [selectedServer, setSelectedServer] = createSignal(null);
  const [channels, setChannels] = createSignal([]);
  const [selectedChannel, setSelectedChannel] = createSignal(null);
  const [messages, setMessages] = createSignal([]);
  const [tasks, setTasks] = createSignal([]);
  const [connection, setConnection] = createSignal(null);
  const [isInCall, setIsInCall] = createSignal(false);
  const [callParticipants, setCallParticipants] = createSignal([]);
  const [userPresences, setUserPresences] = createSignal({});
  const [pinnedMessages, setPinnedMessages] = createSignal([]);

  onMount(async () => {
    // Initialize SignalR
    const conn = new HubConnectionBuilder()
      .withUrl('/chatHub', { accessTokenFactory: () => localStorage.getItem('accessToken') || '' })
      .withAutomaticReconnect()
      .build();

    conn.on('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });

    conn.on('MessageEdited', (messageId, newContent) => {
      setMessages(prev => prev.map(m => m.id === messageId ? { ...m, content: newContent } : m));
    });

    conn.on('MessageDeleted', (messageId) => {
      setMessages(prev => prev.filter(m => m.id !== messageId));
    });

    conn.on('MessagePinned', (messageId) => {
      setPinnedMessages(prev => [...prev, messageId]);
    });

    conn.on('MessageReaction', (messageId, emoji, userId) => {
      setMessages(prev => prev.map(m => 
        m.id === messageId ? { ...m, reactions: [...m.reactions, { emoji, userId }] } : m
      ));
    });

    conn.on('ThreadCreated', (messageId, title) => {
      console.log(`Thread created: ${title}`);
    });

    conn.on('CallInitiated', (call) => {
      setIsInCall(true);
      setCallParticipants([call.initiatorId]);
    });

    conn.on('ParticipantJoined', (userId) => {
      setCallParticipants(prev => [...new Set([...prev, userId])]);
    });

    conn.on('VoiceStateUpdated', (userId, isMuted, isDeafened) => {
      console.log(`${userId} muted: ${isMuted}, deafened: ${isDeafened}`);
    });

    conn.on('UserTyping', (userId) => {
      console.log(`${userId} is typing...`);
    });

    conn.on('UserPresenceUpdated', (userId, presence) => {
      setUserPresences(prev => ({ ...prev, [userId]: presence }));
    });

    conn.on('MessagePinned', (messageId) => {
      setPinnedMessages(prev => [...prev, messageId]);
    });

    await conn.start();
    setConnection(conn);

    // Load servers
    const userServers = await fetch('/api/v1/chat/servers', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    }).then(r => r.json());
    setServers(userServers.servers || []);
  });

  onCleanup(() => {
    connection()?.stop();
  });

  const selectServer = async (server) => {
    setSelectedServer(server);
    setChannels(server.channels);
    await connection()?.invoke('JoinServer', server.id);
  };

  const selectChannel = async (channel) => {
    setSelectedChannel(channel);
    setMessages([]);
    await connection()?.invoke('JoinChannel', channel.id, channel.type);
  };

  const sendMessage = async (content, type = 'Text', attachments = null) => {
    if (selectedChannel()) {
      await connection()?.invoke('SendMessage', selectedChannel().id, content, type, attachments);
    }
  };

  const reactToMessage = async (messageId, emoji) => {
    if (selectedChannel()) {
      await connection()?.invoke('ReactToMessage', messageId, emoji, selectedChannel().id);
    }
  };

  const pinMessage = async (messageId) => {
    if (selectedChannel()) {
      await connection()?.invoke('PinMessage', messageId, selectedChannel().id);
    }
  };

  const initiateCall = async (type) => {
    if (selectedChannel()) {
      const roomId = `room_${Date.now()}`;
      await connection()?.invoke('InitiateCall', selectedChannel().id, type, roomId);
    }
  };

  const sendCode = async (language, code, title) => {
    const response = await fetch('/api/v1/chat/code/snippets', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
      },
      body: JSON.stringify({
        channelId: selectedChannel().id,
        language,
        code,
        title
      })
    });
    const snippet = await response.json();
    await sendMessage(`[Code: ${title}](http://localhost/code/${snippet.snippet.id})`);
  };

  const setPresence = async (status, activity) => {
    await connection()?.invoke('SetPresence', { status, activity });
  };

  return (
    <div class="platform-page">
      <ServerSidebar servers={servers()} onSelectServer={selectServer} />
      
      {selectedServer() && (
        <>
          <ChannelList 
            channels={channels()} 
            onSelectChannel={selectChannel}
            pinnedMessages={pinnedMessages()}
          />
          
          <div class="main-content">
            <UserPresence presences={userPresences()} />
            
            {selectedChannel() && (
              <>
                <ChatArea 
                  channel={selectedChannel()} 
                  messages={messages()}
                  onSendMessage={sendMessage}
                  onReactToMessage={reactToMessage}
                  onPinMessage={pinMessage}
                />
                
                {isInCall() && (
                  <VoicePanel 
                    participants={callParticipants()}
                    onEndCall={() => setIsInCall(false)}
                  />
                )}

                <CodeSnippetShare 
                  onSendCode={sendCode}
                />
              </>
            )}
            
            <CallScheduler server={selectedServer()} />
            <TaskBoard tasks={tasks()} />
          </div>
        </>
      )}
    </div>
  );
}