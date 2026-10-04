using Microsoft.Data.Sqlite;

namespace LogistikApp;

public class Program
{
    private const int MaksPercobaanLogin = 3;

    private static readonly Database database = new();
    private static readonly PenggunaRepository penggunaRepo = new(database);
    private static readonly AuthService auth = new(penggunaRepo);
    private static readonly LogistikRepository penyimpanan = new(database);
    private static readonly LogistikManager manager = new();
    private static readonly LaporanExporter exporter = new();

    private static Pengguna penggunaSaatIni = null!;

    public static void Main()
    {
        try
        {
            database.Inisialisasi();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Gagal menyiapkan database: {ex.Message}");
            return;
        }

        // Login + verifikasi wajib setiap program dijalankan
        Pengguna? pengguna = Autentikasi();
        if (pengguna == null)
        {
            Console.WriteLine();
            Console.WriteLine("Akses ditolak. Program ditutup.");
            return;
        }

        penggunaSaatIni = pengguna;

        penyimpanan.Muat(manager);

        // Jika data masih kosong, isi dengan data contoh
        if (manager.DaftarPaket.Count == 0 && manager.DaftarKurir.Count == 0)
        {
            DataSeeder.Isi(manager);
            penyimpanan.Simpan(manager);
            Console.WriteLine("Data masih kosong, data contoh (3 kurir dan 4 paket) dimuat.");
            InputHelper.TekanEnter();
        }

        bool berjalan = true;

        while (berjalan)
        {
            TampilkanMenu();
            string pilihan = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();

            switch (pilihan)
            {
                case "1": TambahPaket(); break;
                case "2": TambahKurir(); break;
                case "3": TampilkanSemuaPaket(); break;
                case "4": CariPaket(); break;
                case "5": TugaskanKurir(); break;
                case "6": PerbaruiStatus(); break;
                case "7": LihatRiwayat(); break;
                case "8": TampilkanDaftarKurir(); break;
                case "9": HitungBiaya(); break;
                case "10":
                    berjalan = false;
                    Console.WriteLine("Terima kasih telah menggunakan Sistem Manajemen Logistik Pengiriman.");
                    break;
                case "11": BatalkanPaket(); break;
                case "12": EditAlamat(); break;
                case "13": if (HanyaAdmin()) HapusKurir(); break;
                case "14": FilterPaketBerdasarkanStatus(); break;
                case "15": TampilkanDashboard(); break;
                case "16": LacakPaket(); break;
                case "17": EksporLaporan(); break;
                case "18": UbahPasswordSaya(); break;
                case "19": if (HanyaAdmin()) TambahPengguna(); break;
                case "20": if (HanyaAdmin()) TampilkanDaftarPengguna(); break;
                case "21": if (HanyaAdmin()) TampilkanLogLogin(); break;
                default:
                    Console.WriteLine("Pilihan tidak valid. Masukkan angka 1 - 21.");
                    break;
            }

            // Simpan otomatis ke database setiap ada perubahan data
            if (pilihan is "1" or "2" or "5" or "6" or "11" or "12" or "13")
                penyimpanan.Simpan(manager);

            if (berjalan)
                InputHelper.TekanEnter();
        }
    }

    // ===================== LOGIN & VERIFIKASI =====================

    private static Pengguna? Autentikasi()
    {
        Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine("   SISTEM MANAJEMEN LOGISTIK PENGIRIMAN");
        Console.WriteLine("              LOGIN PENGGUNA");
        Console.WriteLine("==========================================");

        if (auth.PerluSetupAwal())
        {
            SetupAdminAwal();
            Console.WriteLine();
        }

        Console.WriteLine("Login wajib dilakukan setiap program dijalankan.");
        Console.WriteLine();

        for (int percobaan = 1; percobaan <= MaksPercobaanLogin; percobaan++)
        {
            string username = InputHelper.BacaTeks("Username");
            string password = InputHelper.BacaPassword("Password");

            Pengguna? pengguna = auth.CekPassword(username, password, out string pesan, out bool terkunci);

            if (pengguna == null)
            {
                Console.WriteLine(pesan);

                if (terkunci)
                    return null;

                Console.WriteLine($"Percobaan {percobaan} dari {MaksPercobaanLogin}.");
                Console.WriteLine();
                continue;
            }

            // Langkah 2: kode verifikasi
            VerifikasiKode verifikasi = new VerifikasiKode();
            if (!verifikasi.Jalankan())
            {
                auth.CatatGagal(pengguna.Username, "Kode verifikasi salah / kedaluwarsa");
                Console.WriteLine("Verifikasi gagal.");
                return null;
            }

            auth.CatatBerhasil(pengguna);

            Console.WriteLine();
            Console.WriteLine($"Login berhasil. Selamat datang, {pengguna.NamaLengkap} ({pengguna.Peran}).");
            InputHelper.TekanEnter();
            return pengguna;
        }

        Console.WriteLine("Terlalu banyak percobaan login yang gagal.");
        return null;
    }

    private static void SetupAdminAwal()
    {
        Console.WriteLine("SETUP AWAL: belum ada akun. Buat akun Admin pertama.");
        Console.WriteLine("(Username 4-20 karakter huruf/angka/_, password minimal 6 karakter)");
        Console.WriteLine();

        while (true)
        {
            string username = InputHelper.BacaTeks("Username admin");
            string nama = InputHelper.BacaTeks("Nama lengkap");
            string password = InputHelper.BacaPassword("Password");
            string konfirmasi = InputHelper.BacaPassword("Ulangi password");

            if (password != konfirmasi)
            {
                Console.WriteLine("Konfirmasi password tidak sama. Ulangi.");
                Console.WriteLine();
                continue;
            }

            bool berhasil = auth.Daftar(username, nama, PeranPengguna.Admin, password, out string pesan);
            Console.WriteLine(pesan);

            if (berhasil)
                return;

            Console.WriteLine();
        }
    }

    private static bool HanyaAdmin()
    {
        if (penggunaSaatIni.Peran == PeranPengguna.Admin)
            return true;

        Console.WriteLine("Akses ditolak. Menu ini hanya untuk Admin.");
        return false;
    }

    // ===================== MENU =====================

    private static void TampilkanMenu()
    {
        Console.Clear();
        Console.WriteLine("==========================================");
        Console.WriteLine("   SISTEM MANAJEMEN LOGISTIK PENGIRIMAN");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Login sebagai: {penggunaSaatIni.NamaLengkap} ({penggunaSaatIni.Peran})");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("1.  Tambah paket");
        Console.WriteLine("2.  Tambah kurir");
        Console.WriteLine("3.  Tampilkan semua paket");
        Console.WriteLine("4.  Cari paket");
        Console.WriteLine("5.  Tugaskan kurir");
        Console.WriteLine("6.  Perbarui status pengiriman");
        Console.WriteLine("7.  Lihat riwayat paket");
        Console.WriteLine("8.  Tampilkan daftar kurir");
        Console.WriteLine("9.  Hitung biaya pengiriman");
        Console.WriteLine("10. Keluar");
        Console.WriteLine("------------ FITUR TAMBAHAN ------------");
        Console.WriteLine("11. Batalkan paket");
        Console.WriteLine("12. Edit alamat tujuan paket");
        Console.WriteLine("13. Hapus kurir (Admin)");
        Console.WriteLine("14. Filter paket berdasarkan status");
        Console.WriteLine("15. Dashboard ringkasan");
        Console.WriteLine("16. Lacak paket & estimasi tiba");
        Console.WriteLine("17. Ekspor laporan (.txt / .csv)");
        Console.WriteLine("---------------- AKUN ------------------");
        Console.WriteLine("18. Ubah password saya");

        if (penggunaSaatIni.Peran == PeranPengguna.Admin)
        {
            Console.WriteLine("19. Tambah pengguna (Admin)");
            Console.WriteLine("20. Daftar pengguna (Admin)");
            Console.WriteLine("21. Log login (Admin)");
        }

        Console.WriteLine("------------------------------------------");
        Console.Write("Pilih menu: ");
    }

    // 1. Tambah paket
    private static void TambahPaket()
    {
        Console.WriteLine("=== TAMBAH PAKET ===");

        string id;
        while (true)
        {
            id = InputHelper.BacaTeks("ID pengiriman");
            if (manager.CariPaket(id) != null)
            {
                Console.WriteLine("ID sudah digunakan, masukkan ID lain.");
                continue;
            }
            break;
        }

        string pengirim = InputHelper.BacaTeks("Nama pengirim");
        string penerima = InputHelper.BacaTeks("Nama penerima");
        string alamat = InputHelper.BacaTeks("Alamat tujuan");
        double berat = InputHelper.BacaBerat("Berat paket (kg)");

        Console.WriteLine();
        Console.WriteLine("Pilih jenis pengiriman:");
        int nomor = 1;
        foreach (JenisPengiriman layanan in manager.DaftarLayanan)
        {
            Console.WriteLine($"{nomor}. {layanan.Nama} - {layanan.EstimasiWaktu(berat)}");
            nomor++;
        }

        int pilihan = InputHelper.BacaPilihan("Pilihan", 1, manager.DaftarLayanan.Count);
        JenisPengiriman jenis = manager.DaftarLayanan[pilihan - 1];

        Console.WriteLine();
        jenis.TampilkanRincian(berat);
        Console.WriteLine();

        if (!InputHelper.Konfirmasi("Konfirmasi pembuatan paket?"))
        {
            Console.WriteLine("Pembuatan paket dibatalkan.");
            return;
        }

        try
        {
            Paket paket = new Paket(id, pengirim, penerima, alamat, berat, jenis);
            manager.TambahPaket(paket, out string pesan);
            Console.WriteLine(pesan);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Gagal: {ex.Message}");
        }
    }

    // 2. Tambah kurir
    private static void TambahKurir()
    {
        Console.WriteLine("=== TAMBAH KURIR ===");

        string id;
        while (true)
        {
            id = InputHelper.BacaTeks("ID kurir");
            if (manager.CariKurir(id) != null)
            {
                Console.WriteLine("ID kurir sudah digunakan, masukkan ID lain.");
                continue;
            }
            break;
        }

        string nama = InputHelper.BacaTeks("Nama kurir");

        Console.WriteLine();
        Console.WriteLine("Pilih jenis kendaraan:");
        int nomor = 1;
        foreach (Kendaraan k in manager.DaftarKendaraan)
        {
            Console.WriteLine($"{nomor}. {k}");
            nomor++;
        }

        int pilihan = InputHelper.BacaPilihan("Pilihan", 1, manager.DaftarKendaraan.Count);
        Kendaraan kendaraan = manager.DaftarKendaraan[pilihan - 1];

        try
        {
            Kurir kurir = new Kurir(id, nama, kendaraan);
            manager.TambahKurir(kurir, out string pesan);
            Console.WriteLine(pesan);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Gagal: {ex.Message}");
        }
    }

    // 3. Tampilkan semua paket
    private static void TampilkanSemuaPaket()
    {
        Console.WriteLine("=== DAFTAR SEMUA PAKET ===");

        if (manager.DaftarPaket.Count == 0)
        {
            Console.WriteLine("Belum ada paket.");
            return;
        }

        foreach (Paket paket in manager.DaftarPaket)
        {
            paket.Tampilkan();
            Console.WriteLine("------------------------------------------");
        }
    }

    // 4. Cari paket (berdasarkan ID atau nama)
    private static void CariPaket()
    {
        Console.WriteLine("=== CARI PAKET ===");
        Console.WriteLine("1. Berdasarkan ID pengiriman");
        Console.WriteLine("2. Berdasarkan nama pengirim / penerima");
        int mode = InputHelper.BacaPilihan("Pilihan", 1, 2);
        Console.WriteLine();

        if (mode == 1)
        {
            string id = InputHelper.BacaTeks("Masukkan ID pengiriman");
            Paket? paket = manager.CariPaket(id);
            Console.WriteLine();

            if (paket == null)
                Console.WriteLine("Paket tidak ditemukan.");
            else
                paket.Tampilkan();
        }
        else
        {
            string kata = InputHelper.BacaTeks("Masukkan nama pengirim / penerima");
            List<Paket> hasil = manager.CariPaketBerdasarkanNama(kata);
            Console.WriteLine();

            if (hasil.Count == 0)
            {
                Console.WriteLine("Tidak ada paket yang cocok.");
                return;
            }

            Console.WriteLine($"Ditemukan {hasil.Count} paket:");
            Console.WriteLine("------------------------------------------");
            foreach (Paket paket in hasil)
            {
                paket.Tampilkan();
                Console.WriteLine("------------------------------------------");
            }
        }
    }

    // 5. Tugaskan kurir (manual / otomatis)
    private static void TugaskanKurir()
    {
        Console.WriteLine("=== TUGASKAN KURIR ===");

        if (manager.DaftarPaket.Count == 0 || manager.DaftarKurir.Count == 0)
        {
            Console.WriteLine("Data paket dan kurir harus tersedia terlebih dahulu.");
            return;
        }

        Console.WriteLine("Paket dengan status 'Menunggu kurir':");
        bool adaPaket = false;
        foreach (Paket paket in manager.DaftarPaket)
        {
            if (paket.Status == StatusPengiriman.MenungguKurir && paket.KurirAktif == null)
            {
                Console.WriteLine($"- {paket.IdPengiriman} ({paket.NamaPenerima}, {paket.Berat} kg, {paket.Jenis.Nama})");
                adaPaket = true;
            }
        }

        if (!adaPaket)
            Console.WriteLine("(tidak ada)");

        Console.WriteLine();
        Console.WriteLine("Daftar kurir:");
        foreach (Kurir kurir in manager.DaftarKurir)
        {
            Console.WriteLine($"- {kurir.IdKurir} | {kurir.Nama} | {kurir.JenisKendaraan.Nama} | beban {kurir.PaketDitangani.Count}/{kurir.JenisKendaraan.MaksJumlahPaket}");
        }

        Console.WriteLine();
        Console.WriteLine("Cara penugasan:");
        Console.WriteLine("1. Pilih kurir manual");
        Console.WriteLine("2. Pilih kurir otomatis (beban paling sedikit)");
        int mode = InputHelper.BacaPilihan("Pilihan", 1, 2);

        string idPaket = InputHelper.BacaTeks("ID paket");
        string pesan;

        if (mode == 1)
        {
            string idKurir = InputHelper.BacaTeks("ID kurir");
            manager.TugaskanKurir(idPaket, idKurir, out pesan);
        }
        else
        {
            manager.TugaskanKurirOtomatis(idPaket, out pesan);
        }

        Console.WriteLine(pesan);
    }

    // 6. Perbarui status pengiriman
    private static void PerbaruiStatus()
    {
        Console.WriteLine("=== PERBARUI STATUS PENGIRIMAN ===");
        string id = InputHelper.BacaTeks("ID paket");

        Paket? paket = manager.CariPaket(id);
        if (paket == null)
        {
            Console.WriteLine("Paket tidak ditemukan.");
            return;
        }

        if (paket.Status == StatusPengiriman.Dibatalkan)
        {
            Console.WriteLine("Paket sudah dibatalkan, status tidak dapat diubah.");
            return;
        }

        Console.WriteLine($"Status saat ini: {paket.Status.KeTeks()}");
        Console.WriteLine();
        Console.WriteLine("Alur status pengiriman:");

        foreach (StatusPengiriman status in Enum.GetValues<StatusPengiriman>())
        {
            if (status == StatusPengiriman.Dibatalkan)
                continue; // bukan bagian dari alur normal

            string tanda = status == paket.Status ? "  <-- saat ini" : "";
            Console.WriteLine($"{(int)status + 1}. {status.KeTeks()}{tanda}");
        }

        Console.WriteLine();
        int jumlahAlur = (int)StatusPengiriman.BerhasilDikirim + 1;
        int pilihan = InputHelper.BacaPilihan("Pilih status baru (nomor)", 1, jumlahAlur);
        StatusPengiriman statusBaru = (StatusPengiriman)(pilihan - 1);
        string keterangan = InputHelper.BacaTeksOpsional("Keterangan (boleh kosong)");

        manager.PerbaruiStatus(id, statusBaru, keterangan, out string pesan);
        Console.WriteLine(pesan);
    }

    // 7. Lihat riwayat paket
    private static void LihatRiwayat()
    {
        Console.WriteLine("=== RIWAYAT PAKET ===");
        string id = InputHelper.BacaTeks("ID paket");

        Paket? paket = manager.CariPaket(id);
        Console.WriteLine();

        if (paket == null)
        {
            Console.WriteLine("Paket tidak ditemukan.");
            return;
        }

        Console.WriteLine($"Riwayat paket {paket.IdPengiriman}:");
        Console.WriteLine("------------------------------------------");

        int nomor = 1;
        foreach (RiwayatPengiriman riwayat in paket.Riwayat)
        {
            Console.WriteLine($"{nomor}. [{riwayat.Waktu:dd/MM/yyyy HH:mm:ss}] {riwayat.Status.KeTeks()}");
            Console.WriteLine($"   Keterangan: {riwayat.Keterangan}");
            nomor++;
        }
    }

    // 8. Tampilkan daftar kurir
    private static void TampilkanDaftarKurir()
    {
        Console.WriteLine("=== DAFTAR KURIR ===");

        if (manager.DaftarKurir.Count == 0)
        {
            Console.WriteLine("Belum ada kurir.");
            return;
        }

        foreach (Kurir kurir in manager.DaftarKurir)
        {
            Console.WriteLine($"ID Kurir   : {kurir.IdKurir}");
            Console.WriteLine($"Nama       : {kurir.Nama}");
            Console.WriteLine($"Kendaraan  : {kurir.JenisKendaraan}");
            Console.WriteLine($"Beban      : {kurir.PaketDitangani.Count}/{kurir.JenisKendaraan.MaksJumlahPaket} paket");
            Console.WriteLine("Paket yang sedang ditangani:");

            if (kurir.PaketDitangani.Count == 0)
            {
                Console.WriteLine("  (tidak ada)");
            }
            else
            {
                foreach (Paket paket in kurir.PaketDitangani)
                {
                    Console.WriteLine($"  - {paket.IdPengiriman} | {paket.NamaPenerima} | {paket.Status.KeTeks()}");
                }
            }

            Console.WriteLine("------------------------------------------");
        }
    }

    // 9. Hitung biaya pengiriman
    private static void HitungBiaya()
    {
        Console.WriteLine("=== HITUNG BIAYA PENGIRIMAN ===");
        double berat = InputHelper.BacaBerat("Berat paket (kg)");
        Console.WriteLine();

        foreach (JenisPengiriman layanan in manager.DaftarLayanan)
        {
            layanan.TampilkanRincian(berat);
            Console.WriteLine();
        }
    }

    // 11. Batalkan paket
    private static void BatalkanPaket()
    {
        Console.WriteLine("=== BATALKAN PAKET ===");
        string id = InputHelper.BacaTeks("ID paket");

        Paket? paket = manager.CariPaket(id);
        if (paket == null)
        {
            Console.WriteLine("Paket tidak ditemukan.");
            return;
        }

        Console.WriteLine($"Status saat ini: {paket.Status.KeTeks()}");
        Console.WriteLine("(Paket hanya dapat dibatalkan sebelum berstatus 'Dalam perjalanan')");
        Console.WriteLine();

        string alasan = InputHelper.BacaTeks("Alasan pembatalan");

        if (!InputHelper.Konfirmasi($"Yakin membatalkan paket {paket.IdPengiriman}?"))
        {
            Console.WriteLine("Pembatalan dibatalkan.");
            return;
        }

        manager.BatalkanPaket(id, alasan, out string pesan);
        Console.WriteLine(pesan);
    }

    // 12. Edit alamat tujuan
    private static void EditAlamat()
    {
        Console.WriteLine("=== EDIT ALAMAT TUJUAN ===");
        string id = InputHelper.BacaTeks("ID paket");

        Paket? paket = manager.CariPaket(id);
        if (paket == null)
        {
            Console.WriteLine("Paket tidak ditemukan.");
            return;
        }

        Console.WriteLine($"Status         : {paket.Status.KeTeks()}");
        Console.WriteLine($"Alamat saat ini: {paket.AlamatTujuan}");
        Console.WriteLine();

        string alamatBaru = InputHelper.BacaTeks("Alamat baru");

        manager.UbahAlamat(id, alamatBaru, out string pesan);
        Console.WriteLine(pesan);
    }

    // 13. Hapus kurir (khusus Admin)
    private static void HapusKurir()
    {
        Console.WriteLine("=== HAPUS KURIR ===");

        if (manager.DaftarKurir.Count == 0)
        {
            Console.WriteLine("Belum ada kurir.");
            return;
        }

        foreach (Kurir kurir in manager.DaftarKurir)
        {
            Console.WriteLine($"- {kurir.IdKurir} | {kurir.Nama} | {kurir.JenisKendaraan.Nama} | paket aktif: {kurir.PaketDitangani.Count}");
        }

        Console.WriteLine();
        string id = InputHelper.BacaTeks("ID kurir yang dihapus");

        if (!InputHelper.Konfirmasi("Yakin menghapus kurir ini?"))
        {
            Console.WriteLine("Penghapusan dibatalkan.");
            return;
        }

        manager.HapusKurir(id, out string pesan);
        Console.WriteLine(pesan);
    }

    // 14. Filter paket berdasarkan status
    private static void FilterPaketBerdasarkanStatus()
    {
        Console.WriteLine("=== FILTER PAKET BERDASARKAN STATUS ===");

        int nomor = 1;
        foreach (StatusPengiriman status in Enum.GetValues<StatusPengiriman>())
        {
            Console.WriteLine($"{nomor}. {status.KeTeks()}");
            nomor++;
        }

        int jumlahStatus = Enum.GetValues<StatusPengiriman>().Length;
        int pilihan = InputHelper.BacaPilihan("Pilih status", 1, jumlahStatus);
        StatusPengiriman statusDipilih = (StatusPengiriman)(pilihan - 1);

        List<Paket> hasil = manager.FilterPaket(statusDipilih);
        Console.WriteLine();

        if (hasil.Count == 0)
        {
            Console.WriteLine($"Tidak ada paket dengan status '{statusDipilih.KeTeks()}'.");
            return;
        }

        Console.WriteLine($"Paket dengan status '{statusDipilih.KeTeks()}' ({hasil.Count}):");
        foreach (Paket paket in hasil)
        {
            Console.WriteLine($"- {paket.BuatRingkasan()}");
        }
    }

    // 15. Dashboard ringkasan
    private static void TampilkanDashboard()
    {
        Console.WriteLine("=== DASHBOARD RINGKASAN ===");
        Console.WriteLine($"Total paket : {manager.DaftarPaket.Count}");
        Console.WriteLine($"Total kurir : {manager.DaftarKurir.Count}");
        Console.WriteLine();

        Console.WriteLine("Jumlah paket per status:");
        foreach (StatusPengiriman status in Enum.GetValues<StatusPengiriman>())
        {
            Console.WriteLine($"  {status.KeTeks(),-24}: {manager.HitungPaketDenganStatus(status)}");
        }

        Console.WriteLine();
        Console.WriteLine("Jumlah paket per layanan:");
        foreach (JenisPengiriman layanan in manager.DaftarLayanan)
        {
            int jumlah = 0;
            foreach (Paket paket in manager.DaftarPaket)
            {
                if (paket.Jenis.Nama == layanan.Nama)
                    jumlah++;
            }

            Console.WriteLine($"  {layanan.Nama,-24}: {jumlah}");
        }

        Console.WriteLine();
        Console.WriteLine($"Pendapatan terealisasi (berhasil dikirim) : {JenisPengiriman.Rupiah(manager.HitungPendapatanTerealisasi())}");
        Console.WriteLine($"Potensi pendapatan (paket masih berjalan) : {JenisPengiriman.Rupiah(manager.HitungPotensiPendapatan())}");

        Console.WriteLine();
        Kurir? tersibuk = manager.KurirTersibuk();
        if (tersibuk == null)
        {
            Console.WriteLine("Kurir tersibuk : - (semua kurir sedang tidak menangani paket)");
        }
        else
        {
            Console.WriteLine($"Kurir tersibuk : {tersibuk.Nama} ({tersibuk.IdKurir}) dengan {tersibuk.PaketDitangani.Count} paket aktif");
        }
    }

    // 16. Lacak paket & estimasi tiba
    private static void LacakPaket()
    {
        Console.WriteLine("=== LACAK PAKET ===");
        string id = InputHelper.BacaTeks("ID paket");

        Paket? paket = manager.CariPaket(id);
        Console.WriteLine();

        if (paket == null)
        {
            Console.WriteLine("Paket tidak ditemukan.");
            return;
        }

        Console.WriteLine($"Paket          : {paket.IdPengiriman} ({paket.NamaPengirim} -> {paket.NamaPenerima})");
        Console.WriteLine($"Layanan        : {paket.Jenis.Nama} ({paket.Jenis.EstimasiWaktu(paket.Berat)})");
        Console.WriteLine($"Dibuat pada    : {paket.TanggalDibuat:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"Estimasi tiba  : {paket.TeksEstimasiTiba()}");
        Console.WriteLine();
        Console.WriteLine("Progres pengiriman:");

        foreach (StatusPengiriman status in Enum.GetValues<StatusPengiriman>())
        {
            if (status == StatusPengiriman.Dibatalkan)
                continue;

            bool sudahLewat = paket.Status != StatusPengiriman.Dibatalkan && (int)status <= (int)paket.Status;
            string tanda = sudahLewat ? "[x]" : "[ ]";
            Console.WriteLine($"  {tanda} {status.KeTeks()}");
        }

        if (paket.Status == StatusPengiriman.Dibatalkan)
        {
            Console.WriteLine();
            Console.WriteLine("Paket ini telah DIBATALKAN.");
        }
    }

    // 17. Ekspor laporan
    private static void EksporLaporan()
    {
        Console.WriteLine("=== EKSPOR LAPORAN ===");
        Console.WriteLine("1. Laporan teks (.txt)");
        Console.WriteLine("2. Laporan CSV (.csv)");
        Console.WriteLine("3. Keduanya");
        int pilihan = InputHelper.BacaPilihan("Pilihan", 1, 3);
        Console.WriteLine();

        if (pilihan == 1 || pilihan == 3)
        {
            exporter.ExportTxt(manager, out string pesanTxt);
            Console.WriteLine(pesanTxt);
        }

        if (pilihan == 2 || pilihan == 3)
        {
            exporter.ExportCsv(manager, out string pesanCsv);
            Console.WriteLine(pesanCsv);
        }
    }

    // 18. Ubah password saya
    private static void UbahPasswordSaya()
    {
        Console.WriteLine("=== UBAH PASSWORD ===");

        string lama = InputHelper.BacaPassword("Password lama");
        string baru = InputHelper.BacaPassword("Password baru");
        string konfirmasi = InputHelper.BacaPassword("Ulangi password baru");

        if (baru != konfirmasi)
        {
            Console.WriteLine("Konfirmasi password tidak sama.");
            return;
        }

        auth.UbahPassword(penggunaSaatIni, lama, baru, out string pesan);
        Console.WriteLine(pesan);
    }

    // 19. Tambah pengguna (Admin)
    private static void TambahPengguna()
    {
        Console.WriteLine("=== TAMBAH PENGGUNA ===");
        Console.WriteLine("(Username 4-20 karakter huruf/angka/_, password minimal 6 karakter)");
        Console.WriteLine();

        string username = InputHelper.BacaTeks("Username");
        string nama = InputHelper.BacaTeks("Nama lengkap");

        Console.WriteLine("Peran:");
        Console.WriteLine("1. Admin (akses penuh)");
        Console.WriteLine("2. Staf (operasional)");
        int pilihanPeran = InputHelper.BacaPilihan("Pilihan", 1, 2);
        PeranPengguna peran = pilihanPeran == 1 ? PeranPengguna.Admin : PeranPengguna.Staf;

        string password = InputHelper.BacaPassword("Password");
        string konfirmasi = InputHelper.BacaPassword("Ulangi password");

        if (password != konfirmasi)
        {
            Console.WriteLine("Konfirmasi password tidak sama.");
            return;
        }

        auth.Daftar(username, nama, peran, password, out string pesan);
        Console.WriteLine(pesan);
    }

    // 20. Daftar pengguna (Admin)
    private static void TampilkanDaftarPengguna()
    {
        Console.WriteLine("=== DAFTAR PENGGUNA ===");

        foreach (Pengguna pengguna in auth.DaftarPengguna())
        {
            Console.WriteLine($"{pengguna.Id}. {pengguna.Username,-15} | {pengguna.NamaLengkap,-20} | {pengguna.Peran}");
        }
    }

    // 21. Log login (Admin)
    private static void TampilkanLogLogin()
    {
        Console.WriteLine("=== LOG LOGIN (20 TERAKHIR) ===");

        List<LogLogin> daftar = auth.LogTerbaru(20);

        if (daftar.Count == 0)
        {
            Console.WriteLine("Belum ada log.");
            return;
        }

        foreach (LogLogin log in daftar)
        {
            string hasil = log.Berhasil ? "BERHASIL" : "GAGAL";
            Console.WriteLine($"[{log.Waktu.ToLocalTime():dd/MM/yyyy HH:mm:ss}] {log.Username,-15} {hasil,-9} {log.Keterangan}");
        }
    }
}