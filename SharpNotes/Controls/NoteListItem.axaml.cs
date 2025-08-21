using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpNotes.Controls;

public partial class NoteListItem : UserControl
{
    public NoteListItem()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}