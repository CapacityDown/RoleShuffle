using System;
using TMPro;

namespace REPOJP.StageRoles;

// MenuLib appends a rich-text caret. Keep the note literal (including typed
// angle brackets) while rendering only its generated caret as a plain bar.
internal sealed class PresetNoteInputText(Func<string> currentValue) : ITextPreprocessor
{
    public string PreprocessText(string text)
    {
        string value = currentValue();
        return text == value + "<b>|</b>" ? value + "|" : text;
    }
}
