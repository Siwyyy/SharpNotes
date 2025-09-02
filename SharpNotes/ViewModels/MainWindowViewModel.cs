﻿using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpNotes.Models;
using SharpNotes.Services;
using System.Linq;
using System;

namespace SharpNotes.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NotesService _notesService = null!;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _titleTextBox = "Choose a note to open, or create a new one!";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Note> _notes = new();
    [ObservableProperty] private Note? _selectedNote;
    // Zachowujemy te właściwości, ale nie będziemy już ich ustawiać na true
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isTagsVisible;
    [ObservableProperty] private TagPanelViewModel _tagPanelViewModel;

    public MainWindowViewModel(NotesService notesService)
    {
        _notesService = notesService;
        _tagPanelViewModel = new TagPanelViewModel(notesService);

        // Subskrybujemy event TagsChanged
        _tagPanelViewModel.TagsChanged += TagPanelViewModelTagPanelChanged;

        LoadNotesCommand.Execute(null);
    }

    // Metoda obsługująca zdarzenie zmiany tagów
    private void TagPanelViewModelTagPanelChanged(object? sender, EventArgs e)
    {
        // Odświeżamy listę notatek
        LoadNotesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadNotes()
    {
        // Usuwamy ustawianie IsLoading = true

        try
        {
            var notes = await _notesService.GetAllNotesAsync();
            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }

            // Odśwież listę tagów
            await TagPanelViewModel.LoadTagsCommand.ExecuteAsync(null);
        }
        finally
        {
            // Usuwamy ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        // Anuluj poprzednie wyszukiwanie, jeśli jest w trakcie
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        // Usuwamy ustawianie IsLoading = true

        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadNotesCommand.ExecuteAsync(null);
                return;
            }

            // Równoległe wyszukiwanie z wykorzystaniem indeksowania
            var results = await _notesService.IndexNotesForSearchAsync(SearchText, _searchCts.Token);

            // Pobierz pełne dane notatek dla znalezionych wyników
            var noteIds = results.OrderByDescending(r => r.Value).Select(r => r.Key).ToList();
            var notes = await Task.WhenAll(noteIds.Select(id => _notesService.GetNoteByIdAsync(id)));

            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }
        }
        catch (OperationCanceledException)
        {
            // Wyszukiwanie zostało anulowane
        }
        finally
        {
            // Usuwamy ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private async Task SearchByTag(Tag tag)
    {
        if (tag == null)
            return;

        // Usuwamy ustawianie IsLoading = true

        try
        {
            var notes = await _notesService.GetNotesByTagAsync(tag.Id);
            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }
        }
        finally
        {
            // Usuwamy ustawianie IsLoading = false
        }
    }

    [RelayCommand]
    private void SelectNote(Note note)
    {
        SelectedNote = note;
        IsEditing = false;

        // Załaduj tagi dla notatki
        TagPanelViewModel.LoadNoteTagsCommand.Execute(note);
    }

    [RelayCommand]
    private void EditNote()
    {
        if (SelectedNote != null)
        {
            IsEditing = true;
        }
    }

    [RelayCommand]
    private async Task SaveNote()
    {
        if (SelectedNote == null) return;

        // Usuwamy ustawianie IsSaving = true

        try
        {
            await _notesService.UpdateNoteAsync(SelectedNote);

            // Odświeżamy notatkę, pobierając ją ponownie z bazy danych
            var noteId = SelectedNote.Id;
            var refreshedNote = await _notesService.GetNoteByIdAsync(noteId);

            // Odświeżamy listę wszystkich notatek
            await LoadNotesCommand.ExecuteAsync(null);

            // Znajdujemy naszą notatkę w odświeżonej liście
            SelectedNote = Notes.FirstOrDefault(n => n.Id == noteId);

            // Wyjście z trybu edycji
            IsEditing = false;
        }
        finally
        {
            // Usuwamy ustawianie IsSaving = false
        }
    }

    [RelayCommand]
    private async Task CreateNewNote()
    {
        var newNote = new Note
        {
            Title = "Nowa notatka",
            Content = "Tutaj wpisz treść notatki...",
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        var addedNote = await _notesService.AddNoteAsync(newNote);
        await LoadNotesCommand.ExecuteAsync(null);

        // Wybierz nową notatkę
        SelectedNote = Notes.FirstOrDefault(n => n.Id == addedNote.Id);
        IsEditing = true;

        // Załaduj tagi dla notatki
        TagPanelViewModel.LoadNoteTagsCommand.Execute(SelectedNote);
    }

    [RelayCommand]
    private async Task DeleteNote()
    {
        if (SelectedNote == null) return;

        await _notesService.DeleteNoteAsync(SelectedNote.Id);
        await LoadNotesCommand.ExecuteAsync(null);
        SelectedNote = null;
    }

    [RelayCommand]
    private void ToggleTagsPanel()
    {
        IsTagsVisible = !IsTagsVisible;
    }

    partial void OnSelectedNoteChanged(Note? value)
    {
        if (value != null)
        {
            TitleTextBox = value.Title;
        }
        else
        {
            TitleTextBox = "Choose a note to open, or create a new one!";
            IsEditing = false;
        }
    }
}