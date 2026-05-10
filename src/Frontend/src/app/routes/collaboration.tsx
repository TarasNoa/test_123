import { createSignal, onMount, For, Show } from 'solid-js';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { CollaborativeEditor } from '../../widgets/CollaborativeEditor';
import { Whiteboard } from '../../widgets/Whiteboard';
import { CollaborationChat } from '../../widgets/CollaborationChat';
import { VideoCallPanel } from '../../widgets/VideoCallPanel';

export default function Collaboration() {
  const [rooms, setRooms] = createSignal([]);
  const [selectedRoom, setSelectedRoom] = createSignal(null);
  const [connection, setConnection] = createSignal(null);
  const [activeTab, setActiveTab] = createSignal('documents'); // documents, whiteboard, chat, call
  const [documents, setDocuments] = createSignal([]);
  const [selectedDocument, setSelectedDocument] = createSignal(null);
  const [whiteboards, setWhiteboards] = createSignal([]);
  const [selectedWhiteboard, setSelectedWhiteboard] = createSignal(null);
  const [messages, setMessages] = createSignal([]);
  const [isInCall, setIsInCall] = createSignal(false);

  onMount(async () => {
    // Initialize SignalR
    const conn = new HubConnectionBuilder()
      .withUrl('/collaborationHub', { accessTokenFactory: () => localStorage.getItem('accessToken') || '' })
      .withAutomaticReconnect()
      .build();

    conn.on('DocumentChanged', (change) => {
      console.log('Document changed:', change);
    });

    conn.on('ElementDrawn', (element) => {
      console.log('Element drawn:', element);
    });

    conn.on('CursorMoved', (userId, x, y) => {
      console.log(`User ${userId} cursor at ${x}, ${y}`);
    });

    conn.on('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });

    conn.on('CallInitiated', (callData) => {
      setIsInCall(true);
    });

    await conn.start();
    setConnection(conn);

    // Load user rooms
    const response = await fetch('/api/collaboration/rooms', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const data = await response.json();
    setRooms(data.rooms || []);
  });

  const selectRoom = async (room) => {
    setSelectedRoom(room);
    await connection()?.invoke('JoinRoom', room.id);
  };

  const selectDocument = (doc) => {
    setSelectedDocument(doc);
    connection()?.invoke('JoinDocument', doc.id);
  };

  const handleDocumentChange = (opType, position, content) => {
    if (selectedDocument()) {
      connection()?.invoke('SendDocumentChange', selectedDocument().id, opType, position, content);
    }
  };

  const selectWhiteboard = (whiteboard) => {
    setSelectedWhiteboard(whiteboard);
    connection()?.invoke('JoinWhiteboard', whiteboard.id);
  };

  const handleDrawing = (elementData) => {
    if (selectedWhiteboard()) {
      connection()?.invoke('DrawElement', selectedWhiteboard().id, elementData);
    }
  };

  const sendMessage = (content) => {
    if (selectedRoom()) {
      connection()?.invoke('SendMessage', selectedRoom().id, content, 'Text');
    }
  };

  const initiateCall = (type) => {
    if (selectedRoom()) {
      const callRoomId = `call_${Date.now()}`;
      connection()?.invoke('InitiateCall', selectedRoom().id, type, callRoomId);
    }
  };

  return (
    <div class="collaboration-page">
      <div class="rooms-sidebar">
        <h2>Rooms</h2>
        <ul>
          <For each={rooms()}>
            {(room) => (
              <li 
                onClick={() => selectRoom(room)}
                class={selectedRoom()?.id === room.id ? 'active' : ''}
              >
                {room.name}
              </li>
            )}
          </For>
        </ul>
      </div>

      {selectedRoom() && (
        <div class="collaboration-content">
          <div class="tabs">
            <button 
              onClick={() => setActiveTab('documents')}
              class={activeTab() === 'documents' ? 'active' : ''}
            >
              📄 Documents
            </button>
            <button 
              onClick={() => setActiveTab('whiteboard')}
              class={activeTab() === 'whiteboard' ? 'active' : ''}
            >
              🎨 Whiteboard
            </button>
            <button 
              onClick={() => setActiveTab('chat')}
              class={activeTab() === 'chat' ? 'active' : ''}
            >
              💬 Chat
            </button>
            <button 
              onClick={() => setActiveTab('call')}
              class={activeTab() === 'call' ? 'active' : ''}
            >
              📞 Call
            </button>
          </div>

          <Show when={activeTab() === 'documents'}>
            <div class="documents-panel">
              <div class="documents-list">
                <For each={documents()}>
                  {(doc) => (
                    <div 
                      class="document-item"
                      onClick={() => selectDocument(doc)}
                      class={selectedDocument()?.id === doc.id ? 'selected' : ''}
                    >
                      {doc.name}
                    </div>
                  )}
                </For>
              </div>
              {selectedDocument() && (
                <CollaborativeEditor 
                  document={selectedDocument()}
                  onChange={handleDocumentChange}
                />
              )}
            </div>
          </Show>

          <Show when={activeTab() === 'whiteboard'}>
            <div class="whiteboard-panel">
              <div class="whiteboards-list">
                <For each={whiteboards()}>
                  {(wb) => (
                    <div 
                      class="whiteboard-item"
                      onClick={() => selectWhiteboard(wb)}
                      class={selectedWhiteboard()?.id === wb.id ? 'selected' : ''}
                    >
                      {wb.name}
                    </div>
                  )}
                </For>
              </div>
              {selectedWhiteboard() && (
                <Whiteboard 
                  whiteboard={selectedWhiteboard()}
                  onDrawing={handleDrawing}
                />
              )}
            </div>
          </Show>

          <Show when={activeTab() === 'chat'}>
            <CollaborationChat 
              messages={messages()}
              onSendMessage={sendMessage}
            />
          </Show>

          <Show when={activeTab() === 'call' || isInCall()}>
            <VideoCallPanel 
              onInitiateCall={initiateCall}
            />
          </Show>
        </div>
      )}
    </div>
  );
}