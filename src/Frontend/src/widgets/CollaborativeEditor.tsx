import { createEffect, onMount } from 'solid-js';
import Prism from 'prismjs';
import 'prismjs/themes/prism-tomorrow.css';

export function CollaborativeEditor(props) {
  let editorRef;
  let cursorPositionRef;

  onMount(() => {
    if (editorRef) {
      editorRef.value = props.document.content;
    }
  });

  const handleInput = (e) => {
    const textarea = e.currentTarget;
    const cursorPos = textarea.selectionStart;
    const oldContent = props.document.content;
    const newContent = textarea.value;

    // Calculate differences
    if (newContent.length > oldContent.length) {
      const insertedText = newContent.substring(cursorPos - (newContent.length - oldContent.length), cursorPos);
      props.onChange('insert', cursorPos - insertedText.length, insertedText);
    } else if (newContent.length < oldContent.length) {
      const deletedLength = oldContent.length - newContent.length;
      props.onChange('delete', cursorPos, deletedLength.toString());
    }

    // Highlight syntax
    const highlighted = Prism.highlight(newContent, Prism.languages.javascript || {}, 'javascript');
    if (editorRef.nextElementSibling) {
      editorRef.nextElementSibling.innerHTML = highlighted;
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Tab') {
      e.preventDefault();
      const start = editorRef.selectionStart;
      const end = editorRef.selectionEnd;
      editorRef.value = editorRef.value.substring(0, start) + '\t' + editorRef.value.substring(end);
      editorRef.selectionStart = editorRef.selectionEnd = start + 1;
      handleInput({ currentTarget: editorRef });
    }
  };

  return (
    <div class="collaborative-editor">
      <h3>{props.document.name}</h3>
      <div class="editor-container">
        <textarea
          ref={editorRef}
          onInput={handleInput}
          onKeyDown={handleKeyDown}
          class="editor-textarea"
          placeholder="Start typing..."
        />
        <pre class="editor-highlight">
          <code class="language-javascript"></code>
        </pre>
      </div>
      <div class="editor-info">
        <p>Versions: {props.document.versionCount}</p>
        <p>Collaborators: {props.document.collaboratingUsers.length}</p>
      </div>
    </div>
  );
}