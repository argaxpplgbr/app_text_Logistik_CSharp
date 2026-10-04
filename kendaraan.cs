namespace LogistikApp;

// Abstract class: induk dari semua jenis kendaraan kurir
public abstract class Kendaraan
{
    public abstract string Nama { get; }
    public abstract int MaksJumlahPaket { get; }
    public abstract double MaksBeratPerPaket { get; }

    public bool BisaMengangkut(double berat)
    {
        return berat <= MaksBeratPerPaket;
    }

    public override string ToString()
    {
        return $"{Nama} (maks {MaksJumlahPaket} paket, {MaksBeratPerPaket} kg per paket)";
    }
}