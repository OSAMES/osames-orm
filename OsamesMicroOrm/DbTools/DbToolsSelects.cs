﻿/*
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
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// 
    /// </summary>
    public class DbToolsSelects
    {
        /// <summary>
        /// Dans le cas d'un select basé sur un template <c>"SELECT {0} FROM {1}...", cée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, </c>.
        /// Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à sélectionner</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ ou paramètres dynamiques pour les paramètres dans la partie WHERE, ainsi que les valeurs associées.</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {1} dans le template sql</param>
        /// <param name="sqlTemplate_">Template SQL</param>
        /// <param name="lstDataObjectColumnNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à sélectionner. Permet de formater {0} dans le template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlForSelect(string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstDataObjectColumnName_, List<string> lstWhereMetaNames_, List<object> oWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_, out List<string> lstDbColumnNames_)
        {
            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire
            string strLocalErrorMsg;

            // 1. Détermine les colonnes pour les champs à sélectionner.
            // lstDbColumnNames_ sert de fournisseur pour remplir sbSqlSelectFieldsCommand
            DbToolsCommon.DetermineDatabaseColumnNames(mappingDictionariesContainerKey_, lstDataObjectColumnNames_, out lstDbColumnNames_, out strErrorMsg_);
            
            string sbSqlSelectFieldsCommand = DbToolsCommon.GenerateCommaSeparatedDbFieldsString(lstDbColumnNames_); //{0} dans le template sql

            // 2. Positionne les deux premiers placeholders
            List<string> sqlPlaceholders = new List<string> { sbSqlSelectFieldsCommand, string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, oWhereValues_, sqlPlaceholders, adoParameters_);

            DbToolsCommon.TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, out strLocalErrorMsg, sqlPlaceholders.ToArray());

            strErrorMsg_ = string.Concat(strErrorMsg_, "\n", strLocalErrorMsg);
        }

        /// <summary>
        /// Dans le cas d'un select basé sur un template "SELECT * FROM {0}...", crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, .
        /// <para>Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ ou paramètres dynamiques pour les paramètres dans la partie WHERE, ainsi que les valeurs associées..</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="sqlTemplate_">Template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB à sélectionner. Sera utilisé pour le data reader</param>
        /// <param name="lstPropertiesNames_">Sortie : liste de noms de propriétés d'objet Db Entité à sélectionner. Sera utilisé pour le data reader</param>
        internal static void FormatSqlForSelect(string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> oWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_, out  List<string> lstPropertiesNames_, out List<string> lstDbColumnNames_)
        {
            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire
            lstDbColumnNames_ = new List<string>(); // Noms des colonnes DB, à construire
            lstPropertiesNames_ = new List<string>(); // Propriétés de l'objet de données, à construire

            DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames(mappingDictionariesContainerKey_, out lstDbColumnNames_, out lstPropertiesNames_);

            // 1. Positionne le premier placeholder
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 2. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, oWhereValues_, sqlPlaceholders, adoParameters_);

            DbToolsCommon.TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, out strErrorMsg_, sqlPlaceholders.ToArray());

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
        internal static void FillDataObjectFromDataReader<T>(T dataObject_, IDataReader reader_, List<string> lstDbColumnNames_, List<string> lstPropertiesNames_)
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
        internal static T SelectSingle<T>(List<string> lstPropertiesNames_, string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWhereColumnNames_ = null, List<object> oWhereValues_ = null) where T : new()
        {
            T dataObject = new T();
            string sqlCommand, strErrorMsg_;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, strWhereColumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstDbColumnNames);

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
            string sqlCommand, strErrorMsg_;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, strWhereColumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstPropertiesNames, out lstDbColumnNames);

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
            string sqlCommand, strErrorMsg_;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, lstPropertiesNames_, strWherecolumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstDbColumnNames);

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
            string sqlCommand, strErrorMsg_;
            List<KeyValuePair<string, object>> adoParameters;
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;

            FormatSqlForSelect(refSqlTemplate_, mappingDictionariesContainerKey_, strWherecolumnNames_, oWhereValues_, out sqlCommand, out adoParameters, out lstPropertiesNames, out lstDbColumnNames);

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

    }
}
