namespace HueWebcamSceneSwitcher.Models;

public class MirekSchema
{
    public int mirek_maximum { get; set; }
    public int mirek_minimum { get; set; }
}

public class ColorTemperature
{
    public int? mirek { get; set; }
    public MirekSchema? mirek_schema { get; set; }
    public bool? mirek_valid { get; set; }
}