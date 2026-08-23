#if ANDROID
using Android.Views;
using Android.Widget;
using MAUIClientUI.Miscellaneous;
using System.Diagnostics;
using static Bumptech.Glide.DiskLruCache.DiskLruCache;

namespace MAUIClientUI.Platforms.Android
{
    public class AndroidContentEditorHandler : IContentEditorInputHandler
    {
        private readonly Microsoft.Maui.Controls.Editor _editor;

        public event Action<int, char> CharacterInserted;
        public event Action<int> CharacterDeleted;

        public AndroidContentEditorHandler(Microsoft.Maui.Controls.Editor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }
        public void HandleKeyPress(object sender, dynamic e)
        {
            var editor = sender as Microsoft.Maui.Controls.Editor;
            if (editor is null || e?.Event is null)
            {
                e.Handled = false;
                return;
            }

            int cursorPosition = GetEditorCursorPosition();

            // Handle Backspace/Delete key
            if (e.KeyCode == Keycode.Del)
            {
                CharacterDeleted?.Invoke(cursorPosition);
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

            CharacterInserted?.Invoke(cursorPosition, typedChar);
            Debug.WriteLine($"Key pressed: {typedChar}");
            e.Handled = false;
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

        public void HandleKeyUp(object sender, dynamic e)
        {
            // Platform-specific key up handling can be added here if needed
            Debug.WriteLine($"Key Up: {e?.KeyCode}");
        }
    }
}
#endif