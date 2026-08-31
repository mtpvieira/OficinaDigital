# Documento de entrega — Tech Challenge Fase 1

> Modelo para exportar em PDF. Preencha os campos entre `<< >>` antes de enviar.

## Identificação

| | |
|---|---|
| Curso | Pós-Tech — Arquitetura de Sistemas .NET com Azure |
| Turma | 15SOAT |
| Fase | 1 — Tech Challenge |

### Participantes

| Nome | RM |
|---|---|---|
| Matheus de Paula Vieira | `<<RM>>` |
| Igor Henrique Salvador | `<<RM>>` |
| Kelvin Gabriel Ribeiro | RM376986 |

## Links

| Item | Link |
|---|---|
| Repositório (privado) | https://github.com/mtpvieira/OficinaDigital |
| Documentação DDD — Event Storming | [`Docs/Oficina_EventStorming_v2.drawio`](Oficina_EventStorming_v2.drawio) |
| Vídeo de demonstração | youtube.com |
| Relatório de vulnerabilidades | [`Docs/RELATORIO-SEGURANCA.md`](RELATORIO-SEGURANCA.md) |

## O sistema

**Oficina Digital** — back-end do Sistema Integrado de Atendimento e Execução de Serviços de uma
oficina mecânica de médio porte. Cobre o ciclo da Ordem de Serviço do recebimento do veículo à
entrega, com orçamento gerado automaticamente, aprovação pelo cliente via aplicativo e controle
de estoque com reserva.

Monolito em camadas (.NET 10, SQL Server 2022, EF Core), modelado com Domain-Driven Design.

### Como executar

```bash
git clone https://github.com/mtpvieira/OficinaDigital.git && cd OficinaDigital
cp .env.example .env
docker compose up -d --build
```

Swagger em http://localhost:8080/swagger. As instruções completas estão no
[README](../README.md).

## Atendimento aos requisitos

| Requisito do enunciado | Onde está |
|---|---|
| Identificação do cliente por CPF/CNPJ | `GET /api/clientes/por-documento/{documento}` |
| Cadastro de veículo (placa, marca, modelo, ano) | `POST /api/veiculos` |
| Inclusão dos serviços solicitados | `POST /api/ordens-servico/{id}/servicos` |
| Inclusão de peças e insumos | `POST /api/ordens-servico/{id}/pecas` |
| Orçamento gerado automaticamente | Política `GerarOrcamentoAoFinalizarDiagnostico` |
| Envio do orçamento ao cliente | `POST /api/orcamentos/{id}/envio` |
| Os seis status da OS | `StatusOS` — mais `CANCELADA` como terminal de exceção |
| Alteração automática de status | Máquina de estados no agregado, disparada pelas políticas |
| Consulta pelo cliente via API | `GET /api/minhas-ordens/{id}`, com token de cliente |
| CRUD de clientes | `/api/clientes` |
| CRUD de veículos | `/api/veiculos` |
| CRUD de serviços | `/api/servicos` |
| CRUD de peças com controle de estoque | `/api/pecas` e `/api/estoque` |
| Listagem e detalhamento de OS | `GET /api/ordens-servico`, `GET /api/ordens-servico/{id}` |
| Tempo médio de execução | `GET /api/indicadores/tempo-medio-execucao` |
| Autenticação JWT nas APIs administrativas | `POST /api/auth/login` + políticas por perfil |
| Validação de CPF/CNPJ e placa | Objetos de valor `CpfCnpj` e `Placa`, com dígito verificador |
| Testes unitários e de integração | 292 testes; 95,4% de cobertura no domínio |
| Back-end monolítico em camadas | Domain · Application · Infrastructure · Api |
| Justificativa do banco | Seção "Escolha do banco de dados" do README |
| APIs documentadas via Swagger | http://localhost:8080/swagger |
| Dockerfile e docker-compose | Raiz do repositório |
| Cobertura mínima de 80% | 95,4% de linhas no total |
| README explicativo | [README.md](../README.md) |

## Relatório de vulnerabilidades

A análise completa está em [`RELATORIO-SEGURANCA.md`](RELATORIO-SEGURANCA.md): escopo, ferramentas,
saída dos scans, 13 achados classificados por severidade e rastreabilidade contra o OWASP API
Security Top 10 (2023).

Resumo:

| Severidade | Encontrados | Corrigidos | Aceitos | Em aberto |
|---|---|---|---|---|
| Crítica | 0 | — | — | — |
| Alta | 3 | 2 | 0 | 1 |
| Média | 4 | 3 | 1 | 0 |
| Baixa | 3 | 1 | 2 | 0 |
| Informativa | 3 | 1 | 2 | 0 |

O scan de dependências (`dotnet list package --vulnerable --include-transitive`) não apontou
nenhuma CVE conhecida. O item em aberto é o **ALTA-01** — ausência de proteção contra força bruta
no login —, cuja correção efetiva mora no ponto de entrada da infraestrutura e está especificada
para a Fase 2.
