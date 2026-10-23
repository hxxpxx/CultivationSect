public class ScanResult
{
    public string scanType;       // "Material" / "Energy" / "Serial"
    public string outputHash;     // 扫描仪输出的签名
    public float hazardLevel;     // 危险值 0~1
}