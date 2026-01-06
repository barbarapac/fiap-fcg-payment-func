using Azure.Messaging.ServiceBus;
using Fiap.FCG.Payment.Functions.Contracts;
using Fiap.FCG.Payment.Functions.Services;
using Fiap.FCG.Payment.Functions.Services.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Fiap.FCG.Payment.Functions.Functions
{
    public class ProcessarComprasFunction
    {
        private const string CorrelationHeader = "X-Correlation-ID";

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
            ServiceBusReceivedMessage message,
            FunctionContext context,
            CancellationToken ct)
        {
            var totalSw = Stopwatch.StartNew();

            message.ApplicationProperties.TryGetValue(CorrelationHeader, out var cidObj);
            message.ApplicationProperties.TryGetValue("traceparent", out var traceParentObj);

            var correlationId = cidObj?.ToString() ?? context.InvocationId;
            var traceParent = traceParentObj?.ToString();

            using var activity = new Activity(nameof(ProcessarComprasFunction));
            if (!string.IsNullOrWhiteSpace(traceParent))
            {
                activity.SetParentId(traceParent);
            }
            activity.Start();

            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = activity.TraceId.ToString(),
                ["SpanId"] = activity.SpanId.ToString(),
                ["InvocationId"] = context.InvocationId,
                ["FunctionName"] = nameof(ProcessarComprasFunction),
                ["QueueName"] = "compras-realizadas"
            });

            var body = message.Body.ToString();
            var msgLength = body?.Length ?? 0;

            _logger.LogInformation(
                "Início do processamento da mensagem do Service Bus. PayloadLength={PayloadLength}",
                msgLength);

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Mensagem vazia recebida da fila.");
                throw new InvalidOperationException("Mensagem recebida está vazia.");
            }

            CompraRealizadaEvent compra;

            try
            {
                compra = JsonSerializer.Deserialize<CompraRealizadaEvent>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? throw new InvalidOperationException("Evento de compra veio nulo após desserialização.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao desserializar CompraRealizadaEvent.");
                throw;
            }

            using var businessScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["CompraId"] = compra.CompraId,
                ["UsuarioId"] = compra.UsuarioId
            });

            _logger.LogInformation(
                "Evento desserializado. CompraId={CompraId}, UsuarioId={UsuarioId}, ValorTotal={ValorTotal}",
                compra.CompraId,
                compra.UsuarioId,
                compra.ValorTotal
            );

            CriarPagamentoResponse pagamentoResp;

            try
            {
                _logger.LogInformation("Chamando Payment API para criar pagamento.");

                pagamentoResp = await _paymentApi.CriarPagamentoAsync(
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
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Processamento cancelado por CancellationToken.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao chamar Payment API para criar pagamento.");
                throw;
            }

            if (pagamentoResp is null || !pagamentoResp.Sucesso)
            {
                _logger.LogError(
                    "Falha ao criar pagamento. Msg={Mensagem}",
                    pagamentoResp?.Mensagem);

                throw new InvalidOperationException(
                    pagamentoResp?.Mensagem ?? "Erro desconhecido ao criar pagamento.");
            }

            totalSw.Stop();

            _logger.LogInformation(
                "Pagamento criado com sucesso. PagamentoId={PagamentoId}, Status={Status}, TotalElapsedMs={TotalElapsedMs}",
                pagamentoResp.PagamentoId,
                pagamentoResp.Status,
                totalSw.ElapsedMilliseconds
            );
        }
    }
}
