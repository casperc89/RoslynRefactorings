using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

internal class DocumentEdits
{
    private readonly Dictionary<DocumentId, DocumentEditor> _documents = new();

    public async Task<DocumentEditor> GetOrAddAsync(Document document)
    {
        if (_documents.TryGetValue(document.Id, out var editedDocument))
            return editedDocument;

        var editor = await DocumentEditor.CreateAsync(document);
        _documents.Add(document.Id, editor);
        return editor;
    }

    public async Task SaveAsync()
    {
        foreach (var documentEdit in _documents.Values)
        {
            var changedDoc = documentEdit.GetChangedDocument();
            var txt = await changedDoc.GetTextAsync();
            await File.WriteAllTextAsync(changedDoc.FilePath, txt.ToString());
        }
    }
}