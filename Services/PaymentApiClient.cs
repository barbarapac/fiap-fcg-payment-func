using Fiap.FCG.Payment.Functions.Services.Models;
using System.Net.Http.Json;

namespace Fiap.FCG.Payment.Functions.Services
{
    public class PaymentApiClient : IPaymentApiClient
    {
        private readonly HttpClient _http;

        public PaymentApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<CriarPagamentoResponse> CriarPagamentoAsync(CriarPagamentoRequest request, CancellationToken ct)
        {            
            using var resp = await _http.PostAsJsonAsync("api/pagamentos", request, ct);
            
            resp.EnsureSuccessStatusCode();

            var result = await resp.Content.ReadFromJsonAsync<CriarPagamentoResponse>(cancellationToken: ct);

            return result ?? new CriarPagamentoResponse
            {
                Sucesso = false,
                Mensagem = "Resposta vazia da Payment API"
            };
        }
    }
}
