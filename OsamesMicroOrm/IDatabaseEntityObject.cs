namespace OsamesMicroOrm
{
    /// <summary>
    /// Interface à utiliser si on ne peut dériver de la classe abstraite DatabaseEntityObject qui est optimisée.
    /// </summary>
    public interface IDatabaseEntityObject
    {
        /// <summary>
        /// Retourne un identifiant unique pour la classe.
        /// </summary>
        string UniqueName { get; }
    }
}
