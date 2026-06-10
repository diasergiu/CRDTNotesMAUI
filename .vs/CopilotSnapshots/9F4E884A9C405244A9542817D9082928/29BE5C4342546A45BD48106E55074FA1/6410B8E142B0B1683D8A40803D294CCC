using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DatabaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRDT_TestShering.MVVM
{
    public class NotesViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Note> _listOfNotes = new ObservableCollection<Note>();

        public ObservableCollection<Note> ListOfNotes
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
            ListOfNotes = new ObservableCollection<Note>();
        }

        public async Task LoadNotesAsync(DbContextUser dbContext)
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
