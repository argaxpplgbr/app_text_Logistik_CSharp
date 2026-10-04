namespace LogistikApp;

public class Motor : Kendaraan
{
    public override string Nama => "Motor";
    public override int MaksJumlahPaket => 3;
    public override double MaksBeratPerPaket => 20;
}