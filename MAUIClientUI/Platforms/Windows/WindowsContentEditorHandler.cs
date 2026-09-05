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
            var keyEvent = e as KeyRoutedEventArgs;
            if (keyEvent is null)
                return;

            VirtualKey virtualKey = keyEvent.Key;
            int cursorPosition = GetEditorCurrentPosition();

            // Handle Backspace key
            if (virtualKey == VirtualKey.Back)
            {
                _ = HandleBackspaceAsync(cursorPosition);
                e.Handled = true;
                Debug.WriteLine("Backspace pressed");
                return;
            }

            // Handle regular character input (letters, digits, punctuation, space, numpad).
            if (TryResolveTypedCharacter(virtualKey, out char typedChar))
            {
                _ = HandleInsertionAsync(cursorPosition, typedChar);
                Debug.WriteLine($"Key pressed: '{typedChar}'");
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

                _ = InvokeRangeDeleted(startPos, endPos);
            }
            // Text was inserted (typing, paste, or multi-char selection replace)
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
        /// Maps a <see cref="VirtualKey"/> to the character it produces on a US keyboard layout,
        /// honoring the current Shift / CapsLock state. Returns false for keys that do not
        /// produce a printable character (e.g. arrows, function keys, modifiers).
        /// </summary>
        private static bool TryResolveTypedCharacter(VirtualKey key, out char typedChar)
        {
            bool shiftDown = IsKeyDown(VirtualKey.Shift);
            bool capsLockOn = IsKeyLocked(VirtualKey.CapitalLock);

            // Letters A-Z
            if (key >= VirtualKey.A && key <= VirtualKey.Z)
            {
                char letter = (char)('a' + (key - VirtualKey.A));
                typedChar = (shiftDown ^ capsLockOn) ? char.ToUpperInvariant(letter) : letter;
                return true;
            }

            // Top-row digits 0-9 (VirtualKey.Number0 == 0x30 == '0')
            if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
            {
                char digit = (char)('0' + (key - VirtualKey.Number0));
                if (shiftDown)
                {
                    // US layout shifted digits
                    typedChar = digit switch
                    {
                        '1' => '!',
                        '2' => '@',
                        '3' => '#',
                        '4' => '$',
                        '5' => '%',
                        '6' => '^',
                        '7' => '&',
                        '8' => '*',
                        '9' => '(',
                        '0' => ')',
                        _ => digit,
                    };
                }
                else
                {
                    typedChar = digit;
                }
                return true;
            }

            // Numpad digits 0-9
            if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
            {
                typedChar = (char)('0' + (key - VirtualKey.NumberPad0));
                return true;
            }

            switch (key)
            {
                case VirtualKey.Space:
                    typedChar = ' ';
                    return true;
                case VirtualKey.Tab:
                    typedChar = '\t';
                    return true;
                case VirtualKey.Multiply:
                    typedChar = '*';
                    return true;
                case VirtualKey.Add:
                    typedChar = '+';
                    return true;
                case VirtualKey.Subtract:
                    typedChar = '-';
                    return true;
                case VirtualKey.Decimal:
                    typedChar = '.';
                    return true;
                case VirtualKey.Divide:
                    typedChar = '/';
                    return true;
            }

            // Oem punctuation keys (US layout).
            // Numeric values are used because some VirtualKey names are not exposed on all SDKs.
            switch ((int)key)
            {
                case 0xBA: // OEM_1  ; :
                    typedChar = shiftDown ? ':' : ';';
                    return true;
                case 0xBB: // OEM_PLUS  = +
                    typedChar = shiftDown ? '+' : '=';
                    return true;
                case 0xBC: // OEM_COMMA  , <
                    typedChar = shiftDown ? '<' : ',';
                    return true;
                case 0xBD: // OEM_MINUS  - _
                    typedChar = shiftDown ? '_' : '-';
                    return true;
                case 0xBE: // OEM_PERIOD  . >
                    typedChar = shiftDown ? '>' : '.';
                    return true;
                case 0xBF: // OEM_2  / ?
                    typedChar = shiftDown ? '?' : '/';
                    return true;
                case 0xC0: // OEM_3  ` ~
                    typedChar = shiftDown ? '~' : '`';
                    return true;
                case 0xDB: // OEM_4  [ {
                    typedChar = shiftDown ? '{' : '[';
                    return true;
                case 0xDC: // OEM_5  \ |
                    typedChar = shiftDown ? '|' : '\\';
                    return true;
                case 0xDD: // OEM_6  ] }
                    typedChar = shiftDown ? '}' : ']';
                    return true;
                case 0xDE: // OEM_7  ' "
                    typedChar = shiftDown ? '"' : '\'';
                    return true;
            }

            typedChar = '\0';
            return false;
        }

        private static bool IsKeyDown(VirtualKey key)
            => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

        private static bool IsKeyLocked(VirtualKey key)
            => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Locked) == CoreVirtualKeyStates.Locked;

        
    }
}
#endif