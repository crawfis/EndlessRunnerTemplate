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
        // Appended (M1 phase A4). Safe to append: existing values 0-3 keep their int mapping,
        // so serialized PowerUpDefinition assets are unaffected. Proves the IPowerUpEffect seam.
        CoinDoubler,
    }
}
