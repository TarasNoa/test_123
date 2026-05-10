import { createSignal, For } from 'solid-js';

export function VoicePanel(props) {
  const [isMuted, setIsMuted] = createSignal(false);
  const [isDeafened, setIsDeafened] = createSignal(false);
  const [isScreenSharing, setIsScreenSharing] = createSignal(false);

  return (
    <div class="voice-panel">
      <h4>Voice Call</h4>
      <div class="participants">
        <For each={props.participants}>
          {(participant) => (
            <div class="participant">
              <div class="avatar">{participant}</div>
            </div>
          )}
        </For>
      </div>
      <div class="controls">
        <button 
          onClick={() => setIsMuted(!isMuted())} 
          class={isMuted() ? 'active' : ''}
        >
          🎤 {isMuted() ? 'Unmute' : 'Mute'}
        </button>
        <button 
          onClick={() => setIsDeafened(!isDeafened())} 
          class={isDeafened() ? 'active' : ''}
        >
          🔊 {isDeafened() ? 'Undeafen' : 'Deafen'}
        </button>
        <button 
          onClick={() => setIsScreenSharing(!isScreenSharing())} 
          class={isScreenSharing() ? 'active' : ''}
        >
          🖥️ {isScreenSharing() ? 'Stop Share' : 'Share Screen'}
        </button>
        <button onClick={props.onEndCall} class="end-call">
          📞 End Call
        </button>
      </div>
    </div>
  );
}