namespace MapProject.Business.Exceptions;

/// <summary>
/// WKT metni bozuk ya da beklenen geometri tipinde değilse fırlatılır.
/// Controller bunu 400'e çeviriyor.
/// </summary>
public class InvalidGeometryException : Exception
{
    public InvalidGeometryException(string message) : base(message)
    {
    }
}
