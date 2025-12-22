using Fiap.FCG.Payment.Functions.Contracts;

namespace Fiap.FCG.Payment.Functions.Services.Models
{
    public class CriarPagamentoRequest
    {
        public int CompraId { get; set; }
        public int UsuarioId { get; set; }        
        public int JogoId { get; set; }
        public decimal ValorTotal { get; set; }
        public EMetodoPagamento MetodoPagamento { get; set; }
        public EBandeiraCartao? BandeiraCartao { get; set; }
    }
}
