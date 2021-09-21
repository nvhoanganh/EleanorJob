// https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-completion-provider-example

// https://codesandbox.io/s/c2nli?file=/src/TargetBox.tsx
function createDependencyProposals(range) {
	// returning a static list of proposals, not even looking at the prefix (filtering is done by the Monaco editor),
	// here you could do a server side lookup
	return [
		{
			label: '"type": "CRM_ENTITY_CREATOR"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText:
				'{\n\t"type": "CRM_ENTITY_CREATOR",\n\t"destEntityName": \n}',
			range: range,
		},
		{
			label: '"type": "UPSERT_CRM_ENTITY"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText:
				'{\n\t"type": "UPSERT_CRM_ENTITY",\n\t"searchByFields": ["fiedl1", "field2"],\n\t"destEntityName": \n}',
			range: range,
		},
		{
			label: '"type": "SET_VARIABLE"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText:
				'{\n\t"type": "SET_VARIABLE",\n\t"name": "var1",\n\t"value": \n}',
			range: range,
		},
		{
			label: '"type": "TRANSFORM"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText:
				'{\n\t"type": "TRANSFORM",\n\t"jsonSchema": {\n\t\t"prop1": "expressionForProp1"\n\t}\n}',
			range: range,
		},
		{
			label: '"type": "FOR_EACH"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText:
				'{\n\t"type": "FOR_EACH",\n\t"collection": "expression",\n\t"steps": [\n\t\t\n\t]\n}',
			range: range,
    },
    // vars
		{
			label: '"$payload"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"$payload"',
			range: range,
		},
		{
			label: '"$vars"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"$vars"',
			range: range,
		},
		{
			label: '"$input"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"$input"',
			range: range,
		},
    // CRM entities
		{
			label: '"entity: vehicle"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"vehicle"',
			range: range,
		},
		{
			label: '"entity: contact"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"contact"',
			range: range,
		},
		{
			label: '"entity: company"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"company"',
			range: range,
		},
		{
			label: '"entity: operationArea"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"operationArea"',
			range: range,
		},
		{
			label: '"entity: driver"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"driver"',
			range: range,
    },
    // helpers
    {
			label: '"field: objectid"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"objectid": ',
			range: range,
    },
    {
			label: '"field: objecttypecode"',
			kind: monaco.languages.CompletionItemKind.Function,
			insertText: '"objecttypecode": ',
			range: range,
    },
	];
}

monaco.languages.registerCompletionItemProvider('json', {
	provideCompletionItems: function (model, position) {
		var word = model.getWordUntilPosition(position);
		var range = {
			startLineNumber: position.lineNumber,
			endLineNumber: position.lineNumber,
			startColumn: word.startColumn,
			endColumn: word.endColumn,
		};
		return {
			suggestions: createDependencyProposals(range),
		};
	},
});

monaco.editor.create(document.getElementById('container'), {
	value: '{\n\t"steps": [\n\t\t\n\t]\n}\n',
	language: 'json',
});
