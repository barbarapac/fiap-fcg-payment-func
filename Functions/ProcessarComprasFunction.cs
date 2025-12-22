using Fiap.FCG.Payment.Functions.Contracts;
using Fiap.FCG.Payment.Functions.Services;
using Fiap.FCG.Payment.Functions.Services.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.FCG.Payment.Functions.Functions
{
    public class ProcessarComprasFunction
    {
        private readonly ILogger<ProcessarComprasFunction> _logger;
        private readonly IPaymentApiClient _paymentApi;

        public ProcessarComprasFunction(
            ILogger<ProcessarComprasFunction> logger,
            IPaymentApiClient paymentApi)
        {
            _logger = logger;
            _paymentApi = paymentApi;
        }

        [Function(nameof(ProcessarComprasFunction))]
        public async Task Run(
            [ServiceBusTrigger("compras-realizadas", Connection = "SERVICEBUS_CONNECTION")]
            string message,
            FunctionContext context,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Mensagem recebida da fila compras-realizadas: {Message}",
                message
            );

            CompraRealizadaEvent compra;

            try
            {
                compra = JsonSerializer.Deserialize<CompraRealizadaEvent>(
                    message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? throw new InvalidOperationException("Evento de compra veio nulo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desserializar CompraRealizadaEvent.");
                throw;
            }

            _logger.LogInformation(
                "Processando pagamento da compra. CompraId={CompraId}, UsuarioId={UsuarioId}, ValorTotal={ValorTotal}",
                compra.CompraId,
                compra.UsuarioId,
                compra.ValorTotal
            );

            var pagamentoResp = await _paymentApi.CriarPagamentoAsync(
                new CriarPagamentoRequest
                {
                    CompraId = compra.CompraId,
                    UsuarioId = compra.UsuarioId,
                    ValorTotal = compra.ValorTotal,
                    MetodoPagamento = compra.MetodoPagamento,
                    BandeiraCartao = compra.BandeiraCartao
                },
                ct
            );

            if (!pagamentoResp.Sucesso)
            {
                _logger.LogError(
                    "Falha ao criar pagamento. CompraId={CompraId}, Msg={Msg}",
                    compra.CompraId,
                    pagamentoResp.Mensagem
                );
                
                throw new InvalidOperationException(
                    pagamentoResp.Mensagem ?? "Erro desconhecido ao criar pagamento."
                );
            }

            _logger.LogInformation(
                "Pagamento criado com sucesso. PagamentoId={PagamentoId}, Status={Status}",
                pagamentoResp.PagamentoId,
                pagamentoResp.Status
            );
        }
    }
}
