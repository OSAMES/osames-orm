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
using System.Diagnostics;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
    class Updates
    {
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
        public static int Update<T>(T dataObject_, string mappingDictionariesContainerKey_, List<string> propertiesNames_, string primaryKeycolumnName_)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;

            DbToolsCommon.FormatSqlForUpdate(ref dataObject_, mappingDictionariesContainerKey_, propertiesNames_, primaryKeycolumnName_, out sqlCommand, out adoParameters);

            int nbRowsAffected = DbManager.Instance.ExecuteNonQuery(sqlCommand, adoParameters);
            if (nbRowsAffected == 0)
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Warning, 0, "Query didn't update any row: " + sqlCommand);

            return nbRowsAffected;
        }

    }
}
