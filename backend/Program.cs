using MySqlConnector;
using TechService.Api.Data;
using TechService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MySqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// SEGURANÇA - VALIDAÇÃO DA API KEY
// ============================================================
// Todos os endpoints que começam por /api ficam protegidos.
// O frontend deve enviar a chave no cabeçalho HTTP:
//     X-API-Key: TechService-Key-2026
//
// A chave correta é lida do appsettings.json.
// Assim, a validação fica centralizada e não precisamos repetir
// o mesmo código em cada endpoint de Clientes, Equipamentos e Ordens.
app.Use(async (context, next) =>
{
    // A rota raiz "/" continua pública para permitir verificar
    // facilmente se a API está ligada. Apenas /api/... exige chave.
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var apiKeyCorreta = builder.Configuration["ApiKey"];

        // Tenta ler o cabeçalho enviado pelo frontend.
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyRecebida))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                mensagem = "API Key não informada. Acesso não autorizado."
            });
            return;
        }

        // Compara a chave recebida com a chave configurada no backend.
        if (string.IsNullOrWhiteSpace(apiKeyCorreta) ||
            !string.Equals(apiKeyRecebida.ToString(), apiKeyCorreta, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                mensagem = "API Key inválida. Acesso não autorizado."
            });
            return;
        }
    }

    // A chave é válida (ou a rota não é protegida).
    // Continua para o endpoint solicitado.
    await next();
});

// ============================================================
// ESTADO DA API
// ============================================================
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "Olá! Bem-vindo à API TechService - Versão 7",
    versao = "V7",
    estado = "API ligada ao MySQL",
    modulos = new[]
    {
        "CRUD de Clientes",
        "CRUD de Equipamentos",
        "CRUD de Ordens de Serviço",
        "Pesquisas extras",
        "Proteção dos endpoints /api com API Key"
    }
}))
.WithName("EstadoDaApi")
.WithSummary("Verificar o estado da API");

// ============================================================
// CLIENTES - CRUD DA V5
// ============================================================

app.MapGet("/api/clientes", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status,
               created_at, updated_at, deleted_at
        FROM clientes
        WHERE status = 1
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        clientes.Add(MapearCliente(reader));

    return Results.Ok(clientes);
})
.WithName("ListarClientes")
.WithSummary("Listar clientes ativos");

app.MapGet("/api/clientes/{id_cliente:int}",
    async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status,
               created_at, updated_at, deleted_at
        FROM clientes
        WHERE id_cliente = @id_cliente AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = $"Cliente {id_cliente} não encontrado." });

    return Results.Ok(MapearCliente(reader));
})
.WithName("ObterClientePorId")
.WithSummary("Consultar cliente por ID");

app.MapPost("/api/clientes", async (Cliente cliente, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(cliente.Nome) || string.IsNullOrWhiteSpace(cliente.Email))
        return Results.BadRequest(new { mensagem = "Nome e email são obrigatórios." });

    const string sql = """
        INSERT INTO clientes (nome, email, telefone, status, created_at)
        VALUES (@nome, @email, @telefone, 1, NOW());
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@nome", cliente.Nome.Trim());
    command.Parameters.AddWithValue("@email", cliente.Email.Trim());
    command.Parameters.AddWithValue("@telefone",
        string.IsNullOrWhiteSpace(cliente.Telefone) ? DBNull.Value : cliente.Telefone.Trim());

    await command.ExecuteNonQueryAsync();
    var novoId = command.LastInsertedId;

    return Results.Created($"/api/clientes/{novoId}",
        new { idCliente = novoId, mensagem = "Cliente inserido com sucesso." });
})
.WithName("InserirCliente")
.WithSummary("Inserir cliente");

app.MapPut("/api/clientes/{id_cliente:int}",
    async (int id_cliente, Cliente cliente, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(cliente.Nome) || string.IsNullOrWhiteSpace(cliente.Email))
        return Results.BadRequest(new { mensagem = "Nome e email são obrigatórios." });

    const string sql = """
        UPDATE clientes
        SET nome = @nome,
            email = @email,
            telefone = @telefone,
            updated_at = NOW()
        WHERE id_cliente = @id_cliente AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);
    command.Parameters.AddWithValue("@nome", cliente.Nome.Trim());
    command.Parameters.AddWithValue("@email", cliente.Email.Trim());
    command.Parameters.AddWithValue("@telefone",
        string.IsNullOrWhiteSpace(cliente.Telefone) ? DBNull.Value : cliente.Telefone.Trim());

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Cliente {id_cliente} não encontrado." })
        : Results.Ok(new { mensagem = $"Cliente {id_cliente} atualizado com sucesso." });
})
.WithName("AtualizarCliente")
.WithSummary("Atualizar cliente");

app.MapDelete("/api/clientes/{id_cliente:int}",
    async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE clientes
        SET status = 0, updated_at = NOW(), deleted_at = NOW()
        WHERE id_cliente = @id_cliente AND status = 1;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Cliente {id_cliente} não encontrado." })
        : Results.Ok(new { mensagem = $"Cliente {id_cliente} desativado com sucesso." });
})
.WithName("DesativarCliente")
.WithSummary("Desativar cliente");

// ============================================================
// DESAFIOS EXTRAS - PESQUISAS DE CLIENTES
// ============================================================

app.MapGet("/api/clientes/nome/{nome}",
    async (string nome, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status,
               created_at, updated_at, deleted_at
        FROM clientes
        WHERE status = 1
          AND nome LIKE @nome
        ORDER BY nome;
        """;

    var clientes = new List<Cliente>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@nome", $"%{nome.Trim()}%");
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        clientes.Add(MapearCliente(reader));

    return Results.Ok(clientes);
})
.WithName("PesquisarClientePorNome")
.WithSummary("Pesquisar clientes por nome");

app.MapGet("/api/clientes/email/{email}",
    async (string email, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_cliente, nome, email, telefone, status,
               created_at, updated_at, deleted_at
        FROM clientes
        WHERE status = 1
          AND email = @email;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@email", email.Trim());
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = "Cliente com este e-mail não encontrado." });

    return Results.Ok(MapearCliente(reader));
})
.WithName("PesquisarClientePorEmail")
.WithSummary("Pesquisar cliente por e-mail");

// ============================================================
// EQUIPAMENTOS - CRUD COMPLETO
// Estrutura real da tabela equipamentos:
// id_equipamento, id_cliente, tipo, marca, modelo, numero_serie,
// observacoes, status, created_at, updated_at, deleted_at
// ============================================================

app.MapGet("/api/equipamentos", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, id_cliente, tipo, marca, modelo,
               numero_serie, observacoes, status,
               created_at, updated_at, deleted_at
        FROM equipamentos
        WHERE deleted_at IS NULL
        ORDER BY id_equipamento;
        """;

    var equipamentos = new List<Equipamento>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        equipamentos.Add(MapearEquipamento(reader));

    return Results.Ok(equipamentos);
})
.WithName("ListarEquipamentos")
.WithSummary("Listar equipamentos");

app.MapGet("/api/equipamentos/{id_equipamento:int}",
    async (int id_equipamento, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, id_cliente, tipo, marca, modelo,
               numero_serie, observacoes, status,
               created_at, updated_at, deleted_at
        FROM equipamentos
        WHERE id_equipamento = @id_equipamento
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_equipamento", id_equipamento);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = $"Equipamento {id_equipamento} não encontrado." });

    return Results.Ok(MapearEquipamento(reader));
})
.WithName("ObterEquipamentoPorId")
.WithSummary("Consultar equipamento por ID");

app.MapPost("/api/equipamentos",
    async (Equipamento equipamento, MySqlConnectionFactory factory) =>
{
    if (equipamento.IdCliente <= 0 || string.IsNullOrWhiteSpace(equipamento.Tipo))
        return Results.BadRequest(new { mensagem = "Cliente e tipo do equipamento são obrigatórios." });

    const string sql = """
        INSERT INTO equipamentos
            (id_cliente, tipo, marca, modelo, numero_serie,
             observacoes, status, created_at)
        VALUES
            (@id_cliente, @tipo, @marca, @modelo, @numero_serie,
             @observacoes, @status, NOW());
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    AdicionarParametrosEquipamento(command, equipamento);

    await command.ExecuteNonQueryAsync();
    var novoId = command.LastInsertedId;

    return Results.Created($"/api/equipamentos/{novoId}",
        new { idEquipamento = novoId, mensagem = "Equipamento inserido com sucesso." });
})
.WithName("InserirEquipamento")
.WithSummary("Inserir equipamento");

app.MapPut("/api/equipamentos/{id_equipamento:int}",
    async (int id_equipamento, Equipamento equipamento, MySqlConnectionFactory factory) =>
{
    if (equipamento.IdCliente <= 0 || string.IsNullOrWhiteSpace(equipamento.Tipo))
        return Results.BadRequest(new { mensagem = "Cliente e tipo do equipamento são obrigatórios." });

    const string sql = """
        UPDATE equipamentos
        SET id_cliente = @id_cliente,
            tipo = @tipo,
            marca = @marca,
            modelo = @modelo,
            numero_serie = @numero_serie,
            observacoes = @observacoes,
            status = @status,
            updated_at = NOW()
        WHERE id_equipamento = @id_equipamento
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_equipamento", id_equipamento);
    AdicionarParametrosEquipamento(command, equipamento);

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Equipamento {id_equipamento} não encontrado." })
        : Results.Ok(new { mensagem = $"Equipamento {id_equipamento} atualizado com sucesso." });
})
.WithName("AtualizarEquipamento")
.WithSummary("Atualizar equipamento");

app.MapDelete("/api/equipamentos/{id_equipamento:int}",
    async (int id_equipamento, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE equipamentos
        SET deleted_at = NOW(), updated_at = NOW()
        WHERE id_equipamento = @id_equipamento
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_equipamento", id_equipamento);

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Equipamento {id_equipamento} não encontrado." })
        : Results.Ok(new { mensagem = $"Equipamento {id_equipamento} excluído logicamente com sucesso." });
})
.WithName("ExcluirEquipamento")
.WithSummary("Excluir equipamento logicamente");

// Desafio extra: equipamentos de um cliente.
app.MapGet("/api/equipamentos/cliente/{id_cliente:int}",
    async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_equipamento, id_cliente, tipo, marca, modelo,
               numero_serie, observacoes, status,
               created_at, updated_at, deleted_at
        FROM equipamentos
        WHERE id_cliente = @id_cliente
          AND deleted_at IS NULL
        ORDER BY id_equipamento;
        """;

    var equipamentos = new List<Equipamento>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        equipamentos.Add(MapearEquipamento(reader));

    return Results.Ok(equipamentos);
})
.WithName("ListarEquipamentosPorCliente")
.WithSummary("Listar equipamentos de um cliente");

// ============================================================
// ORDENS DE SERVIÇO - CRUD COMPLETO
// Estrutura real da tabela ordens_servico:
// id_ordem, id_equipamento, defeito_relatado, diagnostico, solucao,
// status, prioridade, valor_servico, valor_pecas, desconto, valor_total,
// created_at, updated_at, deleted_at
// ============================================================

app.MapGet("/api/ordens-servico", async (MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico,
               solucao, status, prioridade, valor_servico, valor_pecas,
               desconto, valor_total, created_at, updated_at, deleted_at
        FROM ordens_servico
        WHERE deleted_at IS NULL
        ORDER BY created_at DESC, id_ordem DESC;
        """;

    var ordens = new List<OrdemServico>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        ordens.Add(MapearOrdemServico(reader));

    return Results.Ok(ordens);
})
.WithName("ListarOrdensServico")
.WithSummary("Listar ordens de serviço");

app.MapGet("/api/ordens-servico/{id_ordem:int}",
    async (int id_ordem, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico,
               solucao, status, prioridade, valor_servico, valor_pecas,
               desconto, valor_total, created_at, updated_at, deleted_at
        FROM ordens_servico
        WHERE id_ordem = @id_ordem
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_ordem", id_ordem);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = $"Ordem de serviço {id_ordem} não encontrada." });

    return Results.Ok(MapearOrdemServico(reader));
})
.WithName("ObterOrdemServicoPorId")
.WithSummary("Consultar ordem de serviço por ID");

app.MapPost("/api/ordens-servico",
    async (OrdemServico ordem, MySqlConnectionFactory factory) =>
{
    if (ordem.IdEquipamento <= 0 || string.IsNullOrWhiteSpace(ordem.DefeitoRelatado))
        return Results.BadRequest(new { mensagem = "Equipamento e defeito relatado são obrigatórios." });

    const string sql = """
        INSERT INTO ordens_servico
        (
            id_equipamento, defeito_relatado, diagnostico, solucao,
            status, prioridade, valor_servico, valor_pecas,
            desconto, valor_total, created_at
        )
        VALUES
        (
            @id_equipamento, @defeito_relatado, @diagnostico, @solucao,
            @status, @prioridade, @valor_servico, @valor_pecas,
            @desconto, (@valor_servico + @valor_pecas - @desconto), NOW()
        );
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    AdicionarParametrosOrdem(command, ordem);

    await command.ExecuteNonQueryAsync();
    var novoId = command.LastInsertedId;

    return Results.Created($"/api/ordens-servico/{novoId}",
        new { idOrdem = novoId, mensagem = "Ordem de serviço inserida com sucesso." });
})
.WithName("InserirOrdemServico")
.WithSummary("Inserir ordem de serviço");

app.MapPut("/api/ordens-servico/{id_ordem:int}",
    async (int id_ordem, OrdemServico ordem, MySqlConnectionFactory factory) =>
{
    if (ordem.IdEquipamento <= 0 || string.IsNullOrWhiteSpace(ordem.DefeitoRelatado))
        return Results.BadRequest(new { mensagem = "Equipamento e defeito relatado são obrigatórios." });

    const string sql = """
        UPDATE ordens_servico
        SET id_equipamento = @id_equipamento,
            defeito_relatado = @defeito_relatado,
            diagnostico = @diagnostico,
            solucao = @solucao,
            status = @status,
            prioridade = @prioridade,
            valor_servico = @valor_servico,
            valor_pecas = @valor_pecas,
            desconto = @desconto,
            valor_total = (@valor_servico + @valor_pecas - @desconto),
            updated_at = NOW()
        WHERE id_ordem = @id_ordem
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_ordem", id_ordem);
    AdicionarParametrosOrdem(command, ordem);

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Ordem de serviço {id_ordem} não encontrada." })
        : Results.Ok(new { mensagem = $"Ordem de serviço {id_ordem} atualizada com sucesso." });
})
.WithName("AtualizarOrdemServico")
.WithSummary("Atualizar ordem de serviço");

app.MapDelete("/api/ordens-servico/{id_ordem:int}",
    async (int id_ordem, MySqlConnectionFactory factory) =>
{
    const string sql = """
        UPDATE ordens_servico
        SET deleted_at = NOW(), updated_at = NOW()
        WHERE id_ordem = @id_ordem
          AND deleted_at IS NULL;
        """;

    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_ordem", id_ordem);

    var linhas = await command.ExecuteNonQueryAsync();
    return linhas == 0
        ? Results.NotFound(new { mensagem = $"Ordem de serviço {id_ordem} não encontrada." })
        : Results.Ok(new { mensagem = $"Ordem de serviço {id_ordem} excluída logicamente com sucesso." });
})
.WithName("ExcluirOrdemServico")
.WithSummary("Excluir ordem de serviço logicamente");

// Desafio extra: ordens de serviço de um cliente.
// A tabela ordens_servico não possui id_cliente; a relação é feita pelo equipamento.
app.MapGet("/api/ordens-servico/cliente/{id_cliente:int}",
    async (int id_cliente, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT os.id_ordem, os.id_equipamento, os.defeito_relatado,
               os.diagnostico, os.solucao, os.status, os.prioridade,
               os.valor_servico, os.valor_pecas, os.desconto, os.valor_total,
               os.created_at, os.updated_at, os.deleted_at
        FROM ordens_servico os
        INNER JOIN equipamentos e
            ON e.id_equipamento = os.id_equipamento
        WHERE e.id_cliente = @id_cliente
          AND os.deleted_at IS NULL
          AND e.deleted_at IS NULL
        ORDER BY os.created_at DESC, os.id_ordem DESC;
        """;

    var ordens = new List<OrdemServico>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_cliente", id_cliente);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        ordens.Add(MapearOrdemServico(reader));

    return Results.Ok(ordens);
})
.WithName("ListarOrdensPorCliente")
.WithSummary("Listar ordens de serviço de um cliente");

// Desafio extra: ordens de serviço de um equipamento.
app.MapGet("/api/ordens-servico/equipamento/{id_equipamento:int}",
    async (int id_equipamento, MySqlConnectionFactory factory) =>
{
    const string sql = """
        SELECT id_ordem, id_equipamento, defeito_relatado, diagnostico,
               solucao, status, prioridade, valor_servico, valor_pecas,
               desconto, valor_total, created_at, updated_at, deleted_at
        FROM ordens_servico
        WHERE id_equipamento = @id_equipamento
          AND deleted_at IS NULL
        ORDER BY created_at DESC, id_ordem DESC;
        """;

    var ordens = new List<OrdemServico>();
    await using var connection = factory.CreateConnection();
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id_equipamento", id_equipamento);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
        ordens.Add(MapearOrdemServico(reader));

    return Results.Ok(ordens);
})
.WithName("ListarOrdensPorEquipamento")
.WithSummary("Listar ordens de serviço de um equipamento");

app.Run();

// ============================================================
// FUNÇÕES AUXILIARES
// ============================================================

static Cliente MapearCliente(MySqlDataReader reader)
{
    var ordinalTelefone = reader.GetOrdinal("telefone");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    return new Cliente
    {
        IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
        Nome = reader.GetString(reader.GetOrdinal("nome")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        Telefone = reader.IsDBNull(ordinalTelefone) ? null : reader.GetString(ordinalTelefone),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(ordinalUpdatedAt) ? null : reader.GetDateTime(ordinalUpdatedAt),
        DeletedAt = reader.IsDBNull(ordinalDeletedAt) ? null : reader.GetDateTime(ordinalDeletedAt)
    };
}

static Equipamento MapearEquipamento(MySqlDataReader reader)
{
    var ordinalMarca = reader.GetOrdinal("marca");
    var ordinalModelo = reader.GetOrdinal("modelo");
    var ordinalNumeroSerie = reader.GetOrdinal("numero_serie");
    var ordinalObservacoes = reader.GetOrdinal("observacoes");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    return new Equipamento
    {
        IdEquipamento = reader.GetInt32(reader.GetOrdinal("id_equipamento")),
        IdCliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),
        Tipo = reader.GetString(reader.GetOrdinal("tipo")),
        Marca = reader.IsDBNull(ordinalMarca) ? null : reader.GetString(ordinalMarca),
        Modelo = reader.IsDBNull(ordinalModelo) ? null : reader.GetString(ordinalModelo),
        NumeroSerie = reader.IsDBNull(ordinalNumeroSerie) ? null : reader.GetString(ordinalNumeroSerie),
        Observacoes = reader.IsDBNull(ordinalObservacoes) ? null : reader.GetString(ordinalObservacoes),
        Status = reader.GetInt32(reader.GetOrdinal("status")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(ordinalUpdatedAt) ? null : reader.GetDateTime(ordinalUpdatedAt),
        DeletedAt = reader.IsDBNull(ordinalDeletedAt) ? null : reader.GetDateTime(ordinalDeletedAt)
    };
}

static OrdemServico MapearOrdemServico(MySqlDataReader reader)
{
    var ordinalDiagnostico = reader.GetOrdinal("diagnostico");
    var ordinalSolucao = reader.GetOrdinal("solucao");
    var ordinalStatus = reader.GetOrdinal("status");
    var ordinalPrioridade = reader.GetOrdinal("prioridade");
    var ordinalUpdatedAt = reader.GetOrdinal("updated_at");
    var ordinalDeletedAt = reader.GetOrdinal("deleted_at");

    return new OrdemServico
    {
        IdOrdem = reader.GetInt32(reader.GetOrdinal("id_ordem")),
        IdEquipamento = reader.GetInt32(reader.GetOrdinal("id_equipamento")),
        DefeitoRelatado = reader.GetString(reader.GetOrdinal("defeito_relatado")),
        Diagnostico = reader.IsDBNull(ordinalDiagnostico) ? null : reader.GetString(ordinalDiagnostico),
        Solucao = reader.IsDBNull(ordinalSolucao) ? null : reader.GetString(ordinalSolucao),
        Status = reader.IsDBNull(ordinalStatus) ? null : reader.GetString(ordinalStatus),
        Prioridade = reader.IsDBNull(ordinalPrioridade) ? null : reader.GetString(ordinalPrioridade),
        ValorServico = reader.GetDecimal(reader.GetOrdinal("valor_servico")),
        ValorPecas = reader.GetDecimal(reader.GetOrdinal("valor_pecas")),
        Desconto = reader.GetDecimal(reader.GetOrdinal("desconto")),
        ValorTotal = reader.GetDecimal(reader.GetOrdinal("valor_total")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.IsDBNull(ordinalUpdatedAt) ? null : reader.GetDateTime(ordinalUpdatedAt),
        DeletedAt = reader.IsDBNull(ordinalDeletedAt) ? null : reader.GetDateTime(ordinalDeletedAt)
    };
}

static void AdicionarParametrosEquipamento(MySqlCommand command, Equipamento equipamento)
{
    command.Parameters.AddWithValue("@id_cliente", equipamento.IdCliente);
    command.Parameters.AddWithValue("@tipo", equipamento.Tipo.Trim());
    command.Parameters.AddWithValue("@marca", string.IsNullOrWhiteSpace(equipamento.Marca) ? DBNull.Value : equipamento.Marca.Trim());
    command.Parameters.AddWithValue("@modelo", string.IsNullOrWhiteSpace(equipamento.Modelo) ? DBNull.Value : equipamento.Modelo.Trim());
    command.Parameters.AddWithValue("@numero_serie", string.IsNullOrWhiteSpace(equipamento.NumeroSerie) ? DBNull.Value : equipamento.NumeroSerie.Trim());
    command.Parameters.AddWithValue("@observacoes", string.IsNullOrWhiteSpace(equipamento.Observacoes) ? DBNull.Value : equipamento.Observacoes.Trim());
    command.Parameters.AddWithValue("@status", equipamento.Status == 0 ? 1 : equipamento.Status);
}

static void AdicionarParametrosOrdem(MySqlCommand command, OrdemServico ordem)
{
    command.Parameters.AddWithValue("@id_equipamento", ordem.IdEquipamento);
    command.Parameters.AddWithValue("@defeito_relatado", ordem.DefeitoRelatado.Trim());
    command.Parameters.AddWithValue("@diagnostico", string.IsNullOrWhiteSpace(ordem.Diagnostico) ? DBNull.Value : ordem.Diagnostico.Trim());
    command.Parameters.AddWithValue("@solucao", string.IsNullOrWhiteSpace(ordem.Solucao) ? DBNull.Value : ordem.Solucao.Trim());
    command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(ordem.Status) ? "A Diagnosticar" : ordem.Status.Trim());
    command.Parameters.AddWithValue("@prioridade", string.IsNullOrWhiteSpace(ordem.Prioridade) ? "Normal" : ordem.Prioridade.Trim());
    command.Parameters.AddWithValue("@valor_servico", ordem.ValorServico);
    command.Parameters.AddWithValue("@valor_pecas", ordem.ValorPecas);
    command.Parameters.AddWithValue("@desconto", ordem.Desconto);
}
