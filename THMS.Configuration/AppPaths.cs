namespace THMS.Configuration
{
    public static class AppPaths
    {
        public static string Root { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tinker",
            "THMS");

        public static string WebView2 { get; } = Path.Combine(Root, "WebView2");

        public static string PlaidSecrets { get; } = Path.Combine(Root, "plaid.secrets.dat");

        public static string DatabaseFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "thms.db";

            var path = Path.IsPathRooted(fileName)
                ? fileName
                : Path.Combine(Root, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }
    }
}
