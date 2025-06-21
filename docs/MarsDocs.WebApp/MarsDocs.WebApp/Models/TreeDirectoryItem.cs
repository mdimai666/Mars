namespace MarsDocs.WebApp.Models;

public record TreeDirectoryItem(string FullPath, string Name, bool IsDir, TreeDirectoryItem[]? Items = null);
