import { useState } from 'react';
import CodeMirror from '@uiw/react-codemirror';
import { python } from '@codemirror/lang-python';
import { EditorView } from '@codemirror/view';

export const heroCode = '# Твой следующий ход\ndef solve(numbers):\n    result = 0\n    for n in numbers:\n        result += n\n    return result';
const extensions = [python(), EditorView.theme({
  '&': { backgroundColor: 'transparent', fontSize: '14px', height: '100%' },
  '.cm-scroller': { fontFamily: "'Geist Mono', monospace", lineHeight: '24px', overflow: 'auto' },
  '.cm-content': { padding: '8px 0', minHeight: '100%' },
  '.cm-gutters': { backgroundColor: 'transparent', border: 'none', color: '#a3a3a3' },
  '.cm-lineNumbers .cm-gutterElement': { padding: '0 12px' },
  '&.cm-focused': { outline: 'none' },
})];

export default function HeroCodeEditor() {
  const [code, setCode] = useState(heroCode);
  return <CodeMirror value={code} onChange={setCode} extensions={extensions} theme="dark" height="100%"
    aria-label="Попробуй редактировать Python код" indentWithTab={false}
    basicSetup={{ foldGutter: false, highlightActiveLine: false, highlightActiveLineGutter: false, autocompletion: true }} />;
}
