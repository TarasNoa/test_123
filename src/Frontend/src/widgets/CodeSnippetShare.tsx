import { createSignal, For } from 'solid-js';

export function CodeSnippetShare(props) {
  const [selectedLanguage, setSelectedLanguage] = createSignal('javascript');
  const [code, setCode] = createSignal('');
  const [title, setTitle] = createSignal('');

  const languages = ['javascript', 'python', 'csharp', 'rust', 'fsharp', 'typescript', 'html', 'css'];

  const handleSend = () => {
    if (code().trim() && title().trim()) {
      props.onSendCode(selectedLanguage(), code(), title());
      setCode('');
      setTitle('');
    }
  };

  return (
    <div class="code-snippet-share">
      <h4>Share Code Snippet</h4>
      <input 
        type="text" 
        placeholder="Title" 
        value={title()} 
        onInput={(e) => setTitle(e.currentTarget.value)}
      />
      <select value={selectedLanguage()} onChange={(e) => setSelectedLanguage(e.currentTarget.value)}>
        <For each={languages}>
          {(lang) => <option value={lang}>{lang}</option>}
        </For>
      </select>
      <textarea 
        placeholder="Paste your code here..." 
        value={code()} 
        onInput={(e) => setCode(e.currentTarget.value)}
        rows={10}
      />
      <button onClick={handleSend}>Send Code</button>
    </div>
  );
}