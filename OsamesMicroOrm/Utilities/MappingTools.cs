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
    /// </summary>
    public static class MappingTools
    {
        #region obtention du nom de la table en DB
        /// <summary>
        /// Retourne le nom de la table pour la DbEntity paramètre.
        /// Reads value of DatabaseMapping class custom attribute.
        /// </summary>
        /// <param name="dataObject_">data object</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns>Nom de table défini par l'attribut DatabaseMapping porté par le déclaratif de la classe C# de dataObject_</returns>
        /// <exception cref="OOrmHandledException">Attribut défini de manière incorrecte</exception>
        public static string GetTableName<T>(T dataObject_)
        {
            // Get value
            object[] classAttributes = dataObject_.GetType().GetCustomAttributes(typeof(DatabaseMappingAttribute), false);
            if(classAttributes.Length == 0)
                throw new OOrmHandledException(HResultEnum.E_TYPENOTDEFINEDBMAPPINGATTRIBUTE, null, "C# type : '" + dataObject_.GetType().FullName + "'");
            
            if(classAttributes.Length > 1)
                throw new OOrmHandledException(HResultEnum.E_TYPEDEFINESDBMAPPINGATTRIBUTEMOREONETIME, null, "C# type: '" + dataObject_.GetType().FullName + "'");

            string dbTableName = ((DatabaseMappingAttribute) classAttributes[0]).DbTableName;
            
            if (string.IsNullOrWhiteSpace(dbTableName))
                throw new OOrmHandledException(HResultEnum.E_TYPEDEFINESEMPTYDBMAPPINGATTRIBUTE, null, "C# type: '" + dataObject_.GetType().FullName + "'");

            // Check that value exists in mapping
            if(!ConfigurationLoader.MappingDictionnary.ContainsKey(dbTableName))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "Key (table name): '" + dbTableName + "'");

            return dbTableName;

        }

        /// <summary>
        /// Retourne le nom de la table pour la DbEntity paramètre, avec des fields enclosers.
        /// Reads value of DatabaseMapping class custom attribute.
        /// </summary>
        /// <param name="dataObject_">data object</param>
        /// <typeparam name="T">type indication</typeparam>
        /// <returns>Nom de table défini par l'attribut DatabaseMapping porté par le déclaratif de la classe C# de dataObject_</returns>
        /// <exception cref="OOrmHandledException">Attribut défini de manière incorrecte</exception>
        public static string GetProtectedTableName<T>(T dataObject_)
        {
            return ConfigurationLoader.StartFieldEncloser + GetTableName(dataObject_) + ConfigurationLoader.EndFieldEncloser;
        }

        #endregion

        #region obtention du nom de la colonne en DB

        /// <summary>
        /// Asks mapping dictionary for a DB column name, given a Db entity property name.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="propertyName_">DB entity C# object property name. Ex: "CustomerId"</param>
        /// <returns>DB column name. Ex: "id_customer"</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping pour les paramètres donnés</exception>
        internal static string GetDbColumnNameFromMappingDictionary(string mappingDictionaryName_, string propertyName_)
        {
            Dictionary<string, string> mappingObjectSet;
            string resultColumnName;

            if (!ConfigurationLoader.MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "[" + mappingDictionaryName_ + "]");

            mappingObjectSet.TryGetValue(propertyName_, out resultColumnName);

            if (!mappingObjectSet.TryGetValue(propertyName_, out resultColumnName))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEYANDPROPERTY, null, "[No property '" + propertyName_ + "' in dictionary " + mappingDictionaryName_ + "]");

            return resultColumnName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbEntity_"></param>
        /// <param name="propertyName_"></param>
        /// <returns></returns>
        public static string GetDbColumnName<T>(T dbEntity_, string propertyName_)
        {
            if (dbEntity_ == null)
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "dbEntity is null");
            if (string.IsNullOrEmpty(propertyName_) || string.IsNullOrWhiteSpace(propertyName_))
                throw new OOrmHandledException(HResultEnum.E_NULLVALUE, null, "Property name is null");

            return ConfigurationLoader.StartFieldEncloser + GetDbColumnNameFromMappingDictionary(GetTableName(dbEntity_), propertyName_) + ConfigurationLoader.EndFieldEncloser;
        }
  
        #endregion

        #region obtention du nom de la propriété d'un DbEntity
        /// <summary>
        /// Asks mapping dictionary for a Db entity object property name, given a DB column name.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="dbColumnName_">DB column name. Ex: "id_customer"</param>
        /// <returns>DB entity C# object property name. Ex: "CustomerId"</returns>
        internal static string GetDbEntityPropertyNameFromMappingDictionary(string mappingDictionaryName_, string dbColumnName_)
        {
            Dictionary<string, string> mappingObjectSet;

            if (!ConfigurationLoader.MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "[" + mappingDictionaryName_ + "]");

            string resultPropertyName = (from mapping in mappingObjectSet where mapping.Value == dbColumnName_ select mapping.Value).FirstOrDefault();

            if (resultPropertyName == null)
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEYANDCOLUMN, null, "[No column '" + dbColumnName_ + "' in dictionary " + mappingDictionaryName_ + "]");

            return resultPropertyName;
        }

        #endregion

        #region obtention de l'ensemble nom de la colonne en DB + nom de la propriété d'un DbEntity
        /// <summary>
        /// Retourne les informations de mapping pour une table donnée sous forme de dictionnaire :
        /// clés : propriétés de la classe C# DbEntity, valeurs : noms des colonnes en bae de données.
        /// </summary>
        /// <param name="mappingDictionaryName_">Nom du dictionnaire de mapping à utiliser</param>
        /// <returns>Mapping dictionary</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping pour le nom paramètre</exception>
        public static Dictionary<string, string> GetMappingDefinitionsForTable(string mappingDictionaryName_)
        {
            Dictionary<string, string> mappingObjectSet;

            if (!ConfigurationLoader.MappingDictionnary.TryGetValue(mappingDictionaryName_, out mappingObjectSet))
                throw new OOrmHandledException(HResultEnum.E_NOMAPPINGKEY, null, "[" + mappingDictionaryName_ + "]");
            return mappingObjectSet;
        }


        public static List<KeyValuePair<string, string>> GetPropertyToColumnMapping<T>(T dbEntity_)
        {
            return null;
        }

        public static List<KeyValuePair<string, string>> GetProtectedPropertyToColumnMapping<T>(T dbEntity_)
        {
            return null;
        }
        #endregion
    }
}
