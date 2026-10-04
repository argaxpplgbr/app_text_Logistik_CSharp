using System.Globalization;
using System.Text;

namespace LogistikApp;

public static class InputHelper
{
    public static string BacaTeks(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            string input = (Console.ReadLine() ?? "").Trim();

            if (input.Length == 0)
            {
                Console.WriteLine("Input tidak boleh kosong.");
                continue;
            }

            return input;
        }
    }

    public static string BacaTeksOpsional(string label)
    {
        Console.Write($"{label}: ");
        return (Console.ReadLine() ?? "").Trim();
    }

    // Password tampil sebagai '*' dan tidak di-Trim
    public static string BacaPassword(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            string input = BacaTersembunyi();

            if (input.Length == 0)
            {
                Console.WriteLine("Input tidak boleh kosong.");
                continue;
            }

            return input;
        }
    }

    private static string BacaTersembunyi()
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo tombol = Console.ReadKey(true);

                if (tombol.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }

                if (tombol.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(tombol.KeyChar))
                {
                    sb.Append(tombol.KeyChar);
                    Console.Write('*');
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Input dialihkan (bukan terminal biasa): pakai ReadLine
            return Console.ReadLine() ?? "";
        }
    }

    public static double BacaBerat(string label)
    {
        while (true)
        {
            string input = BacaTeks(label).Replace(',', '.');

            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double berat)
                || double.IsNaN(berat) || double.IsInfinity(berat))
            {
                Console.WriteLine("Berat harus berupa angka.");
                continue;
            }

            if (berat <= 0)
            {
                Console.WriteLine("Berat harus lebih dari 0 (tidak boleh negatif).");
                continue;
            }

            return berat;
        }
    }

    public static int BacaPilihan(string label, int min, int max)
    {
        while (true)
        {
            string input = BacaTeks(label);

            if (int.TryParse(input, out int pilihan) && pilihan >= min && pilihan <= max)
                return pilihan;

            Console.WriteLine($"Masukkan angka antara {min} sampai {max}.");
        }
    }

    public static bool Konfirmasi(string label)
    {
        while (true)
        {
            string input = BacaTeks($"{label} (y/n)").ToLower();

            if (input == "y") return true;
            if (input == "n") return false;

            Console.WriteLine("Ketik 'y' untuk ya atau 'n' untuk tidak.");
        }
    }

    public static void TekanEnter()
    {
        Console.WriteLine();
        Console.Write("Tekan Enter untuk kembali ke menu...");
        Console.ReadLine();
    }
}