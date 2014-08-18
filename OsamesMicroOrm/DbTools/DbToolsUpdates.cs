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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe servant à formater et exécuter des requêtes SQL de type UPDATE, en proposant une abstraction au dessus de ADO.NET.
    /// </summary>
    public class DbToolsUpdates
    {
        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un update sur un seul objet.
        /// Utilise le template "BaseUpdate" : <c>"UPDATE {0} SET {1} WHERE {2}"</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>un objet de données dataObject_</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>nom de la propriété de dataObject_ correspondant au champ clé primaire</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplate_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="primaryKeycolumnName_">Nom de la propriété de dataObject_ correspondant au champ clé primaire</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string sqlTemplate_, List<string> lstDataObjectcolumnName_, string primaryKeycolumnName_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)
        {
            StringBuilder sbSqlSetCommand = new StringBuilder();
            StringBuilder sbSqlWhereCommand = new StringBuilder();

            List<string> lstDbColumnNames;
            string primaryKeyDbColumnName;
            KeyValuePair<string, object> adoParamForPrimaryKey;

            // 1. properties
            DbToolsCommon.DetermineDatabaseColumnNamesAndAdoParameters(ref dataObject_, mappingDictionariesContainerKey_, lstDataObjectcolumnName_, out lstDbColumnNames, out adoParameters_);
            DbToolsCommon.FormatSqlNameEqualValueString(lstDbColumnNames, adoParameters_, ref sbSqlSetCommand, ", ");

            // 2. primary key
            DbToolsCommon.DetermineDatabaseColumnNameAndAdoParameter(ref dataObject_, mappingDictionariesContainerKey_, primaryKeycolumnName_, out primaryKeyDbColumnName, out adoParamForPrimaryKey);
            DbToolsCommon.FormatSqlNameEqualValueString(primaryKeyDbColumnName, adoParamForPrimaryKey, ref sbSqlWhereCommand);

            // TODO ici rendre comme pour le select, indépendant du template

            // 3. Final formatting "UPDATE {0} SET {1} WHERE {2};"
            try{
                DbToolsCommon.TryFormat(ConfigurationLoader.DicUpdateSql[sqlTemplate_], out sqlCommand_, new object[] { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser), sbSqlSetCommand, sbSqlWhereCommand });
            }
            catch (Exception ex)
            {
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Error, 0, "Query didn't update any row. Exception error: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Exécution d'une mise à jour d'un objet de données vers la base de données
        /// <list type="bullet">
        /// <item><description>formatage des éléments nécessaires par appel à <c>FormatSqlForUpdate &lt;T&gt;()</c></description></item>
        /// <item><description>appel de bas niveau ADO.NET</description></item>
        /// <item><description>sortie : nombre de lignes mises à jour</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="propertiesNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="primaryKeycolumnName_">Nom de la propriété de dataObject_ correspondant au champ clé primaire</param>
        /// <returns>Retourne le nombre d'enregistrements modifiés dans la base de données.</returns>
        public static int Update<T>(T dataObject_, string mappingDictionariesContainerKey_, string sqlTemplate_, List<string> propertiesNames_, string primaryKeycolumnName_)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;

            FormatSqlForUpdate(ref dataObject_, mappingDictionariesContainerKey_, sqlTemplate_, propertiesNames_, primaryKeycolumnName_, out sqlCommand, out adoParameters);

            long lastInsertedRowId;
            int nbRowsAffected = DbManager.Instance.ExecuteNonQuery(sqlCommand, adoParameters, out lastInsertedRowId);
            if (nbRowsAffected == 0)
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Warning, 0, "Query didn't update any row: " + sqlCommand);

            return nbRowsAffected;
        }

    }
}
