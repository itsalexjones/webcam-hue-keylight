namespace HueWebcamSceneSwitcher.Models;

public class Color
{
    public Gamut gamut { get; set; }
    public string gamut_type { get; set; }
    public Xy xy { get; set; }
}

public class Gamut
{
    public Blue blue { get; set; }
    public Green green { get; set; }
    public Red red { get; set; }
}

public class Blue
{
    public double x { get; set; }
    public double y { get; set; }
}

public class Green
{
    public double x { get; set; }
    public double y { get; set; }
}

public class Red
{
    public double x { get; set; }
    public double y { get; set; }
}

public class Xy
{
    public double x { get; set; }
    public double y { get; set; }
}