// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.ViewModels;

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISTAvalon.Models;
using ISTAvalon.Services;

public class MainWindowViewModel : ObservableObject
{
    private CommandTabViewModel? _selectedTab;
    private bool _isDarkTheme;
    private readonly GuiSettings _settings;
    private AppMetadata AppMetadata { get; }

    public ObservableCollection<CommandTabViewModel> CommandTabs { get; }

    public string WindowTitle => AppMetadata.WindowTitle;

    public CommandTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeTooltip));
                ApplyTheme();
                _settings.Theme = value ? "Dark" : "Default";
                GuiSettingsService.Save(_settings);
            }
        }
    }

    public string ThemeIcon => _isDarkTheme ? "fa-solid fa-sun" : "fa-solid fa-moon";

    public string ThemeTooltip => _isDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";

    public ICommand ToggleThemeCommand { get; }

    public MainWindowViewModel(IReadOnlyList<Models.CommandDescriptor>? descriptors = null)
    {
        AppMetadata = AppMetadataProvider.Get();
        _settings = GuiSettingsService.Load();
        descriptors ??= CommandDiscoveryService.DiscoverCommands();
        CommandTabs = new ObservableCollection<CommandTabViewModel>(
            descriptors.Select(d => new CommandTabViewModel(d, _settings.GetPresetFor(d.Name))));
        SelectedTab = CommandTabs.FirstOrDefault();

        ToggleThemeCommand = new RelayCommand(() => IsDarkTheme = !IsDarkTheme);

        _isDarkTheme = string.Equals(_settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Default;
        }
    }
}
