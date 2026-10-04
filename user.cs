namespace LogistikApp;

public class Pengguna
{
    public int Id { get; }
    public string Username { get; }
    public string NamaLengkap { get; }
    public PeranPengguna Peran { get; }

    // Hash password hanya bisa dibaca di dalam project (encapsulation)
    internal string PasswordHash { get; }

    public Pengguna(int id, string username, string namaLengkap, PeranPengguna peran, string passwordHash)
    {
        Id = id;
        Username = username;
        NamaLengkap = namaLengkap;
        Peran = peran;
        PasswordHash = passwordHash;
    }
}