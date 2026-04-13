using FileServer.WPF.Models;
using FileServer.WPF.Services;
using FileServer.WPF.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FileServer.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _apiClient;
    private ObservableCollection<FileItem> _files = new();
    private string _statusMessage = "Готов";
    private string _notificationMessage = string.Empty;
    private bool _showNotification;
    private double _uploadProgress;
    private bool _isUploading;

    public MainViewModel()
    {
        _apiClient = new ApiClient();

        UploadCommand = new RelayCommand(async _ => await UploadFileAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadFilesAsync());
        DownloadCommand = new RelayCommand<FileItem>(async f => await DownloadFileAsync(f));
        ViewImageCommand = new RelayCommand<FileItem>(async f => await ViewImageAsync(f));
        DeleteCommand = new RelayCommand<FileItem>(async f => await DeleteFileAsync(f));

        Task.Run(LoadFilesAsync);
        StartNotificationTimer();
    }

    public ObservableCollection<FileItem> Files
    {
        get => _files;
        set { _files = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string NotificationMessage
    {
        get => _notificationMessage;
        set { _notificationMessage = value; OnPropertyChanged(); }
    }

    public bool ShowNotification
    {
        get => _showNotification;
        set { _showNotification = value; OnPropertyChanged(); }
    }

    public double UploadProgress
    {
        get => _uploadProgress;
        set { _uploadProgress = value; OnPropertyChanged(); }
    }

    public bool IsUploading
    {
        get => _isUploading;
        set { _isUploading = value; OnPropertyChanged(); }
    }

    public ICommand UploadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand ViewImageCommand { get; }
    public ICommand DeleteCommand { get; }

    private async Task LoadFilesAsync()
    {
        try
        {
            StatusMessage = "📂 Загрузка списка файлов...";
            var files = await _apiClient.GetFilesAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Files.Clear();
                foreach (var file in files)
                    Files.Add(file);
            });

            StatusMessage = $"✅ Загружено файлов: {Files.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task UploadFileAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите файл для загрузки",
            Filter = "Все файлы|*.*|Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Документы|*.pdf;*.doc;*.docx;*.txt|Архивы|*.zip"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            IsUploading = true;
            StatusMessage = $"📤 Загрузка: {System.IO.Path.GetFileName(dialog.FileName)}";

            await _apiClient.UploadFileAsync(dialog.FileName, p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UploadProgress = p;
                    StatusMessage = $"📤 Загрузка: {System.IO.Path.GetFileName(dialog.FileName)} ({p:F0}%)";
                });
            });

            UploadProgress = 0;
            await LoadFilesAsync();
            ShowTemporaryNotification("✅ Файл успешно загружен");
        }
        catch (Exception ex)
        {
            ShowTemporaryNotification($"❌ Ошибка: {ex.Message}");
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task DownloadFileAsync(FileItem file)
    {
        if (file == null) return;

        try
        {
            StatusMessage = $"📥 Скачивание: {file.FileName}";
            await _apiClient.DownloadAndSaveAsync(file.Id, file.FileName, file.IsImage);
            ShowTemporaryNotification($"✅ Сохранено: {file.FileName}");
            StatusMessage = "✅ Готово";
        }
        catch (Exception ex)
        {
            ShowTemporaryNotification($"❌ {ex.Message}");
            StatusMessage = $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task ViewImageAsync(FileItem file)
    {
        if (file == null || !file.IsImage) return;

        try
        {
            if (!file.IsFullImageLoaded)
            {
                StatusMessage = $"🖼️ Загрузка: {file.FileName}";
                file.FullImageData = await _apiClient.GetFullFileAsync(file.Id);
                StatusMessage = "✅ Готово";
            }

            var previewWindow = new PreviewWindow(file);
            previewWindow.Owner = Application.Current.MainWindow;
            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowTemporaryNotification($"❌ {ex.Message}");
        }
    }

    private async Task DeleteFileAsync(FileItem file)
    {
        if (file == null) return;

        var result = MessageBox.Show(
            $"Удалить файл?\n{file.FileName}",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            StatusMessage = $"🗑️ Удаление: {file.FileName}";
            await _apiClient.DeleteFileAsync(file.Id);
            Files.Remove(file);
            ShowTemporaryNotification($"✅ Удалено: {file.FileName}");
            StatusMessage = $"✅ Файлов: {Files.Count}";
        }
        catch (Exception ex)
        {
            ShowTemporaryNotification($"❌ {ex.Message}");
        }
    }

    private void ShowTemporaryNotification(string message)
    {
        NotificationMessage = message;
        ShowNotification = true;
    }

    private void StartNotificationTimer()
    {
        var timer = new System.Timers.Timer(3000);
        timer.Elapsed += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() => ShowNotification = false);
            timer.Stop();
        };

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ShowNotification) && ShowNotification)
            {
                timer.Stop();
                timer.Start();
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    public RelayCommand(Action<T?> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}