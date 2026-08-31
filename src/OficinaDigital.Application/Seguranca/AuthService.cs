using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Identidade;
using OficinaDigital.Domain.Identidade.Repositories;

namespace OficinaDigital.Application.Seguranca;

public interface ITokenService
{
    TokenResponse GerarParaUsuario(Usuario usuario);
    TokenResponse GerarParaCliente(ClienteId clienteId, string nome);
}

public interface IPasswordHasher
{
    (string Hash, string Salt) Gerar(string senha);
    bool Verificar(string senha, string hash, string salt);
}

public class AuthService(
    IUsuarioRepository usuarios,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    ITokenService tokens,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    ILogger<AuthService> logger)
{
    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        if (usuario is null || !usuario.Ativo || !hasher.Verificar(request.Senha, usuario.SenhaHash, usuario.SenhaSalt))
        {
            logger.LogWarning("Tentativa de login malsucedida para {Email}.", request.Email);
            throw new CredencialInvalidaException("Credenciais inválidas.");
        }

        usuario.RegistrarAcesso();
        await uow.CommitAsync(ct);

        return tokens.GerarParaUsuario(usuario);
    }

    public async Task<TokenResponse> LoginClienteAsync(LoginClienteRequest request, CancellationToken ct = default)
    {
        var documento = CpfCnpj.Criar(request.Documento);
        var placa = Placa.Criar(request.Placa);

        var cliente = await clientes.ObterPorDocumentoAsync(documento, ct);
        var veiculo = await veiculos.ObterPorPlacaAsync(placa, ct);

        if (cliente is null || !cliente.Ativo || veiculo is null || veiculo.ClienteId != cliente.Id)
        {
            logger.LogWarning("Tentativa de login de cliente malsucedida para a placa {Placa}.", placa.Valor);
            throw new CredencialInvalidaException("Documento ou placa inválidos.");
        }

        return tokens.GerarParaCliente(cliente.Id, cliente.Nome);
    }

    public async Task<UsuarioResponse> CriarUsuarioAsync(CriarUsuarioRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await usuarios.ExisteComEmailAsync(email, ct))
            throw new ConflitoDeNegocioException($"Já existe usuário com o e-mail {email}.");

        var (hash, salt) = hasher.Gerar(request.Senha);
        var usuario = Usuario.Criar(request.Nome, email, hash, salt, request.Perfil);

        await usuarios.AdicionarAsync(usuario, ct);
        await uow.CommitAsync(ct);

        return UsuarioResponse.De(usuario);
    }

    public async Task<IReadOnlyList<UsuarioResponse>> ListarUsuariosAsync(CancellationToken ct = default)
    {
        var lista = await usuarios.ListarAsync(ct);
        return lista.Select(UsuarioResponse.De).ToList();
    }
}
