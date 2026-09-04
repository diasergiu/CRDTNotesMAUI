using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MAUIClientUI.Miscellaneous
{
    public abstract class ContentEditorInputHandler : IContentEditorInputHandler
    {
        protected readonly Editor _editor;
        protected string _previousText = string.Empty;
        public event Func<int, char, Task> CharacterInserted;
        public event Func<int, Task> CharacterDeleted;
        public event Func<int, string, Task> StringInserted;
        public event Func<int, int, Task> RangeDeleted;

        protected ContentEditorInputHandler(Editor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _previousText = _editor.Text ?? string.Empty;
        }

        protected async Task InvokeCharacterInserted(int cursorPosition, char typedChar)
        {
            if (CharacterInserted != null)
            {
                try
                {
                    await CharacterInserted(cursorPosition, typedChar);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in CharacterInserted: {ex.Message}");
                }
            }
        }

        protected async Task InvokeCharacterDeleted(int cursorPosition)
        {
            if (CharacterDeleted != null)
            {
                try
                {
                    await CharacterDeleted(cursorPosition);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in CharacterDeleted: {ex.Message}");
                }
            }
        }

        protected async Task InvokeStringInserted(int cursorPosition, string text)
        {
            if (StringInserted != null)
            {
                try
                {
                    await StringInserted(cursorPosition, text);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in StringInserted: {ex.Message}");
                }
            }
        }

        protected async Task InvokeRangeDeleted(int startPos, int endPos)
        {
            if (RangeDeleted != null)
            {
                try
                {
                    await RangeDeleted(startPos, endPos);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in RangeDeleted: {ex.Message}");
                }
            }
        }

        public abstract void HandleKeyPress(object sender, dynamic e);
        public abstract void HandleKeyUp(object sender, dynamic e);
        public abstract void HandleTextChanged(object sender, dynamic e);
    }
}
