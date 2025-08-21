using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpNotes.Controls;

public partial class NoteEditor : UserControl
{
    public NoteEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}