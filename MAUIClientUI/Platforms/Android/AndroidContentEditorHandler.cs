#if ANDROID
using Android.Views;
using Android.Widget;
using MAUIClientUI.Miscellaneous;
using System.Diagnostics;
using static Bumptech.Glide.DiskLruCache.DiskLruCache;

namespace MAUIClientUI.Platforms.Android
{
    public class AndroidContentEditorHandler : ContentEditorInputHandler
    {
        public AndroidContentEditorHandler(Microsoft.Maui.Controls.Editor editor) : base(editor)
        {
        }
        public override void HandleKeyPress(object sender, dynamic e)
        {
             if (_editor is null || e?.Event is null)
            {
                e.Handled = false;
                return;
            }

            int cursorPosition = GetEditorCursorPosition();

            // Handle Backspace/Delete key
            if (e.KeyCode == Keycode.Back)
            {
                _ = HandleBackspaceAsync(cursorPosition);
                e.Handled = true;
                Debug.WriteLine("Backspace pressed");
                return;
            }

            // Handle regular character input
            char typedChar = (char)e.Event.UnicodeChar;
            if (typedChar == '\0')
            {
                e.Handled = false;
                return;
            }

            _ = HandleInsertionAsync(cursorPosition, typedChar);
            Debug.WriteLine($"Key pressed: {typedChar}");
            e.Handled = false;
        }

        private async Task HandleInsertionAsync(int cursorPosition, char typedChar)
        {
            await InvokeCharacterInserted(cursorPosition, typedChar);
        }
        private async Task HandleBackspaceAsync(int cursorPosition)
        {
            await InvokeCharacterDeleted(cursorPosition);
        }

        public override void HandleTextChanged(object sender, dynamic e)
        {
            if (_editor == null) return;

            string currentText = _editor.Text;
            string oldText = e.OldTextValue;
            string newText = e.NewTextValue;

            int cursorPosition = GetEditorCursorPosition();
            int oldLength = _previousText.Length;
            int newLength = newText.Length;

            Debug.WriteLine($"Text changed: '{_previousText}' -> '{newText}', Cursor: {cursorPosition}");

            // Text was deleted
            if (newLength < oldLength)
            {
                int deletedCount = oldLength - newLength;
                int startPos = cursorPosition;
                int endPos = cursorPosition + deletedCount;

                _ = InvokeRangeDeleted(startPos, endPos);
            }
            // Text was inserted
            else if (newLength > oldLength)
            {
                int insertedCount = newLength - oldLength;
                string insertedText = ExtractInsertedText(newText, _previousText, cursorPosition, insertedCount);

                _ = InvokeStringInserted(cursorPosition - insertedCount, insertedText);
                Debug.WriteLine($"String inserted: '{insertedText}' at position {cursorPosition - insertedCount}");
            }

            _previousText = newText;
        }

        private string ExtractInsertedText(string newText, string oldText, int cursorPosition, int insertedCount)
        {
            // The inserted text should be before the cursor position
            int startIndex = cursorPosition - insertedCount;
            if (startIndex >= 0 && startIndex + insertedCount <= newText.Length)
            {
                return newText.Substring(startIndex, insertedCount);
            }
            return string.Empty;
        }
        private int GetEditorCursorPosition()
        {
            try
            {
                var handler = _editor.Handler as Microsoft.Maui.Handlers.EditorHandler;
                if (handler?.PlatformView is Microsoft.Maui.Platform.MauiAppCompatEditText editText)
                    return editText.SelectionStart;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting cursor position: {ex.Message}");
            }

            return 0;
        }

        public override void HandleKeyUp(object sender, dynamic e)
        {
            // Platform-specific key up handling can be added here if needed
            Debug.WriteLine($"Key Up: {e?.KeyCode}");
        }
    }
}
#endif