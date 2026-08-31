using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.Events;

public sealed record ClienteCadastrado(ClienteId ClienteId, string Documento) : DomainEvent;

public sealed record DadosDoClienteAlterados(ClienteId ClienteId) : DomainEvent;

public sealed record ClienteInativado(ClienteId ClienteId) : DomainEvent;

public sealed record VeiculoCadastrado(VeiculoId VeiculoId, ClienteId ClienteId, string Placa) : DomainEvent;

public sealed record DadosDoVeiculoAlterados(VeiculoId VeiculoId) : DomainEvent;
