using MediatR;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Common.Interfaces;

namespace PaymentGateway.Application.Orders.Commands.ProcessOrderPayment;

/// <summary>
/// Esse Handler simula a conversa com um provedor de pagamento externo (tipo Stripe,
/// PagSeguro, etc). Numa integração de verdade, aqui entraria uma chamada HTTP pra API
/// do provedor. Por enquanto, simulamos com uma espera + uma decisão aleatória — o
/// suficiente pra provar que o fluxo assíncrono ponta a ponta funciona.
/// </summary>
public class ProcessOrderPaymentCommandHandler : IRequestHandler<ProcessOrderPaymentCommand>
{
    // 80% de chance de aprovar, só pra não ficar tudo recusado ou tudo aprovado
    // toda vez que você testar — deixa a demonstração mais realista.
    private const int PercentualDeAprovacao = 80;

    private static readonly Random Random = new();

    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessOrderPaymentCommandHandler> _logger;

    public ProcessOrderPaymentCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessOrderPaymentCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessOrderPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            // Isso não deveria acontecer no fluxo normal, mas é bom logar caso aconteça
            // (ex: se alguém apagar o pedido do banco manualmente entre a criação e o processamento).
            _logger.LogWarning("Pedido {OrderId} não encontrado ao tentar processar pagamento", request.OrderId);
            return;
        }

        order.StartProcessing();

        // Simula a latência de rede de uma chamada real a um provedor de pagamento externo.
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        var aprovado = Random.Next(100) < PercentualDeAprovacao;

        if (aprovado)
        {
            order.ApprovePayment();
            _logger.LogInformation("Pagamento do pedido {OrderId} APROVADO", order.Id);
        }
        else
        {
            order.DeclinePayment("Pagamento recusado pelo provedor (simulado)");
            _logger.LogInformation("Pagamento do pedido {OrderId} RECUSADO", order.Id);
        }

        // O SaveChangesAsync aqui também publica os eventos PaymentApprovedEvent /
        // PaymentDeclinedEvent na fila — a mesma lógica de UnitOfWork que já criamos
        // lá na Infrastructure é reaproveitada, sem precisar mudar nada nela.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
