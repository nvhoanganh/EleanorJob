import React, { useEffect } from 'react';

import Editor, { useMonaco } from '@monaco-editor/react';
import { registerAutoComplete } from './autoComplete';

function App() {
	const monaco = useMonaco();
	useEffect(() => {
		if (monaco) {
			console.log('here is the monaco isntance:', monaco);
			registerAutoComplete(monaco);
		}
	}, [monaco]);
	
	return (
		<Editor
			height='90vh'
			defaultLanguage='json'
			defaultValue='{ "steps": [] }'
		/>
	);
}

export default App;
