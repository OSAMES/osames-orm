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
using System.Diagnostics;
using System.Text;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Utilities;

namespace OsamesMicroOrm
{
    /// <summary>
    /// DbTools Class
    /// </summary>
    public static class DbTools
    {
        #region SQL string formatting

        /// <summary>
        /// Utilitaire de formatage d'une chaîne texte <c>"my_column = @myParam"</c> en l'ajoutant à un <see cref="System.Text.StringBuilder"/>.
        /// </summary>
        /// <param name="dbColumnName_">Nom de la colonne en Db</param>
        /// <param name="adoParameters_">Objets représentatifs des paramètres ADO.NET</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <param name="optionalSuffix_">Suffixe optionnel, par exemple ","</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlNameEqualValueString(string dbColumnName_, KeyValuePair<string, object> adoParameters_, ref StringBuilder sqlCommand_, string optionalSuffix_ = "")
        {
            sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(dbColumnName_).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_.Key).Append(optionalSuffix_);
        }

        /// <summary>
        /// Utilitaire de formatage d'une chaîne texte <c>"my_column = @myParam, my_column2 = @myValue2"</c> en l'ajoutant à un <see cref="System.Text.StringBuilder"/>.
        /// <para>Le suffixe est ajouté entre chaque élément de la liste lstDbColumnName_.</para>
        /// </summary>
        /// <param name="lstDbColumnName_">Liste de noms de colonne en Db</param>
        /// <param name="adoParameters_">Objets représentatifs des paramètres ADO.NET</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <param name="optionalSuffix_">Suffixe optionnel, par exemple ",", ajouté entre chaque élément (pas à la fin)</param>
        /// <returns>Ne renvoie rien.</returns>
        internal static void FormatSqlNameEqualValueString(List<string> lstDbColumnName_, List<KeyValuePair<string, object>> adoParameters_, ref StringBuilder sqlCommand_, string optionalSuffix_ = "")
        {
            int iCountMinusOne = lstDbColumnName_.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_[i].Key).Append(optionalSuffix_);
            }
            sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_[iCountMinusOne].Key);
        }

        /// <summary>
        /// Formatage d'une chaîne de texte sérialisant la liste des noms de colonnes DB paramètre et mettant une virgule entre chaque élément.
        /// </summary>
        /// <param name="lstDbColumnName_">Liste de noms de colonnes DB</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlFieldsListAsString(List<string> lstDbColumnName_, out StringBuilder sqlCommand_)
        {
            sqlCommand_ = new StringBuilder();

            int iCount = lstDbColumnName_.Count;
            for (int i = 0; i < iCount; i++)
            {
                sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
            }
            sqlCommand_.Remove(sqlCommand_.Length - 2, 2);
        }
        /// <summary>
        /// En connaissant un objet et le nom de sa propriété, génération en sortie des informations suivantes :
        /// <list type="bullet">
        /// <item><description>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</description></item>
        /// <item><description>nom et valeur du paramètre ADO.NET correspondant à la propriété (nom : proche du nom de la propriété, valeur : valeur de la propriété).</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="dataObjectcolumnName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <param name="adoParameterNameAndValue_">Sortie : clé/valeur du paramètre ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNameAndAdoParameter<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectcolumnName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)
        {
            dbColumnName_ = null;
            adoParameterNameAndValue_ = new KeyValuePair<string, object>();

            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetMappingDbColumnName(mappingDictionariesContainerKey_, dataObjectcolumnName_);

                adoParameterNameAndValue_ = new KeyValuePair<string, object>(
                                        string.Format("@{0}", dataObjectcolumnName_.ToLowerInvariant()),
                                        dataObject_.GetType().GetProperty(dataObjectcolumnName_).GetValue(dataObject_)
                                        );
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant un objet et le nom de ses propriétés, génération en sortie des informations suivantes :
        /// <list type="bullet">
        /// <item><description>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</description></item>
        /// <item><description>nom et valeur des paramètres ADO.NET correspondant aux propriétés (nom : proche du nom de la propriété, valeur : valeur de la propriété). </description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés de l'objet dataObject_</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <param name="adoParameterNameAndValue_">Sortie : clé/valeur des paramètres ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectcolumnName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_)
        {
            lstDbColumnName_ = new List<string>();

            adoParameterNameAndValue_ = new List<KeyValuePair<string, object>>();
            try
            {
                foreach (string columnName in lstDataObjectcolumnName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetMappingDbColumnName(mappingDictionariesContainerKey_, columnName));

                    adoParameterNameAndValue_.Add(new KeyValuePair<string, object>(
                                                    string.Format("@{0}", columnName.ToLowerInvariant()),
                                                    dataObject_.GetType().GetProperty(columnName).GetValue(dataObject_)
                                                ));
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de ses propriétés, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés d'un objet</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNames(string mappingDictionariesContainerKey_, List<string> lstDataObjectcolumnName_, out List<string> lstDbColumnName_)
        {
            lstDbColumnName_ = new List<string>();

            try
            {
                foreach (string columnName in lstDataObjectcolumnName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetMappingDbColumnName(mappingDictionariesContainerKey_, columnName));
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de sa propriété, génération en sortie de l'information suivante :
        /// <para>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="dataObjectcolumnName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnName(string mappingDictionariesContainerKey_, string dataObjectcolumnName_, out string dbColumnName_)
        {
            dbColumnName_ = null;

            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetMappingDbColumnName(mappingDictionariesContainerKey_, dataObjectcolumnName_);
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping, lister toutes les colonnes)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <param name="lstDataObjectPropertiesNames_">Sortie : noms des propriétés de l'objet associé au mapping</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnsAndPropertiesNames(string mappingDictionariesContainerKey_, out List<string> lstDbColumnName_, out List<string> lstDataObjectPropertiesNames_)
        {
            lstDbColumnName_ = new List<string>();
            lstDataObjectPropertiesNames_ = new List<string>();

            try
            {
                // Ce dictionnaire contient clé/valeur : propriété/nom de colonne
                Dictionary<string, string> mappingObjectSet = ConfigurationLoader.Instance.GetMapping(mappingDictionariesContainerKey_);
                foreach (string key in mappingObjectSet.Keys)
                {
                    lstDataObjectPropertiesNames_.Add(key);
                    lstDbColumnName_.Add(mappingObjectSet[key]);
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// Détermine le nom du paramètre ADO.NET selon quelques règles.
        /// <list type="bullet">
        /// <item><description>si chaîne : retourner le nom issu du mapping</description></item>
        /// <item><description>si null : retourner un nom de paramètre "@pN"</description></item>
        /// <item><description>si commence par "@" : retourne la chaîne en lowercase avec espaces remplacés.</description></item>
        /// </list>
        /// </summary>
        /// <param name="value_">Chaîne à traiter selon les règles énoncées ci-dessus</param>
        /// <param name="mappingDictionariesContainerKey_">Clé dans le dictionnaire de mapping</param>
        /// <param name="index_">Index incrémenté servant à savoir où on se trouve dans la liste des paramètres et valeurs.
        /// Sert aussi pour le nom du paramètre dynamique si on avait passé null.</param>
        /// <returns>Nom de colonne DB</returns>
        internal static string DetermineAdoParameterName(string value_, string mappingDictionariesContainerKey_, ref int index_)
        {
            if (value_ == null)
            {
                index_++;
                return string.Format("@p{0}", index_);
            }

            if (value_.StartsWith("@"))
            {
                index_++;
                return value_.ToLowerInvariant().Replace(" ", "_");
            }

            string columnName;
            DetermineDatabaseColumnName(mappingDictionariesContainerKey_, value_, out columnName);
            return columnName;
        }

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
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="primaryKeycolumnName_">Nom de la propriété de dataObject_ correspondant au champ clé primaire</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlForUpdate<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectcolumnName_, string primaryKeycolumnName_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)
        {
            StringBuilder sbSqlSetCommand = new StringBuilder();
            StringBuilder sbSqlWhereCommand = new StringBuilder();

            List<string> lstDbColumnNames;
            string primaryKeyDbColumnName;
            KeyValuePair<string, object> adoParamForPrimaryKey;

            // 1. properties
            DetermineDatabaseColumnNamesAndAdoParameters(ref dataObject_, mappingDictionariesContainerKey_, lstDataObjectcolumnName_, out lstDbColumnNames, out adoParameters_);
            FormatSqlNameEqualValueString(lstDbColumnNames, adoParameters_, ref sbSqlSetCommand, ", ");

            // 2. primary key
            DetermineDatabaseColumnNameAndAdoParameter(ref dataObject_, mappingDictionariesContainerKey_, primaryKeycolumnName_, out primaryKeyDbColumnName, out adoParamForPrimaryKey);
            FormatSqlNameEqualValueString(primaryKeyDbColumnName, adoParamForPrimaryKey, ref sbSqlWhereCommand);

            // TODO ici rendre comme pour le select, indépendant du template

            // 3. Final formatting "UPDATE {0} SET {1} WHERE {2};"
            TryFormat(ConfigurationLoader.DicUpdateSql["BaseUpdate"], out sqlCommand_, new object[] { mappingDictionariesContainerKey_, sbSqlSetCommand, sbSqlWhereCommand });
        }

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
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à sélectionner. Permet de formater {0} dans le template SQL</param>
        /// <param name="strWherecolumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlForSelect(string sqlTemplate_, List<string> lstDataObjectcolumnName_, string mappingDictionariesContainerKey_, List<string> strWherecolumnNames_, List<object> oWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_, out List<string> lstDbColumnNames_)
        {
            StringBuilder sbSqlSelectFieldsCommand;   //{0} dans le template sql

            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 1. Détermine les colonnes pour les champs à sélectionner.
            // lstDbColumnNames_ sert de fournisseur pour remplir sbSqlSelectFieldsCommand
            DetermineDatabaseColumnNames(mappingDictionariesContainerKey_, lstDataObjectcolumnName_, out lstDbColumnNames_);
            FormatSqlFieldsListAsString(lstDbColumnNames_, out sbSqlSelectFieldsCommand);

            // 2. Positionne les deux premiers placeholders
            List<string> sqlPlaceholders = new List<string> { sbSqlSelectFieldsCommand.ToString(), mappingDictionariesContainerKey_ };

            // 3. Détermine les noms des paramètres pour le where
            if (strWherecolumnNames_ != null)
            {
                int iCount = strWherecolumnNames_.Count;
                int dynamicParameterIndex = -1;
                for (int i = 0; i < iCount; i++)
                {
                    string paramName = DetermineAdoParameterName(strWherecolumnNames_[i], mappingDictionariesContainerKey_, ref dynamicParameterIndex);
                    // Ajout pour les placeholders
                    sqlPlaceholders.Add(paramName);
                    // Ajout d'un paramètre ADO.NET dans la liste
                    if (paramName.StartsWith("@"))
                        adoParameters_.Add(new KeyValuePair<string, object>(paramName, oWhereValues_[dynamicParameterIndex]));
                }
            }

            TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, sqlPlaceholders.ToArray());

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
        /// <param name="strWherecolumnNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="oWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="adoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes DB. Sera utilisé pour le data reader</param>
        internal static void FormatSqlForSelect(string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> strWherecolumnNames_, List<object> oWhereValues_, List<string> lstDbColumnNames_, out string sqlCommand_, out List<KeyValuePair<string, object>> adoParameters_)
        {
            adoParameters_ = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à construire

            // 1. Positionne le premier placeholder
            List<string> sqlPlaceholders = new List<string> { mappingDictionariesContainerKey_ };

            // 2. Détermine les noms des paramètres pour le where
            if (strWherecolumnNames_ != null)
            {
                int iCount = strWherecolumnNames_.Count;
                int dynamicParameterIndex = -1;
                for (int i = 0; i < iCount; i++)
                {
                    string paramName = DetermineAdoParameterName(strWherecolumnNames_[i], mappingDictionariesContainerKey_, ref dynamicParameterIndex);
                    // Ajout pour les placeholders
                    sqlPlaceholders.Add(paramName);
                    // Ajout d'un paramètre ADO.NET dans la liste
                    if (paramName.StartsWith("@"))
                        adoParameters_.Add(new KeyValuePair<string, object>(paramName, oWhereValues_[dynamicParameterIndex]));
                }
            }

            TryFormat(ConfigurationLoader.DicSelectSql[sqlTemplate_], out sqlCommand_, sqlPlaceholders.ToArray());

        }

        #endregion
        #region SQL execution

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

            FormatSqlForUpdate(ref dataObject_, mappingDictionariesContainerKey_, propertiesNames_, primaryKeycolumnName_, out sqlCommand, out adoParameters);

            int nbRowsAffected = DbManager.Instance.ExecuteNonQuery(sqlCommand, adoParameters);
            if (nbRowsAffected == 0)
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Warning, 0, "Query didn't update any row: " + sqlCommand);

            return nbRowsAffected;
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

            DetermineDatabaseColumnsAndPropertiesNames(mappingDictionariesContainerKey_, out lstDbColumnNames, out lstPropertiesNames);

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

            DetermineDatabaseColumnsAndPropertiesNames(mappingDictionariesContainerKey_, out lstDbColumnNames, out lstPropertiesNames);

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
                    throw new Exception(string.Format("Column '{0}' doesn't exist in sql data reader", columnName));
                }

                if (dataInReaderIndex == -1)
                {
                    throw new Exception(string.Format("Column '{0}' doesn't exist in sql data reader", columnName));
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

        #endregion

        #region utilities
        /// <summary>
        /// <c>String.Format</c> avec gestion d'exception.
        /// <para>Renvoie faux si le nombre de placeholders ou de paramètres ne sont pas égale.</para>
        /// </summary>
        /// <param name="format_">Chaîne texte avec des placeholders</param>
        /// <param name="result_">vrai/faux de type retour de valeur</param>
        /// <param name="args_">Valeurs à mettre dans les placeholders</param>
        /// <returns>Renvoie vrai si réussi, sinon retourne faux.</returns>
        public static bool TryFormat(string format_, out string result_, params Object[] args_)
        {
            try
            {
                result_ = String.Format(format_, args_);
                return true;
            }
            catch (FormatException ex)
            {
                int nbOfPlaceholders = Common.CountPlaceholders(format_);
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 0,
                    string.Format("Error, not same number of placeholders : {0} and parameters : {1}, exception: {2}", nbOfPlaceholders, args_.Length, ex.Message));
                result_ = null;
                return false;
            }
        }

        #endregion

    }
}
