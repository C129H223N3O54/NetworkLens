using System.Windows;
using System.Windows.Input;

namespace NetworkLens.Views;

public partial class AliasDialog : Window
{
    public string AliasText => TxtAlias.Text.Trim();

    public AliasDialog(string currentAlias, string deviceName)
    {
        InitializeComponent();
        TxtAlias.Text = currentAlias;
        TxtDeviceName.Text = $"{Localization.LocalizationManager.Instance.T("Dlg_Device")}: {deviceName}";
        Loaded += (_, _) =>
        {
            TxtAlias.Focus();
            TxtAlias.SelectAll();
        };
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TxtAlias_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnSave_Click(sender, new RoutedEventArgs());
        if (e.Key == Key.Escape) BtnCancel_Click(sender, new RoutedEventArgs());
    }
}
