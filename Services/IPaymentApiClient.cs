using Fiap.FCG.Payment.Functions.Services.Models;

namespace Fiap.FCG.Payment.Functions.Services
{
    public interface IPaymentApiClient
    {
        Task<CriarPagamentoResponse> CriarPagamentoAsync(CriarPagamentoRequest request, CancellationToken ct);
    }
}
