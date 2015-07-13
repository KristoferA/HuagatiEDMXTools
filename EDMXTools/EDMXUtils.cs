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
    public enum EDMXVersionEnum
    {
        Unknown = 0,
        EDMX2008 = 1,
        EDMX2010 = 2,
        EDMX2012 = 3
    }

    internal class EDMXUtils
    {
        internal static XmlNamespaceManager GetNamespaceManager(XmlDocument edmxFile, out EDMXVersionEnum edmxVersion)
        {

            XmlNamespaceManager nsm = new XmlNamespaceManager(edmxFile.NameTable);
            switch (edmxFile.DocumentElement.NamespaceURI)
            {
                case "http://schemas.microsoft.com/ado/2009/11/edmx":
                    nsm.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2009/11/edmx", edmxFile.DocumentElement);
                    nsm.AddNamespace("store", "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator", edmxFile.DocumentElement);
                    nsm.AddNamespace("ssdl", "http://schemas.microsoft.com/ado/2009/11/edm/ssdl", edmxFile.DocumentElement);
                    nsm.AddNamespace("edm", "http://schemas.microsoft.com/ado/2009/11/edm", edmxFile.DocumentElement);
                    nsm.AddNamespace("annotation", "http://schemas.microsoft.com/ado/2009/02/edm/annotation", edmxFile.DocumentElement);
                    nsm.AddNamespace("map", "http://schemas.microsoft.com/ado/2009/11/mapping/cs", edmxFile.DocumentElement);
                    nsm.AddNamespace("codegen", "http://schemas.microsoft.com/ado/2006/04/codegeneration", edmxFile.DocumentElement);
                    nsm.AddNamespace("huagati", "http://www.huagati.com/edmxtools/annotations", edmxFile.DocumentElement);
                    edmxVersion = EDMXVersionEnum.EDMX2012;
                    break;
                case "http://schemas.microsoft.com/ado/2008/10/edmx":
                    nsm.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2008/10/edmx", edmxFile.DocumentElement);
                    nsm.AddNamespace("store", "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator", edmxFile.DocumentElement);
                    nsm.AddNamespace("ssdl", "http://schemas.microsoft.com/ado/2009/02/edm/ssdl", edmxFile.DocumentElement);
                    nsm.AddNamespace("edm", "http://schemas.microsoft.com/ado/2008/09/edm", edmxFile.DocumentElement);
                    nsm.AddNamespace("annotation", "http://schemas.microsoft.com/ado/2009/02/edm/annotation", edmxFile.DocumentElement);
                    nsm.AddNamespace("map", "http://schemas.microsoft.com/ado/2008/09/mapping/cs", edmxFile.DocumentElement);
                    nsm.AddNamespace("codegen", "http://schemas.microsoft.com/ado/2006/04/codegeneration", edmxFile.DocumentElement);
                    nsm.AddNamespace("huagati", "http://www.huagati.com/edmxtools/annotations", edmxFile.DocumentElement);
                    edmxVersion = EDMXVersionEnum.EDMX2010;
                    break;
                case "http://schemas.microsoft.com/ado/2007/06/edmx":
                    nsm.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
                    nsm.AddNamespace("store", "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator");
                    nsm.AddNamespace("ssdl", "http://schemas.microsoft.com/ado/2006/04/edm/ssdl");
                    nsm.AddNamespace("edm", "http://schemas.microsoft.com/ado/2006/04/edm");
                    nsm.AddNamespace("map", "urn:schemas-microsoft-com:windows:storage:mapping:CS");
                    edmxVersion = EDMXVersionEnum.EDMX2008;
                    break;
                default:
                    edmxVersion = EDMXVersionEnum.Unknown;
                    break;
            }
            return nsm;
        }

        internal static string StripTypeOf(string entityTypeName)
        {
            if (entityTypeName != null && entityTypeName.StartsWith("IsTypeOf(", StringComparison.InvariantCultureIgnoreCase) && entityTypeName.EndsWith(")"))
            {
                entityTypeName = entityTypeName.Substring(9, entityTypeName.Length - 10);
            }
            return entityTypeName;
        }

        internal static MultiplicityTypeEnum GetMultiplicity(XmlElement endElement)
        {
            MultiplicityTypeEnum multiplicity = MultiplicityTypeEnum.Unknown;
            if (endElement != null)
            {
                switch (endElement.GetAttribute("Multiplicity"))
                {
                    case "0..1":
                        multiplicity = MultiplicityTypeEnum.ZeroOrOne;
                        break;
                    case "1":
                        multiplicity = MultiplicityTypeEnum.One;
                        break;
                    default:
                        multiplicity = MultiplicityTypeEnum.Many;
                        break;
                }
            }
            return multiplicity;
        }

        internal static void SetMultiplicity(XmlElement endElement, MultiplicityTypeEnum multiplicity)
        {
            switch (multiplicity)
            {
                case MultiplicityTypeEnum.ZeroOrOne:
                    endElement.SetAttribute("Multiplicity", "0..1");
                    break;
                case MultiplicityTypeEnum.One:
                    endElement.SetAttribute("Multiplicity", "1");
                    break;
                default:
                    endElement.SetAttribute("Multiplicity", "*");
                    break;
            }
        }

    }

    internal static class NSMExtensions
    {
        internal static void AddNamespace(this XmlNamespaceManager nsm, string prefix, string namespaceURI, XmlElement addToNode)
        {
            nsm.AddNamespace(prefix, namespaceURI);
            if (string.IsNullOrEmpty(addToNode.GetAttribute("xmlns:" + prefix)))
            {
                addToNode.SetAttribute("xmlns:" + prefix, namespaceURI);
            }
        }
    }
}
