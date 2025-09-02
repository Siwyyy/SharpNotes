using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpNotes.Models;
using SharpNotes.Services;

namespace SharpNotes.ViewModels;

public partial class TagPanelViewModel : ViewModelBase
{
    private readonly NotesService _notesService;

    // Event do powiadamiania o zmianach w tagach
    public event EventHandler? TagsChanged;

    [ObservableProperty] private ObservableCollection<Tag> _allTags = new();
    [ObservableProperty] private string _newTagName = string.Empty;
    [ObservableProperty] private Tag? _selectedTag;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isTagging;
    [ObservableProperty] private ObservableCollection<Tag> _noteTagsCollection = new();
    [ObservableProperty] private Note? _currentNote;

    public TagPanelViewModel(NotesService notesService)
    {
        _notesService = notesService;
    }

    // Metoda do wywoływania eventu
    protected virtual void OnTagsChanged()
    {
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task LoadTags()
    {
        // Usunięto ustawianie IsLoading = true

        try
        {
            var tags = await _notesService.GetAllTagsAsync();
            AllTags.Clear();
            foreach (var tag in tags)
            {
                AllTags.Add(tag);
            }
        }
        finally
        {
            // Usunięto ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private async Task AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
            return;

        // Usunięto ustawianie IsLoading = true

        try
        {
            var tag = await _notesService.AddTagAsync(NewTagName);
            AllTags.Add(tag);
            NewTagName = string.Empty;
        }
        finally
        {
            // Usunięto ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private async Task DeleteTag()
    {
        if (SelectedTag == null)
            return;

        // Usunięto ustawianie IsLoading = true

        try
        {
            await _notesService.DeleteTagAsync(SelectedTag.Id);
            AllTags.Remove(SelectedTag);
            SelectedTag = null;

            // Powiadom o zmianie tagów
            OnTagsChanged();
        }
        finally
        {
            // Usunięto ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private async Task LoadNoteTags(Note? note)
    {
        if (note == null)
            return;

        CurrentNote = note;
        NoteTagsCollection.Clear();

        // Używamy HashSet do przechowywania unikalnych ID tagów
        var uniqueTags = new HashSet<int>();

        foreach (var noteTag in note.NoteTags)
        {
            // Sprawdzamy, czy tag o tym ID nie został już dodany
            if (!uniqueTags.Contains(noteTag.TagId))
            {
                NoteTagsCollection.Add(noteTag.Tag);
                uniqueTags.Add(noteTag.TagId);
            }
        }
    }


    [RelayCommand]
    private async Task AddTagToNote(Tag? tag)
    {
        if (CurrentNote == null || tag == null)
            return;

        // Usunięto ustawianie IsTagging = true

        try
        {
            bool tagExists = CurrentNote.NoteTags.Any(nt => nt.TagId == tag.Id);

            if (!tagExists)
            {
                // Dodaj tag do notatki w bazie danych
                await _notesService.AddTagToNoteAsync(CurrentNote.Id, tag.Id);

                // Odśwież notatkę z bazy danych
                CurrentNote = await _notesService.GetNoteByIdAsync(CurrentNote.Id);

                // Odśwież listę tagów notatki
                NoteTagsCollection.Clear();
                var uniqueTags = new HashSet<int>();

                foreach (var noteTag in CurrentNote.NoteTags)
                {
                    if (!uniqueTags.Contains(noteTag.TagId))
                    {
                        NoteTagsCollection.Add(noteTag.Tag);
                        uniqueTags.Add(noteTag.TagId);
                    }
                }

                // Powiadom o zmianie tagów
                OnTagsChanged();
            }
        }
        finally
        {
            // Usunięto ustawianie IsTagging = false
        }
    }

    [RelayCommand]
    private async Task RemoveTagFromNote(Tag? tag)
    {
        if (CurrentNote == null || tag == null)
            return;

        // Usunięto ustawianie IsTagging = true

        try
        {
            await _notesService.RemoveTagFromNoteAsync(CurrentNote.Id, tag.Id);

            // Usuń tag z kolekcji
            var tagToRemove = NoteTagsCollection.FirstOrDefault(t => t.Id == tag.Id);
            if (tagToRemove != null)
            {
                NoteTagsCollection.Remove(tagToRemove);

                // Usuń NoteTag z CurrentNote
                var noteTagToRemove = CurrentNote.NoteTags.FirstOrDefault(nt => nt.TagId == tag.Id);
                if (noteTagToRemove != null)
                {
                    CurrentNote.NoteTags.Remove(noteTagToRemove);
                }

                // Powiadom o zmianie tagów
                OnTagsChanged();
            }
        }
        finally
        {
            // Usunięto ustawianie IsTagging = false
        }
    }
}