using System.Text;
using THMS.Configuration;

class Program
{
    static void Main()
    {
        Console.Write("Plaid Sandbox Secret: ");
        var sandboxSecret = Console.ReadLine() ?? string.Empty;

        Console.Write("Plaid Production Secret: ");
        var productionSecret = Console.ReadLine() ?? string.Empty;

        var sandboxProtected = SecretProtector.Protect(sandboxSecret);
        var productionProtected = SecretProtector.Protect(productionSecret);

        var path = Path.Combine(AppContext.BaseDirectory, "plaid.secrets.dat");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs, Encoding.UTF8);

        bw.Write(sandboxProtected.Length);
        bw.Write(sandboxProtected);

        bw.Write(productionProtected.Length);
        bw.Write(productionProtected);

        Console.WriteLine($"Encrypted secrets written to: {path}");
    }
}
