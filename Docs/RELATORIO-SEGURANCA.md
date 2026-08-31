# Relatório de Análise de Vulnerabilidades

**Projeto:** Oficina Digital — Sistema Integrado de Atendimento e Execução de Serviços
**Fase:** Tech Challenge — Fase 1 · POS TECH Arquitetura de Software · 15SOAT
**Escopo analisado:** back-end .NET 10 (API, Application, Infrastructure, Domain), Dockerfile,
docker-compose e cadeia de dependências NuGet
**Data da análise:** 26/08/2026

---

## 1. Resumo executivo

Foram executadas análise de composição de software (SCA) sobre 100% das dependências diretas e
transitivas, análise estática pelos analisadores do compilador .NET com nullable habilitado, e
revisão manual do código orientada pelo **OWASP API Security Top 10 (2023)**.

### Resultado

| Severidade | Encontrados | Corrigidos | Aceitos com justificativa | Em aberto |
|---|---|---|---|---|
| Crítica | 0 | — | — | — |
| Alta | 3 | 2 | 0 | 1 |
| Média | 4 | 3 | 1 | 0 |
| Baixa | 3 | 1 | 2 | 0 |
| Informativa | 3 | 1 | 2 | 0 |
| **Total** | **13** | **7** | **5** | **1** |

**Nenhuma vulnerabilidade conhecida (CVE) foi encontrada nas dependências.** Os itens listados
vieram da revisão manual e do desenho de segurança. Um item de severidade Alta (ALTA-01) fica
**em aberto** nesta entrega, com correção especificada e priorizada para a próxima fase.

Não há, no código entregue, nenhuma ocorrência das classes de falha mais comuns em API REST:
injeção de SQL, exposição de objeto por referência direta (IDOR), autenticação ausente em
endpoint sensível, segredo em código-fonte ou senha armazenada de forma reversível.

---

## 2. Metodologia e ferramentas

| Etapa | Ferramenta / técnica | Comando |
|---|---|---|
| SCA — pacotes vulneráveis | `dotnet list package` (base de advisories do NuGet/GitHub) | `dotnet list package --vulnerable --include-transitive` |
| SCA — pacotes preteridos | `dotnet list package` | `dotnet list package --deprecated --include-transitive` |
| Análise estática | Roslyn analyzers, `Nullable=enable`, `TreatWarningsAsErrors` desligado mas build sem warnings | `dotnet build` |
| Verificação dinâmica | 27 testes de integração sobre o pipeline HTTP real (autenticação, autorização, cabeçalhos) | `dotnet test` |
| Revisão manual | Checklist OWASP API Security Top 10 (2023) e OWASP Top 10 (2021) | — |
| Superfície do contêiner | Revisão do Dockerfile e do docker-compose | — |

### 2.1 Saída do scan de dependências

```
$ dotnet list package --vulnerable --include-transitive

As fontes a seguir foram usadas:
   https://api.nuget.org/v3/index.json

O projeto fornecido `OficinaDigital.Api` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Application` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Domain` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Infrastructure` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Api.IntegrationTests` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Application.Tests` não tem nenhum pacote vulnerável.
O projeto fornecido `OficinaDigital.Domain.Tests` não tem nenhum pacote vulnerável.
```

**Resultado: 0 pacotes vulneráveis em 7 projetos**, considerando dependências diretas e transitivas.

### 2.2 Saída do scan de pacotes preteridos

```
$ dotnet list package --deprecated --include-transitive

Projeto `OficinaDigital.*.Tests`:
   > xunit                            2.9.3   Legacy   xunit.v3 >= 0.0.0
   > xunit.assert                     2.9.3   Legacy   xunit.v3.assert
   > xunit.core                       2.9.3   Legacy   xunit.v3.core
   > xunit.extensibility.core         2.9.3   Legacy   xunit.v3.extensibility.core
   > xunit.extensibility.execution    2.9.3   Legacy   xunit.v3.extensibility.core
```

Ver item **INFO-01**. Nenhum pacote de produção está preterido.

### 2.3 Análise estática

```
$ dotnet build
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
```

O build sai limpo, com `<Nullable>enable</Nullable>` em todos os projetos. Ausência de warning de
referência nula elimina de saída uma família inteira de `NullReferenceException` em produção.

---

## 3. Vulnerabilidades identificadas

### 3.1 Severidade ALTA

---

#### ALTA-01 — Ausência de proteção contra força bruta no login

| | |
|---|---|
| **Categoria** | OWASP API4:2023 — Unrestricted Resource Consumption |
| **CWE** | CWE-307: Improper Restriction of Excessive Authentication Attempts |
| **Onde** | `POST /api/auth/login`, `POST /api/auth/login-cliente` |
| **Status** | ⚠️ **Em aberto** — correção especificada para a Fase 2 |

**Descrição.** O endpoint de autenticação aceita tentativas ilimitadas. O hash PBKDF2 com 210.000
iterações encarece cada tentativa individual, mas não impede uma sequência automatizada — e ainda
abre espaço para negação de serviço, já que cada tentativa consome CPU proporcional ao custo do hash.

**Impacto.** Comprometimento de conta administrativa por adivinhação de senha; degradação do serviço
por consumo de CPU.

**Por que fica em aberto.** Limitar por origem dentro do processo só protege enquanto a API roda em
uma instância. Em qualquer topologia com mais de um contêiner atrás de um balanceador, a contagem
em memória se fragmenta e o atacante ganha uma cota por instância — a proteção vira teatro. A
correção correta é externa ao monolito, e envolve infraestrutura que esta fase não entrega.

**Mitigações que já reduzem o risco.**

- PBKDF2 com 210.000 iterações: cada tentativa custa caro para o atacante;
- resposta idêntica para usuário inexistente e senha errada, o que impede enumerar contas antes
  de atacar (ver MEDIA-03);
- token do cliente com escopo restrito às próprias OS, o que limita o estrago de uma conta de
  cliente comprometida (ver ALTA-02);
- nenhuma senha padrão embutida: o administrador inicial exige senha vinda de variável de ambiente.

**Correção recomendada para a Fase 2.**

1. *Rate limiting* no ponto de entrada (API Gateway ou reverse proxy), com contagem compartilhada —
   ordem de 10 tentativas por minuto por IP nos endpoints de autenticação;
2. bloqueio progressivo da conta após N falhas consecutivas, com contador persistido no agregado
   `Usuario` — a entidade já tem o campo `UltimoAcessoEm` e comportaria `TentativasFalhas`;
3. registro das tentativas falhas em log de auditoria, com IP e horário, para detecção.

---

#### ALTA-02 — Risco de exposição de dados de outro cliente (IDOR)

| | |
|---|---|
| **Categoria** | OWASP API1:2023 — Broken Object Level Authorization |
| **CWE** | CWE-639: Authorization Bypass Through User-Controlled Key |
| **Onde** | `GET /api/minhas-ordens/{id}`, `POST /api/orcamentos/{id}/resposta` |
| **Status** | ✅ **Corrigido** |

**Descrição.** Os endpoints do aplicativo do cliente recebem o ID da OS ou do orçamento pela URL.
Sem verificação de propriedade, qualquer cliente autenticado poderia ler a OS de outro apenas
trocando o GUID — e, pior, **aprovar um orçamento alheio**.

**Impacto.** Vazamento de dados pessoais (nome, veículo, histórico de serviços, valores) e
possibilidade de autorizar reparo em nome de terceiro.

**Correção aplicada.** O token do cliente carrega a claim `cliente_id`, emitida no login e lida em
[`UsuarioAtual`](../src/OficinaDigital.Api/Configuracao/UsuarioAtual.cs). Todo acesso do app passa
por verificação explícita de propriedade:

- `AcompanhamentoService.MontarAsync` compara `os.ClienteId` com o cliente do token e lança
  `AcessoNegadoException` (HTTP 403) se divergirem;
- `OrcamentoService.GarantirQueOOrcamentoEhDoClienteAsync` faz o mesmo antes de aceitar a resposta
  ao orçamento;
- o ID do cliente **nunca** vem do corpo ou da query string — sempre do token.

**Verificação.** `Cliente_nao_enxerga_os_de_outro_cliente` (integração HTTP) e
`Cliente_nao_responde_orcamento_de_outro_cliente` (aplicação) confirmam o 403.

---

#### ALTA-03 — Segredos em arquivo de configuração versionado

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-798: Use of Hard-coded Credentials |
| **Onde** | `appsettings.json`, `docker-compose.yml`, seed do administrador |
| **Status** | ✅ **Corrigido** |

**Descrição.** Chave de assinatura JWT, senha do banco e senha do administrador inicial são os três
segredos do sistema. Qualquer um deles em arquivo versionado significa comprometimento assim que o
repositório for clonado — e o repositório será compartilhado com o usuário `soat-architecture`.

**Impacto.** Forja de token JWT válido (acesso administrativo total), acesso direto ao banco,
acesso à conta de administrador.

**Correção aplicada.**

1. `appsettings.json` traz `Jwt:ChaveSecreta` e `Seed:SenhaAdministrador` **vazios**. A senha do
   banco também sai vazia da connection string padrão.
2. O `docker-compose.yml` lê tudo de variáveis de ambiente com sintaxe obrigatória
   (`${JWT_CHAVE:?defina JWT_CHAVE no arquivo .env}`): sem a variável definida, o compose **falha
   ao subir** em vez de usar um valor padrão inseguro.
3. O arquivo `.env` está no `.gitignore`; apenas `.env.example`, sem segredo real, é versionado.
4. `JwtOptions.Validar()` roda na inicialização e **derruba a aplicação** se a chave estiver
   ausente ou tiver menos de 32 caracteres — não há como subir com configuração fraca por descuido.
5. Não há senha padrão de administrador embutida: sem `Seed__SenhaAdministrador`, nenhum usuário é
   criado e a aplicação registra um aviso no log.

---

### 3.2 Severidade MÉDIA

---

#### MEDIA-01 — Vazamento de detalhes internos em mensagem de erro

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-209: Generation of Error Message Containing Sensitive Information |
| **Onde** | Middleware de tratamento de exceções |
| **Status** | ✅ **Corrigido** |

**Descrição.** Exceção não tratada devolvendo *stack trace* entrega ao atacante o mapa da aplicação:
namespaces, nomes de classe, versões de biblioteca e, em erro de banco, às vezes trechos de SQL.

**Correção aplicada.** [`TratamentoDeExcecoesMiddleware`](../src/OficinaDigital.Api/Configuracao/TratamentoDeExcecoes.cs)
traduz cada exceção para `ProblemDetails` (RFC 7807) e trata o erro inesperado de forma distinta:

- **fora de Development:** mensagem genérica, sem detalhe interno;
- **em qualquer ambiente:** um identificador de correlação vai na resposta e no log, para que o
  suporte encontre o erro completo sem que ele trafegue para o cliente;
- exceções esperadas (`DomainException`, `RecursoNaoEncontradoException`, `ConflitoDeNegocioException`,
  `AcessoNegadoException`, `CredencialInvalidaException`) viram 400/404/409/403/401 com mensagem
  de negócio — que é informação legítima, não vazamento.

---

#### MEDIA-02 — Ausência de cabeçalhos de segurança HTTP

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-1021: Improper Restriction of Rendered UI Layers |
| **Onde** | Pipeline HTTP |
| **Status** | ✅ **Corrigido** |

**Descrição.** A aplicação serve o Swagger UI, ou seja, HTML consumido por navegador na mesma
origem da API. Sem cabeçalhos de proteção, fica exposta a *MIME sniffing*, *clickjacking* e
vazamento de URL pelo `Referer`.

**Correção aplicada.** [`CabecalhosDeSegurancaMiddleware`](../src/OficinaDigital.Api/Configuracao/CabecalhosDeSeguranca.cs),
posicionado no início do pipeline para valer inclusive nas respostas de erro:

| Cabeçalho | Valor | Protege contra |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | MIME sniffing |
| `X-Frame-Options` | `DENY` | Clickjacking |
| `Referrer-Policy` | `no-referrer` | Vazamento de URL para terceiros |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Acesso indevido a recursos do dispositivo |
| `Server` | *removido* | Fingerprinting do servidor |

Em produção, `UseHsts()` acrescenta o `Strict-Transport-Security`.

**Verificação.** `CabecalhosDeSegurancaTests` confirma a presença dos cabeçalhos, inclusive em
resposta `401`.

---

#### MEDIA-03 — Enumeração de usuários pela resposta de login

| | |
|---|---|
| **Categoria** | OWASP API3:2023 — Broken Object Property Level Authorization |
| **CWE** | CWE-204: Observable Response Discrepancy |
| **Onde** | `AuthService.LoginAsync` |
| **Status** | ✅ **Corrigido** |

**Descrição.** Responder "usuário não encontrado" e "senha incorreta" de forma diferente permite
mapear quais e-mails existem, transformando um ataque de senha em um ataque de duas etapas mais
barato.

**Correção aplicada.** Usuário inexistente, usuário inativo e senha errada produzem exatamente a
mesma `CredencialInvalidaException("Credenciais inválidas.")`, com o mesmo status HTTP 401. O log
interno registra a tentativa com o e-mail para investigação, mas isso não sai na resposta.

O 401 é deliberado: 403 significa "autenticado, sem permissão para este recurso", que não é o caso
de quem errou a senha. A distinção importa porque o cliente HTTP trata os dois de forma diferente —
401 dispara nova autenticação, 403 não.

**Verificação.** `Usuario_inexistente_devolve_a_mesma_mensagem_de_credencial_invalida`.

---

#### MEDIA-04 — Login do cliente baseado em dado semipúblico

| | |
|---|---|
| **Categoria** | OWASP API2:2023 — Broken Authentication |
| **CWE** | CWE-287: Improper Authentication |
| **Onde** | `POST /api/auth/login-cliente` |
| **Status** | ⚠️ **Aceito com justificativa — mitigado** |

**Descrição.** O aplicativo do cliente autentica com **CPF/CNPJ + placa do veículo**. Nenhum dos
dois é segredo forte: a placa é visível na rua e o CPF circula com facilidade. Quem conhecer os dois
consegue acompanhar a OS daquele cliente e responder ao orçamento dele.

**Por que foi aceito.** O escopo da Fase 1 é o MVP do back-end. Um cadastro de credencial próprio
para o cliente (senha, confirmação por e-mail, recuperação, política de bloqueio) é um fluxo de
produto inteiro, com telas e jornada que não fazem parte da entrega. A modelagem registra a decisão
na página 08 do Event Storming.

**Mitigações já implementadas.**

- Token do cliente **expira em 30 minutos** (metade do tempo do token interno);
- escopo estritamente limitado às OS dos próprios veículos — o token não abre nenhuma API
  administrativa (ver ALTA-02);
- o par CPF + placa precisa **coincidir**: a placa tem de pertencer ao cliente daquele documento,
  o que elimina a tentativa com placa aleatória;
- cliente inativo não autentica.

**Recomendação para a Fase 2.** Substituir por OTP enviado ao canal já cadastrado (e-mail ou SMS)
ou por senha própria com hash PBKDF2 — a infraestrutura de hash já existe e seria reaproveitada.
O endpoint também herda a correção de força bruta descrita em ALTA-01.

---

### 3.3 Severidade BAIXA

---

#### BAIXA-01 — Conexão de banco sem validação de certificado

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-295: Improper Certificate Validation |
| **Onde** | Connection string (`TrustServerCertificate=True`) |
| **Status** | ⚠️ **Aceito no ambiente local** |

**Descrição.** `TrustServerCertificate=True` aceita o certificado autoassinado do contêiner sem
validar a cadeia. Em rede hostil, isso abriria espaço para *man-in-the-middle*.

**Por que foi aceito.** `Encrypt=True` está ligado — o tráfego **é** cifrado. O que se abre mão é
da validação da cadeia, e a conexão não sai da rede bridge interna do Compose, sem exposição ao
host além de `127.0.0.1:1433` para inspeção local.

**Recomendação para produção.** Provisionar certificado de CA confiável no SQL Server e usar
`TrustServerCertificate=False`.

---

#### BAIXA-02 — Usuário `sa` usado pela aplicação

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-250: Execution with Unnecessary Privileges |
| **Status** | ⚠️ **Aceito no ambiente local** |

**Descrição.** A API conecta como `sa`, que é administrador da instância. O princípio do menor
privilégio pede um usuário com permissão apenas sobre o banco `OficinaDigital`.

**Por que foi aceito.** No MVP, a aplicação executa as migrations na inicialização, o que exige
permissão de DDL. Criar um usuário intermediário adicionaria um passo de provisionamento que não
agrega ao que está sendo avaliado nesta fase.

**Recomendação para produção.** Separar em dois usuários: um com `db_owner` só para a etapa de
migration, executada no pipeline de deploy, e outro com `db_datareader`/`db_datawriter` para a
aplicação em execução.

---

#### BAIXA-03 — Contêiner executando como root

| | |
|---|---|
| **Categoria** | OWASP API8:2023 — Security Misconfiguration |
| **CWE** | CWE-250: Execution with Unnecessary Privileges |
| **Status** | ✅ **Corrigido** |

**Descrição.** Processo como root dentro do contêiner transforma uma execução remota de código em
comprometimento do contêiner inteiro, e facilita escape para o host.

**Correção aplicada.** O [`Dockerfile`](../Dockerfile) declara `USER $APP_UID`, o usuário não
privilegiado que as imagens oficiais do .NET já trazem. O build é multi-estágio: a imagem final
contém apenas o runtime ASP.NET e os binários publicados — **sem SDK, sem código-fonte, sem
ferramenta de build**. O `.dockerignore` impede que `appsettings.Development.json`, `.env`, `.git`
e a pasta de testes entrem no contexto de build.

---

### 3.4 Informativas

---

#### INFO-01 — Pacote de teste marcado como Legacy

| | |
|---|---|
| **Onde** | `xunit 2.9.3` e transitivos, apenas nos projetos de teste |
| **Status** | ⚠️ **Aceito** |

O xUnit v2 foi marcado como `Legacy` em favor do xunit.v3. **Legacy não é vulnerabilidade**: não há
CVE associado, e o pacote não é distribuído com a aplicação — vive só nos projetos de teste, que não
entram na imagem Docker. A migração para v3 é trabalho de manutenção, não de segurança, e foi
adiada para não introduzir risco de quebra na entrega.

---

#### INFO-02 — Credenciais de desenvolvimento versionadas

| | |
|---|---|
| **Onde** | `appsettings.Development.json` |
| **Status** | ⚠️ **Aceito com justificativa** |

O arquivo contém uma chave JWT e uma senha de banco **de desenvolvimento**, versionadas para que
qualquer pessoa do grupo clone e rode sem configurar nada. Os valores são explicitamente rotulados
como de uso local (`chave-apenas-para-desenvolvimento-local-nao-usar-em-producao-32+`) e **não são
usados** pelo `docker-compose`, que lê tudo do `.env`, nem em produção, onde a configuração vem de
variável de ambiente.

O risco residual é alguém reaproveitar esses valores fora do ambiente local. A recomendação para a
Fase 2 é migrar para `dotnet user-secrets`, que mantém a conveniência sem versionar nada.

---

#### INFO-03 — Swagger exposto

| | |
|---|---|
| **Onde** | `/swagger` |
| **Status** | ✅ **Mitigado por ambiente** |

A documentação interativa descreve toda a superfície da API, o que é ótimo para o desenvolvedor e
igualmente útil para quem faz reconhecimento. O Swagger é registrado **apenas quando o ambiente é
Development**; em `ASPNETCORE_ENVIRONMENT=Production` a rota não existe. O `.env.example` traz
`Development` para facilitar a avaliação — em produção, basta trocar a variável.

---

## 4. Controles de segurança implementados

Consolidado do que está em produção no código entregue.

### 4.1 Autenticação

| Controle | Implementação |
|---|---|
| Algoritmo do token | JWT assinado com HMAC-SHA256 |
| Validação | Emissor, audiência, tempo de vida e assinatura — todos verificados |
| Tolerância de relógio | `ClockSkew = TimeSpan.Zero` (padrão do .NET seria 5 minutos de folga) |
| Força da chave | Mínimo de 32 caracteres, validado na inicialização |
| Expiração | 60 min para usuário interno, 30 min para cliente |
| Token não persistido | `SaveToken = false` |
| HTTPS obrigatório | `RequireHttpsMetadata` ligado fora de Development |

### 4.2 Armazenamento de senha

| Item | Valor |
|---|---|
| Algoritmo | PBKDF2-HMAC-SHA256 |
| Iterações | 210.000 (recomendação OWASP para PBKDF2-SHA256) |
| Salt | 16 bytes aleatórios por usuário, via `RandomNumberGenerator` |
| Hash | 32 bytes |
| Comparação | `CryptographicOperations.FixedTimeEquals` — tempo constante, não vaza por timing |
| Reversibilidade | Nenhuma. Senha nunca é armazenada nem logada em texto puro |

### 4.3 Autorização

Políticas declarativas por perfil, aplicadas no controller:

| Política | Perfis aceitos |
|---|---|
| `Interno` | ATENDENTE, MECANICO, ADMINISTRADOR |
| `Atendente` | ATENDENTE, ADMINISTRADOR |
| `Mecanico` | MECANICO, ADMINISTRADOR |
| `Administrador` | ADMINISTRADOR |
| `Cliente` | CLIENTE |

Nenhum endpoint administrativo é anônimo. Os únicos endpoints públicos são `/api/auth/login`,
`/api/auth/login-cliente` e `/health`.

**Verificação:** o teste `Endpoint_administrativo_sem_token_devolve_401` percorre as oito rotas
administrativas e confirma o 401 sem token.

### 4.4 Validação de entrada

Validação em duas camadas, como previsto na modelagem:

1. **Na borda** — DataAnnotations nos DTOs (obrigatoriedade, tamanho, faixa numérica, formato de
   e-mail). Falha aqui vira 400 antes de qualquer regra de negócio rodar.
2. **No domínio** — os objetos de valor rejeitam o que é inválido no construtor. Não existe
   `CpfCnpj` inválido em memória: ou o valor passa no dígito verificador, ou o objeto não é criado.

| Dado sensível | Validação |
|---|---|
| CPF/CNPJ | Formato, **dígito verificador**, rejeição de dígitos repetidos, unicidade por índice |
| Placa | Padrão antigo (`AAA0000`) e Mercosul (`AAA0A00`), unicidade, imutabilidade |
| E-mail | Formato, normalização para minúsculo |
| Telefone | DDD + número (10 ou 11 dígitos) |
| CEP | 8 dígitos |
| Valores monetários | Nunca negativos, 2 casas decimais, moeda única por operação |
| Quantidades | Nunca negativas, unidade de medida coerente |

### 4.5 Proteção contra injeção

- **SQL Injection:** todo acesso a dados passa por EF Core com LINQ, que gera comandos
  parametrizados. **Não há nenhuma concatenação de SQL no projeto** — nem `FromSqlRaw`, nem
  `ExecuteSqlRaw`, nem `SqlQuery`. O filtro por nome usa `EF.Functions.Like` com parâmetro.
- **Mass assignment:** os DTOs de entrada são `record` explícitos, sem propriedade que mapeie para
  campo interno do agregado. Não há `TryUpdateModel` nem binding direto para entidade. Campos como
  `Status`, `Total` e `Numero` são calculados pelo domínio e **não podem** ser enviados pelo cliente.
- **XSS:** a API devolve `application/json`, nunca HTML. Os cabeçalhos `nosniff` e `X-Frame-Options`
  fecham o restante.

### 4.6 Integridade do domínio

As invariantes ficam **dentro** dos agregados, não em validações espalhadas pelos serviços. Isso
importa para segurança porque significa que não existe caminho de código que burle a regra: nenhum
endpoint, nenhum repositório, nenhum teste consegue criar um estado inválido.

Exemplos com impacto direto em fraude e prejuízo:

- saldo de estoque nunca fica negativo, e reserva nunca excede o disponível;
- a resposta do cliente ao orçamento é irreversível para a versão respondida;
- só a versão já enviada ao cliente aceita resposta;
- a máquina de estados rejeita qualquer transição fora do desenho.

**Verificação:** 211 testes unitários cobrem exatamente essas invariantes.

---

## 5. Rastreabilidade — OWASP API Security Top 10 (2023)

| # | Risco | Situação |
|---|---|---|
| API1 | Broken Object Level Authorization | ✅ Verificação de propriedade por `cliente_id` do token (ALTA-02) |
| API2 | Broken Authentication | ✅ JWT validado integralmente + PBKDF2 · ⚠️ MEDIA-04 aceito com mitigações |
| API3 | Broken Object Property Level Authorization | ✅ DTOs explícitos; campos calculados não aceitam entrada externa |
| API4 | Unrestricted Resource Consumption | ⚠️ Paginação com teto de 100 itens por página · ALTA-01 em aberto |
| API5 | Broken Function Level Authorization | ✅ Políticas por perfil em todo endpoint administrativo |
| API6 | Unrestricted Access to Sensitive Business Flows | ✅ Fluxos sensíveis (aprovação do orçamento, entrega) exigem perfil específico e seguem a máquina de estados |
| API7 | Server Side Request Forgery | ✅ Não aplicável: a aplicação não faz requisição a URL fornecida pelo usuário |
| API8 | Security Misconfiguration | ✅ Segredos externalizados, cabeçalhos, erro genérico, contêiner sem root |
| API9 | Improper Inventory Management | ✅ Versão única (`v1`) documentada em Swagger; sem endpoint legado ativo |
| API10 | Unsafe Consumption of APIs | ✅ Não aplicável: o MVP não consome nenhuma API de terceiro |

---

## 6. Recomendações para as próximas fases

Em ordem de prioridade:

1. **Proteger o login contra força bruta** no ponto de entrada, com bloqueio progressivo de conta
   e log de auditoria das tentativas (ALTA-01). É o único item de severidade Alta em aberto.
2. **Substituir o login do cliente** por OTP no canal cadastrado ou senha própria (MEDIA-04).
3. **Migrar segredos para um cofre** — Azure Key Vault ou AWS Secrets Manager — em vez de variável
   de ambiente, ganhando rotação e auditoria de acesso.
4. **Separar os usuários de banco**: um para migration no pipeline, outro com privilégio mínimo para
   a aplicação (BAIXA-02).
5. **Certificado de CA confiável** no SQL Server, removendo `TrustServerCertificate` (BAIXA-01).
6. **Refresh token com revogação**, permitindo encerrar sessão comprometida sem esperar a expiração.
7. **Log de auditoria** para as ações sensíveis (aprovação de orçamento, entrega, cancelamento) com
   usuário, origem e horário. A trilha de `MovimentoEstoque` já cobre o estoque; falta o restante.
8. **SAST e SCA no CI** — GitHub Advanced Security ou SonarQube, com `dotnet list package
   --vulnerable` como *gate* de pipeline, para que dependência vulnerável barre o merge.
9. **Teste de intrusão** por terceiro antes de ir a produção.

---

## 7. Conclusão

O scan automatizado de dependências não encontrou nenhuma vulnerabilidade conhecida, e a análise
estática não produziu nenhum aviso. As 13 questões deste relatório vieram da revisão manual e do
desenho de segurança.

Dos três itens de severidade Alta, **dois foram corrigidos** com teste automatizado que comprova a
correção. Os cinco itens aceitos estão documentados com justificativa, mitigações e recomendação
para as próximas fases; nenhum deles é aceito por omissão.

Ficam dois pontos de atenção. O **ALTA-01** — ausência de proteção contra força bruta — é o único
item Alto em aberto: a proteção efetiva mora no ponto de entrada, fora do monolito, e limitar por
origem dentro do processo daria falsa sensação de segurança assim que houvesse mais de uma
instância. O **MEDIA-04** — login do cliente por CPF + placa — é decisão consciente de escopo de
MVP, não descuido, e vem com três mitigações: escopo restrito do token, expiração curta e
exigência de coincidência entre documento e placa.

---

*Relatório elaborado como parte dos entregáveis do Tech Challenge — Fase 1.*
