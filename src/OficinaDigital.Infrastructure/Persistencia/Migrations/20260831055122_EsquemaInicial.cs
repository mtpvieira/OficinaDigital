using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OficinaDigital.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cadastro");

            migrationBuilder.EnsureSchema(
                name: "estoque");

            migrationBuilder.EnsureSchema(
                name: "orcamento");

            migrationBuilder.EnsureSchema(
                name: "atendimento");

            migrationBuilder.EnsureSchema(
                name: "catalogo");

            migrationBuilder.EnsureSchema(
                name: "identidade");

            migrationBuilder.CreateTable(
                name: "Clientes",
                schema: "cadastro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    TipoPessoa = table.Column<int>(type: "int", nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NumeroEndereco = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensDeEstoque",
                schema: "estoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PecaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unidade = table.Column<int>(type: "int", nullable: false),
                    EstoqueMinimo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reservado = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDeEstoque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                schema: "orcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    VersaoVigente = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdensDeServico",
                schema: "atendimento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1000, 1"),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MotivoCancelamento = table.Column<int>(type: "int", nullable: true),
                    ObservacaoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescricaoDoProblema = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecucaoInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecucaoFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TempoAguardandoAprovacaoTicks = table.Column<long>(type: "bigint", nullable: false),
                    ExecucaoSuspensaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrcamentoVigenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrcamentoAprovado = table.Column<bool>(type: "bit", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AbertaEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensDeServico", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pecas",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unidade = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pecas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servicos",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TempoPadraoMinutos = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "identidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenhaSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Perfil = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcessoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                schema: "cadastro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    PadraoPlaca = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AnoFabricacao = table.Column<int>(type: "int", nullable: false),
                    Cor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClienteContatos",
                schema: "cadastro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Canal = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteContatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteContatos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalSchema: "cadastro",
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosDeEstoque",
                schema: "estoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SaldoResultante = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RegistradoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosDeEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosDeEstoque_ItensDeEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalSchema: "estoque",
                        principalTable: "ItensDeEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservasDeEstoque",
                schema: "estoque",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservadaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasDeEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservasDeEstoque_ItensDeEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalSchema: "estoque",
                        principalTable: "ItensDeEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VersoesDeOrcamento",
                schema: "orcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GeradoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Decisao = table.Column<int>(type: "int", nullable: true),
                    RespondidoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrcamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersoesDeOrcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersoesDeOrcamento_Orcamentos_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalSchema: "orcamento",
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensDePecaDaOs",
                schema: "atendimento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PecaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecoCongelado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Origem = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AguardandoAprovacaoDoCliente = table.Column<bool>(type: "bit", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDePecaDaOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensDePecaDaOs_OrdensDeServico_OrdemDeServicoId",
                        column: x => x.OrdemDeServicoId,
                        principalSchema: "atendimento",
                        principalTable: "OrdensDeServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensDeServicoDaOs",
                schema: "atendimento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrecoCongelado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TempoPadraoMinutos = table.Column<int>(type: "int", nullable: false),
                    Origem = table.Column<int>(type: "int", nullable: false),
                    AguardandoAprovacaoDoCliente = table.Column<bool>(type: "bit", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdemDeServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDeServicoDaOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensDeServicoDaOs_OrdensDeServico_OrdemDeServicoId",
                        column: x => x.OrdemDeServicoId,
                        principalSchema: "atendimento",
                        principalTable: "OrdensDeServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PecaPrecos",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VigenteDe = table.Column<DateOnly>(type: "date", nullable: false),
                    PecaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PecaPrecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PecaPrecos_Pecas_PecaId",
                        column: x => x.PecaId,
                        principalSchema: "catalogo",
                        principalTable: "Pecas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicoPrecos",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VigenteDe = table.Column<DateOnly>(type: "date", nullable: false),
                    ServicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicoPrecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicoPrecos_Servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalSchema: "catalogo",
                        principalTable: "Servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensDeOrcamento",
                schema: "orcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VersaoOrcamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDeOrcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensDeOrcamento_VersoesDeOrcamento_VersaoOrcamentoId",
                        column: x => x.VersaoOrcamentoId,
                        principalSchema: "orcamento",
                        principalTable: "VersoesDeOrcamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteContatos_ClienteId",
                schema: "cadastro",
                table: "ClienteContatos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Documento",
                schema: "cadastro",
                table: "Clientes",
                column: "Documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensDeEstoque_PecaId",
                schema: "estoque",
                table: "ItensDeEstoque",
                column: "PecaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensDeOrcamento_VersaoOrcamentoId",
                schema: "orcamento",
                table: "ItensDeOrcamento",
                column: "VersaoOrcamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDePecaDaOs_OrdemDeServicoId",
                schema: "atendimento",
                table: "ItensDePecaDaOs",
                column: "OrdemDeServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDePecaDaOs_PecaId",
                schema: "atendimento",
                table: "ItensDePecaDaOs",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDeServicoDaOs_OrdemDeServicoId",
                schema: "atendimento",
                table: "ItensDeServicoDaOs",
                column: "OrdemDeServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDeServicoDaOs_ServicoId",
                schema: "atendimento",
                table: "ItensDeServicoDaOs",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosDeEstoque_ItemEstoqueId",
                schema: "estoque",
                table: "MovimentosDeEstoque",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_OrdemDeServicoId",
                schema: "orcamento",
                table: "Orcamentos",
                column: "OrdemDeServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeServico_ClienteId",
                schema: "atendimento",
                table: "OrdensDeServico",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeServico_Numero",
                schema: "atendimento",
                table: "OrdensDeServico",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeServico_Status",
                schema: "atendimento",
                table: "OrdensDeServico",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensDeServico_VeiculoId",
                schema: "atendimento",
                table: "OrdensDeServico",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_PecaPrecos_PecaId",
                schema: "catalogo",
                table: "PecaPrecos",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pecas_Sku",
                schema: "catalogo",
                table: "Pecas",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasDeEstoque_ItemEstoqueId",
                schema: "estoque",
                table: "ReservasDeEstoque",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasDeEstoque_OsId",
                schema: "estoque",
                table: "ReservasDeEstoque",
                column: "OrdemDeServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicoPrecos_ServicoId",
                schema: "catalogo",
                table: "ServicoPrecos",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_Nome",
                schema: "catalogo",
                table: "Servicos",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                schema: "identidade",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_ClienteId",
                schema: "cadastro",
                table: "Veiculos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_Placa",
                schema: "cadastro",
                table: "Veiculos",
                column: "Placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDeOrcamento_OrcamentoId",
                schema: "orcamento",
                table: "VersoesDeOrcamento",
                column: "OrcamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteContatos",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "ItensDeOrcamento",
                schema: "orcamento");

            migrationBuilder.DropTable(
                name: "ItensDePecaDaOs",
                schema: "atendimento");

            migrationBuilder.DropTable(
                name: "ItensDeServicoDaOs",
                schema: "atendimento");

            migrationBuilder.DropTable(
                name: "MovimentosDeEstoque",
                schema: "estoque");

            migrationBuilder.DropTable(
                name: "PecaPrecos",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "ReservasDeEstoque",
                schema: "estoque");

            migrationBuilder.DropTable(
                name: "ServicoPrecos",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "identidade");

            migrationBuilder.DropTable(
                name: "Veiculos",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "Clientes",
                schema: "cadastro");

            migrationBuilder.DropTable(
                name: "VersoesDeOrcamento",
                schema: "orcamento");

            migrationBuilder.DropTable(
                name: "OrdensDeServico",
                schema: "atendimento");

            migrationBuilder.DropTable(
                name: "Pecas",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "ItensDeEstoque",
                schema: "estoque");

            migrationBuilder.DropTable(
                name: "Servicos",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "Orcamentos",
                schema: "orcamento");
        }
    }
}
