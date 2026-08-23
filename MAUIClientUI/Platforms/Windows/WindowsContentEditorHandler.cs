#if WINDOWS
using MAUIClientUI.Miscellaneous;
using Microsoft.UI.Input;
using System.Diagnostics;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Input;

namespace MAUIClientUI.Platforms.Windows
{
    public class WindowsContentEditorHandler : IContentEditorInputHandler
    {
        private readonly Editor _editor;

        public event Action<int, char> CharacterInserted;
        public event Action<int> CharacterDeleted;

        public WindowsContentEditorHandler(Editor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }


        public void HandleKeyPress(object sender, dynamic e)
        {
            var key = GetkeyPressed(e);

            int cursorPosition = GetEditorCurrentPosition();


            // Handle Backspace key
            if (key == VirtualKey.Back.ToString())
            {
                CharacterDeleted?.Invoke(cursorPosition);
                e.Handled = true;
                Debug.WriteLine("Backspace pressed");
                return;
            }

            // Handle regular character input
            if (key.Length == 1 || key == "Space")
            {
                string charToInsert = key == "Space" ? " " : key;
                char typedChar = ResolveTypedCharacter((VirtualKey)Enum.Parse(typeof(VirtualKey), key), charToInsert[0]);

                CharacterInserted?.Invoke(cursorPosition, typedChar);
                Debug.WriteLine("Key pressed");
            }
        }

        private string GetkeyPressed(dynamic e)
        {
            var keyEvent = e as KeyRoutedEventArgs;
            if (keyEvent is null)
                return string.Empty;
            return keyEvent.Key.ToString();
        }

        public void HandleKeyUp(object sender, dynamic e)
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