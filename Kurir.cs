namespace LogistikApp;

public class Kurir : IPengantar, IDapatDiringkas
{
    private readonly List<Paket> _paketDitangani = new();

    public string IdKurir { get; }
    public string Nama { get; }
    public Kendaraan JenisKendaraan { get; }
    public IReadOnlyList<Paket> PaketDitangani => _paketDitangani;

    public Kurir(string idKurir, string nama, Kendaraan jenisKendaraan)
    {
        if (string.IsNullOrWhiteSpace(idKurir))
            throw new ArgumentException("ID kurir tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(nama))
            throw new ArgumentException("Nama kurir tidak boleh kosong.");
        if (jenisKendaraan == null)
            throw new ArgumentException("Jenis kendaraan tidak boleh kosong.");

        IdKurir = idKurir.Trim();
        Nama = nama.Trim();
        JenisKendaraan = jenisKendaraan;
    }

    public bool BisaMenerima(Paket paket, out string alasan)
    {
        if (_paketDitangani.Count >= JenisKendaraan.MaksJumlahPaket)
        {
            alasan = $"Kurir {Nama} sudah mencapai batas {JenisKendaraan.MaksJumlahPaket} paket ({JenisKendaraan.Nama}).";
            return false;
        }

        if (!JenisKendaraan.BisaMengangkut(paket.Berat))
        {
            alasan = $"Berat paket {paket.Berat} kg melebihi batas {JenisKendaraan.MaksBeratPerPaket} kg untuk {JenisKendaraan.Nama}.";
            return false;
        }

        alasan = "";
        return true;
    }

    public void AmbilPaket(Paket paket)
    {
        if (paket.KurirAktif != null)
            throw new InvalidOperationException("Paket sudah memiliki kurir aktif.");

        if (!BisaMenerima(paket, out string alasan))
            throw new InvalidOperationException(alasan);

        paket.TugaskanKurir(this);
        _paketDitangani.Add(paket);
    }

    public void AntarPaket(Paket paket)
    {
        if (!_paketDitangani.Remove(paket))
            throw new InvalidOperationException("Paket ini tidak sedang ditangani kurir tersebut.");

        paket.LepasKurir();
    }

    public void BatalkanTugas(Paket paket)
    {
        if (!_paketDitangani.Remove(paket))
            throw new InvalidOperationException("Paket ini tidak sedang ditangani kurir tersebut.");

        paket.LepasKurir();
    }

    // Dipakai saat memuat data dari JSON (tanpa mencatat riwayat baru)
    internal void PulihkanPaket(Paket paket)
    {
        paket.PulihkanKurir(this);
        _paketDitangani.Add(paket);
    }

    public string BuatRingkasan()
    {
        return $"Kurir {IdKurir} | {Nama} | {JenisKendaraan.Nama} | Paket aktif: {_paketDitangani.Count}/{JenisKendaraan.MaksJumlahPaket}";
    }
}