
public class RiwayatPengiriman
{
    public StatusPengiriman Status { get; }
    public DateTime Waktu { get; }
    public string Keterangan { get; }

    public RiwayatPengiriman(StatusPengiriman status, string keterangan)
    {
        Status = status;
        Waktu = DateTime.Now;
        Keterangan = keterangan;
    }

    // Dipakai saat memuat data dari JSON (waktu asli dipertahankan)
    public RiwayatPengiriman(StatusPengiriman status, DateTime waktu, string keterangan)
    {
        Status = status;
        Waktu = waktu;
        Keterangan = keterangan;
    }
}