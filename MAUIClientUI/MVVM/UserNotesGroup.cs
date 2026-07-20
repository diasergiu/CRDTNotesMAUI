using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DatabaseLibrary.Entities.Client;

namespace MAUIClientUI.MVVM
{
    public class UserNotesGroup : INotifyPropertyChanged
    {
        private bool _isExpanded = false;
        private ObservableCollection<NoteClient> _notes = new ObservableCollection<NoteClient>();
        private ICommand _toggleExpandCommand;

        public UserClient User { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<NoteClient> Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleExpandCommand
        {
            get
            {
                _toggleExpandCommand ??= new Command(() =>
                {
                    IsExpanded = !IsExpanded;
                });
                return _toggleExpandCommand;
            }
        }

        public UserNotesGroup(UserClient user)
        {
            User = user;
            Notes = new ObservableCollection<NoteClient>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
