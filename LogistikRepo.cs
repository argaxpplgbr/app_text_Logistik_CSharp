using System.Globalization;
using Microsoft.Data.Sqlite;

namespace LogistikApp;

public class LogistikRepository
{
    private readonly Database _database;

    public LogistikRepository(Database database)
    {
        _database = database;
    }

    // Menyimpan seluruh data kurir, paket, dan riwayat dalam satu transaksi
    public void Simpan(LogistikManager manager)
    {
        try
        {
            using SqliteConnection koneksi = _database.BukaKoneksi();
            using SqliteTransaction transaksi = koneksi.BeginTransaction();

            Jalankan(koneksi, transaksi, "DELETE FROM riwayat");
            Jalankan(koneksi, transaksi, "DELETE FROM paket");
            Jalankan(koneksi, transaksi, "DELETE FROM kurir");

            foreach (Kurir kurir in manager.DaftarKurir)
            {
                Jalankan(koneksi, transaksi,
                    "INSERT INTO kurir (id_kurir, nama, jenis_kendaraan) VALUES ($id, $nama, $kendaraan)",
                    ("$id", kurir.IdKurir),
                    ("$nama", kurir.Nama),
                    ("$kendaraan", kurir.JenisKendaraan.Nama));
            }

            foreach (Paket paket in manager.DaftarPaket)
            {
                Jalankan(koneksi, transaksi,
                    @"INSERT INTO paket (id_pengiriman, nama_pengirim, nama_penerima, alamat_tujuan,
                                         berat, jenis_pengiriman, status, id_kurir)
                      VALUES ($id, $pengirim, $penerima, $alamat, $berat, $jenis, $status, $idKurir)",
                    ("$id", paket.IdPengiriman),
                    ("$pengirim", paket.NamaPengirim),
                    ("$penerima", paket.NamaPenerima),
                    ("$alamat", paket.AlamatTujuan),
                    ("$berat", paket.Berat),
                    ("$jenis", paket.Jenis.Nama),
                    ("$status", paket.Status.ToString()),
                    ("$idKurir", paket.KurirAktif?.IdKurir));

                foreach (RiwayatPengiriman riwayat in paket.Riwayat)
                {
                    Jalankan(koneksi, transaksi,
                        @"INSERT INTO riwayat (id_pengiriman, status, waktu, keterangan)
                          VALUES ($id, $status, $waktu, $keterangan)",
                        ("$id", paket.IdPengiriman),
                        ("$status", riwayat.Status.ToString()),
                        ("$waktu", riwayat.Waktu.ToString("o")),
                        ("$keterangan", riwayat.Keterangan));
                }
            }

            transaksi.Commit();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Gagal menyimpan ke database: {ex.Message}");
        }
    }

    public void Muat(LogistikManager manager)
    {
        try
        {
            using SqliteConnection koneksi = _database.BukaKoneksi();

            // 1. Kurir dimuat lebih dulu agar bisa dihubungkan ke paket
            using (SqliteCommand cmdKurir = koneksi.CreateCommand())
            {
                cmdKurir.CommandText = "SELECT id_kurir, nama, jenis_kendaraan FROM kurir ORDER BY rowid";

                using SqliteDataReader readerKurir = cmdKurir.ExecuteReader();
                while (readerKurir.Read())
                {
                    // Jika nama kendaraan tidak dikenal, dipakai kendaraan pertama (Motor)
                    Kendaraan kendaraan = manager.CariKendaraan(readerKurir.GetString(2)) ?? manager.DaftarKendaraan[0];
                    manager.TambahKurir(new Kurir(readerKurir.GetString(0), readerKurir.GetString(1), kendaraan), out _);
                }
            }

            // 2. Riwayat dikelompokkan per paket
            Dictionary<string, List<RiwayatPengiriman>> riwayatPerPaket = new(StringComparer.OrdinalIgnoreCase);

            using (SqliteCommand cmdRiwayat = koneksi.CreateCommand())
            {
                cmdRiwayat.CommandText = "SELECT id_pengiriman, status, waktu, keterangan FROM riwayat ORDER BY id";

                using SqliteDataReader readerRiwayat = cmdRiwayat.ExecuteReader();
                while (readerRiwayat.Read())
                {
                    string idPaket = readerRiwayat.GetString(0);

                    if (!riwayatPerPaket.TryGetValue(idPaket, out List<RiwayatPengiriman>? daftar))
                    {
                        daftar = new List<RiwayatPengiriman>();
                        riwayatPerPaket[idPaket] = daftar;
                    }

                    daftar.Add(new RiwayatPengiriman(
                        Enum.Parse<StatusPengiriman>(readerRiwayat.GetString(1)),
                        DateTime.Parse(readerRiwayat.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        readerRiwayat.GetString(3)));
                }
            }

            // 3. Paket
            using (SqliteCommand cmdPaket = koneksi.CreateCommand())
            {
                cmdPaket.CommandText = @"SELECT id_pengiriman, nama_pengirim, nama_penerima, alamat_tujuan,
                                                berat, jenis_pengiriman, status, id_kurir
                                         FROM paket ORDER BY rowid";

                using SqliteDataReader readerPaket = cmdPaket.ExecuteReader();
                while (readerPaket.Read())
                {
                    JenisPengiriman? jenis = manager.CariLayanan(readerPaket.GetString(5));
                    if (jenis == null)
                        continue;

                    Paket paket = new Paket(
                        readerPaket.GetString(0),
                        readerPaket.GetString(1),
                        readerPaket.GetString(2),
                        readerPaket.GetString(3),
                        readerPaket.GetDouble(4),
                        jenis);

                    StatusPengiriman status = Enum.Parse<StatusPengiriman>(readerPaket.GetString(6));

                    List<RiwayatPengiriman> riwayat = riwayatPerPaket.TryGetValue(paket.IdPengiriman, out List<RiwayatPengiriman>? ditemukan)
                        ? ditemukan
                        : new List<RiwayatPengiriman>();

                    if (riwayat.Count == 0)
                        riwayat.Add(new RiwayatPengiriman(status, DateTime.Now, "Data dipulihkan dari database"));

                    paket.PulihkanKondisi(status, riwayat);

                    if (!readerPaket.IsDBNull(7))
                    {
                        Kurir? kurir = manager.CariKurir(readerPaket.GetString(7));
                        kurir?.PulihkanPaket(paket);
                    }

                    manager.TambahPaket(paket, out _);
                }
            }
        }
        catch (Exception ex) when (ex is SqliteException || ex is ArgumentException || ex is FormatException)
        {
            Console.WriteLine($"Gagal memuat data dari database: {ex.Message}");
            Console.WriteLine("Program dilanjutkan dengan data yang berhasil dimuat.");
        }
    }

    private static void Jalankan(SqliteConnection koneksi, SqliteTransaction transaksi, string sql,
                                 params (string nama, object? nilai)[] parameter)
    {
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.Transaction = transaksi;
        cmd.CommandText = sql;

        foreach ((string nama, object? nilai) in parameter)
        {
            cmd.Parameters.AddWithValue(nama, nilai ?? DBNull.Value);
        }

        cmd.ExecuteNonQuery();
    }
}