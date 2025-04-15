// Models/ProtectedFolder.cs
public class ProtectedFolder
{
    public string Path { get; set; }
    public DateTime DateAdded { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }

    public ProtectedFolder(string path, bool isPremium = false)
    {
        Path = path;
        DateAdded = DateTime.Now;
        IsPremium = isPremium;
        IsActive = true;
    }
}