using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using FileServer.WPF.Models;
using FileServer.WPF.Services;

namespace FileServer.WPF.Views;

public partial class PreviewWindow : Window
{
    private readonly FileItem _fileItem;
    private readonly ApiClient _apiClient;

    public PreviewWindow(FileItem fileItem)
    {
        InitializeComponent();
        _fileItem = fileItem;
        _apiClient = new ApiClient();
        DataContext = fileItem;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _apiClient.DownloadAndSaveAsync(_fileItem.Id, _fileItem.FileName, _fileItem.IsImage);
            MessageBox.Show($"✅ Файл сохранен:\nDownloads/Images/{_fileItem.FileName}",
                           "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}