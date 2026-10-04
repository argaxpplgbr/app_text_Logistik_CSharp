namespace LogistikApp;

public class LogLogin
{
    public string Username { get; }
    public DateTime Waktu { get; } 
    public bool Berhasil { get; }
    public string Keterangan { get; }

    public LogLogin(string username, DateTime waktu, bool berhasil, string keterangan)
    {
        Username = username;
        Waktu = waktu;
        Berhasil = berhasil;
        Keterangan = keterangan;
    }
}