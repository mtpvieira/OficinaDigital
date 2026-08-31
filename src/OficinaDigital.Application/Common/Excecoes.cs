namespace OficinaDigital.Application.Common;

public class RecursoNaoEncontradoException(string recurso, object identificador)
    : Exception($"{recurso} não encontrado(a): {identificador}.")
{
    public string Recurso { get; } = recurso;
    public object Identificador { get; } = identificador;
}

public class ConflitoDeNegocioException(string mensagem) : Exception(mensagem);

public class AcessoNegadoException(string mensagem) : Exception(mensagem);

/// <summary>Credencial inválida no login. Traduzida para HTTP 401.</summary>
public class CredencialInvalidaException(string mensagem) : Exception(mensagem);
