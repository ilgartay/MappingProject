namespace MapProject.Business.Exceptions;

/// <summary>
/// Kullanıcı işlemi kurallara aykırıysa fırlatılır (ör. kendi hesabını
/// kapatmaya çalışmak). Controller bunu 400'e çeviriyor.
/// </summary>
public class InvalidUserOperationException : Exception
{
    public InvalidUserOperationException(string message) : base(message)
    {
    }
}
