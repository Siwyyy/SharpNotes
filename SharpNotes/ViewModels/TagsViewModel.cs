using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpNotes.Models;
using SharpNotes.Services;

namespace SharpNotes.ViewModels;

public partial class TagsViewModel : ViewModelBase
{
    private readonly NotesService _notesService;

    [ObservableProperty] private ObservableCollection<Tag> _allTags = new();
    [ObservableProperty] private string _newTagName = string.Empty;
    [ObservableProperty] private Tag? _selectedTag;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isTagging;
    [ObservableProperty] private ObservableCollection<Tag> _noteTagsCollection = new();
    [ObservableProperty] private Note? _currentNote;

    public TagsViewModel(NotesService notesService)
    {
        _notesService = notesService;
    }

    [RelayCommand]
    private async Task LoadTags()
    {
        IsLoading = true;

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
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
            return;

        IsLoading = true;

        try
        {
            var tag = await _notesService.AddTagAsync(NewTagName);
            AllTags.Add(tag);
            NewTagName = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTag()
    {
        if (SelectedTag == null)
            return;

        IsLoading = true;

        try
        {
            await _notesService.DeleteTagAsync(SelectedTag.Id);
            AllTags.Remove(SelectedTag);
            SelectedTag = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadNoteTags(Note note)
    {
        if (note == null)
            return;

        CurrentNote = note;
        NoteTagsCollection.Clear();

        // Używamy HashSet do przechowywania unikalnych ID tagów
        var addedTagIds = new HashSet<int>();

        foreach (var noteTag in note.NoteTags)
        {
            // Sprawdzamy, czy tag o tym ID nie został już dodany
            if (!addedTagIds.Contains(noteTag.Tag.Id))
            {
                NoteTagsCollection.Add(noteTag.Tag);
                addedTagIds.Add(noteTag.Tag.Id);
            }
        }
    }

    [RelayCommand]
    private async Task AddTagToNote(Tag tag)
    {
        if (CurrentNote == null || tag == null)
            return;

        IsTagging = true;

        try
        {
            bool tagExists = CurrentNote.NoteTags.Any(nt => nt.TagId == tag.Id);

            if (!tagExists)
            {
                await _notesService.AddTagToNoteAsync(CurrentNote.Id, tag.Id);

                // Odśwież notatkę, aby pobrać zaktualizowane powiązania z bazy danych
                CurrentNote = await _notesService.GetNoteByIdAsync(CurrentNote.Id);

                // Zaktualizuj kolekcję tagów
                NoteTagsCollection.Clear();
                foreach (var noteTag in CurrentNote.NoteTags)
                {
                    NoteTagsCollection.Add(noteTag.Tag);
                }
            }
        }
        finally
        {
            IsTagging = false;
        }
    }

    [RelayCommand]
    private async Task RemoveTagFromNote(Tag tag)
    {
        if (CurrentNote == null || tag == null)
            return;

        IsTagging = true;

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
            }
        }
        finally
        {
            IsTagging = false;
        }
    }
}