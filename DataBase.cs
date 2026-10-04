using Microsoft.Data.Sqlite;

namespace LogistikApp;

public class Database
{
    private readonly string _connectionString;

    public Database(string namaFile = "logistik.db")
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = namaFile
        }.ToString();
    }

    public SqliteConnection BukaKoneksi()
    {
        SqliteConnection koneksi = new SqliteConnection(_connectionString);
        koneksi.Open();

        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        return koneksi;
    }

    // Membuat semua tabel jika belum ada
    public void Inisialisasi()
    {
        using SqliteConnection koneksi = BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS pengguna (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                nama_lengkap TEXT NOT NULL,
                peran TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                dibuat_pada TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS log_login (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                waktu TEXT NOT NULL,
                berhasil INTEGER NOT NULL,
                keterangan TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS kurir (
                id_kurir TEXT PRIMARY KEY,
                nama TEXT NOT NULL,
                jenis_kendaraan TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS paket (
                id_pengiriman TEXT PRIMARY KEY,
                nama_pengirim TEXT NOT NULL,
                nama_penerima TEXT NOT NULL,
                alamat_tujuan TEXT NOT NULL,
                berat REAL NOT NULL,
                jenis_pengiriman TEXT NOT NULL,
                status TEXT NOT NULL,
                id_kurir TEXT NULL REFERENCES kurir(id_kurir)
            );

            CREATE TABLE IF NOT EXISTS riwayat (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                id_pengiriman TEXT NOT NULL REFERENCES paket(id_pengiriman) ON DELETE CASCADE,
                status TEXT NOT NULL,
                waktu TEXT NOT NULL,
                keterangan TEXT NOT NULL
            );";

        cmd.ExecuteNonQuery();
    }
}