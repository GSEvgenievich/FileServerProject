using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FileServer.WPF.Models;

public class FileItem : INotifyPropertyChanged
{
    private byte[]? _fullImageData;
    private bool _isFullImageLoaded;

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    [JsonPropertyName("isImage")]
    public bool IsImage { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? ThumbnailBase64 { get; set; }

    [JsonIgnore]
    public byte[]? FullImageData
    {
        get => _fullImageData;
        set { _fullImageData = value; IsFullImageLoaded = true; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public bool IsFullImageLoaded
    {
        get => _isFullImageLoaded;
        set { _isFullImageLoaded = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string FormattedSize => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F2} KB",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024):F2} MB",
        _ => $"{Size / (1024.0 * 1024 * 1024):F2} GB"
    };

    [JsonIgnore]
    public string DisplayIcon
    {
        get
        {
            if (IsImage)
            {
                // Для изображений определяем по расширению или content-type
                var ext = GetFileExtension().ToLower();
                return ext switch
                {
                    ".jpg" or ".jpeg" => "🖼️",
                    ".png" => "🖼️",
                    ".gif" => "🎬",
                    ".bmp" => "🖼️",
                    ".webp" => "🖼️",
                    ".svg" => "📐",
                    ".ico" => "🎨",
                    _ => "🖼️"
                };
            }

            // Определяем по расширению файла
            var extension = GetFileExtension().ToLower();

            return extension switch
            {
                // Документы
                ".pdf" => "📕",
                ".doc" => "📄",
                ".docx" => "📄",
                ".txt" => "📝",
                ".rtf" => "📝",
                ".md" => "📝",
                ".log" => "📋",

                // Таблицы
                ".xls" => "📊",
                ".xlsx" => "📊",
                ".csv" => "📊",
                ".ods" => "📊",

                // Презентации
                ".ppt" => "📽️",
                ".pptx" => "📽️",
                ".odp" => "📽️",

                // Архивы
                ".zip" => "📦",
                ".rar" => "📦",
                ".7z" => "📦",
                ".tar" => "📦",
                ".gz" => "📦",
                ".bz2" => "📦",

                // Код
                ".cs" => "💻",
                ".js" => "💻",
                ".ts" => "💻",
                ".py" => "💻",
                ".java" => "💻",
                ".cpp" or ".c" or ".h" => "💻",
                ".html" => "🌐",
                ".css" => "🎨",
                ".json" => "📋",
                ".xml" => "📋",
                ".yaml" or ".yml" => "📋",
                ".sql" => "🗄️",

                // Медиа
                ".mp3" => "🎵",
                ".wav" => "🎵",
                ".flac" => "🎵",
                ".aac" => "🎵",
                ".ogg" => "🎵",
                ".mp4" => "🎬",
                ".avi" => "🎬",
                ".mkv" => "🎬",
                ".mov" => "🎬",
                ".wmv" => "🎬",
                ".flv" => "🎬",
                ".webm" => "🎬",

                // Системные и прочие
                ".exe" => "⚙️",
                ".msi" => "⚙️",
                ".dll" => "🔧",
                ".iso" => "💿",
                ".torrent" => "🔗",
                ".psd" => "🎨",
                ".ai" => "🎨",
                ".eps" => "🎨",
                ".ttf" or ".otf" or ".woff" => "🔤",
                ".db" or ".sqlite" => "🗄️",

                _ => "📎"
            };
        }
    }

    [JsonIgnore]
    public string FileTypeDescription
    {
        get
        {
            var ext = GetFileExtension().ToUpper().TrimStart('.');
            return string.IsNullOrEmpty(ext) ? "Файл" : $"{ext} Файл";
        }
    }

    private string GetFileExtension()
    {
        return Path.GetExtension(FileName);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}