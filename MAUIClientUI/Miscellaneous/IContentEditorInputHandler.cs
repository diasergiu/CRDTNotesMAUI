using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Miscellaneous
{
    internal interface IContentEditorInputHandler
    {
        /// <summary>
        /// Raised when a character is typed. Provides the cursor position and the character.
        /// </summary>
        event Action<int, char> CharacterInserted;

        /// <summary>
        /// Raised when a character deletion (backspace) is requested. Provides the cursor position.
        /// </summary>
        event Action<int> CharacterDeleted;

        /// <summary>
        /// Handles key press events for inserting or deleting characters.
        /// </summary>
        void HandleKeyPress(object sender, dynamic e);

        /// <summary>
        /// Handles key up events.
        /// </summary>
        void HandleKeyUp(object sender, dynamic e);
    }
}
