namespace LogistikApp;

public class Paket : IDapatDiringkas
{
    private readonly List<RiwayatPengiriman> _riwayat = new();

    public string IdPengiriman { get; }
    public string NamaPengirim { get; }
    public string NamaPenerima { get; }
    public string AlamatTujuan { get; private set; }
    public double Berat { get; }
    public JenisPengiriman Jenis { get; }
    public StatusPengiriman Status { get; private set; }
    public Kurir? KurirAktif { get; private set; }
    public IReadOnlyList<RiwayatPengiriman> Riwayat => _riwayat;
    public decimal Biaya => Jenis.HitungBiaya(Berat);

    // Tanggal dibuat = waktu riwayat pertama
    public DateTime TanggalDibuat => _riwayat[0].Waktu;
    public DateTime EstimasiTiba => Jenis.HitungTanggalTiba(TanggalDibuat, Berat);

    public Paket(string idPengiriman, string namaPengirim, string namaPenerima,
                 string alamatTujuan, double berat, JenisPengiriman jenis)
    {
        if (string.IsNullOrWhiteSpace(idPengiriman))
            throw new ArgumentException("ID pengiriman tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(namaPengirim))
            throw new ArgumentException("Nama pengirim tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(namaPenerima))
            throw new ArgumentException("Nama penerima tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(alamatTujuan))
            throw new ArgumentException("Alamat tujuan tidak boleh kosong.");
        if (berat <= 0)
            throw new ArgumentException("Berat paket harus lebih dari 0.");
        if (jenis == null)
            throw new ArgumentException("Jenis pengiriman tidak boleh kosong.");

        IdPengiriman = idPengiriman.Trim();
        NamaPengirim = namaPengirim.Trim();
        NamaPenerima = namaPenerima.Trim();
        AlamatTujuan = alamatTujuan.Trim();
        Berat = berat;
        Jenis = jenis;
        Status = StatusPengiriman.PaketDibuat;

        _riwayat.Add(new RiwayatPengiriman(Status, "Paket dibuat dan didaftarkan ke sistem"));
    }

    // Status hanya boleh maju satu tahap sesuai alur
    public bool UbahStatus(StatusPengiriman statusBaru, string keterangan, out string pesan)
    {
        if (Status == StatusPengiriman.Dibatalkan)
        {
            pesan = "Paket sudah dibatalkan, status tidak dapat diubah.";
            return false;
        }

        if (Status == StatusPengiriman.BerhasilDikirim)
        {
            pesan = "Paket sudah berhasil dikirim, status tidak dapat diubah lagi.";
            return false;
        }

        if (statusBaru == StatusPengiriman.Dibatalkan)
        {
            pesan = "Untuk membatalkan paket gunakan menu 'Batalkan paket'.";
            return false;
        }

        if ((int)statusBaru != (int)Status + 1)
        {
            StatusPengiriman berikutnya = (StatusPengiriman)((int)Status + 1);
            pesan = $"Status tidak boleh melompati/mundur. Status berikutnya yang diizinkan: {berikutnya.KeTeks()}.";
            return false;
        }

        if (statusBaru >= StatusPengiriman.DijemputKurir && KurirAktif == null)
        {
            pesan = "Paket belum memiliki kurir. Tugaskan kurir terlebih dahulu (menu 5).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(keterangan))
            keterangan = $"Status diperbarui menjadi {statusBaru.KeTeks()}";

        if (KurirAktif != null)
            keterangan += $" (Kurir: {KurirAktif.Nama})";

        Status = statusBaru;
        _riwayat.Add(new RiwayatPengiriman(Status, keterangan));

        pesan = $"Status berhasil diperbarui menjadi: {statusBaru.KeTeks()}.";
        return true;
    }

    // Paket hanya bisa dibatalkan sebelum berstatus "Dalam perjalanan"
    public bool Batalkan(string alasan, out string pesan)
    {
        if (Status == StatusPengiriman.Dibatalkan)
        {
            pesan = "Paket sudah dibatalkan sebelumnya.";
            return false;
        }

        if (Status >= StatusPengiriman.DalamPerjalanan)
        {
            pesan = $"Paket tidak dapat dibatalkan karena sudah berstatus '{Status.KeTeks()}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(alasan))
        {
            pesan = "Alasan pembatalan tidak boleh kosong.";
            return false;
        }

        string keterangan = $"Dibatalkan: {alasan.Trim()}";
        if (KurirAktif != null)
            keterangan += $" (Kurir: {KurirAktif.Nama})";

        Status = StatusPengiriman.Dibatalkan;
        _riwayat.Add(new RiwayatPengiriman(Status, keterangan));

        pesan = "Paket berhasil dibatalkan.";
        return true;
    }

    // Alamat hanya bisa diubah sebelum paket dijemput kurir
    public bool UbahAlamat(string alamatBaru, out string pesan)
    {
        if (Status > StatusPengiriman.MenungguKurir)
        {
            pesan = "Alamat hanya dapat diubah sebelum paket dijemput kurir (dan paket belum dibatalkan).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(alamatBaru))
        {
            pesan = "Alamat baru tidak boleh kosong.";
            return false;
        }

        string alamatLama = AlamatTujuan;
        AlamatTujuan = alamatBaru.Trim();
        _riwayat.Add(new RiwayatPengiriman(Status, $"Alamat tujuan diubah dari '{alamatLama}' menjadi '{AlamatTujuan}'"));

        pesan = "Alamat tujuan berhasil diubah.";
        return true;
    }

    internal void TugaskanKurir(Kurir kurir)
    {
        if (KurirAktif != null)
            throw new InvalidOperationException("Paket sudah memiliki kurir aktif.");

        KurirAktif = kurir;
        _riwayat.Add(new RiwayatPengiriman(Status, $"Kurir {kurir.Nama} ({kurir.IdKurir}) ditugaskan"));
    }

    internal void LepasKurir()
    {
        KurirAktif = null;
    }

    // Dipakai saat memuat data dari JSON
    internal void PulihkanKondisi(StatusPengiriman status, IEnumerable<RiwayatPengiriman> riwayat)
    {
        Status = status;
        _riwayat.Clear();
        _riwayat.AddRange(riwayat);
    }

    internal void PulihkanKurir(Kurir kurir)
    {
        KurirAktif = kurir;
    }

    public string TeksEstimasiTiba()
    {
        if (Status == StatusPengiriman.Dibatalkan)
            return "- (paket dibatalkan)";

        if (Status == StatusPengiriman.BerhasilDikirim)
            return $"Sudah tiba pada {_riwayat[_riwayat.Count - 1].Waktu:dd/MM/yyyy HH:mm}";

        return EstimasiTiba.ToString("dd/MM/yyyy HH:mm");
    }

    public string BuatRingkasan()
    {
        return $"[{IdPengiriman}] {NamaPengirim} -> {NamaPenerima} | {Jenis.Nama} | {Berat} kg | {Status.KeTeks()} | {JenisPengiriman.Rupiah(Biaya)}";
    }

    public void Tampilkan()
    {
        Console.WriteLine($"ID Pengiriman  : {IdPengiriman}");
        Console.WriteLine($"Pengirim       : {NamaPengirim}");
        Console.WriteLine($"Penerima       : {NamaPenerima}");
        Console.WriteLine($"Alamat Tujuan  : {AlamatTujuan}");
        Console.WriteLine($"Berat          : {Berat} kg");
        Console.WriteLine($"Layanan        : {Jenis.Nama}");
        Console.WriteLine($"Estimasi       : {Jenis.EstimasiWaktu(Berat)}");
        Console.WriteLine($"Estimasi tiba  : {TeksEstimasiTiba()}");
        Console.WriteLine($"Biaya          : {JenisPengiriman.Rupiah(Biaya)}");
        Console.WriteLine($"Status         : {Status.KeTeks()}");
        Console.WriteLine($"Kurir          : {(KurirAktif != null ? KurirAktif.Nama + " (" + KurirAktif.IdKurir + ")" : "-")}");
    }
}