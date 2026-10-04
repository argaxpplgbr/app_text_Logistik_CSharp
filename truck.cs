namespace LogistikApp;

public class Truk : Kendaraan
{
    public override string Nama => "Truk";
    public override int MaksJumlahPaket => 25;
    public override double MaksBeratPerPaket => 1000;
}