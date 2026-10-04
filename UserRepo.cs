using System.Globalization;
using Microsoft.Data.Sqlite;

namespace LogistikApp;

public class PenggunaRepository
{
    private readonly Database _database;

    public PenggunaRepository(Database database)
    {
        _database = database;
    }

    public int HitungPengguna()
    {
        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pengguna";

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public Pengguna? Cari(string username)
    {
        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "SELECT id, username, nama_lengkap, peran, password_hash FROM pengguna WHERE username = $username";
        cmd.Parameters.AddWithValue("$username", username);

        using SqliteDataReader reader = cmd.ExecuteReader();
        return reader.Read() ? BacaPengguna(reader) : null;
    }

    public List<Pengguna> Semua()
    {
        List<Pengguna> hasil = new();

        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "SELECT id, username, nama_lengkap, peran, password_hash FROM pengguna ORDER BY id";

        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            hasil.Add(BacaPengguna(reader));
        }

        return hasil;
    }

    public void Tambah(string username, string namaLengkap, PeranPengguna peran, string passwordHash)
    {
        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = @"INSERT INTO pengguna (username, nama_lengkap, peran, password_hash, dibuat_pada)
                            VALUES ($username, $nama, $peran, $hash, $dibuat)";
        cmd.Parameters.AddWithValue("$username", username);
        cmd.Parameters.AddWithValue("$nama", namaLengkap);
        cmd.Parameters.AddWithValue("$peran", peran.ToString());
        cmd.Parameters.AddWithValue("$hash", passwordHash);
        cmd.Parameters.AddWithValue("$dibuat", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void UbahPassword(int id, string hashBaru)
    {
        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "UPDATE pengguna SET password_hash = $hash WHERE id = $id";
        cmd.Parameters.AddWithValue("$hash", hashBaru);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void CatatLogin(string username, bool berhasil, string keterangan)
    {
        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = @"INSERT INTO log_login (username, waktu, berhasil, keterangan)
                            VALUES ($username, $waktu, $berhasil, $keterangan)";
        cmd.Parameters.AddWithValue("$username", username);
        cmd.Parameters.AddWithValue("$waktu", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$berhasil", berhasil ? 1 : 0);
        cmd.Parameters.AddWithValue("$keterangan", keterangan);
        cmd.ExecuteNonQuery();
    }

    
    public List<LogLogin> LogPengguna(string username, int batas)
    {
        List<LogLogin> hasil = new();

        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = @"SELECT username, waktu, berhasil, keterangan FROM log_login
                            WHERE username = $username COLLATE NOCASE
                            ORDER BY id DESC LIMIT $batas";
        cmd.Parameters.AddWithValue("$username", username);
        cmd.Parameters.AddWithValue("$batas", batas);

        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            hasil.Add(BacaLog(reader));
        }

        return hasil;
    }

    
    public List<LogLogin> LogTerakhir(int batas)
    {
        List<LogLogin> hasil = new();

        using SqliteConnection koneksi = _database.BukaKoneksi();
        using SqliteCommand cmd = koneksi.CreateCommand();
        cmd.CommandText = "SELECT username, waktu, berhasil, keterangan FROM log_login ORDER BY id DESC LIMIT $batas";
        cmd.Parameters.AddWithValue("$batas", batas);

        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            hasil.Add(BacaLog(reader));
        }

        return hasil;
    }

    private static Pengguna BacaPengguna(SqliteDataReader reader)
    {
        return new Pengguna(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            Enum.Parse<PeranPengguna>(reader.GetString(3)),
            reader.GetString(4));
    }

    private static LogLogin BacaLog(SqliteDataReader reader)
    {
        return new LogLogin(
            reader.GetString(0),
            DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetInt32(2) == 1,
            reader.GetString(3));
    }
}