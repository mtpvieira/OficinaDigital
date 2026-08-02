using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Clientes;
using OficinaDigital.Domain.OrdensServico;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Infrastructure.Persistence.Seed;

//Esta classe foi criada com o objetivo de popular o banco de dados com dados iniciais para testes.
public static class OficinaDigitalSeeder
{
    public static async Task SeedAsync(OficinaDigitalDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (await context.Clientes.AnyAsync(cancellationToken))
            return;

        var trocaOleo = Servico.Criar("Troca de óleo", 120m, "Troca de óleo e filtro", 30);
        var alinhamento = Servico.Criar("Alinhamento e balanceamento", 150m, "Alinhamento e balanceamento das quatro rodas", 60);
        var revisaoFreios = Servico.Criar("Revisão de freios", 200m, "Inspeção e troca de pastilhas de freio", 90);
        var trocaCorreia = Servico.Criar("Troca de correia dentada", 350m, "Substituição da correia dentada e tensores", 120);
        var diagnosticoEletronico = Servico.Criar("Diagnóstico eletrônico", 80m, "Leitura de códigos de falha via scanner", 30);

        context.Servicos.AddRange(trocaOleo, alinhamento, revisaoFreios, trocaCorreia, diagnosticoEletronico);
        
        var filtroOleo = Peca.Criar("Filtro de óleo", 35m, 50);
        var pastilhaFreio = Peca.Criar("Pastilha de freio (jogo)", 180m, 30);
        var oleoMotor = Peca.Criar("Óleo do motor 1L", 45m, 100);
        var correiaDentada = Peca.Criar("Correia dentada", 220m, 15);
        var bateria = Peca.Criar("Bateria 60Ah", 450m, 10);

        context.Pecas.AddRange(filtroOleo, pastilhaFreio, oleoMotor, correiaDentada, bateria);

        var joao = Cliente.Criar("João da Silva", "111.444.777-35", "joao.silva@example.com", "(11) 91234-5678");
        var maria = Cliente.Criar("Maria Oliveira", "529.982.247-25", "maria.oliveira@example.com", "(21) 99876-5432");
        var autoPecasCenter = Cliente.Criar("Auto Peças Center Ltda", "11.222.333/0001-81", "contato@autopecascenter.com.br", "(31) 3344-5566");

        context.Clientes.AddRange(joao, maria, autoPecasCenter);

        var uno = Veiculo.Criar(joao.Id, "ABC1234", "Fiat", "Uno", 2015);
        var civic = Veiculo.Criar(joao.Id, "BRA2E19", "Honda", "Civic", 2020);
        var corsa = Veiculo.Criar(maria.Id, "XYZ9988", "Chevrolet", "Corsa", 2012);
        var fiorino = Veiculo.Criar(autoPecasCenter.Id, "RIO4F56", "Fiat", "Fiorino", 2019);

        context.Veiculos.AddRange(uno, civic, corsa, fiorino);

        // OS-0001: recém-criada (Recebida), com itens já lançados mas sem orçamento enviado.
        var os1 = OrdemDeServico.Criar("OS-0001", joao.Id, uno.Id);
        os1.AdicionarItemServico(trocaOleo.Id, trocaOleo.Nome, trocaOleo.PrecoBase);
        os1.AdicionarItemPeca(filtroOleo.Id, filtroOleo.Nome, 1, filtroOleo.Preco);

        // OS-0002: em diagnóstico.
        var os2 = OrdemDeServico.Criar("OS-0002", joao.Id, civic.Id);
        os2.AdicionarItemServico(diagnosticoEletronico.Id, diagnosticoEletronico.Nome, diagnosticoEletronico.PrecoBase);
        os2.IniciarDiagnostico();

        // OS-0003: orçamento enviado, aguardando aprovação do cliente.
        var os3 = OrdemDeServico.Criar("OS-0003", maria.Id, corsa.Id);
        os3.AdicionarItemServico(alinhamento.Id, alinhamento.Nome, alinhamento.PrecoBase);
        os3.AdicionarItemPeca(oleoMotor.Id, oleoMotor.Nome, 2, oleoMotor.Preco);
        os3.IniciarDiagnostico();
        os3.EnviarOrcamento();

        // OS-0004: orçamento aprovado, em execução (baixa o estoque das peças utilizadas).
        var os4 = OrdemDeServico.Criar("OS-0004", autoPecasCenter.Id, fiorino.Id);
        os4.AdicionarItemServico(revisaoFreios.Id, revisaoFreios.Nome, revisaoFreios.PrecoBase);
        os4.AdicionarItemPeca(pastilhaFreio.Id, pastilhaFreio.Nome, 1, pastilhaFreio.Preco);
        os4.IniciarDiagnostico();
        os4.EnviarOrcamento();
        os4.Aprovar();
        pastilhaFreio.BaixarEstoque(1);

        // OS-0005: execução concluída (finalizada), aguardando entrega do veículo.
        var os5 = OrdemDeServico.Criar("OS-0005", joao.Id, uno.Id);
        os5.AdicionarItemServico(trocaCorreia.Id, trocaCorreia.Nome, trocaCorreia.PrecoBase);
        os5.AdicionarItemPeca(correiaDentada.Id, correiaDentada.Nome, 1, correiaDentada.Preco);
        os5.IniciarDiagnostico();
        os5.EnviarOrcamento();
        os5.Aprovar();
        correiaDentada.BaixarEstoque(1);
        os5.ConcluirExecucao();

        // OS-0006: ciclo completo, veículo já entregue ao cliente.
        var os6 = OrdemDeServico.Criar("OS-0006", maria.Id, corsa.Id);
        os6.AdicionarItemServico(trocaOleo.Id, trocaOleo.Nome, trocaOleo.PrecoBase);
        os6.AdicionarItemPeca(filtroOleo.Id, filtroOleo.Nome, 2, filtroOleo.Preco);
        os6.IniciarDiagnostico();
        os6.EnviarOrcamento();
        os6.Aprovar();
        filtroOleo.BaixarEstoque(2);
        os6.ConcluirExecucao();
        os6.Entregar();

        context.OrdensServico.AddRange(os1, os2, os3, os4, os5, os6);

        await context.SaveChangesAsync(cancellationToken);
    }
}
