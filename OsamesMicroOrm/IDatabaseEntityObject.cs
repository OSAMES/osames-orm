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
        
        /// <summary>
        /// Copie des valeurs de l'objet paramètre vers l'objet courant.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object_"></param>
        /// <returns></returns>
        void Copy<T>(T object_);
    }
}
