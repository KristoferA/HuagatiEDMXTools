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
    /// <summary>
    /// Represents a parameter definition for a storage model function.
    /// </summary>
    public class StoreFunctionParameter : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private StoreFunction _parentFunction = null;
        private XmlElement _parameterElement = null;

        internal StoreFunctionParameter(EDMXFile parentFile, StoreFunction parentFunction, XmlElement parameterElement)
            : base(parentFile)
        {
            _parentFunction = parentFunction;
            _parameterElement = parameterElement;
        }

        internal StoreFunctionParameter(EDMXFile parentFile, StoreFunction storeFunction, string name, int ordinal, XmlElement parentTypeElement)
            : base(parentFile)
        {
            _parentFunction = storeFunction;

            _parameterElement = EDMXDocument.CreateElement("Parameter", NameSpaceURIssdl);
            if (ordinal > 0)
            {
                XmlNodeList propertyNodes = parentTypeElement.SelectNodes("ssdl:Parameter", NSM);
                if (propertyNodes.Count >= ordinal)
                {
                    parentTypeElement.InsertAfter(_parameterElement, propertyNodes[ordinal - 1]);
                }
                else
                {
                    parentTypeElement.AppendChild(_parameterElement);
                }
            }
            else
            {
                parentTypeElement.AppendChild(_parameterElement);
            }

            this.Name = name;
        }

        /// <summary>
        /// Event fired when the object has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        /// Removes the object from the model.
        /// </summary>
        public void Remove()
        {
            try
            {
                if (_parameterElement.ParentNode != null)
                {
                    _parameterElement.ParentNode.RemoveChild(_parameterElement);

                    if (Removed != null)
                    {
                        Removed(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionTools.AddExceptionData(ex, this);
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Event fired when the object changes name
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        /// <summary>
        /// Name of the model object
        /// </summary>
        public string Name
        {
            get
            {
                return _parameterElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _parameterElement.GetAttribute("Name");

                //set the property name
                _parameterElement.SetAttribute("Name", value);

                //raise the name change event
                if (NameChanged != null)
                {
                    NameChanged(this, new NameChangeArgs { OldName = oldName, NewName = value });
                }
            }
        }

        /// <summary>
        /// Fully qualified name, including parent object names.
        /// </summary>
        public string FullName
        {
            get
            {
                return _parentFunction.FullName + "." + Name;
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get
            {
                return _parentFunction.AliasName +"." + Name;
            }
        }

        /// <summary>
        /// Data type for the parameter
        /// </summary>
        public string DataType
        {
            get
            {
                string typeName = _parameterElement.GetAttribute("Type");
                if (typeName.EndsWith("(max)", StringComparison.InvariantCultureIgnoreCase))
                {
                    typeName = typeName.Substring(0, typeName.Length - 5);
                }
                return typeName;
            }
            set
            {
                _parameterElement.SetAttribute("Type", value);
            }
        }

        /// <summary>
        /// Data type description; type, nullability, length/precision/scale
        /// </summary>
        public string DataTypeDescription
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "uniqueidentifier":
                    case "smalldatetime":
                    case "image":
                    case "datetime":
                    case "datetime2":
                    case "date":
                    case "time":
                    case "bit":
                    case "binary":
                    case "text":
                    case "tinyint":
                    case "smallint":
                    case "int":
                    case "ntext":
                    case "bigint":
                    case "xml":
                    case "real":
                    case "money":
                    case "float":
                    case "smallmoney":
                        return this.DataType;
                    case "sql_variant":
                        return "Variant";
                    case "timestamp":
                        return "rowversion";
                    case "decimal":
                    case "numeric":
                        return "decimal" //this.DataType
                            + ((this.Precision > 0 && this.Scale > 0) ? "(" + this.Precision + "," + this.Scale + ")" : "");
                    case "varbinary":
                    case "varchar":
                    case "char":
                        return this.DataType
                            + ((this.MaxLength > 0) ? "(" + this.MaxLength.ToString() + ")" : ((this.MaxLength == -1) ? "(max)" : ""));
                    case "nvarchar":
                    case "nchar":
                    case "sysname":
                        return this.DataType
                            + ((this.MaxLength > 0) ? "(" + (this.MaxLength).ToString() + ")" : ((this.MaxLength == -1) ? "(max)" : ""));
                    case "hierarchyid":
                        return "nvarchar";
                    case "geometry":
                        return "geometry";
                    case "geography":
                        return "geography";
                    default:
                        return this.DataType;
                }
            }
        }

        /// <summary>
        /// Parameter direction; input, output, or bidirectional (inout)
        /// </summary>
        public ParameterModeEnum Mode
        {
            get
            {
                string paramMode = _parameterElement.GetAttribute("Mode");
                switch (paramMode.ToLower())
                {
                    case "out":
                        return ParameterModeEnum.Out;
                    case "inout":
                        return ParameterModeEnum.InOut;
                    default:
                        return ParameterModeEnum.In;
                }
            }
            set
            {
                _parameterElement.SetAttribute("Mode", value.ToString());
            }
        }

        /// <summary>
        /// Corresponding CSDL type, used for comparing type with other storage or conceptual model members
        /// </summary>
        public CSDLType CSDLType
        {
            get
            {
                return new CSDLType(this);
            }
        }

        /// <summary>
        /// True if the maxlength attribute can be used for the type, false if not
        /// </summary>
        public bool MaxLengthApplies
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "varbinary":
                    case "varchar":
                    case "char":
                    case "nvarchar":
                    case "nchar":
                    case "sysname":
                    case "binary":
                    case "xml":
                    case "timestamp":
                    case "rowversion":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// True if the maxlength attribute is specified, false if not
        /// </summary>
        public bool MaxLengthSpecified
        {
            get
            {
                return (_parameterElement.HasAttribute("MaxLength"));
            }
        }

        /// <summary>
        /// Max length for strings and binary members.
        /// </summary>
        public int MaxLength
        {
            get
            {
                int maxLength = 0;
                if (MaxLengthApplies)
                {
                    string value = _parameterElement.GetAttribute("MaxLength");
                    if (DataType.EndsWith("(max)", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty(value))
                    {
                        string type = DataType.ToLower().Replace("(max)", "");
                        maxLength = -1;
                    }
                    else
                    {
                        int.TryParse(value, out maxLength);
                    }
                }
                if (DataType.Equals("timestamp", StringComparison.InvariantCultureIgnoreCase) || DataType.Equals("rowversion", StringComparison.InvariantCultureIgnoreCase))
                {
                    maxLength = 8;
                }
                return maxLength;
            }
            set
            {
                if (value > 0)
                {
                    string sValue = (value > 0 ? value.ToString() : string.Empty);
                    _parameterElement.SetAttribute("MaxLength", value.ToString());
                }
                else
                {
                    if (_parameterElement.HasAttribute("MaxLength"))
                    {
                        _parameterElement.RemoveAttribute("MaxLength");
                    }
                }
            }
        }

        /// <summary>
        /// True if precision and scale is specified, false if not.
        /// </summary>
        public bool PrecisionScaleSpecified
        {
            get
            {
                return (_parameterElement.HasAttribute("Precision") || _parameterElement.HasAttribute("Scale"));
            }
        }

        /// <summary>
        /// Precision (for numeric/decimal/money types).
        /// </summary>
        public int Precision
        {
            get
            {
                int precision = 0;
                int.TryParse(_parameterElement.GetAttribute("Precision"), out precision);
                if (precision == 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            precision = 38;
                            break;
                        case "money":
                            precision = 19;
                            break;
                        case "smallmoney":
                            precision = 10;
                            break;
                        default:
                            //nothing
                            break;
                    }
                }
                return precision;
            }
            set
            {
                _parameterElement.SetAttribute("Precision", value.ToString());
            }
        }

        /// <summary>
        /// Scale (for numeric/decimal/money types).
        /// </summary>
        public int Scale
        {
            get
            {
                int scale = 0;
                int.TryParse(_parameterElement.GetAttribute("Scale"), out scale);
                if (scale == 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            scale = 6;
                            break;
                        case "money":
                            scale = 4;
                            break;
                        case "smallmoney":
                            scale = 4;
                            break;
                        default:
                            //nothing
                            break;
                    }
                }
                return scale;
            }
            set
            {
                _parameterElement.SetAttribute("Scale", value.ToString());
            }
        }

        /// <summary>
        /// Store function that this parameter belongs to.
        /// </summary>
        public StoreFunction Function
        {
            get
            {
                return _parentFunction;
            }
        }

        /// <summary>
        /// Fixed length member (for binary/string types).
        /// </summary>
        public bool FixedLength
        {
            get
            {
                bool fixedLength = _parameterElement.GetAttribute("FixedLength").Equals("true", StringComparison.InvariantCultureIgnoreCase);
                switch (DataType.ToLower())
                {
                    case "binary":
                    case "binary(max)":
                    case "char":
                    case "char(max)":
                    case "nchar":
                    case "nchar(max)":
                    case "timestamp":
                        fixedLength = true;
                        break;
                    default:
                        //nothing
                        break;
                }
                return fixedLength;
            }
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _parameterElement.GetOrCreateElement("ssdl", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_parameterElement.SelectSingleNode("ssdl:Documentation/ssdl:Summary", NSM);
                if (summaryElement != null)
                {
                    return summaryElement.InnerText;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                XmlElement summaryElement = DocumentationElement.GetOrCreateElement("ssdl", "Summary", NSM, true);
                summaryElement.InnerText = value;
            }
        }

        /// <summary>
        /// Long description, part of the documentation attributes for model members
        /// </summary>
        public string LongDescription
        {
            get
            {
                XmlElement descriptionElement = (XmlElement)_parameterElement.SelectSingleNode("ssdl:Documentation/ssdl:LongDescription", NSM);
                if (descriptionElement != null)
                {
                    return descriptionElement.InnerText;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                XmlElement descriptionElement = DocumentationElement.GetOrCreateElement("ssdl", "LongDescription", NSM);
                descriptionElement.InnerText = value;
            }
        }
        #endregion
    }
}
