using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpNotes.Models;
using SharpNotes.Services;

namespace SharpNotes.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NotesService _notesService;

    [ObservableProperty] private string _titleTextBox = "Choose a note to open, or create a new one!";

    [ObservableProperty] private ObservableCollection<Note> _notes = new();

    [ObservableProperty] private Note? _selectedNote;

    [ObservableProperty] private bool _isLoading;

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

    partial void OnSelectedNoteChanged(Note? value)
    {
        if (value != null)
        {
            TitleTextBox = value.Title;
        }
        else
        {
            TitleTextBox = "Choose a note to open, or create a new one!";
        }
    }
}