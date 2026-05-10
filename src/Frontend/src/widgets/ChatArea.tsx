import { createSignal, For, Show } from 'solid-js';

export function ChatArea(props) {
  const [selectedMessage, setSelectedMessage] = createSignal(null);

  const formatCode = (content) => {
    const codeBlockRegex = /```(\w+)\n([\s\S]*?)```/g;
    return content.replace(codeBlockRegex, (match, lang, code) => {
      return `<pre><code class="language-${lang}">${code.trim()}</code></pre>`;
    });
  };

  return (
    <div class="chat-area">
      <div class="messages">
        <For each={props.messages}>
          {(message) => (
            <div 
              class="message-item" 
              onContextMenu={(e) => {
                e.preventDefault();
                setSelectedMessage(message);
              }}
            >
              <div class="message-header">
                <strong>{message.senderId}</strong>
                <small>{new Date(message.timestamp).toLocaleTimeString()}</small>
              </div>
              <div class="message-content" innerHTML={formatCode(message.content)} />
              
              <Show when={message.attachments?.length > 0}>
                <div class="attachments">
                  <For each={message.attachments}>
                    {(attachment) => (
                      <a href={attachment.url} target="_blank">
                        📎 {attachment.fileName}
                      </a>
                    )}
                  </For>
                </div>
              </Show>

              <div class="reactions">
                <For each={message.reactions || []}>
                  {(reaction) => (
                    <button 
                      class="reaction"
                      onClick={() => props.onReactToMessage(message.id, reaction.emoji)}
                    >
                      {reaction.emoji}
                    </button>
                  )}
                </For>
              </div>

              <Show when={selectedMessage()?.id === message.id}>
                <div class="context-menu">
                  <button onClick={() => props.onReactToMessage(message.id, '👍')}>👍 React</button>
                  <button onClick={() => props.onPinMessage(message.id)}>📌 Pin</button>
                </div>
              </Show>
            </div>
          )}
        </For>
      </div>

      <div class="chat-input">
        <input type="text" placeholder="Message #channel..." />
        <button onClick={() => props.onSendMessage('message')}>Send</button>
      </div>
    </div>
  );
}