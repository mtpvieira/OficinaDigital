# Oficina Digital

Sistema Integrado de Atendimento e Execução de Serviços para uma oficina mecânica de médio porte.

MVP do back-end construído com **Domain-Driven Design**, monolito em camadas, .NET 10 e SQL Server.
Cobre o ciclo completo da Ordem de Serviço — do recebimento do veículo à entrega — com orçamento
versionado, aprovação pelo cliente via aplicativo e controle de estoque com reserva.

> Tech Challenge — Fase 1 · POS TECH Arquitetura de Software · 15SOAT

---

## Sumário

- [O que o sistema faz](#o-que-o-sistema-faz)
- [Arquitetura](#arquitetura)
- [Modelagem DDD](#modelagem-ddd)
- [Escolha do banco de dados](#escolha-do-banco-de-dados)
- [Como executar](#como-executar)
- [Autenticação e perfis](#autenticação-e-perfis)
- [Roteiro de uso da API](#roteiro-de-uso-da-api)
- [Endpoints](#endpoints)
- [Testes e cobertura](#testes-e-cobertura)
- [Segurança](#segurança)
- [Estrutura do repositório](#estrutura-do-repositório)

---

## O que o sistema faz

A oficina trabalhava com anotações e planilhas. O sistema resolve os cinco problemas
levantados no diagnóstico:

| Problema | Como o sistema resolve |
|---|---|
| Erros na priorização dos atendimentos | Fila de OS por status, com painel de contagem e listagem filtrável |
| Falhas no controle de peças e insumos | Estoque com saldo, reservado e disponível; reserva no diagnóstico e subtração na execução |
| Dificuldade em acompanhar o status | Sete status com transições automáticas por política; consulta pelo app do cliente |
| Perda de histórico de clientes e veículos | Toda OS fica ligada ao cliente e ao veículo por ID; inativar cadastro nunca apaga histórico |
| Ineficiência no fluxo de orçamentos | Orçamento gerado automaticamente ao fechar o diagnóstico, versionado e respondido pelo app |

### Ciclo de vida da Ordem de Serviço

```
RECEBIDA ──► EM_DIAGNOSTICO ──► AGUARDANDO_APROVACAO ──► EM_EXECUCAO ──► FINALIZADA ──► ENTREGUE
    │              │                      │  ▲                 │
    │              │                      │  └─────────────────┘
    │              │                      │   orçamento complementar
    └──────────────┴──────────────────────┴──► CANCELADA
```

Nenhum endpoint muda o status diretamente. As sete transições são consequência de comandos
e políticas — exatamente como no Event Storming. `CANCELADA` é o estado terminal de exceção:
reaproveitar `FINALIZADA` sujaria o indicador de tempo médio com OS que nunca executaram.

---

## Arquitetura

Monolito em camadas, conforme permitido para o MVP. As dependências apontam sempre para dentro:

```
┌───────────────────────────────────────────────────────────┐
│  OficinaDigital.Api            Controllers, JWT, Swagger  │
│                                middleware de erros        │
├───────────────────────────────────────────────────────────┤
│  OficinaDigital.Infrastructure EF Core, repositórios,     │
│                                UnitOfWork                 │
├───────────────────────────────────────────────────────────┤
│  OficinaDigital.Application    Casos de uso, DTOs,        │
│                                POLÍTICAS do Event Storming│
├───────────────────────────────────────────────────────────┤
│  OficinaDigital.Domain         Agregados, objetos de      │
│                                valor, invariantes, eventos│
└───────────────────────────────────────────────────────────┘
```

O projeto **Domain** não referencia nenhum pacote: é C# puro, com as regras de negócio e
nenhuma dependência de framework.

### As políticas são código

Cada post-it roxo do Event Storming (*"quando X, então Y"*) virou uma classe que implementa
`IPoliticaDeDominio<TEvento>`. Depois do commit, o `UnitOfWork` colhe os eventos publicados
pelos agregados e despacha para as políticas registradas, repetindo enquanto surgirem eventos
novos. É o que faz, por exemplo, o orçamento nascer sozinho quando o diagnóstico fecha.

| Evento | Política |
|---|---|
| `PecaCadastrada` | Abre a posição de estoque com saldo zero |
| `PecasDaOsRegistradas` | Reserva no estoque; faltando peça, marca o item como pendente de compra |
| `EntradaDePecasRegistrada` | Reserva automaticamente para as OS que estavam na fila |
| `DiagnosticoFinalizado` | Gera o orçamento principal |
| `ServicosEPecasAdicionaisRegistrados` | Reserva as peças e gera o orçamento complementar |
| `OrcamentoEnviadoAoCliente` | Muda o status da OS para *Aguardando aprovação* |
| `OrcamentoAprovado` | Libera a execução (ou retoma, no complementar) |
| `OrcamentoReprovado` | Cancela a OS (ou remove os itens adicionais) |
| `ServicoIniciado` | Subtrai do estoque as peças reservadas |
| `OsCancelada` | Libera as reservas de peça |

Os contextos vivem no mesmo processo, mas o desenho já é o de consistência eventual: trocar
o despachante em memória por uma fila não exige mudar o domínio nem os casos de uso.

---

## Modelagem DDD

A documentação completa está em [`Docs/Oficina_EventStorming_v2.drawio`](Docs/Oficina_EventStorming_v2.drawio)
(abra em [diagrams.net](https://app.diagrams.net) e ative o separador de páginas). São 10 páginas:
big picture, process modeling dos dois fluxos, design level, mapa de contextos, linguagem ubíqua,
decisões de modelagem e rastreabilidade dos requisitos.

### Bounded Contexts

| Contexto | Tipo | Agregados |
|---|---|---|
| **Atendimento** | Núcleo | `OrdemDeServico` (com `ItemDeServico`, `ItemDePeca`) |
| **Orçamento** | Núcleo | `Orcamento` (com `VersaoOrcamento`, `ItemOrcado`) |
| **Estoque** | Apoio | `ItemEstoque` (com `ReservaEstoque`, `MovimentoEstoque`) |
| **Catálogo** | Apoio | `Servico`, `Peca` |
| **Cadastro** | Apoio | `Cliente`, `Veiculo` |
| **Identidade** | Genérico | `Usuario` |

Referência entre agregados é sempre por ID, nunca por objeto. Uma transação altera um agregado
só; o que precisa chegar nos outros vai por política.

### Objetos de valor com regra de negócio dentro

- `CpfCnpj` — valida formato **e dígito verificador**; distingue PF de PJ
- `Placa` — aceita o padrão antigo (`AAA0000`) e o Mercosul (`AAA0A00`); imutável após o cadastro
- `Dinheiro` — sempre 2 casas, nunca negativo, não soma moedas diferentes
- `Quantidade` — nunca negativa, não opera unidades diferentes
- `JanelaDeExecucao` — calcula a duração real descontando o tempo parado aguardando aprovação
- `Contato`, `Endereco`, `Sku`, `Duracao`, `PrecoVigente`, `RespostaCliente`

### Invariantes centrais

**Ordem de Serviço**
- Um veículo tem no máximo uma OS ativa por vez
- Só entra em execução com orçamento aprovado pelo cliente
- Não conclui com item adicional pendente de resposta do cliente
- Só cancela antes de o serviço ser concluído, e sempre com motivo

**Estoque**
- `saldo >= 0` sempre e `reservado <= saldo`
- Reserva só é aceita se a quantidade for menor ou igual ao **disponível** (`saldo − reservado`)
- A subtração consome a reserva: saldo e reservado caem na mesma transação

**Orçamento**
- Exatamente uma versão vigente por vez
- A resposta do cliente é irreversível para a versão respondida
- Alterar cria nova versão; a anterior vira histórico e nunca é apagada
- Só a versão já enviada ao cliente aceita resposta

---

## Escolha do banco de dados

**SQL Server 2022**, com Entity Framework Core 10.

O enunciado deixa a escolha livre, desde que justificada. O critério que pesou foi o
**transacional**, não o de familiaridade:

1. **As invariantes do domínio são transacionais.** Reservar peça exige ler o disponível e
   gravar o reservado atomicamente; a subtração precisa derrubar saldo e reservado *na mesma
   transação*. Sem ACID, essas regras viram condição de corrida — estoque negativo em produção.

2. **O modelo é relacional por natureza.** Cliente → Veículo → OS → Itens → Orçamento → Versões
   formam um grafo de chaves estrangeiras. Um banco de documentos exigiria duplicar dados de
   catálogo dentro da OS ou fazer *joins* na aplicação.

3. **Consultas analíticas com `JOIN` e agregação.** O painel de tempo médio de execução cruza
   OS, itens de serviço e catálogo. É SQL, e SQL é o que um relacional faz melhor.

4. **Unicidade garantida pelo banco.** CPF/CNPJ, placa, SKU e nome de serviço têm índice único.
   A regra não depende de a aplicação lembrar de checar antes.

5. **Ecossistema .NET.** Provider oficial, migrations maduras e ferramental (SSMS, Azure Data
   Studio) que o time já usa. Menos atrito para quem for manter.

**Por que não PostgreSQL:** atenderia igualmente bem e a imagem Docker é bem mais leve. A
decisão pelo SQL Server foi do grupo, pela padronização com o restante do stack.

**Por que não MongoDB:** as invariantes que atravessam agregados (`OrdemDeServico` ↔ `ItemEstoque`)
precisariam de transação multi-documento — que existe, mas custa caro e joga fora a simplicidade
que seria a razão de escolher documentos.

---

## Como executar

### Opção 1 — Docker (recomendado)

Único pré-requisito: **Docker** com Compose v2.

```bash
git clone <url-do-repositorio>
cd OficinaDigital
cp .env.example .env
docker compose up -d --build
```

Pronto. A API sobe em `http://localhost:8080`, aplica as migrations sozinha e cria um catálogo
de exemplo com serviços, peças e estoque inicial.

| Recurso | URL |
|---|---|
| Swagger | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |

Acompanhar os logs e derrubar o ambiente:

```bash
docker compose logs -f api
```

```bash
docker compose down -v
```

> **Antes de subir:** edite o `.env`. Ele já vem com valores que funcionam localmente, mas
> `JWT_CHAVE` e `ADMIN_SENHA` **devem** ser trocados em qualquer ambiente que não seja a sua
> máquina. O `.env` está no `.gitignore`.

### Opção 2 — Rodando local, com o banco no Docker

Pré-requisitos: **.NET SDK 10** e Docker (só para o SQL Server).

```bash
docker compose up -d banco
```

```bash
dotnet run --project src/OficinaDigital.Api
```

A API sobe em `https://localhost:7xxx` (a porta aparece no console) usando o
`appsettings.Development.json`, que já aponta para `localhost:1433`.

### Migrations

São aplicadas automaticamente na inicialização. Para rodar à mão:

```bash
dotnet tool install --global dotnet-ef
```

```bash
dotnet ef database update --project src/OficinaDigital.Infrastructure --startup-project src/OficinaDigital.Api
```

Para desligar a aplicação automática, defina `AplicarMigrationsNaInicializacao=false`.

---

## Autenticação e perfis

Todas as APIs administrativas exigem **JWT**. São dois escopos de token:

| Escopo | Endpoint de login | Alcance |
|---|---|---|
| Interno | `POST /api/auth/login` | APIs administrativas, conforme o perfil |
| Cliente | `POST /api/auth/login-cliente` | Apenas as OS dos próprios veículos |

### Perfis internos

| Perfil | O que faz |
|---|---|
| `ATENDENTE` | Abre a OS, cuida do orçamento e da entrega; mantém os cadastros |
| `MECANICO` | Faz o diagnóstico e executa o serviço |
| `ADMINISTRADOR` | Tudo, mais a gestão de usuários |
| `CLIENTE` | Acompanha e responde o orçamento das próprias OS |

O primeiro administrador é criado na primeira execução com banco vazio, usando
`Seed__EmailAdministrador` e `Seed__SenhaAdministrador`. **Não há senha padrão embutida
no código**: sem a variável configurada, nenhum usuário é criado e a aplicação avisa no log.

Para criar os demais usuários, autentique como administrador e chame `POST /api/auth/usuarios`.

---

## Roteiro de uso da API

Sequência mínima para percorrer o fluxo inteiro no Swagger:

1. **Autentique** em `POST /api/auth/login` e clique em *Authorize*.
2. **Identifique o cliente** em `GET /api/clientes/por-documento/{documento}`.
   Devolveu 404? Cadastre em `POST /api/clientes`.
3. **Identifique o veículo** em `GET /api/veiculos/por-placa/{placa}`, ou cadastre em `POST /api/veiculos`.
4. **Abra a OS** em `POST /api/ordens-servico`. Status: *Recebida*.
5. **Inicie o diagnóstico** em `POST /api/ordens-servico/{id}/diagnostico/iniciar`.
6. **Registre serviços e peças** (`/servicos` e `/pecas`). A reserva no estoque é automática.
7. **Finalize o diagnóstico**. O orçamento é gerado sozinho.
8. **Envie o orçamento** em `POST /api/orcamentos/{id}/envio`. Status: *Aguardando aprovação*.
9. **Entre como cliente** em `POST /api/auth/login-cliente` e responda em
   `POST /api/orcamentos/{id}/resposta`.
10. **Inicie o serviço**. As peças saem do estoque. Status: *Em execução*.
11. **Finalize o serviço** e **registre a entrega**.
12. **Confira o indicador** em `GET /api/indicadores/tempo-medio-execucao`.

Achou um problema adicional no meio da execução? `POST /api/ordens-servico/{id}/itens-adicionais`
gera o orçamento complementar, suspende a execução e espera a resposta do cliente pelo app.

> Para percorrer tudo isso sem copiar GUID à mão, abra [`Docs/demo.http`](Docs/demo.http) no VS Code
> com a extensão REST Client e execute de cima para baixo.

---

## Endpoints

### Autenticação
| Método | Rota | Perfil |
|---|---|---|
| POST | `/api/auth/login` | público |
| POST | `/api/auth/login-cliente` | público |
| POST | `/api/auth/usuarios` | ADMINISTRADOR |
| GET | `/api/auth/usuarios` | ADMINISTRADOR |

### Clientes e veículos
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/clientes` · `/api/clientes/{id}` · `/api/clientes/por-documento/{documento}` | interno |
| POST · PUT · DELETE | `/api/clientes` | interno |
| GET | `/api/veiculos` · `/api/veiculos/{id}` · `/api/veiculos/por-placa/{placa}` · `/api/veiculos/do-cliente/{clienteId}` | interno |
| POST · PUT | `/api/veiculos` | interno |

### Catálogo
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/servicos` · `/api/pecas` | interno |
| POST · PUT · DELETE | `/api/servicos` · `/api/pecas` | ATENDENTE |
| PATCH | `/api/servicos/{id}/preco` · `/api/pecas/{id}/preco` | ATENDENTE |

### Estoque
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/estoque` · `/api/estoque/alertas` · `/api/estoque/pecas/{pecaId}` | interno |
| POST | `/api/estoque/disponibilidade` | interno |
| POST | `/api/estoque/pecas/{pecaId}/entradas` | ATENDENTE |
| PUT | `/api/estoque/pecas/{pecaId}/estoque-minimo` | ATENDENTE |

### Ordens de serviço
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/ordens-servico` · `/{id}` · `/por-numero/{numero}` · `/painel` | interno |
| POST | `/api/ordens-servico` | ATENDENTE |
| POST · DELETE | `/{id}/servicos` · `/{id}/pecas` | MECANICO |
| POST | `/{id}/diagnostico/iniciar` · `/finalizar` | MECANICO |
| POST | `/{id}/execucao/iniciar` · `/finalizar` | MECANICO |
| POST | `/{id}/problemas-adicionais` · `/{id}/itens-adicionais` | MECANICO |
| POST | `/{id}/entrega` · `/{id}/cancelamento` | ATENDENTE |

### Orçamentos
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/orcamentos/{id}` · `/api/orcamentos/da-os/{osId}` | interno |
| POST | `/api/orcamentos/{id}/envio` · `/alteracao` | ATENDENTE |
| POST | `/api/orcamentos/{id}/resposta` | CLIENTE ou interno |

### Cliente e indicadores
| Método | Rota | Perfil |
|---|---|---|
| GET | `/api/minhas-ordens` · `/api/minhas-ordens/{id}` | CLIENTE |
| GET | `/api/indicadores/tempo-medio-execucao` | interno |
| GET | `/health` | público |

---

## Testes e cobertura

```bash
dotnet test
```

Com relatório de cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Resultado

| Projeto | Testes | O que cobre |
|---|---|---|
| `OficinaDigital.Domain.Tests` | 211 | Invariantes, máquina de estados, objetos de valor — unitários puros |
| `OficinaDigital.Application.Tests` | 54 | Casos de uso e **políticas**, com EF Core real sobre SQLite em memória |
| `OficinaDigital.Api.IntegrationTests` | 27 | HTTP de ponta a ponta: JWT, autorização por perfil, cabeçalhos, middleware de erros e o roteiro de demonstração inteiro |
| **Total** | **292** | |

Cobertura de linhas, excluindo código gerado (migrations, source generators) e auto-properties:

| Camada | Cobertura |
|---|---|
| Domain | **95,4%** |
| Application | **93,9%** |
| Infrastructure | 96,4% |
| Api | 92,9% |

Os 80% exigidos nos domínios críticos são atingidos com folga.

Os testes de integração usam SQLite em memória para rodar sem depender de um SQL Server no
ambiente do avaliador. As particularidades do mapeamento para SQL Server são exercitadas pelas
migrations e pelo `docker compose`.

---

## Segurança

O relatório completo de análise de vulnerabilidades está em
[`Docs/RELATORIO-SEGURANCA.md`](Docs/RELATORIO-SEGURANCA.md).

Resumo do que foi implementado:

| Controle | Como |
|---|---|
| Autenticação | JWT HMAC-SHA256, sem tolerância de relógio, chave mínima de 32 caracteres validada na inicialização |
| Autorização | Políticas por perfil; o token do cliente carrega o `cliente_id` e só enxerga as próprias OS |
| Senhas | PBKDF2-HMAC-SHA256, 210.000 iterações, salt por usuário, comparação em tempo constante |
| Dados sensíveis | CPF/CNPJ e placa validados no formato, no dígito verificador e na unicidade |
| SQL Injection | EF Core com consultas parametrizadas; nenhuma concatenação de SQL |
| Vazamento de informação | Erro inesperado devolve mensagem genérica fora de Development, com id de correlação |
| Cabeçalhos HTTP | `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`; `Server` removido |
| Enumeração de contas | Login sempre responde "Credenciais inválidas", exista o usuário ou não |
| Segredos | Nenhum segredo no código; tudo por variável de ambiente, `.env` fora do versionamento |
| Contêiner | Imagem só com runtime, usuário não-root, sem SDK nem código-fonte |
| Dados pessoais em log | Contato mascarado antes de ir para o log |

Para rodar a análise de dependências:

```bash
dotnet list package --vulnerable --include-transitive
```

---

## Estrutura do repositório

```
OficinaDigital/
├── Docs/
│   ├── Oficina_EventStorming_v2.drawio    Event Storming completo (10 páginas)
│   ├── RELATORIO-SEGURANCA.md             Análise de vulnerabilidades
│   ├── ENTREGA.md                         Documento de entrega da fase
│   └── demo.http                          Fluxo completo pronto para executar
├── src/
│   ├── OficinaDigital.Domain/             Agregados, VOs, eventos, invariantes
│   │   ├── Common/                        Blocos de construção e objetos de valor
│   │   ├── Cadastro/                      Cliente, Veiculo
│   │   ├── Catalogo/                      Servico, Peca
│   │   ├── Estoque/                       ItemEstoque
│   │   ├── Atendimento/                   OrdemDeServico
│   │   ├── Orcamentos/                    Orcamento
│   │   └── Identidade/                    Usuario
│   ├── OficinaDigital.Application/        Casos de uso, DTOs
│   │   └── Politicas/                     Os post-its roxos do Event Storming
│   ├── OficinaDigital.Infrastructure/     EF Core, repositórios, JWT
│   └── OficinaDigital.Api/                Controllers, Swagger, middleware
├── tests/
│   ├── OficinaDigital.Domain.Tests/
│   ├── OficinaDigital.Application.Tests/
│   └── OficinaDigital.Api.IntegrationTests/
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── coverlet.runsettings
```

---

## Escopo deliberadamente fora do MVP

O Event Storming levantou o domínio inteiro da oficina. O MVP implementa a fatia que o Tech
Challenge pede — nem mais, nem menos. O que ficou modelado no quadro mas fora do código, e por quê:

- **Notificação ao cliente** — o quadro mapeia os avisos de orçamento pronto e veículo pronto.
  Implementar exigiria um provedor de e-mail/SMS e uma política de reenvio que o enunciado não
  pede. O cliente acompanha pelo app, em `GET /api/minhas-ordens`.
- **Pagamento** — acontece fora da aplicação (maquininha, PIX ou espécie). Registrar o
  recebimento sem conciliação financeira nem nota fiscal seria meio caminho: a OS vai de
  *Finalizada* para *Entregue* sem essa etapa.
- **Prazo estimado de conclusão** — o indicador que o enunciado pede é o **tempo médio real**
  de execução, que o sistema calcula a partir da `JanelaDeExecucao`. Prometer prazo ao cliente
  é outra conversa, e depende de capacidade da oficina.
- **Inventário, devolução ao estoque e transferência de veículo** — operações reais da oficina,
  mas fora do CRUD com controle de estoque que o enunciado descreve.
- **Pedido de compra e cadastro de fornecedor** — o estoque emite alerta de mínimo e registra a
  entrada; a compra acontece fora do sistema.
- **Garantia e retrabalho pós-entrega** — OS entregue é terminal; retorno do mesmo defeito abre
  OS nova.
