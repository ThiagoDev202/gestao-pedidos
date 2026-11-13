namespace GestaoPedidos.Api.RateLimiting;

public sealed class RateLimitingSettings
{
    public bool Enabled { get; set; } = true;

    public PostPedidosSettings PostPedidos { get; set; } = new();

    public GlobalSettings Global { get; set; } = new();

    public sealed class PostPedidosSettings
    {
        public int TokenLimit { get; set; } = 10;

        public int TokensPerPeriod { get; set; } = 5;

        public int ReplenishmentPeriodSeconds { get; set; } = 1;
    }

    public sealed class GlobalSettings
    {
        public int PermitLimit { get; set; } = 300;

        public int WindowSeconds { get; set; } = 60;
    }
}
