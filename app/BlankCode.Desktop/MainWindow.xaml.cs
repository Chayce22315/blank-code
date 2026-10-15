using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BlankCode;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProjectFile> _files = new();
    private string? _projectPath;
    private string? _activeFilePath;
    private bool _isLoadingEditor;
    private bool _isDirty;

    public MainWindow()
    {
        InitializeComponent();
        FileList.ItemsSource = _files;
    }

    private void ShowView(UIElement view)
    {
        WelcomeView.Visibility = Visibility.Collapsed;
        AuthView.Visibility = Visibility.Collapsed;
        WorkspaceView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
    }

    private void ShowAuth_Click(object sender, RoutedEventArgs e) => ShowView(AuthView);
    private void BackToWelcome_Click(object sender, RoutedEventArgs e) => ShowView(WelcomeView);

    private void CreateProject_Click(object sender, RoutedEventArgs e)
    {
        var folder = new OpenFolderDialog { Title = "choose where to create your project", Multiselect = false };
        if (folder.ShowDialog(this) != true) return;
        var nameDialog = new ProjectNameDialog { Owner = this };
        if (nameDialog.ShowDialog() != true) return;

        var name = string.Join("_", nameDialog.ProjectName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "new-project";
        var path = Path.Combine(folder.FolderName, name);
        try
        {
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "README.md"), $"# {name}\n\ncreated with blank code.\n\nstart building something real.\n");
            OpenProject(path);
        }
        catch (Exception ex) { ShowError("could not create the project", ex); }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "open a project folder", Multiselect = false };
        if (dialog.ShowDialog(this) == true) OpenProject(dialog.FolderName);
    }

    private void SampleProject_Click(object sender, RoutedEventArgs e)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BlankCode Samples", "hello-project");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "README.md"), "# hello-project\n\nwelcome to your first blank code sample.\n\n## try this\n\n- open main.py in the explorer\n- change the message\n- save the file\n");
            File.WriteAllText(Path.Combine(root, "main.py"), "def main():\n    message = \"hello from blank code!\"\n    print(message)\n\nif __name__ == \"__main__\":\n    main()\n");
            File.WriteAllText(Path.Combine(root, "ideas.md"), "# ideas\n\n- build a tiny game\n- add a score counter\n- make the game react to player input\n");
            OpenProject(root);
        }
        catch (Exception ex) { ShowError("could not create the sample project", ex); }
    }

    private void OpenProject(string path)
    {
        if (!Directory.Exists(path)) return;
        if (_isDirty && !SaveChangesIfNeeded()) return;
        _projectPath = path;
        _activeFilePath = null;
        ProjectTitleText.Text = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        ExplorerRootText.Text = ProjectTitleText.Text.ToUpperInvariant();
        RecentProjectsText.Text = path;
        _files.Clear();
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                _files.Add(new ProjectFile(Path.GetFileName(file), file));
        }
        catch (Exception ex) { ShowError("could not read project files", ex); }
        EditorText.Text = "";
        ActiveFileText.Text = "select a file";
        ShowView(WorkspaceView);
        OutputText.Text = $"opened local project: {path}\n\nchoose a file from the explorer to edit it.\nterminal and ai execution are not connected in this preview.";
        StatusText.Text = "local project opened";
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is not ProjectFile file) return;
        if (_isDirty && !SaveChangesIfNeeded()) return;
        try
        {
            _isLoadingEditor = true;
            _activeFilePath = file.FullPath;
            EditorText.Text = File.ReadAllText(file.FullPath);
            ActiveFileText.Text = file.Name;
            _isDirty = false;
            StatusText.Text = $"opened {file.Name}";
        }
        catch (Exception ex) { ShowError("could not open this file", ex); }
        finally { _isLoadingEditor = false; }
    }

    private void EditorText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingEditor || _activeFilePath is null) return;
        _isDirty = true;
        StatusText.Text = "unsaved changes";
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (_activeFilePath is null)
        {
            MessageBox.Show(this, "choose a file from the explorer first.", "blank code", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        SaveActiveFile();
    }

    private bool SaveActiveFile()
    {
        if (_activeFilePath is null) return true;
        try
        {
            File.WriteAllText(_activeFilePath, EditorText.Text);
            _isDirty = false;
            StatusText.Text = $"saved {Path.GetFileName(_activeFilePath)}";
            OutputText.Text = $"saved file:\n{_activeFilePath}";
            return true;
        }
        catch (Exception ex) { ShowError("could not save the file", ex); return false; }
    }

    private bool SaveChangesIfNeeded()
    {
        if (!_isDirty) return true;
        var result = MessageBox.Show(this, "save changes to the current file before switching?", "unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return false;
        return result != MessageBoxResult.Yes || SaveActiveFile();
    }

    private void AuthProvider_Click(object sender, RoutedEventArgs e)
    {
        var provider = (sender as Button)?.Tag?.ToString() ?? "provider";
        MessageBox.Show(this, $"{provider} sign-in is not connected yet.\n\nthis preview deliberately does not fake a login or store credentials. a hosted auth provider and secure oauth flow must be configured first.", "authentication not configured", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EmailAuth_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show(this, "email sign-in is a design preview only. no credentials were sent or saved.\n\nnext step: configure hosted auth, then implement verified sign-in and secure token storage.", "authentication not configured", MessageBoxButton.OK, MessageBoxImage.Information);

    private void AssistantPreview_Click(object sender, RoutedEventArgs e)
    {
        var prompt = AssistantPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt) || prompt == "what are we building?")
        {
            MessageBox.Show(this, "type an idea first, then send it once a model provider is connected.", "blank code", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        OutputText.Text = $"assistant preview received your idea:\n\n{prompt}\n\nno model is connected yet, so this was not sent to an external service.";
        StatusText.Text = "prompt kept local · model not connected";
    }

    private void FocusAssistant_Click(object sender, RoutedEventArgs e) => AssistantPrompt.Focus();
    private void SearchInfo_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "search is planned. this preview does not index or send your project files.", "search preview", MessageBoxButton.OK, MessageBoxImage.Information);
    private void GitInfo_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this, "git actions are not wired up yet. your files remain in their normal local folder.", "source control preview", MessageBoxButton.OK, MessageBoxImage.Information);

    private void ShowError(string title, Exception ex) =>
        MessageBox.Show(this, $"{title}:\n{ex.Message}", "blank code", MessageBoxButton.OK, MessageBoxImage.Error);

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isDirty && !SaveChangesIfNeeded()) { e.Cancel = true; return; }
        base.OnClosing(e);
    }
}

public sealed record ProjectFile(string Name, string FullPath)
{
    public override string ToString() => Name;
}

public sealed class ProjectNameDialog : Window
{
    private readonly TextBox _nameInput;
    public string ProjectName => _nameInput.Text.Trim();

    public ProjectNameDialog()
    {
        Title = "new project";
        Width = 390; Height = 220; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 25, 31));
        Foreground = System.Windows.Media.Brushes.White;
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        var layout = new StackPanel { Margin = new Thickness(22) };
        layout.Children.Add(new TextBlock { Text = "name your project", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        _nameInput = new TextBox { Text = "my-project", Padding = new Thickness(10), FontSize = 13 };
        layout.Children.Add(_nameInput);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var cancel = new Button { Content = "cancel", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 8, 14, 8) };
        cancel.Click += (_, _) => { DialogResult = false; };
        var create = new Button { Content = "create project", Padding = new Thickness(14, 8, 14, 8), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 159, 67)), Foreground = System.Windows.Media.Brushes.Black, FontWeight = FontWeights.SemiBold };
        create.Click += (_, _) => { DialogResult = true; };
        actions.Children.Add(cancel); actions.Children.Add(create); layout.Children.Add(actions);
        Content = layout;
        Loaded += (_, _) => { _nameInput.Focus(); _nameInput.SelectAll(); };
    }
}
