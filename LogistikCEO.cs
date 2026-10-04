namespace LogistikApp;

public class LogistikManager
{
    private readonly List<Paket> _daftarPaket = new();
    private readonly List<Kurir> _daftarKurir = new();

    private readonly List<JenisPengiriman> _daftarLayanan = new()
    {
        new PengirimanReguler(),
        new PengirimanExpress(),
        new PengirimanSameDay(),
        new PengirimanKargo()
    };

    private readonly List<Kendaraan> _daftarKendaraan = new()
    {
        new Motor(),
        new Mobil(),
        new Truk()
    };

    public IReadOnlyList<Paket> DaftarPaket => _daftarPaket;
    public IReadOnlyList<Kurir> DaftarKurir => _daftarKurir;
    public IReadOnlyList<JenisPengiriman> DaftarLayanan => _daftarLayanan;
    public IReadOnlyList<Kendaraan> DaftarKendaraan => _daftarKendaraan;

    // ===================== PENCARIAN =====================

    public Paket? CariPaket(string id)
    {
        foreach (Paket paket in _daftarPaket)
        {
            if (string.Equals(paket.IdPengiriman, id.Trim(), StringComparison.OrdinalIgnoreCase))
                return paket;
        }
        return null;
    }

    public List<Paket> CariPaketBerdasarkanNama(string kata)
    {
        List<Paket> hasil = new();
        string kunci = kata.Trim();

        foreach (Paket paket in _daftarPaket)
        {
            bool cocok = paket.NamaPengirim.Contains(kunci, StringComparison.OrdinalIgnoreCase)
                      || paket.NamaPenerima.Contains(kunci, StringComparison.OrdinalIgnoreCase);

            if (cocok)
                hasil.Add(paket);
        }

        return hasil;
    }

    public List<Paket> FilterPaket(StatusPengiriman status)
    {
        List<Paket> hasil = new();

        foreach (Paket paket in _daftarPaket)
        {
            if (paket.Status == status)
                hasil.Add(paket);
        }

        return hasil;
    }

    public Kurir? CariKurir(string id)
    {
        foreach (Kurir kurir in _daftarKurir)
        {
            if (string.Equals(kurir.IdKurir, id.Trim(), StringComparison.OrdinalIgnoreCase))
                return kurir;
        }
        return null;
    }

    public JenisPengiriman? CariLayanan(string nama)
    {
        foreach (JenisPengiriman layanan in _daftarLayanan)
        {
            if (string.Equals(layanan.Nama, nama.Trim(), StringComparison.OrdinalIgnoreCase))
                return layanan;
        }
        return null;
    }

    public Kendaraan? CariKendaraan(string nama)
    {
        foreach (Kendaraan kendaraan in _daftarKendaraan)
        {
            if (string.Equals(kendaraan.Nama, nama.Trim(), StringComparison.OrdinalIgnoreCase))
                return kendaraan;
        }
        return null;
    }

    // ===================== TAMBAH / HAPUS =====================

    public bool TambahPaket(Paket paket, out string pesan)
    {
        if (CariPaket(paket.IdPengiriman) != null)
        {
            pesan = "ID pengiriman sudah digunakan.";
            return false;
        }

        _daftarPaket.Add(paket);
        pesan = "Paket berhasil ditambahkan.";
        return true;
    }

    public bool TambahKurir(Kurir kurir, out string pesan)
    {
        if (CariKurir(kurir.IdKurir) != null)
        {
            pesan = "ID kurir sudah digunakan.";
            return false;
        }

        _daftarKurir.Add(kurir);
        pesan = "Kurir berhasil ditambahkan.";
        return true;
    }

    public bool HapusKurir(string idKurir, out string pesan)
    {
        Kurir? kurir = CariKurir(idKurir);
        if (kurir == null)
        {
            pesan = "Kurir tidak ditemukan.";
            return false;
        }

        if (kurir.PaketDitangani.Count > 0)
        {
            pesan = $"Kurir {kurir.Nama} masih menangani {kurir.PaketDitangani.Count} paket aktif, tidak dapat dihapus.";
            return false;
        }

        _daftarKurir.Remove(kurir);
        pesan = $"Kurir {kurir.Nama} berhasil dihapus.";
        return true;
    }

    // ===================== PENUGASAN KURIR =====================

    private static bool PaketSiapDitugaskan(Paket paket, out string pesan)
    {
        if (paket.KurirAktif != null)
        {
            pesan = $"Paket sudah ditangani kurir {paket.KurirAktif.Nama}. Satu paket hanya boleh punya satu kurir aktif.";
            return false;
        }

        if (paket.Status != StatusPengiriman.MenungguKurir)
        {
            pesan = $"Kurir hanya dapat ditugaskan saat status 'Menunggu kurir'. Status saat ini: {paket.Status.KeTeks()}.";
            return false;
        }

        pesan = "";
        return true;
    }

    private static bool Tugaskan(Paket paket, Kurir kurir, out string pesan)
    {
        IPengantar pengantar = kurir; // dipakai lewat interface

        if (!pengantar.BisaMenerima(paket, out string alasan))
        {
            pesan = alasan;
            return false;
        }

        pengantar.AmbilPaket(paket);
        pesan = $"Kurir {kurir.Nama} berhasil ditugaskan untuk paket {paket.IdPengiriman}.";
        return true;
    }

    public bool TugaskanKurir(string idPaket, string idKurir, out string pesan)
    {
        Paket? paket = CariPaket(idPaket);
        if (paket == null)
        {
            pesan = "Paket tidak ditemukan.";
            return false;
        }

        Kurir? kurir = CariKurir(idKurir);
        if (kurir == null)
        {
            pesan = "Kurir tidak ditemukan.";
            return false;
        }

        if (!PaketSiapDitugaskan(paket, out pesan))
            return false;

        return Tugaskan(paket, kurir, out pesan);
    }

  
    public Kurir? PilihKurirOtomatis(Paket paket)
    {
        Kurir? terpilih = null;

        foreach (Kurir kurir in _daftarKurir)
        {
            IPengantar pengantar = kurir;
            if (!pengantar.BisaMenerima(paket, out _))
                continue;

            if (terpilih == null || kurir.PaketDitangani.Count < terpilih.PaketDitangani.Count)
                terpilih = kurir;
        }

        return terpilih;
    }

    public bool TugaskanKurirOtomatis(string idPaket, out string pesan)
    {
        Paket? paket = CariPaket(idPaket);
        if (paket == null)
        {
            pesan = "Paket tidak ditemukan.";
            return false;
        }

        if (!PaketSiapDitugaskan(paket, out pesan))
            return false;

        Kurir? kurir = PilihKurirOtomatis(paket);
        if (kurir == null)
        {
            pesan = "Tidak ada kurir yang tersedia atau mampu membawa paket ini (cek kapasitas kendaraan).";
            return false;
        }

        return Tugaskan(paket, kurir, out pesan);
    }

    // ===================== STATUS, BATAL, EDIT =====================

    public bool PerbaruiStatus(string idPaket, StatusPengiriman statusBaru, string keterangan, out string pesan)
    {
        Paket? paket = CariPaket(idPaket);
        if (paket == null)
        {
            pesan = "Paket tidak ditemukan.";
            return false;
        }

        IPengantar? pengantar = paket.KurirAktif; 

        bool berhasil = paket.UbahStatus(statusBaru, keterangan, out pesan);

        if (berhasil && statusBaru == StatusPengiriman.BerhasilDikirim && pengantar != null)
        {
            pengantar.AntarPaket(paket); 
        }

        return berhasil;
    }

    public bool BatalkanPaket(string idPaket, string alasan, out string pesan)
    {
        Paket? paket = CariPaket(idPaket);
        if (paket == null)
        {
            pesan = "Paket tidak ditemukan.";
            return false;
        }

        IPengantar? pengantar = paket.KurirAktif; 

        bool berhasil = paket.Batalkan(alasan, out pesan);

        if (berhasil && pengantar != null)
        {
            pengantar.BatalkanTugas(paket); 
        }

        return berhasil;
    }

    public bool UbahAlamat(string idPaket, string alamatBaru, out string pesan)
    {
        Paket? paket = CariPaket(idPaket);
        if (paket == null)
        {
            pesan = "Paket tidak ditemukan.";
            return false;
        }

        return paket.UbahAlamat(alamatBaru, out pesan);
    }

    // ===================== STATISTIK DASHBOARD =====================

    public int HitungPaketDenganStatus(StatusPengiriman status)
    {
        int jumlah = 0;

        foreach (Paket paket in _daftarPaket)
        {
            if (paket.Status == status)
                jumlah++;
        }

        return jumlah;
    }

    public decimal HitungPendapatanTerealisasi()
    {
        decimal total = 0;

        foreach (Paket paket in _daftarPaket)
        {
            if (paket.Status == StatusPengiriman.BerhasilDikirim)
                total += paket.Biaya;
        }

        return total;
    }

    
    public decimal HitungPotensiPendapatan()
    {
        decimal total = 0;

        foreach (Paket paket in _daftarPaket)
        {
            if (paket.Status != StatusPengiriman.BerhasilDikirim && paket.Status != StatusPengiriman.Dibatalkan)
                total += paket.Biaya;
        }

        return total;
    }

    
    public Kurir? KurirTersibuk()
    {
        Kurir? tersibuk = null;

        foreach (Kurir kurir in _daftarKurir)
        {
            if (kurir.PaketDitangani.Count == 0)
                continue;

            if (tersibuk == null || kurir.PaketDitangani.Count > tersibuk.PaketDitangani.Count)
                tersibuk = kurir;
        }

        return tersibuk;
    }
}