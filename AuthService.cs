using Microsoft.Data.Sqlite;

namespace LogistikApp;

public class AuthService
{
    private const int MaksGagal = 3;
    private const int MenitKunci = 5;

    // Dipakai agar waktu proses sama meski username tidak ditemukan
    private static readonly string HashPalsu = PasswordHasher.Hash("password-palsu");

    private readonly PenggunaRepository _repo;

    public AuthService(PenggunaRepository repo)
    {
        _repo = repo;
    }

    public bool PerluSetupAwal()
    {
        return _repo.HitungPengguna() == 0;
    }

    public bool Daftar(string username, string namaLengkap, PeranPengguna peran, string password, out string pesan)
    {
        username = username.Trim();

        if (!ValidasiUsername(username, out pesan))
            return false;

        if (string.IsNullOrWhiteSpace(namaLengkap))
        {
            pesan = "Nama lengkap tidak boleh kosong.";
            return false;
        }

        if (!ValidasiPassword(password, out pesan))
            return false;

        if (_repo.Cari(username) != null)
        {
            pesan = "Username sudah digunakan.";
            return false;
        }

        try
        {
            _repo.Tambah(username, namaLengkap.Trim(), peran, PasswordHasher.Hash(password));
        }
        catch (SqliteException)
        {
            pesan = "Gagal menyimpan pengguna (username mungkin sudah dipakai).";
            return false;
        }

        pesan = $"Pengguna '{username}' berhasil dibuat dengan peran {peran}.";
        return true;
    }

    // Langkah 1 login: cek username & password. Belum dianggap berhasil sebelum kode verifikasi benar.
    public Pengguna? CekPassword(string username, string password, out string pesan, out bool terkunci)
    {
        terkunci = false;
        username = username.Trim();

        if (SedangTerkunci(username))
        {
            terkunci = true;
            pesan = $"Akun terkunci sementara karena {MaksGagal} kali gagal login. Coba lagi dalam {MenitKunci} menit.";
            return null;
        }

        Pengguna? pengguna = _repo.Cari(username);
        string hash = pengguna != null ? pengguna.PasswordHash : HashPalsu;
        bool cocok = PasswordHasher.Verifikasi(password, hash);

        if (pengguna == null || !cocok)
        {
            _repo.CatatLogin(username, false, "Username atau password salah");
            pesan = "Username atau password salah.";
            return null;
        }

        pesan = "";
        return pengguna;
    }

    public void CatatBerhasil(Pengguna pengguna)
    {
        _repo.CatatLogin(pengguna.Username, true, "Login berhasil (password + kode verifikasi)");
    }

    public void CatatGagal(string username, string keterangan)
    {
        _repo.CatatLogin(username, false, keterangan);
    }

    public bool UbahPassword(Pengguna pengguna, string lama, string baru, out string pesan)
    {
        // Selalu cek ke database agar tidak memakai data lama di memori
        Pengguna? terbaru = _repo.Cari(pengguna.Username);

        if (terbaru == null || !PasswordHasher.Verifikasi(lama, terbaru.PasswordHash))
        {
            pesan = "Password lama salah.";
            return false;
        }

        if (!ValidasiPassword(baru, out pesan))
            return false;

        if (lama == baru)
        {
            pesan = "Password baru tidak boleh sama dengan password lama.";
            return false;
        }

        _repo.UbahPassword(terbaru.Id, PasswordHasher.Hash(baru));
        pesan = "Password berhasil diubah.";
        return true;
    }

    public List<Pengguna> DaftarPengguna()
    {
        return _repo.Semua();
    }

    public List<LogLogin> LogTerbaru(int jumlah)
    {
        return _repo.LogTerakhir(jumlah);
    }

    // Terkunci jika ada MaksGagal kegagalan beruntun dalam MenitKunci menit terakhir
    private bool SedangTerkunci(string username)
    {
        DateTime batas = DateTime.UtcNow.AddMinutes(-MenitKunci);
        int gagal = 0;

        foreach (LogLogin log in _repo.LogPengguna(username, MaksGagal + 5))
        {
            if (log.Berhasil || log.Waktu < batas)
                break;

            gagal++;
        }

        return gagal >= MaksGagal;
    }

    private static bool ValidasiUsername(string username, out string pesan)
    {
        if (username.Length < 4 || username.Length > 20)
        {
            pesan = "Username harus 4 - 20 karakter.";
            return false;
        }

        foreach (char c in username)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                pesan = "Username hanya boleh huruf, angka, dan underscore (_).";
                return false;
            }
        }

        pesan = "";
        return true;
    }

    private static bool ValidasiPassword(string password, out string pesan)
    {
        if (password.Length < 6)
        {
            pesan = "Password minimal 6 karakter.";
            return false;
        }

        pesan = "";
        return true;
    }
}