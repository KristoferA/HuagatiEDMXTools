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
    /// Represents a parameter to a function defined in the conceptual model
    /// </summary>
    public class ModelFunctionParameter : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private ModelFunction _parentFunction = null;
        private XmlElement _parameterElement = null;

        internal ModelFunctionParameter(EDMXFile parentFile, ModelFunction parentFunction, XmlElement parameterElement)
            : base(parentFile)
        {
            _parentFunction = parentFunction;
            _parameterElement = parameterElement;
        }

        internal ModelFunctionParameter(EDMXFile parentFile, ModelFunction storeFunction, string name, int ordinal, XmlElement parentTypeElement)
            : base(parentFile)
        {
            _parentFunction = storeFunction;

            _parameterElement = EDMXDocument.CreateElement("Parameter", NameSpaceURIcsdl);
            if (ordinal > 0)
            {
                XmlNodeList propertyNodes = parentTypeElement.SelectNodes("edm:Parameter", NSM);
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
                return _parentFunction.AliasName + "." + Name;
            }
        }

        /// <summary>
        /// Data type name
        /// </summary>
        public string TypeName
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
        /// Data type description
        /// </summary>
        public string TypeDescription
        {
            get
            {
                string typeDesc = null;
                if (MaxLengthApplies && MaxLength > 0)
                {
                    typeDesc = typeDesc + ", max: " + MaxLength;
                }
                return typeDesc;
            }
        }

        /// <summary>
        /// Parameter direction; Input, Output, or bidirectional (InOut)
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
        /// Corresponding CSDL type descriptor, abstraction used for comparing type with other members in the conceptual or storage layer
        /// </summary>
        public CSDLType CSDLType
        {
            get
            {
                try
                {
                    return new CSDLType(this);
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
        }

        /// <summary>
        /// True if the maxlength attribute is valid, False if not.
        /// </summary>
        public bool MaxLengthApplies
        {
            get
            {
                switch (TypeName.ToLower())
                {
                    case "binary":
                    case "string":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Indicates if maxlength is specified
        /// </summary>
        public bool MaxLengthSpecified
        {
            get
            {
                return (_parameterElement.HasAttribute("MaxLength"));
            }
        }

        /// <summary>
        /// Maxlength for string or binary members
        /// </summary>
        public int MaxLength
        {
            get
            {
                int maxLength = 0;
                if (MaxLengthApplies)
                {
                    string value = _parameterElement.GetAttribute("MaxLength");
                    if (value.Equals("max", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty(value))
                    {
                        maxLength = -1;
                    }
                    else
                    {
                        int.TryParse(value, out maxLength);
                    }
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
                else if (value == -1)
                {
                    _parameterElement.SetAttribute("MaxLength", "Max");
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
        /// Indicates if Precision/Scale applies (to numeric/decimal types)
        /// </summary>
        public bool PrecisionScaleApplies
        {
            get
            {
                switch (TypeName.ToLower())
                {
                    case "decimal":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Precision (for numeric/decimal types)
        /// </summary>
        public int Precision
        {
            get
            {
                int precision = 0;
                int.TryParse(_parameterElement.GetAttribute("Precision"), out precision);
                return precision;
            }
            set
            {
                if (value >= 0)
                {
                    string sValue = (value >= 0 ? value.ToString() : string.Empty);
                    _parameterElement.SetAttribute("Precision", sValue);
                }
                else
                {
                    if (_parameterElement.HasAttribute("Precision"))
                    {
                        _parameterElement.RemoveAttribute("Precision");
                    }
                }
            }
        }

        /// <summary>
        /// Scale (for numeric/decimal types)
        /// </summary>
        public int Scale
        {
            get
            {
                int scale = 0;
                int.TryParse(_parameterElement.GetAttribute("Scale"), out scale);
                return scale;
            }
            set
            {
                if (value >= 0)
                {
                    string sValue = (value >= 0 ? value.ToString() : string.Empty);
                    _parameterElement.SetAttribute("Scale", sValue);
                }
                else
                {
                    if (_parameterElement.HasAttribute("Scale"))
                    {
                        _parameterElement.RemoveAttribute("Scale");
                    }
                }
            }
        }

        /// <summary>
        /// Model function that this parameter belongs to
        /// </summary>
        public ModelFunction Function
        {
            get
            {
                return _parentFunction;
            }
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _parameterElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_parameterElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement summaryElement = DocumentationElement.GetOrCreateElement("edm", "Summary", NSM, true);
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
                XmlElement descriptionElement = (XmlElement)_parameterElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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
                XmlElement descriptionElement = DocumentationElement.GetOrCreateElement("edm", "LongDescription", NSM);
                descriptionElement.InnerText = value;
            }
        }
        #endregion
    }
}
