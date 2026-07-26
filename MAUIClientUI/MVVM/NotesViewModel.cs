using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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

        // Add overload for NoteRepository
        //public async Task LoadNotesAsync(NoteRepository noteRepository, UserClient user)
        //{
        //    var notes = noteRepository.getAllChanges(user);
        //    ListOfNotes.Clear();
        //    foreach (var note in notes)
        //    {
        //        ListOfNotes.Add(note);
        //    }
        //}

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
