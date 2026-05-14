import { createSignal, onMount, onCleanup, For } from 'solid-js';
import { apiClient, type ChatDto, type MessageDto } from '../../lib/api-client';
import { HubConnectionBuilder } from '@microsoft/signalr';

export default function Chat() {
  const [chats, setChats] = createSignal<ChatDto[]>([]);
  const [selectedChat, setSelectedChat] = createSignal<ChatDto | null>(null);
  const [messages, setMessages] = createSignal<MessageDto[]>([]);
  const [newMessage, setNewMessage] = createSignal('');
  const [connection, setConnection] = createSignal<any>(null);
  const [isInCall, setIsInCall] = createSignal(false);
  const [localStream, setLocalStream] = createSignal<MediaStream | null>(null);
  const [remoteStream, setRemoteStream] = createSignal<MediaStream | null>(null);
  const [peerConnection, setPeerConnection] = createSignal<RTCPeerConnection | null>(null);

  onMount(async () => {
    try {
      const userChats = await apiClient.getUserChats();
      setChats(userChats);

      // Initialize SignalR
      const conn = new HubConnectionBuilder()
        .withUrl('/chatHub', { accessTokenFactory: () => localStorage.getItem('accessToken') || '' })
        .build();

      conn.on('ReceiveMessage', (message: MessageDto) => {
        setMessages(prev => [...prev, message]);
      });

      await conn.start();
      setConnection(conn);
    } catch (error) {
      console.error('Failed to load chats or connect to hub:', error);
    }
  });

  onCleanup(() => {
    connection()?.stop();
  });

  const selectChat = async (chat: ChatDto) => {
    setSelectedChat(chat);
    try {
      const chatMessages = await apiClient.getChatMessages(chat.id);
      setMessages(chatMessages);
      await connection()?.invoke('JoinChat', chat.id);
    } catch (error) {
      console.error('Failed to load messages:', error);
    }
  };

  const sendMessage = async () => {
    if (!newMessage().trim() || !selectedChat()) return;

    try {
      await connection()?.invoke('SendMessage', selectedChat()!.id, newMessage(), 'Text');
      setNewMessage('');
    } catch (error) {
      console.error('Failed to send message:', error);
    }
  };

  const initiateCall = async (callType: 'Audio' | 'Video') => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        video: callType === 'Video',
      });
      setLocalStream(stream);

      const pc = new RTCPeerConnection();
      setPeerConnection(pc);

      stream.getTracks().forEach(track => pc.addTrack(track, stream));

      pc.ontrack = (event) => {
        setRemoteStream(event.streams[0]);
      };

      pc.onicecandidate = (event) => {
        if (event.candidate) {
          connection()?.invoke('SendIceCandidate', selectedChat()!.id, JSON.stringify(event.candidate));
        }
      };

      await connection()?.invoke('InitiateCall', selectedChat()!.id, callType);
      setIsInCall(true);
    } catch (error) {
      console.error('Failed to initiate call:', error);
    }
  };

  const joinCall = async (callId: string) => {
    // Similar to initiate, but for joining
  };

  const endCall = () => {
    localStream()?.getTracks().forEach(track => track.stop());
    peerConnection()?.close();
    setIsInCall(false);
    connection()?.invoke('EndCall', selectedChat()!.id);
  };

  const sendFile = async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await fetch('/api/v1/chat/files/upload', {
        method: 'POST',
        body: formData,
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
      });
      const data = await response.json();

      await connection()?.invoke('SendMessage', selectedChat()!.id, '', 'File', [data]);
    } catch (error) {
      console.error('Failed to send file:', error);
    }
  };

  const sendCode = async (code: string, language: string) => {
    const formattedCode = `\`\`\`${language}\n${code}\n\`\`\``;
    await connection()?.invoke('SendMessage', selectedChat()!.id, formattedCode, 'Code');
  };

  return (
    <div class="chat-page">
      <div class="chat-sidebar">
        <h3>Chats</h3>
        <ul>
          <For each={chats()}>
            {(chat) => (
              <li onClick={() => selectChat(chat)} class={selectedChat()?.id === chat.id ? 'active' : ''}>
                {chat.name}
              </li>
            )}
          </For>
        </ul>
      </div>
      <div class="chat-main">
        {selectedChat() ? (
          <>
            <div class="chat-header">
              <h2>{selectedChat()!.name}</h2>
            </div>
            <div class="chat-messages">
              <For each={messages()}>
                {(message) => (
                  <div class="message">
                    <strong>{message.senderId}</strong>: {message.content}
                    <small>{new Date(message.timestamp).toLocaleString()}</small>
                  </div>
                )}
              </For>
            </div>
            <div class="chat-input">
              <input
                type="text"
                value={newMessage()}
                onInput={(e) => setNewMessage(e.currentTarget.value)}
                onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
                placeholder="Type a message..."
              />
              <button onClick={() => initiateCall('Audio')}>Audio Call</button>
              <button onClick={() => initiateCall('Video')}>Video Call</button>
              <input type="file" onChange={(e) => e.target.files && sendFile(e.target.files[0])} />
              <button onClick={() => sendCode('console.log("Hello");', 'javascript')}>Send Code</button>
              <button onClick={sendMessage}>Send</button>
            </div>
          </>
        ) : (
          <p>Select a chat to start messaging</p>
        )}
        {isInCall() && (
          <div class="call-overlay">
            <video ref={(el) => el.srcObject = localStream()} autoplay muted />
            <video ref={(el) => el.srcObject = remoteStream()} autoplay />
            <button onClick={endCall}>End Call</button>
          </div>
        )}
      </div>
    </div>
  );
}