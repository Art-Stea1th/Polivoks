namespace Polivoks.Desktop.Services;

public static class AppDataRoot
{
    public static string Resolve()
    {
        var current = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(current.FullName, "Polivoks.slnx")))
            {
                return System.IO.Path.Combine(current.FullName, "AppData");
            }

            current = current.Parent;
        }

        return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Polivoks");
    }
}
