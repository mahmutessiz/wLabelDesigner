using System.Windows;

namespace wLabelDesigner;

using wLabelDesigner.Services;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => WpfLanguageService.Instance.Apply(this);
    }
}
