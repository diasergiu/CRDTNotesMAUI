#if WINDOWS
using MAUIClientUI.Miscellaneous;
using Microsoft.UI.Input;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Input;

namespace MAUIClientUI.Platforms.Windows
{
    public class WindowsContentEditorHandler : ContentEditorInputHandler
    {
        public WindowsContentEditorHandler(Editor editor) : base(editor)
        {
        }
        public override void HandleKeyPress(object sender, dynamic e)
        {
            var key = GetkeyPressed(e);

            int cursorPosition = GetEditorCurrentPosition();


            // Handle Backspace key
            if (key == VirtualKey.Back.ToString())
            {
                _ = HandleBackspaceAsync(cursorPosition);
                e.Handled = true;
                Debug.WriteLine("Backspace pressed");
                return;
            }

            // Handle regular character input
            if (key.Length == 1 || key == "Space")
            {
                string charToInsert = key == "Space" ? " " : key;
                char typedChar = ResolveTypedCharacter((VirtualKey)Enum.Parse(typeof(VirtualKey), key), charToInsert[0]);

                _ = HandleInsertionAsync(cursorPosition, typedChar);
                Debug.WriteLine("Key pressed");
            }
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
            string newText = _editor.Text ?? string.Empty;
            int cursorPosition = GetEditorCurrentPosition();
            int oldLength = _previousText.Length;
            int newLength = newText.Length;

            Debug.WriteLine($"Text changed: '{_previousText}' -> '{newText}', Cursor: {cursorPosition}");

            // Text was deleted (backspace, delete, or multi-char selection delete)
            if (newLength < oldLength)
            {
                int deletedCount = oldLength - newLength;
                int startPos = cursorPosition;
                int endPos = cursorPosition + deletedCount;

                if (deletedCount == 1)
                {
                    _ = InvokeCharacterDeleted(cursorPosition);
                }
                else
                {
                    _ = InvokeRangeDeleted(startPos, endPos);
                }
            }
            // Text was inserted (typing, paste, or multi-char selection replace)
            else if (newLength > oldLength)
            {
                int insertedCount = newLength - oldLength;
                string insertedText = ExtractInsertedText(newText, _previousText, cursorPosition, insertedCount);

                if (insertedCount == 1)
                {
                    _ = InvokeCharacterInserted(cursorPosition - 1, insertedText[0]);
                }
                else
                {
                    _ = InvokeStringInserted(cursorPosition - insertedCount, insertedText);
                    Debug.WriteLine($"String inserted: '{insertedText}' at position {cursorPosition - insertedCount}");
                }
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
        private string GetkeyPressed(dynamic e)
        {
            var keyEvent = e as KeyRoutedEventArgs;
            if (keyEvent is null)
                return string.Empty;
            return keyEvent.Key.ToString();
        }

        public override void HandleKeyUp(object sender, dynamic e)
        {
            var keyEvent = e as KeyRoutedEventArgs;
            if (keyEvent is not null)
            {
                Debug.WriteLine($"Key Up: {keyEvent.Key}");
            }
        }

        private int GetEditorCurrentPosition()
        {
            try
            {
                var handler = _editor.Handler as Microsoft.Maui.Handlers.EditorHandler;
                if (handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
                    return textBox.SelectionStart;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting cursor position: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// VirtualKey names for letters are always uppercase, so the actual casing has to be
        /// derived from the current Shift / CapsLock state.
        /// </summary>
        private static char ResolveTypedCharacter(VirtualKey key, char fallback)
        {
            if (key < VirtualKey.A || key > VirtualKey.Z)
                return fallback;

            bool shiftDown = IsKeyDown(VirtualKey.Shift);
            bool capsLockOn = IsKeyLocked(VirtualKey.CapitalLock);

            return (shiftDown ^ capsLockOn) ? char.ToUpperInvariant(fallback) : char.ToLowerInvariant(fallback);
        }

        private static bool IsKeyDown(VirtualKey key)
            => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

        private static bool IsKeyLocked(VirtualKey key)
            => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Locked) == CoreVirtualKeyStates.Locked;

        
    }
}
#endif