using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

/*
Copyright (C) 2010-2015, Huagati Systems Co., Ltd. - https://huagati.com

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

namespace HuagatiEDMXTools
{
    internal static class XmlHelpers
    {
        internal static string XPathLiteral(string text)
        {
            if (text.Contains("'"))
            {
                string[] textParts = text.Split('\'');
                return "concat('" + string.Join("', \"'\", '", textParts) + "')";
            }
            else
            {
                return "'" + text + "'";
            }
        }

        internal static XmlElement GetOrCreateElement(this XmlElement parentElement, string prefix, string elementName, XmlNamespaceManager nsm)
        {
            return GetOrCreateElement(parentElement, prefix, elementName, nsm, false);
        }

        internal static XmlElement GetOrCreateElement(this XmlElement parentElement, string prefix, string elementName, XmlNamespaceManager nsm, bool insertAtBeginning)
        {
            XmlNode refNode = null;            
            if (insertAtBeginning == true)
            {
                refNode = parentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault();
            }
            return GetOrCreateElement(parentElement, prefix, elementName, nsm, insertAtBeginning, refNode);
        }

        internal static XmlElement GetOrCreateElement(this XmlElement parentElement, string prefix, string elementName, XmlNamespaceManager nsm, bool insertBefore, XmlNode refNode)
        {
            XmlElement elem = (XmlElement)parentElement.SelectSingleNode(prefix + ":" + elementName, nsm);
            if (elem == null)
            {
                elem = parentElement.OwnerDocument.CreateElement(elementName, nsm.LookupNamespace(prefix));
                if (insertBefore == true && refNode != null)
                {
                    parentElement.InsertBefore(elem, refNode);
                }
                else if (insertBefore == false && refNode != null)
                {
                    parentElement.InsertAfter(elem, refNode);
                }
                else
                {
                    parentElement.AppendChild(elem);
                }
            }
            return elem;
        }
    }
}
