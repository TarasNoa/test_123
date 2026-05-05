using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain;

public static class TradingErrors
{
    public static Error AssetNotFound => Error.NotFound(
        "Trading.AssetNotFound",
        "Актив не найден");

    public static Error OrderNotFound => Error.NotFound(
        "Trading.OrderNotFound",
        "Ордер не найден");

    public static Error PortfolioNotFound => Error.NotFound(
        "Trading.PortfolioNotFound",
        "Портфель не найден");

    public static Error InsufficientFunds => Error.Conflict(
        "Trading.InsufficientFunds",
        "Недостаточно средств для сделки");

    public static Error InsufficientPosition => Error.Conflict(
        "Trading.InsufficientPosition",
        "Недостаточно позиции для продажи");

    public static Error InvalidOrderPrice => Error.Validation(
        "Trading.InvalidOrderPrice",
        "Некорректная цена ордера");

    public static Error OrderAlreadyFilled => Error.Conflict(
        "Trading.OrderAlreadyFilled",
        "Ордер уже исполнен");

    public static Error CannotCancelFilledOrder => Error.Conflict(
        "Trading.CannotCancelFilledOrder",
        "Нельзя отменить исполненный ордер");

    public static Error MarketDataNotAvailable => Error.Failure(
        "Trading.MarketDataNotAvailable",
        "Данные рынка недоступны");

    public static Error TradingDisabled => Error.Failure(
        "Trading.TradingDisabled",
        "Торговля временно отключена");

    public static Error DemoOnly => Error.Failure(
        "Trading.DemoOnly",
        "Эта функция доступна только в demo-режиме");
}
