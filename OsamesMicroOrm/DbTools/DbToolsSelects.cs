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
using System.Reflection;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe servant à formater et exécuter des requêtes SQL de type SELECT, en proposant une abstraction au dessus de ADO.NET.
    /// </summary>
    public static class DbToolsSelects
    {
        /// <summary>
        /// Dans le cas d'un select basé sur un template <c>"SELECT {0} FROM {1} [WHERE ...]", crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET</c>.
        /// Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés d'une classe C# à utiliser, pour les champs à sélectionner</description></item>
        /// <item><description>liste de noms de propriétés d'une classe C# à utiliser, ou paramètres dynamiques, ou littéraux, pour les paramètres dans la partie WHERE, ainsi que leurs valeurs associées.</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {1} dans le template sql</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstDataObjectPropertyNames_">Noms des propriétés d'une classe C# à utiliser, pour les champs à sélectionner. Permet de formater {0} dans le template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        /// <returns>Sortie : structure contenant : texte de la commande SQL paramétrée, clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static InternalPreparedStatement FormatSqlForSelect(string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyNames_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, out List<string> lstDbColumnNames_)
        {
            // 1. Détermine les colonnes pour les champs à sélectionner.
            // lstDbColumnNames_ sert de fournisseur pour remplir sbSqlSelectFieldsCommand
            DbToolsCommon.DetermineDatabaseColumnNames(mappingDictionariesContainerKey_, lstDataObjectPropertyNames_, out lstDbColumnNames_);

            string sbSqlSelectFieldsCommand = DbToolsCommon.GenerateCommaSeparatedDbFieldsString(lstDbColumnNames_); //{0} dans le template sql

            // 2. Positionne les deux premiers placeholders : chaîne pour les champs à sélectionner, nom de la table
            List<string> sqlPlaceholders = new List<string> { sbSqlSelectFieldsCommand, string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            List<KeyValuePair<string, object>> lstAdoParameters = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters);

            string sqlCommand;
            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicSelectSql, sqlTemplateName_, out sqlCommand, sqlPlaceholders.ToArray());

            return new InternalPreparedStatement(new PreparedStatement(sqlCommand, lstAdoParameters.Count), lstAdoParameters);
        }

        /// <summary>
        /// Dans le cas d'un select basé sur un template "SELECT * FROM {0} [WHERE ...]", crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET.
        /// <para>Utilise :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>liste de noms de propriétés d'une classe C# à utiliser, ou paramètres dynamiques, ou littéraux, pour les paramètres dans la partie WHERE, ainsi que leurs valeurs associées</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB à sélectionner. Sera utilisé pour le data reader</param>
        /// <param name="lstPropertiesNames_">Sortie : liste de noms de propriétés d'objet Db Entité à sélectionner. Sera utilisé pour le data reader</param>
        /// <param name="skipAutoDetermine_">Si a vrai alors on ne fait pas de détermination des noms des colonnes à partir des noms des propriétéss.</param>
        /// <returns>Sortie : structure contenant : texte de la commande SQL paramétrée, clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static InternalPreparedStatement FormatSqlForSelectAutoDetermineSelectedFields(string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, out  List<string> lstPropertiesNames_, out List<string> lstDbColumnNames_, bool skipAutoDetermine_ = false)
        {
            lstDbColumnNames_ = new List<string>(); // Noms des colonnes DB, à construire
            lstPropertiesNames_ = new List<string>(); // Propriétés de l'objet de données, à construire

            if (!skipAutoDetermine_)
                DbToolsCommon.DetermineDatabaseColumnNamesAndDataObjectPropertyNames(mappingDictionariesContainerKey_, out lstDbColumnNames_, out lstPropertiesNames_);

            // 1. Positionne le premier placeholder : nom de la table
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            List<KeyValuePair<string, object>> lstAdoParameters = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 2. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters);

            string sqlCommand;
            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicSelectSql, sqlTemplateName_, out sqlCommand, sqlPlaceholders.ToArray());

            return new InternalPreparedStatement(new PreparedStatement(sqlCommand, lstAdoParameters.Count), lstAdoParameters);
        }

        /// <summary>
        /// Lit les champs indiqués en paramètre lstDbColumnNames_ dans le tableau de données du DataReader et positionne les valeurs sur les propriétés de lstPropertiesNames_ de l'instance databaseEntityObject_ (une classe C#) paramètre.
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="databaseEntityObject_">Objet de données</param>
        /// <param name="reader_">IDataReader ADO.NET</param>
        /// <param name="lstDbColumnNames_">Noms des colonnes DB à lire dans le data reader paramètre</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet databaseEntityObject_ de type T à utiliser pour les champs à sélectionner</param>
        /// <returns>Ne retourne rien</returns>
        /// <exception cref="OOrmHandledException">Problème de lecture du IDataReader (demande d'une colonne incorrecte...)</exception>
        private static void FillDataObjectFromDataReader<T>(T databaseEntityObject_, IDataReader reader_, List<string> lstDbColumnNames_, List<string> lstPropertiesNames_)
       where T : IDatabaseEntityObject
        {
            // parcourir toutes les colonnes de résultat et affecter la valeur à la propriété correspondante.
            for (int i = 0; i < lstDbColumnNames_.Count; i++)
            {
                string columnName = lstDbColumnNames_[i];
                int dataInReaderIndex;
                PropertyInfo databaseEntityObjectProperty = databaseEntityObject_.GetType().GetProperty(lstPropertiesNames_[i]);

                try
                {
                    dataInReaderIndex = reader_.GetOrdinal(columnName);
                }
                catch (IndexOutOfRangeException ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_COLUMNDOESNOTEXIST, ex, "column name: '" + columnName + "'");
                }

                if (dataInReaderIndex == -1)
                {
                    throw new OOrmHandledException(HResultEnum.E_COLUMNDOESNOTEXIST, null, "column name: '" + columnName + "'");
                }

                object dbValue = reader_[dataInReaderIndex];
                Type dbValueType = reader_.GetFieldType(dataInReaderIndex);

                // affecter la valeur à la propriété de T sauf si System.DbNull (la propriété est déjà à null)
                if (dbValue is DBNull) continue;

                if (databaseEntityObjectProperty == null)
                    throw new OOrmHandledException(HResultEnum.E_TYPEDOESNTDEFINEPROPERTY, null, "Class name : " + databaseEntityObject_.GetType().FullName + " Property name : " + lstPropertiesNames_[i]);

                try
                {
                    var dbValueWithType = Convert.ChangeType(dbValue, dbValueType);
                    databaseEntityObjectProperty.SetValue(databaseEntityObject_, dbValueWithType);
                }
                catch (Exception ex)
                {
                    throw new OOrmHandledException(HResultEnum.E_CANNOTSETVALUEDATAREADERTODBENTITY, ex, "[Data raw value]: '" + dbValue + "', [C# type from data reader]: ' " + dbValueType + "', [C# type of DbEntity property]: '" + databaseEntityObjectProperty.PropertyType.FullName + "'");
                }
            }
        }

        /// <summary>
        /// Retourne un objet du type T avec les données rendues par une requete SELECT dont on ne s'intéresse qu'au premier résultat retourné. Si pas de résultat retourne null.
        /// <para>Le template sera du type <c>"SELECT {0} FROM {1} [WHERE ...]"</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE" </param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <returns>Retourne un objet de type T rempli par les donnnées du DataReader, ou null.</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static T SelectSingle<T>(List<string> lstPropertiesNames_, string sqlTemplateName_, List<string> lstWhereMetaNames_ = null, List<object> lstWhereValues_ = null, OOrmDbTransactionWrapper transaction_ = null)
            where T : IDatabaseEntityObject, new()
        {
            List<string> lstDbColumnNames;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForSelect(sqlTemplateName_, mappingDictionariesContainerKey, lstPropertiesNames_, lstWhereMetaNames_, lstWhereValues_, out lstDbColumnNames);

            return GetDataObject<T>(transaction_, statement.PreparedStatement.PreparedSqlCommand, lstDbColumnNames, lstPropertiesNames_, statement.AdoParameters);
        }

        /// <summary>
        /// Retourne un objet du type T avec les données rendues par une requete SELECT dont on ne s'intéresse qu'au premier résultat retourné. Si pas de résultat retourne null.
        /// <para>Le template sera du type <c>"SELECT * FROM {0} [WHERE ...]"</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE" </param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <returns>Retourne un objet de type T rempli par les donnnées du DataReader, ou null</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static T SelectSingleAllColumns<T>(string sqlTemplateName_, List<string> lstWhereMetaNames_ = null, List<object> lstWhereValues_ = null, OOrmDbTransactionWrapper transaction_ = null)
            where T : IDatabaseEntityObject, new()
        {
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForSelectAutoDetermineSelectedFields(sqlTemplateName_, mappingDictionariesContainerKey, lstWhereMetaNames_, lstWhereValues_, out lstPropertiesNames, out lstDbColumnNames);

            return GetDataObject<T>(transaction_, statement.PreparedStatement.PreparedSqlCommand, lstDbColumnNames, lstPropertiesNames, statement.AdoParameters);
        }

        /// <summary>
        /// Retourne une liste d'objets du type T avec les données rendues par une requete SELECT. Si pas de résultat retourne une liste vide.
        /// <para>Le template sera du type <c>"SELECT {0} FROM {1} [WHERE ...]"</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE". Peut être null </param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <returns>Retourne une liste composée d'objets de type T</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static List<T> Select<T>(List<string> lstPropertiesNames_, string sqlTemplateName_, List<string> lstWhereMetaNames_ = null, List<object> lstWhereValues_ = null, OOrmDbTransactionWrapper transaction_ = null)
            where T : IDatabaseEntityObject, new()
        {
            List<string> lstDbColumnNames;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForSelect(sqlTemplateName_, mappingDictionariesContainerKey, lstPropertiesNames_, lstWhereMetaNames_, lstWhereValues_, out lstDbColumnNames);

            return GetListDataObject<T>(transaction_, statement.PreparedStatement.PreparedSqlCommand, lstDbColumnNames, lstPropertiesNames_, statement.AdoParameters);
        }

        /// <summary>
        /// Retourne une liste d'objets du type T avec les données rendues par une requete SELECT. Si pas de résultat retourne une liste vide.
        /// <para>Le template sera du type <c>"SELECT * FROM {0} WHERE ..."</c></para>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE". Peut être null </param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <returns>Retourne une liste composée d'objets de type T</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static List<T> SelectAllColumns<T>(string sqlTemplateName_, List<string> lstWhereMetaNames_ = null, List<object> lstWhereValues_ = null, OOrmDbTransactionWrapper transaction_ = null)
            where T : IDatabaseEntityObject, new()
        {
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;
            string mappingDictionariesContainerKey = MappingTools.GetTableNameFromMappingDictionary(typeof(T));

            InternalPreparedStatement statement = FormatSqlForSelectAutoDetermineSelectedFields(sqlTemplateName_, mappingDictionariesContainerKey, lstWhereMetaNames_, lstWhereValues_, out lstPropertiesNames, out lstDbColumnNames);

            return GetListDataObject<T>(transaction_, statement.PreparedStatement.PreparedSqlCommand, lstDbColumnNames, lstPropertiesNames, statement.AdoParameters);
        }

        /// <summary>
        /// Exécute une requête de type "SELECT COUNT(*) FROM {0} WHERE ..." et retourne le nombre résultat.
        /// </summary>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping. Toujours {0} dans le template sql.</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET pour la partie du template après "WHERE". Peut être null </param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <returns>Entier long</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        public static long Count(string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_ = null, List<object> lstWhereValues_ = null, OOrmDbTransactionWrapper transaction_ = null)
        {
            List<string> lstDbColumnNames;
            List<string> lstPropertiesNames;
            long count;

            InternalPreparedStatement statement = FormatSqlForSelectAutoDetermineSelectedFields(sqlTemplateName_, mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, 
                out lstPropertiesNames, out lstDbColumnNames, true);

            // Transaction
            if (transaction_ != null)
            {
                long.TryParse(DbManager.Instance.ExecuteScalar(transaction_, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters).ToString(), out count);
                return count;
            
            }
            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                long.TryParse(DbManager.Instance.ExecuteScalar(conn, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters).ToString(), out count);
                return count;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }
        }

        /// <summary>
        /// Retourne un objet du type T avec les données rendues par une requete SELECT. Si pas de résultat retourne null.
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstDbColumnNames_">Colonnes DB à utiliser pour les champs à sélectionner</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <param name="sqlCommand_">Texte final de la requête SQL</param>
        /// <param name="adoParameters_">Représentation des paramètres ADO.NET (nom et valeur)</param>
        /// <returns>Retourne un objet de type T rempli par les donnnées du DataReader, ou null</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        private static T GetDataObject<T>(OOrmDbTransactionWrapper transaction_, string sqlCommand_, List<string> lstDbColumnNames_, List<string> lstPropertiesNames_, IEnumerable<KeyValuePair<string, object>> adoParameters_)
            where T : IDatabaseEntityObject, new()
        {
            T dataObject = new T();
            // Transaction
            if (transaction_ != null)
            {
                using (IDataReader reader = DbManager.Instance.ExecuteReader(transaction_, sqlCommand_, adoParameters_))
                {
                    if (reader.Read())
                    {
                        FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames_, lstPropertiesNames_);
                    }
                    else
                    {
                        return default(T);
                    }
                }
                return dataObject;
            }
            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                using (IDataReader reader = DbManager.Instance.ExecuteReader(conn, sqlCommand_, adoParameters_))
                {
                    if (reader.Read())
                    {
                        FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames_, lstPropertiesNames_);
                    }
                    else
                    {
                        return default(T);
                    }
                }
                return dataObject;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }
        }

        /// <summary>
        /// Retourne une liste d'objets du type T avec les données rendues par une requete SELECT. Si pas de résultat retourne une liste vide.
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="lstDbColumnNames_">Colonnes DB à utiliser pour les champs à sélectionner</param>
        /// <param name="lstPropertiesNames_">Noms des propriétés de l'objet T à utiliser pour les champs à sélectionner</param>
        /// <param name="transaction_">Transaction optionelle (créée par appel à DbManager)</param>
        /// <param name="sqlCommand_">Texte final de la requête SQL</param>
        /// <param name="adoParameters_">Représentation des paramètres ADO.NET (nom et valeur)</param>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        private static List<T> GetListDataObject<T>(OOrmDbTransactionWrapper transaction_, string sqlCommand_, List<string> lstDbColumnNames_, List<string> lstPropertiesNames_, IEnumerable<KeyValuePair<string, object>> adoParameters_)
            where T : IDatabaseEntityObject, new()
        {
            List<T> dataObjects = new List<T>();
            // Transaction
            if (transaction_ != null)
            {
                using (IDataReader reader = DbManager.Instance.ExecuteReader(transaction_, sqlCommand_, adoParameters_))
                {
                    while (reader.Read())
                    {
                        T dataObject = new T();
                        dataObjects.Add(dataObject);
                        FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames_, lstPropertiesNames_);
                    }
                }
                return dataObjects;
            }
            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                using (IDataReader reader = DbManager.Instance.ExecuteReader(conn, sqlCommand_, adoParameters_))
                {
                    while (reader.Read())
                    {
                        T dataObject = new T();
                        dataObjects.Add(dataObject);
                        FillDataObjectFromDataReader(dataObject, reader, lstDbColumnNames_, lstPropertiesNames_);
                    }
                }
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }
            return dataObjects;
        }
    }
}
