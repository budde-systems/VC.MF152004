namespace MF152004.Models.Values.Types;

public enum ScanType : byte
{
    successful_scan = 0,
    no_scan = 1,
    wrong_weight = 2,
    wrong_carrier = 3,
    scanner_error = 4
}