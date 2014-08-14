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
using System.Xml.XPath;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class XmlTools
    {
        /// <summary>
        /// Reads XML root tag and get information.
        /// </summary>
        /// <param name="xmlFile_">XML file full path</param>
        /// <param name="rootTagPrefix_">Prefix associated to namespace, for example "orm"</param>
        /// <param name="rootTagNamespace_">Namespace, for example "http://www.osames.org/osamesorm"</param>
        /// <returns>XPathNavigator for later use</returns>
        public static XPathNavigator GetRootTagInfos(string xmlFile_, out string rootTagPrefix_, out string rootTagNamespace_)
        {
            XPathDocument doc = new XPathDocument(xmlFile_);
            XPathNavigator navigator = doc.CreateNavigator();

            XPathNodeIterator nodes = navigator.Select("./*");
            nodes.MoveNext();
            rootTagPrefix_ = nodes.Current.Prefix;
            rootTagNamespace_ = nodes.Current.NamespaceURI;

            return navigator;
        }
    }
}
