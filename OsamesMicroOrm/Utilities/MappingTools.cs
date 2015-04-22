/*
This file is part of OSAMES Micro ORM.
Copyright 2014 OSAMES

OSAMES Micro ORM is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

OSAMES Micro ORM is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with OSAMES Micro ORM.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Linq;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// Utility methods related to database/object mapping.
    /// Les méthodes publiques renvoient des noms de table et colonne protégés.
    /// Les méthodes qui renvoient des noms de table et colonne non protégés doivent être internal ou private.
    /// </summary>
    public static class MappingTools
    {
        private static Dictionary<string, string[]> MappingDictionary = new Dictionary<string, string[]>();

        #region remplissage du dictionnaire pour une clé donnée

        /// <summary>
        /// Détermine les données à mettre dans le dictionnaire interne.
        /// </summary>
        /// <typeparam name="T">IDatabaseEntityObject</typeparam>
        /// <param name="dictionaryKey_"></param>
        /// <param name="databaseEntityObject_"></param>
        /// <param name="propertyName_"></param>
        /// <returns>Tableau des valeurs qu'on vient d'ajouter au dictionnaire</returns>
        private static string[] FillInternalDictionary<T>(string dictionaryKey_, T databaseEntityObject_, string propertyName_)
        {
            string[] values = new string[2];
            
            if (databaseEntityObject_ == null)
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "dbEntity is null");
            if (string.IsNullOrEmpty(propertyName_) || string.IsNullOrWhiteSpace(propertyName_))
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "Property name is null");
            if (databaseEntityObject_.GetType().GetProperty(propertyName_) == null)
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "Property does not exist in object");

            string tableName = GetTableNameFromMappingDictionary(databaseEntityObject_);
            string protectedColumnName = ConfigurationLoader.StartFieldEncloser + GetDbColumnNameFromMappingDictionary(tableName, propertyName_) + ConfigurationLoader.EndFieldEncloser;
            string protectedTableName = ConfigurationLoader.StartFieldEncloser + tableName + ConfigurationLoader.EndFieldEncloser;

            // Première valeur : colonne protégée
            values[0] = protectedColumnName;

            // Deuxième valeur : colonne et table protégées
            values[1] = protectedTableName + "." + protectedColumnName;

            MappingDictionary[dictionaryKey_] = values;

            return values;

        }

        #endregion

        #region obtention du nom de la table en DB
        /// <summary>
        /// Retourne le nom de la table (protégé) pour la classe paramètre.
        /// Reads value of DatabaseMapping class custom attribute.
        /// Méthode utile pour formater des requêtes SQL personnalisées à exécuter par DbManager (API de bas niveau).
        /// </summary>
        /// <param name="databaseEntityObject_">Objet de données, classe C# dont on s'attend à ce qu'elle soit décorée par DatabaseMappingAttribute</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns>Nom de table défini par l'attribut DatabaseMapping porté par le déclaratif de la classe C# de databaseEntityObject_</returns>
        /// <exception cref="OOrmHandledException">Attribut défini de manière incorrecte</exception>
        public static string GetTableName<T>(T databaseEntityObject_) where T : IDatabaseEntityObject
        {
            return ConfigurationLoader.StartFieldEncloser + GetTableNameFromMappingDictionary(databaseEntityObject_) + ConfigurationLoader.EndFieldEncloser;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEntityObject_"></param>
        /// <returns></returns>
        public static string GetUnprotectedTableName<T>(T databaseEntityObject_) where T : IDatabaseEntityObject
        {
            return GetTableNameFromMappingDictionary(databaseEntityObject_);
        }

        /// <summary>
        /// Retourne le nom de la table (non protégé) pour la DbEntity paramètre.
        /// Reads value of DatabaseMapping class custom attribute.
        /// </summary>
        /// <param name="databaseEntityObject_">Objet de données, classe C# dont on s'attend à ce qu'elle soit décorée par DatabaseMappingAttribute</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns>Nom de table défini par l'attribut DatabaseMapping porté par le déclaratif de la classe C# de databaseEntityObject_</returns>
        /// <exception cref="OOrmHandledException">Attribut défini de manière incorrecte</exception>
        private static string GetTableNameFromMappingDictionary<T>(T databaseEntityObject_)
        {
            // Get value
            object[] classAttributes = databaseEntityObject_.GetType().GetCustomAttributes(typeof(DatabaseMappingAttribute), false);
            if (classAttributes.Length == 0)
                throw new OOrmHandledException(HResultEnum.E_TYPENOTDEFINEDBMAPPINGATTRIBUTE, null, "C# type : '" + databaseEntityObject_.GetType().FullName + "'");

            if (classAttributes.Length > 1)
                throw new OOrmHandledException(HResultEnum.E_TYPEDEFINESDBMAPPINGATTRIBUTEMOREONETIME, null, "C# type: '" + databaseEntityObject_.GetType().FullName + "'");

            string dbTableName = ((DatabaseMappingAttribute)classAttributes[0]).DbTableName;

            if (string.IsNullOrWhiteSpace(dbTableName))
                throw new OOrmHandledException(HResultEnum.E_TYPEDEFINESEMPTYDBMAPPINGATTRIBUTE, null, "C# type: '" + databaseEntityObject_.GetType().FullName + "'");

            // Check that value exists in mapping
            if (!ConfigurationLoader.MappingDictionnary.ContainsKey(dbTableName))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "Key (table name): '" + dbTableName + "'");

            return dbTableName;
        }

        #endregion

        #region obtention du nom de la colonne en DB

        /// <summary>
        /// Retourne le nom de la colonne (non protégé) dans la table paramètre pour le nom de propriété paramètre.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="propertyName_">Nom d'une propriété de databaseEntityObject_. Ex: "CustomerId"</param>
        /// <returns>DB column name. Ex: "id_customer"</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping pour les paramètres donnés</exception>
        internal static string GetDbColumnNameFromMappingDictionary(string mappingDictionaryName_, string propertyName_)
        {
            Dictionary<string, string> mappingObjectSet;
            string resultColumnName;

            if (!ConfigurationLoader.MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "'" + mappingDictionaryName_ + "'");

            mappingObjectSet.TryGetValue(propertyName_, out resultColumnName);

            if (!mappingObjectSet.TryGetValue(propertyName_, out resultColumnName))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEYANDPROPERTY, null, "[No property '" + propertyName_ + "' in dictionary " + mappingDictionaryName_ + "]");

            return resultColumnName;
        }

        /// <summary>
        /// Retourne le nom de la colonne protégé pour la propriété du DbEntity paramètre.
        /// Méthode utile pour formater des requêtes SQL personnalisées à exécuter par DbManager (API de bas niveau).
        /// </summary>
        /// <param name="databaseEntityObject_">Objet de données, classe C# dont on s'attend à ce qu'elle soit décorée par DatabaseMappingAttribute</param>
        /// <param name="propertyName_">Nom d'une propriété de databaseEntityObject_. Ex: "CustomerId"</param>
        /// <returns></returns>
        public static string GetDbColumnName<T>(T databaseEntityObject_, string propertyName_) where T : IDatabaseEntityObject
        {
            string key = databaseEntityObject_.UniqueName + "#" + propertyName_;
            return MappingDictionary.ContainsKey(key) ? MappingDictionary[key][0] : FillInternalDictionary(key, databaseEntityObject_, propertyName_)[0];
        }

        /// <summary>
        /// Retourne le nom de la colonne protégé préfixé par le nom de la table protégé pour la propriété du DbEntity paramètre.
        /// Méthode utile pour formater des requêtes SQL personnalisées à exécuter par DbManager (API de bas niveau).
        /// </summary>
        /// <param name="databaseEntityObject_">Objet de données, classe C# dont on s'attend à ce qu'elle soit décorée par DatabaseMappingAttribute</param>
        /// <param name="propertyName_">Nom d'une propriété de databaseEntityObject_. Ex: "CustomerId"</param>
        /// <returns></returns>
        public static string GetDbTableAndColumnName<T>(T databaseEntityObject_, string propertyName_) where T : IDatabaseEntityObject
        {
            string key = databaseEntityObject_.UniqueName + "#" + propertyName_;
            return MappingDictionary.ContainsKey(key) ? MappingDictionary[key][1] : FillInternalDictionary(key, databaseEntityObject_, propertyName_)[1];
        }
        #endregion

        #region obtention de l'ensemble nom de la propriété d'un DbEntity et nom de la colonne en DB (dictionnaire)
        /// <summary>
        /// Retourne les informations de mapping pour une table donnée sous forme de dictionnaire :
        /// clés : propriétés de la classe C# DbEntity, valeurs : noms des colonnes en base de données.
        /// Les noms des colonnes sont non protégées.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <returns>Mapping dictionary</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping pour le nom paramètre</exception>
        internal static Dictionary<string, string> GetMappingDefinitionsForTable(string mappingDictionaryName_)
        {
            Dictionary<string, string> mappingObjectSet;

            if (!ConfigurationLoader.MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "'" + mappingDictionaryName_ + "'");
            return mappingObjectSet;
        }

        /// <summary>
        /// Retourne les informations de mapping pour la table associée à une classe C# décorée d'un DatabaseMappingAttribute, sous forme de dictionnaire :
        /// clés : propriétés de la classe C# DbEntity, valeurs : noms des colonnes en base de données, protégées et préfixées par le nom de la table.
        /// Méthode utile pour formater des requêtes SQL personnalisées à exécuter par DbManager (API de bas niveau).
        /// </summary>
        /// <param name="databaseEntityObject_">Objet de données, classe C# dont on s'attend à ce qu'elle soit décorée par DatabaseMappingAttribute</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns></returns>
        public static Dictionary<string, string> GetMappingDefinitionsFor<T>(T databaseEntityObject_)
        {
            if (databaseEntityObject_ == null)
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "dbEntity is null");

            string table = GetTableNameFromMappingDictionary(databaseEntityObject_);

            return GetMappingDefinitionsForTable(table).ToDictionary(
                item_ => item_.Key, item_ => string.Concat(
                    ConfigurationLoader.StartFieldEncloser, table, ConfigurationLoader.EndFieldEncloser, '.', ConfigurationLoader.StartFieldEncloser, item_.Value, ConfigurationLoader.EndFieldEncloser));
        }

        #endregion
    }
}
