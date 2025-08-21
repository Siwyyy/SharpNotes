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
    private readonly NotesService _notesService;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _titleTextBox = "Choose a note to open, or create a new one!";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Note> _notes = new();
    [ObservableProperty] private Note? _selectedNote;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isSaving;

    public MainWindowViewModel(NotesService notesService)
    {
        _notesService = notesService;
        LoadNotesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadNotes()
    {
        IsLoading = true;

        try
        {
            var notes = await _notesService.GetAllNotesAsync();
            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        // Anuluj poprzednie wyszukiwanie, jeśli jest w trakcie
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        
        IsLoading = true;

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
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectNote(Note note)
    {
        SelectedNote = note;
        IsEditing = false;
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
        
        IsSaving = true;
        
        try
        {
            await _notesService.UpdateNoteAsync(SelectedNote);
            IsEditing = false;
        }
        finally
        {
            IsSaving = false;
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
    }

    [RelayCommand]
    private async Task DeleteNote()
    {
        if (SelectedNote == null) return;

        await _notesService.DeleteNoteAsync(SelectedNote.Id);
        await LoadNotesCommand.ExecuteAsync(null);
        SelectedNote = null;
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