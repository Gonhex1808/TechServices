namespace TechService.Api.Models;

public class OrdemServico
{
    public int IdOrdem { get; set; }
    public int IdEquipamento { get; set; }
    public string DefeitoRelatado { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? Solucao { get; set; }
    public string? Status { get; set; }
    public string? Prioridade { get; set; }
    public decimal ValorServico { get; set; }
    public decimal ValorPecas { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
