using System.Security.Cryptography;

namespace LogistikApp;

public class VerifikasiKode
{
    private const int DurasiDetik = 60;
    private const int MaksPercobaan = 3;

    public bool Jalankan()
    {
        string kode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        DateTime kedaluwarsa = DateTime.UtcNow.AddSeconds(DurasiDetik);

        Console.WriteLine();
        Console.WriteLine("=== VERIFIKASI ===");
        Console.WriteLine($"Kode verifikasi Anda: {kode}  (berlaku {DurasiDetik} detik)");
        Console.WriteLine("(Simulasi: pada sistem nyata kode dikirim lewat email/SMS)");
        Console.WriteLine();

        for (int percobaan = 1; percobaan <= MaksPercobaan; percobaan++)
        {
            string input = InputHelper.BacaTeks("Masukkan kode verifikasi");

            if (DateTime.UtcNow > kedaluwarsa)
            {
                Console.WriteLine("Kode verifikasi sudah kedaluwarsa.");
                return false;
            }

            if (input == kode)
                return true;

            Console.WriteLine($"Kode salah. Sisa percobaan: {MaksPercobaan - percobaan}.");
        }

        return false;
    }
}