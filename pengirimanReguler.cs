namespace LogistikApp;

public class PengirimanReguler : JenisPengiriman
{
    public override string Nama => "Reguler";
    public override decimal TarifPerKg => 8000m;
    public override decimal BiayaTetap => 0m;

    public override decimal HitungBiaya(double berat)
    {
        return TarifPerKg * BeratTertagih(berat);
    }

    public override string EstimasiWaktu(double berat)
    {
        return berat <= 10 ? "3 - 5 hari kerja" : "5 - 7 hari kerja";
    }

    public override DateTime HitungTanggalTiba(DateTime mulai, double berat)
    {
        return TambahHariKerja(mulai, berat <= 10 ? 5 : 7);
    }
}