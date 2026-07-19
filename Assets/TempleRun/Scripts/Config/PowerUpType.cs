namespace CrawfisSoftware.TempleRun.GameConfig
{
    /// <summary>
    /// Enum of power-up buff types. Used by PowerUpBuffController to apply/remove effects.
    /// Extensible — add new types here and handle in PowerUpBuffController.
    /// </summary>
    public enum PowerUpType
    {
        SpeedBoost,
        CoinMagnet,
        Shield,
        ScoreMultiplier,
    }
}
