using System.Security.Cryptography;

namespace LogistikApp;

public static class PasswordHasher
{
    private const int UkuranSalt = 16;
    private const int UkuranHash = 32;
    private const int Iterasi = 210000;

    
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(UkuranSalt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterasi, HashAlgorithmName.SHA256, UkuranHash);

        return $"{Iterasi}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verifikasi(string password, string hashTersimpan)
    {
        string[] bagian = hashTersimpan.Split('.');
        if (bagian.Length != 3 || !int.TryParse(bagian[0], out int iterasi))
            return false;

        byte[] salt;
        byte[] hashAsli;

        try
        {
            salt = Convert.FromBase64String(bagian[1]);
            hashAsli = Convert.FromBase64String(bagian[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] hashHitung = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterasi, HashAlgorithmName.SHA256, hashAsli.Length);

       
        return CryptographicOperations.FixedTimeEquals(hashHitung, hashAsli);
    }
}