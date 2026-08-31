using OficinaDigital.Domain.Cadastro.Events;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro;

public class Cliente : AggregateRoot<ClienteId>
{
    private readonly List<Contato> _contatos = [];

    public string Nome { get; private set; } = null!;
    public CpfCnpj Documento { get; private set; } = null!;
    public IReadOnlyCollection<Contato> Contatos => _contatos.AsReadOnly();
    public Endereco? Endereco { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected Cliente() { }

    private Cliente(ClienteId id, string nome, CpfCnpj documento, IEnumerable<Contato> contatos, Endereco? endereco)
    {
        Id = id;
        Nome = nome;
        Documento = documento;
        _contatos.AddRange(contatos);
        Endereco = endereco;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public static Cliente Cadastrar(string? nome, string? documento, IEnumerable<Contato> contatos,
        Endereco? endereco = null)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(nome), "Nome do cliente é obrigatório.");

        var listaContatos = contatos?.Distinct().ToList() ?? [];
        DomainException.Se(listaContatos.Count == 0,
            "Ao menos um contato (e-mail ou telefone) é obrigatório. É por ele que sai toda notificação.");

        var cliente = new Cliente(ClienteId.Novo(), nome!.Trim(), CpfCnpj.Criar(documento), listaContatos, endereco);
        cliente.Publicar(new ClienteCadastrado(cliente.Id, cliente.Documento.Numero));
        return cliente;
    }

    public void AlterarDados(string? nome, IEnumerable<Contato> contatos, Endereco? endereco)
    {
        DomainException.Se(!Ativo, "Cliente inativo não pode ser alterado.");
        DomainException.Se(string.IsNullOrWhiteSpace(nome), "Nome do cliente é obrigatório.");

        var listaContatos = contatos?.Distinct().ToList() ?? [];
        DomainException.Se(listaContatos.Count == 0,
            "Ao menos um contato (e-mail ou telefone) é obrigatório.");

        Nome = nome!.Trim();
        _contatos.Clear();
        _contatos.AddRange(listaContatos);
        Endereco = endereco;

        Publicar(new DadosDoClienteAlterados(Id));
    }

    public void Inativar()
    {
        if (!Ativo) return;

        Ativo = false;
        Publicar(new ClienteInativado(Id));
    }

    public Contato ContatoPreferencial() =>
        _contatos.FirstOrDefault(c => c.Canal == CanalContato.EMAIL)
        ?? _contatos.First();
}
