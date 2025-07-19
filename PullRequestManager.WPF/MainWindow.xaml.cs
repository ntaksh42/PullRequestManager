using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using AzureDevopsTool.Core.Models;
using AzureDevopsTool.Core.Services;

namespace AzureDevopsTool.WPF;

public partial class MainWindow : Window
{
    private IAzureDevOpsService? _azureDevOpsService;
    private IEnumerable<PullRequest>? _allPullRequests;
    private readonly ISettingsService _settingsService;
    private AppSettings _currentSettings;
    private string _currentUserName = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
        _currentSettings = new AppSettings();
        
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void LoadPullRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusTextBlock.Text = "Loading pull requests...";
            
            var config = new AzureDevOpsConfig
            {
                Organization = OrganizationTextBox.Text,
                Project = ProjectTextBox.Text,
                Repository = RepositoryTextBox.Text,
                PersonalAccessToken = PatPasswordBox.Password
            };

            if (string.IsNullOrWhiteSpace(config.Organization) || 
                string.IsNullOrWhiteSpace(config.Project) || 
                string.IsNullOrWhiteSpace(config.Repository) || 
                string.IsNullOrWhiteSpace(config.PersonalAccessToken))
            {
                MessageBox.Show("Please fill in all configuration fields.", "Configuration Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusTextBlock.Text = "Ready";
                return;
            }

            _azureDevOpsService = new AzureDevOpsService(config);
            
            // Get current user for "My PRs" quick filter
            if (string.IsNullOrEmpty(_currentUserName))
            {
                _currentUserName = Environment.UserName; // Fallback to Windows username
            }
            
            var authorFilter = string.IsNullOrWhiteSpace(AuthorTextBox.Text) ? null : AuthorTextBox.Text.Trim();
            var targetBranchFilter = string.IsNullOrWhiteSpace(TargetBranchTextBox.Text) ? null : TargetBranchTextBox.Text.Trim();
            var pullRequests = await _azureDevOpsService.GetPullRequestsAsync(config.Project, config.Repository, FromDatePicker.SelectedDate, authorFilter, targetBranchFilter);
            
            _allPullRequests = pullRequests;
            ApplyCurrentFilters();
            StatusTextBlock.Text = $"Loaded {pullRequests.Count()} pull requests";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading pull requests: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Error occurred";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_azureDevOpsService != null)
        {
            LoadPullRequestsButton_Click(sender, e);
        }
        else
        {
            MessageBox.Show("Please load pull requests first.", "No Configuration", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ColumnMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag != null)
        {
            var columnIndex = int.Parse(menuItem.Tag.ToString()!);
            var column = PullRequestsDataGrid.Columns[columnIndex];
            
            if (menuItem.IsChecked)
            {
                column.Visibility = Visibility.Visible;
            }
            else
            {
                column.Visibility = Visibility.Collapsed;
            }

            // Update settings immediately
            if (columnIndex < _currentSettings.ColumnVisibility.Length)
            {
                _currentSettings.ColumnVisibility[columnIndex] = menuItem.IsChecked;
            }
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    private void FromDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    private void AuthorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    private void TargetBranchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    private void ApplyCurrentFilters()
    {
        if (_allPullRequests == null) return;

        var filteredRequests = _allPullRequests.AsEnumerable();

        // Apply search filter
        var searchText = SearchTextBox.Text?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredRequests = filteredRequests.Where(pr =>
                pr.Title.ToLowerInvariant().Contains(searchText) ||
                pr.CreatedBy.ToLowerInvariant().Contains(searchText) ||
                pr.Description.ToLowerInvariant().Contains(searchText)
            );
        }

        // Apply status filter
        if (StatusComboBox.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "All")
        {
            var selectedStatus = statusItem.Content.ToString()!.ToLower();
            filteredRequests = filteredRequests.Where(pr => pr.Status.ToLowerInvariant().Contains(selectedStatus));
        }

        // Apply file extension filter
        var fileExtension = FileExtensionTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(fileExtension))
        {
            if (!fileExtension.StartsWith(".")) fileExtension = "." + fileExtension;
            filteredRequests = filteredRequests.Where(pr => 
                pr.ModifiedFiles.Any(file => file.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Apply minimum changes filter
        if (int.TryParse(MinChangesTextBox.Text, out int minChanges) && minChanges > 0)
        {
            filteredRequests = filteredRequests.Where(pr => pr.ChangedFiles >= minChanges);
        }

        var finalList = filteredRequests.ToList();
        PullRequestsDataGrid.ItemsSource = finalList;
        UpdateStatusWithFilter(finalList.Count, _allPullRequests.Count());
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        AuthorTextBox.Text = string.Empty;
        TargetBranchTextBox.Text = string.Empty;
        SearchTextBox.Text = string.Empty;
        FromDatePicker.SelectedDate = null;
        StatusComboBox.SelectedIndex = 0;
        FileExtensionTextBox.Text = string.Empty;
        MinChangesTextBox.Text = string.Empty;
        SavedSearchesComboBox.SelectedIndex = -1;
    }

    // Status ComboBox Filter
    private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    // File Extension Filter
    private void FileExtensionTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    // Min Changes Filter
    private void MinChangesTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyCurrentFilters();
    }

    // Quick Filter Buttons
    private void QuickFilter_MyPRs(object sender, RoutedEventArgs e)
    {
        AuthorTextBox.Text = _currentUserName;
        StatusComboBox.SelectedIndex = 1; // Active
    }

    private void QuickFilter_Active(object sender, RoutedEventArgs e)
    {
        StatusComboBox.SelectedIndex = 1; // Active
        AuthorTextBox.Text = string.Empty;
    }

    private void QuickFilter_NeedsReview(object sender, RoutedEventArgs e)
    {
        StatusComboBox.SelectedIndex = 1; // Active
        SearchTextBox.Text = "needs review";
    }

    private void QuickFilter_Approved(object sender, RoutedEventArgs e)
    {
        StatusComboBox.SelectedIndex = 1; // Active
        SearchTextBox.Text = "approved";
    }

    // Saved Searches
    private void SavedSearchesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedSearchesComboBox.SelectedItem is SavedSearch savedSearch)
        {
            LoadSavedSearch(savedSearch);
        }
    }

    private void SaveSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentSearch();
    }

    private void DeleteSearchButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedSearch();
    }

    private void UpdateStatusWithFilter(int filteredCount, int totalCount)
    {
        if (filteredCount == totalCount)
        {
            StatusTextBlock.Text = $"Loaded {totalCount} pull requests";
        }
        else
        {
            StatusTextBlock.Text = $"Showing {filteredCount} of {totalCount} pull requests";
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _currentSettings = await _settingsService.LoadSettingsAsync();
        
        // Ensure column visibility array is properly initialized
        if (_currentSettings.ColumnVisibility.Length != PullRequestsDataGrid.Columns.Count)
        {
            var newArray = new bool[PullRequestsDataGrid.Columns.Count];
            if (_currentSettings.ColumnVisibility.Length > 0)
            {
                Array.Copy(_currentSettings.ColumnVisibility, newArray, 
                    Math.Min(_currentSettings.ColumnVisibility.Length, newArray.Length));
            }
            // Default any new columns to visible
            for (int i = _currentSettings.ColumnVisibility.Length; i < newArray.Length; i++)
            {
                newArray[i] = true;
            }
            _currentSettings.ColumnVisibility = newArray;
        }
        
        RefreshSavedSearchesComboBox();
        ApplySettings(_currentSettings);
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveCurrentSettings();
        await _settingsService.SaveSettingsAsync(_currentSettings);
    }

    private void ApplySettings(AppSettings settings)
    {
        // Restore window position and size
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            Left = settings.WindowLeft;
            Top = settings.WindowTop;
        }

        // Restore form fields
        OrganizationTextBox.Text = settings.Organization;
        ProjectTextBox.Text = settings.Project;
        RepositoryTextBox.Text = settings.Repository;
        PatPasswordBox.Password = settings.PersonalAccessToken;
        AuthorTextBox.Text = settings.AuthorFilter;
        TargetBranchTextBox.Text = settings.TargetBranchFilter;
        FromDatePicker.SelectedDate = settings.FromDate;

        // Restore column visibility
        // Ensure the array is the right size for the current number of columns
        if (settings.ColumnVisibility.Length < PullRequestsDataGrid.Columns.Count)
        {
            // Extend array if needed (new columns default to visible)
            var newArray = new bool[PullRequestsDataGrid.Columns.Count];
            Array.Copy(settings.ColumnVisibility, newArray, settings.ColumnVisibility.Length);
            for (int i = settings.ColumnVisibility.Length; i < newArray.Length; i++)
            {
                newArray[i] = true; // Default new columns to visible
            }
            settings.ColumnVisibility = newArray;
            _currentSettings.ColumnVisibility = newArray;
        }

        for (int i = 0; i < PullRequestsDataGrid.Columns.Count && i < settings.ColumnVisibility.Length; i++)
        {
            PullRequestsDataGrid.Columns[i].Visibility = settings.ColumnVisibility[i] ? Visibility.Visible : Visibility.Collapsed;
            
            // Update menu item check state
            UpdateMenuItemCheckState(i, settings.ColumnVisibility[i]);
        }
    }

    private void SaveCurrentSettings()
    {
        _currentSettings.WindowWidth = Width;
        _currentSettings.WindowHeight = Height;
        _currentSettings.WindowLeft = Left;
        _currentSettings.WindowTop = Top;

        _currentSettings.Organization = OrganizationTextBox.Text;
        _currentSettings.Project = ProjectTextBox.Text;
        _currentSettings.Repository = RepositoryTextBox.Text;
        _currentSettings.PersonalAccessToken = PatPasswordBox.Password;
        _currentSettings.AuthorFilter = AuthorTextBox.Text;
        _currentSettings.TargetBranchFilter = TargetBranchTextBox.Text;
        _currentSettings.FromDate = FromDatePicker.SelectedDate;

        // Save column visibility
        // Ensure the array is the right size
        if (_currentSettings.ColumnVisibility.Length != PullRequestsDataGrid.Columns.Count)
        {
            _currentSettings.ColumnVisibility = new bool[PullRequestsDataGrid.Columns.Count];
        }
        
        for (int i = 0; i < PullRequestsDataGrid.Columns.Count; i++)
        {
            _currentSettings.ColumnVisibility[i] = PullRequestsDataGrid.Columns[i].Visibility == Visibility.Visible;
        }
    }

    private void UpdateMenuItemCheckState(int columnIndex, bool isVisible)
    {
        var menuItems = new[] { IdMenuItem, TitleMenuItem, StatusMenuItem, ApprovalMenuItem, CreatedByMenuItem, CreatedDateMenuItem, SourceBranchMenuItem, TargetBranchMenuItem, ModifiedFilesMenuItem };
        if (columnIndex >= 0 && columnIndex < menuItems.Length)
        {
            menuItems[columnIndex].IsChecked = isVisible;
        }
    }

    private void TitleHyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink hyperlink && hyperlink.Tag is PullRequest pullRequest)
        {
            try
            {
                if (!string.IsNullOrEmpty(pullRequest.Url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pullRequest.Url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PullRequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            ShowPullRequestDetails(selectedPr);
        }
        else
        {
            HidePullRequestDetails();
        }
    }

    private void ShowPullRequestDetails(PullRequest pullRequest)
    {
        DetailPane.Visibility = Visibility.Visible;
        
        // Basic info
        DetailTitle.Text = pullRequest.Title;
        DetailId.Text = $"#{pullRequest.Id}";
        DetailStatus.Text = $"Status: {pullRequest.Status}";
        DetailCreatedBy.Text = $"Created by: {pullRequest.CreatedBy}";
        DetailCreatedDate.Text = $"Created: {pullRequest.CreatedDate:yyyy/MM/dd HH:mm}";
        
        // Description
        DetailDescription.Text = string.IsNullOrEmpty(pullRequest.Description) 
            ? "No description provided" 
            : pullRequest.Description;
        
        // Branches
        DetailSourceBranch.Text = $"From: {pullRequest.SourceBranch}";
        DetailTargetBranch.Text = $"To: {pullRequest.TargetBranch}";
        
        // Status Information
        DetailBuildStatus.Text = $"{pullRequest.BuildStatusIcon} {pullRequest.BuildStatus}";
        DetailConflictStatus.Text = $"{pullRequest.ConflictIcon} {(pullRequest.HasMergeConflicts ? "Has conflicts" : "No conflicts")}";
        DetailCommentStatus.Text = $"💬 {pullRequest.CommentCount} total, {pullRequest.UnresolvedCommentCount} unresolved";
        
        // Changes
        DetailChangesSummary.Text = pullRequest.ChangesSummary;
        DetailModifiedFiles.Text = pullRequest.ModifiedFiles.Any() 
            ? string.Join(", ", pullRequest.ModifiedFiles) 
            : "No files modified";
        
        // Reviewers
        DetailApprovalStatus.Text = pullRequest.ApprovalStatus;
        DetailReviewers.Text = pullRequest.Reviewers.Any() 
            ? string.Join("\n", pullRequest.Reviewers.Select(r => $"• {r.DisplayName} ({GetVoteText(r.Vote)})"))
            : "No reviewers assigned";
        
        // Work Items
        DetailWorkItems.Text = pullRequest.RelatedWorkItems.Any() 
            ? string.Join("\n", pullRequest.RelatedWorkItems.Select(w => $"• #{w.Id}: {w.Title}"))
            : "No related work items";
    }

    private void HidePullRequestDetails()
    {
        DetailPane.Visibility = Visibility.Collapsed;
    }

    private string GetVoteText(int vote)
    {
        return vote switch
        {
            -10 => "Rejected",
            -5 => "Waiting for Author",
            0 => "No Vote",
            5 => "Approved with Suggestions",
            10 => "Approved",
            _ => "Unknown"
        };
    }

    private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedPr.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CopyUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            try
            {
                Clipboard.SetText(selectedPr.Url);
                MessageBox.Show("URL copied to clipboard!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Context Menu Handlers
    private void ContextMenu_OpenInBrowser(object sender, RoutedEventArgs e)
    {
        OpenSelectedPullRequestInBrowser();
    }

    private void ContextMenu_CopyUrl(object sender, RoutedEventArgs e)
    {
        CopySelectedPullRequestUrl();
    }

    private void ContextMenu_CopyPrId(object sender, RoutedEventArgs e)
    {
        CopySelectedPullRequestId();
    }

    private void ContextMenu_Refresh(object sender, RoutedEventArgs e)
    {
        RefreshButton_Click(sender, e);
    }

    // Keyboard Event Handler
    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle keyboard shortcuts
        if (e.Key == Key.F5)
        {
            RefreshButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && PullRequestsDataGrid.SelectedItem != null)
        {
            OpenSelectedPullRequestInBrowser();
            e.Handled = true;
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.C:
                    CopySelectedPullRequestUrl();
                    e.Handled = true;
                    break;
                case Key.F when e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift):
                    SearchTextBox.Focus();
                    e.Handled = true;
                    break;
            }
        }
        else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (e.Key == Key.C)
            {
                CopySelectedPullRequestId();
                e.Handled = true;
            }
        }
    }

    // Double Click Handler
    private void PullRequestsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest)
        {
            OpenSelectedPullRequestInBrowser();
        }
    }

    // Helper Methods
    private void OpenSelectedPullRequestInBrowser()
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedPr.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CopySelectedPullRequestUrl()
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            try
            {
                Clipboard.SetText(selectedPr.Url);
                StatusTextBlock.Text = "URL copied to clipboard";
                
                // Reset status message after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    StatusTextBlock.Text = "Ready";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Failed to copy URL: {ex.Message}";
            }
        }
    }

    private void CopySelectedPullRequestId()
    {
        if (PullRequestsDataGrid.SelectedItem is PullRequest selectedPr)
        {
            try
            {
                Clipboard.SetText(selectedPr.Id.ToString());
                StatusTextBlock.Text = $"PR ID #{selectedPr.Id} copied to clipboard";
                
                // Reset status message after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    StatusTextBlock.Text = "Ready";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Failed to copy PR ID: {ex.Message}";
            }
        }
    }

    // Saved Search Methods
    private void LoadSavedSearch(SavedSearch savedSearch)
    {
        AuthorTextBox.Text = savedSearch.AuthorFilter;
        TargetBranchTextBox.Text = savedSearch.TargetBranchFilter;
        SearchTextBox.Text = savedSearch.SearchText;
        FileExtensionTextBox.Text = savedSearch.FileExtension;
        MinChangesTextBox.Text = savedSearch.MinChanges > 0 ? savedSearch.MinChanges.ToString() : string.Empty;
        FromDatePicker.SelectedDate = savedSearch.FromDate;
        
        // Set status combobox
        if (!string.IsNullOrEmpty(savedSearch.Status))
        {
            for (int i = 0; i < StatusComboBox.Items.Count; i++)
            {
                if (StatusComboBox.Items[i] is ComboBoxItem item && 
                    item.Content.ToString()!.Equals(savedSearch.Status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void SaveCurrentSearch()
    {
        var dialog = new SaveSearchDialog();
        if (dialog.ShowDialog() == true)
        {
            var savedSearch = new SavedSearch
            {
                Name = dialog.SearchName,
                AuthorFilter = AuthorTextBox.Text,
                TargetBranchFilter = TargetBranchTextBox.Text,
                SearchText = SearchTextBox.Text,
                FileExtension = FileExtensionTextBox.Text,
                Status = StatusComboBox.SelectedItem is ComboBoxItem item ? item.Content.ToString()! : "All",
                FromDate = FromDatePicker.SelectedDate,
                CreatedDate = DateTime.Now
            };
            
            if (int.TryParse(MinChangesTextBox.Text, out int minChanges))
            {
                savedSearch.MinChanges = minChanges;
            }

            _currentSettings.SavedSearches.Add(savedSearch);
            RefreshSavedSearchesComboBox();
            SavedSearchesComboBox.SelectedItem = savedSearch;
        }
    }

    private void DeleteSelectedSearch()
    {
        if (SavedSearchesComboBox.SelectedItem is SavedSearch selectedSearch)
        {
            var result = MessageBox.Show($"Delete saved search '{selectedSearch.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _currentSettings.SavedSearches.Remove(selectedSearch);
                RefreshSavedSearchesComboBox();
            }
        }
    }

    private void RefreshSavedSearchesComboBox()
    {
        SavedSearchesComboBox.Items.Clear();
        foreach (var search in _currentSettings.SavedSearches.OrderBy(s => s.Name))
        {
            SavedSearchesComboBox.Items.Add(search);
        }
    }

}

// Simple dialog for saving searches
public partial class SaveSearchDialog : Window
{
    public string SearchName { get; private set; } = string.Empty;

    public SaveSearchDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Title = "Save Search";
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        var label = new Label { Content = "Search Name:", Margin = new Thickness(10) };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);
        
        var textBox = new TextBox { Name = "NameTextBox", Margin = new Thickness(10, 0, 10, 10) };
        Grid.SetRow(textBox, 1);
        grid.Children.Add(textBox);
        
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };
        var okButton = new Button { Content = "OK", Width = 60, Margin = new Thickness(5, 0, 0, 0), IsDefault = true };
        var cancelButton = new Button { Content = "Cancel", Width = 60, Margin = new Thickness(5, 0, 0, 0), IsCancel = true };
        
        okButton.Click += (s, e) => {
            SearchName = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(SearchName))
            {
                DialogResult = true;
                Close();
            }
        };
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        Content = grid;
        textBox.Focus();
    }
}