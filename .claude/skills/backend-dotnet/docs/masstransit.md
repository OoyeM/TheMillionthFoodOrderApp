# MassTransit

## Purpose
Async messaging between bounded contexts. Handles domain event publishing, consumers, and saga/state machines for complex workflows.

## Transport Strategy

- **Local dev**: In-memory transport (no broker needed, spins up with Aspire)
- **Production**: RabbitMQ or Azure Service Bus (swap via config, no code changes)

## Key Use Cases

- **Domain events**: OrderPlaced, StockDepleted, ProductApprovalRequested — cross-context communication
- **Order lifecycle saga**: State machine managing Placed → Confirmed → Preparing → Ready → Picked Up
- **Notifications**: Trigger push/sound/ticket printer when orders come in
- **Stock management**: Decrement stock on order, auto-disable product at zero

## Patterns
<!-- Add patterns as they emerge during development -->

## Gotchas
<!-- Add gotchas discovered during development -->
