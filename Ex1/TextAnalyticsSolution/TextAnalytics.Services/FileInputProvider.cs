using System;
using System.IO;

namespace TextAnalytics.Services;

public sealed class FileInputProvider : IInputProvider
{
    private readonly string _path;
    public FileInputProvider(string path) => _path = path ?? string.Empty;

    public string GetInput()
    {
        if (string.IsNullOrWhiteSpace(_path))
            throw new ArgumentException("Ścieżka pliku jest pusta.");

        if (!File.Exists(_path))
            throw new FileNotFoundException("Plik nie istnieje.", _path);

        return File.ReadAllText(_path);
    }
}