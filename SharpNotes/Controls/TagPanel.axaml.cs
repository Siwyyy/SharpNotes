using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpNotes.Controls;

public partial class TagPanel : UserControl
{
    public TagPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}