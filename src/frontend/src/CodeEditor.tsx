import { useMemo } from 'react';
import CodeMirror from '@uiw/react-codemirror';
import { python } from '@codemirror/lang-python';
import { javascript } from '@codemirror/lang-javascript';
import { cpp } from '@codemirror/lang-cpp';
import { StreamLanguage } from '@codemirror/language';
import { go } from '@codemirror/legacy-modes/mode/go';
import { csharp } from '@codemirror/legacy-modes/mode/clike';
import { languages } from './catalog';

export default function CodeEditor({ value, onChange, languageId, theme, editable = true }: { value: string; onChange: (value: string) => void; languageId: number; theme: 'dark' | 'light'; editable?: boolean }) {
  const extensions = useMemo(() => {
    switch (languages.find(x => x.id === languageId)?.extension) {
      case '.py': return [python()];
      case '.js': return [javascript()];
      case '.cs': return [StreamLanguage.define(csharp)];
      case '.go': return [StreamLanguage.define(go)];
      default: return [cpp()];
    }
  }, [languageId]);
  return <CodeMirror value={value} onChange={onChange} extensions={extensions} theme={theme} editable={editable} height="100%" aria-label="Редактор исходного кода" basicSetup={{ autocompletion: true, foldGutter: true, highlightActiveLine: true }} indentWithTab={false} />;
}
