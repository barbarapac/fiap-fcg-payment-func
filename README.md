# fiap-fcg-payment-functions

**Azure Function Serverless** responsável por processar eventos de compras realizadas no ecossistema **FIAP Cloud Games**, integrando a fila do **Azure Service Bus** com a **API de Pagamentos**.

## Tech Challenge – Cloud Games (Fase 3)

Este projeto faz parte do Tech Challenge da FIAP, aplicando conceitos de arquitetura orientada a eventos, processamento assíncrono e serverless com Azure Functions.

## Sobre o Projeto

A Function é acionada automaticamente sempre que uma mensagem é publicada na fila **compras-realizadas** do Azure Service Bus.

Fluxo resumido:
1. API de Games publica evento de compra.
2. Azure Service Bus armazena a mensagem.
3. Azure Function consome a mensagem.
4. Function chama a API de Payments.
5. API de Payments decide aprovação ou recusa e persiste no banco.

## Pré-requisitos

- .NET SDK 8.0
- Azure Service Bus
- Azure Functions Core Tools (opcional)

## Configuração Local

Arquivo `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SERVICEBUS_CONNECTION": "<connection-string>"
  },
  "PaymentApi": {
    "BaseUrl": "https://localhost:7267"
  }
}
```

## Variáveis de Ambiente (Azure)

- SERVICEBUS_CONNECTION
- PaymentApi__BaseUrl

## Execução Local

```bash
func start
```

## Tecnologias

- .NET 8 (Isolated)
- Azure Functions
- Azure Service Bus
- HttpClient Factory
