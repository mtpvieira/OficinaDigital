namespace OficinaDigital.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem) { }

    public static void Se(bool condicao, string mensagem)
    {
        if (condicao) throw new DomainException(mensagem);
    }
}
