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
using System.Data;
using System.Text;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// 
    /// </summary>
    public class DbToolsSelects
    {
        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un select basé sur un template <c>"SELECT {0} FROM {1}..."</c>.
        /// Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ ou paramètres dynamiques pour les paramètres dans la partie WHERE.</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {1} dans le template sql</param>
        /// <param name="sqlTemplate_">Template SQL</param>
        /// <param name="lstDataObjectColumnName_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à sélectionner. Permet de formater {0} dans le template SQL</param>
        /// <param name="strWhereColumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlForSelect(string sqlTemplate_, List<string> lstDataObjectColumnName_, string mappingDictionariesContainerKey_, List<string> strWhereColumnNames_, List<object> oWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_, out List<string> lstDbColumnNames_)
        {
            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 1. Détermine les colonnes pour les champs à sélectionner.
            // lstDbColumnNames_ sert de fournisseur pour remplir sbSqlSelectFieldsCommand
            DbToolsCommon.DetermineDatabaseColumnNames(mappingDictionariesContainerKey_, lstDataObjectColumnName_, out lstDbColumnNames_);
            string sbSqlSelectFieldsCommand = DbToolsCommon.GenerateCommaSeparatedDbFieldsString(lstDbColumnNames_); //{0} dans le template sql

            // 2. Positionne les deux premiers placeholders
            List<string> sqlPlaceholders = new List<string> { sbSqlSelectFieldsCommand, string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 3. Détermine les noms des paramètres pour le where
            if (strWhereColumnNames_ != null)
            {
                int iCount = strWhereColumnNames_.Count;
                int dynamicParameterIndex = -1;
                for (int i = 0; i < iCount; i++)
                {
                    string paramName = DbToolsCommon.DetermineAdoParameterName(strWhereColumnNames_[i], mappingDictionariesContainerKey_, ref dynamicParameterIndex);

                    // Si paramètre dynamique, ajout d'un paramètre ADO.NET dans la liste. Sinon protection du champ.
                    if (paramName.StartsWith("@"))
                        adoParameters_.Add(new KeyValuePair<string, object>(paramName, oWhereValues_[dynamicParameterIndex]));
                    else
                        paramName = string.Concat(ConfigurationLoader.StartFieldEncloser, paramName, ConfigurationLoader.EndFieldEncloser);

                    // Ajout pour les placeholders
                    sqlPlaceholders.Add(paramName);

                }
            }
            DbToolsCommon.TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, sqlPlaceholders.ToArray());
        }

        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un select basé sur un template "SELECT * FROM {0}...".
        /// <para>Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ ou paramètres dynamiques pour les paramètres dans la partie WHERE.</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="sqlTemplate_">Template SQL</param>
        /// <param name="strWhereColumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        private static void FormatSqlForSelect(string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWhereColumnNames_, List<object> oWhereValues_, List<string> lstDbColumnNames_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)
        {
            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 1. Positionne le premier placeholder
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 2. Détermine les noms des paramètres pour le where
            if (strWhereColumnNames_ != null)
            {
                int iCount = strWhereColumnNames_.Count;
                int dynamicParameterIndex = -1;
                for (int i = 0; i < iCount; i++)
                {
                    string paramName = DbToolsCommon.DetermineAdoParameterName(strWhereColumnNames_[i], mappingDictionariesContainerKey_, ref dynamicParameterIndex);

                    // Si paramètre dynamique, ajout d'un paramètre ADO.NET dans la liste. Sinon protection du champ.
                    if (paramName.StartsWith("@"))
                        adoParameters_.Add(new KeyValuePair<string, object>(paramName, oWhereValues_[dynamicParameterIndex]));
                    else
                        paramName = string.Concat(ConfigurationLoader.StartFieldEncloser, paramName, ConfigurationLoader.EndFieldEncloser);

                    // Ajout pour les placeholders
                    sqlPlaceholders.Add(paramName);
                }
            }

            DbToolsCommon.TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, sqlPlaceholders.ToArray());

        }

        /// <summary>
        /// Retourne un objet du type T avec les données rendues par une requete SELECT dont on ne s'intéresse qu'au premier résultat retourné.
        /// <para>Le template sera du type <c>"SELECT {0} FROM {1} WHERE ..."</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="refSqlTemplate_">Clé pour le template à utiliser. Le template sera du type <c>"SELECT {0} FROM {1} WHERE ..."</c></param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="strWhereColumnNames_">Noms des colonnes ou indications de paramètres dynamiques pour la partie du template après "WHERE" </param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE" </param>
        /// <returns>Retourne un objet de type T rempli par les donnnées du DataReader.</returns>
        public static T SelectSingle<T>(List<string> lstPropertiesNames_, string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWhereColumnNames_ = null, List<object> oWhereValues_ = null) where T : new()
        {
            T dataObject = new T();
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;

            FormatSqlForSelect(refSqlTemplate_, lstPropertiesNames_, mappingDictionariesContainerKey_, strWhereColumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstDbColumnNames);

            using (IDataReader reader = DbManager.Instance.ExecuteReader(sqlCommand, adoParameters))
            {
                if (reader.Read())
                {
                    FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames, lstPropertiesNames_);
                }
            }
            return dataObject;
        }

        /// <summary>
        /// Retourne un objet du type T avec les données rendues par une requete SELECT dont on ne s'intéresse qu'au premier résultat retourné.
        /// <para>Le template sera du type <c>"SELECT * FROM {0} WHERE ..."</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="refSqlTemplate_">Clé pour le template à utiliser. Le template sera du type <c>"SELECT * FROM {0} WHERE ..."</c></param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="strWhereColumnNames_">Noms des colonnes ou indications de paramètres dynamiques pour la partie du template après "WHERE" </param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE" </param>
        /// <returns>Retourne un objet de type T</returns>
        public static T SelectSingleAllColumns<T>(string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWhereColumnNames_ = null, List<object> oWhereValues_ = null) where T : new()
        {
            T dataObject = new T();
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;

            DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames(mappingDictionariesContainerKey_, out lstDbColumnNames, out lstPropertiesNames);

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, strWhereColumnNames_, oWhereValues_, lstDbColumnNames, out sqlCommand, out adoParameters);

            using (IDataReader reader = DbManager.Instance.ExecuteReader(sqlCommand, adoParameters))
            {
                if (reader.Read())
                {
                    FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames, lstPropertiesNames);
                }
            }
            return dataObject;
        }

        /// <summary>
        /// Retourne une liste d'objets du type T avec les données rendues par une requete SELECT.
        /// <para>Le template sera du type <c>"SELECT {0} FROM {1} WHERE ..."</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="refSqlTemplate_">Clé pour le template à utiliser. Le template sera du type <c>"SELECT {0} FROM {1} WHERE ..."</c></param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="strWherecolumnNames_">Noms des colonnes ou indications de paramètres dynamiques pour la partie du template après "WHERE". Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE". Peut être null </param>
        /// <returns>Retourne une liste composée d'objets de type T</returns>
        public static List<T> Select<T>(List<string> lstPropertiesNames_, string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWherecolumnNames_ = null, List<object> oWhereValues_ = null) where T : new()
        {
            List<T> dataObjects = new List<T>();
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;

            FormatSqlForSelect(refSqlTemplate_, lstPropertiesNames_, mappingDictionariesContainerKey_, strWherecolumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstDbColumnNames);

            using (IDataReader reader = DbManager.Instance.ExecuteReader(sqlCommand, adoParameters))
            {
                while (reader.Read())
                {
                    T dataObject = new T();
                    dataObjects.Add(dataObject);

                    FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames, lstPropertiesNames_);
                }
            }
            return dataObjects;
        }

        /// <summary>
        /// Retourne une liste d'objets du type T avec les données rendues par une requete SELECT.
        /// <para>Le template sera du type <c>"SELECT * FROM {0} WHERE ..."</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="refSqlTemplate_">Clé pour le template à utiliser. Le template sera du type <c>"SELECT * FROM {0} WHERE ..."</c></param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="strWherecolumnNames_">Noms des colonnes ou indications de paramètres dynamiques pour la partie du template après "WHERE". Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE". Peut être null </param>
        /// <returns>Retourne une liste composée d'objets de type T</returns>
        public static List<T> SelectAllColumns<T>(string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWherecolumnNames_ = null, List<object> oWhereValues_ = null) where T : new()
        {
            List<T> dataObjects = new List<T>();
            string sqlCommand;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;

            DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames(mappingDictionariesContainerKey_, out lstDbColumnNames, out lstPropertiesNames);

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, strWherecolumnNames_, oWhereValues_, lstDbColumnNames, out sqlCommand, out adoParameters);

            using (IDataReader reader = DbManager.Instance.ExecuteReader(sqlCommand, adoParameters))
            {
                while (reader.Read())
                {
                    T dataObject = new T();
                    dataObjects.Add(dataObject);

                    FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames, lstPropertiesNames);
                }
            }
            return dataObjects;
        }

        /// <summary>
        /// Lit les champs indiqués en paramètre dans le tableau de données du DataReader et positionne les valeurs sur les propriétés de dataObject_ paramètre.
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_"></param>
        /// <param name="reader_"></param>
        /// <param name="lstDbColumnNames_"></param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <returns>Ne retourne rien</returns>
        private static void FillDataObjectFromDataReader<T>(T dataObject_, IDataReader reader_, List<string> lstDbColumnNames_, List<string> lstPropertiesNames_)
        {
            // parcourir toutes les colonnes de résultat et affecter la valeur à la propriété correspondante.
            for (int i = 0; i < lstDbColumnNames_.Count; i++)
            {
                string columnName = lstDbColumnNames_[i];
                int dataInReaderIndex;
                try
                {
                    dataInReaderIndex = reader_.GetOrdinal(columnName);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new Exception("Column '" + columnName + "' doesn't exist in sql data reader");
                }

                if (dataInReaderIndex == -1)
                {
                    throw new Exception("Column '" + columnName + "' doesn't exist in sql data reader");
                }

                // TODO traiter ORM-45 pour cast vers le bon type.
                object dbValue = reader_[dataInReaderIndex];

                // affecter la valeur à la propriété de T sauf si System.DbNull (la propriété est déjà à null)
                if (dbValue.GetType() != typeof(DBNull))
                {
                    try
                    {
                        dataObject_.GetType().GetProperty(lstPropertiesNames_[i]).SetValue(dataObject_, dbValue);
                    }
                    catch (ArgumentException)
                    {
                        // par exemple valeur entière et propriété de type string
                        dataObject_.GetType().GetProperty(lstPropertiesNames_[i]).SetValue(dataObject_, dbValue.ToString());
                    }
                }
            }
        }

    }
}
