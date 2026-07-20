using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using Microsoft.EntityFrameworkCore;

namespace MAUIClientUI.MVVM
{
    public class NotesViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<NoteClient> _listOfNotes = new ObservableCollection<NoteClient>();

        public ObservableCollection<NoteClient> ListOfNotes
        {
            get => _listOfNotes;
            set
            {
                _listOfNotes = value;
                OnPropertyChanged();
            }
        }

        public NotesViewModel()
        {
            ListOfNotes = new ObservableCollection<NoteClient>();
        }

        public async Task LoadNotesAsync(DbContextClient dbContext)
        {
            var notes = await dbContext.Notes.ToListAsync();
            ListOfNotes.Clear();
            foreach (var note in notes)
            {
                ListOfNotes.Add(note);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
