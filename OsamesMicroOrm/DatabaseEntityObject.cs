namespace OsamesMicroOrm
{
    /// <summary>
    /// Class mère des classes de données.
    /// Les classes filles doivent porter une décoration OsamesMicroOrm.DatabaseMappingAttribute.
    /// </summary>
    public abstract class DatabaseEntityObject : IDatabaseEntityObject
    {
        private string FullName;

        /// <summary>
        /// Retourne la valeur de GetType().FullName mais avec un cache pour ne construire sa valeur qu'une fois.
        /// </summary>
        public string UniqueName {
            get { return FullName ?? (FullName = GetType().FullName); }
        }

        /// <summary>
        /// Copie des valeurs de l'objet paramètre vers l'objet courant.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object_"></param>
        /// <returns></returns>
        public abstract void Copy<T>(T object_);
        
    }
}
