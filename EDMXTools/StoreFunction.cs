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
    /// Represents a function/procedure definition in the storage model.
    /// </summary>
    public class StoreFunction : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        StorageModel _storageModel = null;
        XmlElement _functionElement = null;

        internal StoreFunction(EDMXFile ParentFile, StorageModel storageModel, XmlElement functionElement)
            : base(ParentFile)
        {
            _storageModel = storageModel;
            _functionElement = functionElement;
        }

        internal StoreFunction(EDMXFile ParentFile, StorageModel storageModel, string name)
            : base(ParentFile)
        {
            _storageModel = storageModel;

            //create and add the function element
            XmlElement schemaContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels/ssdl:Schema", NSM);
            _functionElement = EDMXDocument.CreateElement("Function", NameSpaceURIssdl);
            schemaContainer.AppendChild(_functionElement);

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
                if (Mapping != null)
                {
                    Mapping.Remove();
                }

                if (_functionElement.ParentNode != null)
                {
                    _functionElement.ParentNode.RemoveChild(_functionElement);

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
                return _functionElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _functionElement.GetAttribute("Name");
                _functionElement.SetAttribute("Name", value);

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
                if (_functionElement.ParentNode != null)
                {
                    return ((XmlElement)_functionElement.ParentNode).GetAttribute("Namespace") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get
            {
                if (_functionElement.ParentNode != null)
                {
                    return ((XmlElement)_functionElement.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Function/procedure name in the database
        /// </summary>
        public string FunctionName
        {
            get
            {
                string storeFunctionName = _functionElement.GetAttribute("StoreFunctionName");
                if (string.IsNullOrEmpty(storeFunctionName))
                {
                    if (_functionElement.HasAttribute("Name", NameSpaceURIstore))
                    {
                        storeFunctionName = _functionElement.GetAttribute("Name", NameSpaceURIstore);
                    }
                    else
                    {
                        storeFunctionName = Name;
                    }
                }
                return storeFunctionName;
            }
            set
            {
                bool hasNameAttrib = false;
                if (_functionElement.HasAttribute("Name", NameSpaceURIstore))
                {
                    _functionElement.SetAttribute("Name", NameSpaceURIstore, value);
                    hasNameAttrib = true;
                }
                if (_functionElement.HasAttribute("StoreFunctionName"))
                {
                    _functionElement.SetAttribute("StoreFunctionName", value);
                    hasNameAttrib = true;
                }
                if (!hasNameAttrib)
                {
                    _functionElement.SetAttribute("StoreFunctionName", value);
                }
            }
        }
        
        /// <summary>
        /// Database schema that this function/procedure belongs to.
        /// </summary>
        public string Schema
        {
            get
            {
                string schemaName = _functionElement.GetAttribute("Schema");
                if (string.IsNullOrEmpty(schemaName))
                {
                    schemaName = _functionElement.GetAttribute("Schema", NameSpaceURIstore);
                }
                return schemaName;
            }
            set
            {
                _functionElement.SetAttribute("Schema", value);
                if (_functionElement.HasAttribute("Schema", NameSpaceURIstore))
                {
                    _functionElement.SetAttribute("Schema", NameSpaceURIstore, value);
                }
            }
        }

        /// <summary>
        /// Return type name
        /// </summary>
        public string ReturnType
        {
            get
            {
                return _functionElement.GetAttribute("ReturnType");
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (_functionElement.HasAttribute("ReturnType"))
                    {
                        _functionElement.RemoveAttribute("ReturnType");
                    }
                }
                else
                {
                    _functionElement.SetAttribute("ReturnType", value);
                }
            }
        }

        /// <summary>
        /// True if the function/procedure returns an aggregate value, false if not.
        /// </summary>
        public bool Aggregate
        {
            get
            {
                return _functionElement.GetAttribute("Aggregate").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                _functionElement.SetAttribute("Aggregate", value.ToLString());
            }
        }

        /// <summary>
        /// True if the function/procedure is a built-in function, false if not.
        /// </summary>
        public bool BuiltIn
        {
            get
            {
                return _functionElement.GetAttribute("BuiltIn").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                if (value == true && IsComposable)
                {
                    _functionElement.SetAttribute("BuiltIn", value.ToLString());
                }
                else
                {
                    if (_functionElement.HasAttribute("BuiltIn"))
                    {
                        _functionElement.RemoveAttribute("BuiltIn");
                    }
                }
            }
        }

        /// <summary>
        /// True if the function/procedure accepts no parameters.
        /// </summary>
        public bool NiladicFunction
        {
            get
            {
                return _functionElement.GetAttribute("NiladicFunction").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                if (value == true && IsComposable)
                {
                    _functionElement.SetAttribute("NiladicFunction", value.ToLString());
                }
                else
                {
                    if (_functionElement.HasAttribute("NiladicFunction"))
                    {
                        _functionElement.RemoveAttribute("NiladicFunction");
                    }
                }
            }
        }

        /// <summary>
        /// True if the function is composable / can be part of a query or wrapped within a function call.
        /// </summary>
        public bool IsComposable
        {
            get
            {
                return _functionElement.GetAttribute("IsComposable").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                _functionElement.SetAttribute("IsComposable", value.ToLString());
            }
        }

        /// <summary>
        /// Defines the semantics used to resolve function overloads.
        /// </summary>
        public ParameterTypeSemanticsEnum ParameterTypeSemantics
        {
            get
            {
                switch (_functionElement.GetAttribute("ParameterTypeSemantics").ToLower())
                {
                    case "allowimplicitpromotion":
                        return ParameterTypeSemanticsEnum.AllowImplicitPromotion;
                    case "exactmatchonly":
                        return ParameterTypeSemanticsEnum.ExactMatchOnly;
                    default:
                        return ParameterTypeSemanticsEnum.AllowImplicitConversion;
                }
            }
            set
            {
                _functionElement.SetAttribute("ParameterTypeSemantics", value.ToString());
            }
        }

        /// <summary>
        /// Command text if defined within the model rather than mapped to a database function or procedure.
        /// </summary>
        public string CommandText
        {
            get
            {
                XmlElement definingQueryElement = (XmlElement)_functionElement.SelectSingleNode("ssdl:CommandText", NSM);
                if (definingQueryElement != null)
                {
                    return definingQueryElement.InnerText;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                XmlElement documentationElement = (XmlElement)_functionElement.SelectSingleNode("ssdl:Documentation", NSM);
                XmlElement definingQueryElement = null;
                if (documentationElement == null)
                {
                    definingQueryElement = XmlHelpers.GetOrCreateElement(_functionElement, "ssdl", "CommandText", NSM, true);
                }
                else
                {
                    definingQueryElement = XmlHelpers.GetOrCreateElement(_functionElement, "ssdl", "CommandText", NSM, false, documentationElement);
                }
                if (definingQueryElement != null)
                {
                    definingQueryElement.InnerText = value;
                }
            }
        }

        private XmlNodeList _parameterElements = null;
        private XmlNodeList ParameterElements
        {
            get
            {
                try
                {
                    if (_parameterElements == null)
                    {
                        _parameterElements = _functionElement.SelectNodes("ssdl:Parameter", NSM);
                    }
                    return _parameterElements;
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

        private bool _parametersEnumerated = false;
        private Dictionary<string, StoreFunctionParameter> _parameters = new Dictionary<string, StoreFunctionParameter>();

        /// <summary>
        /// Enumeration of parameters to the function/procedure.
        /// </summary>
        public IEnumerable<StoreFunctionParameter> Parameters
        {
            get
            {
                try
                {
                    if (_parametersEnumerated == false)
                    {
                        foreach (XmlElement parameterElement in ParameterElements)
                        {
                            StoreFunctionParameter prop = null;
                            string propName = parameterElement.GetAttribute("Name");
                            if (!_parameters.ContainsKey(propName))
                            {
                                prop = new StoreFunctionParameter(ParentFile, this, parameterElement);
                                prop.NameChanged += new EventHandler<NameChangeArgs>(prop_NameChanged);
                                prop.Removed += new EventHandler(prop_Removed);
                                _parameters.Add(propName, prop);
                            }
                            else
                            {
                                prop = _parameters[propName];
                            }
                            yield return prop;
                        }
                        _parametersEnumerated = true;
                        _parameterElements = null;
                    }
                    else
                    {
                        foreach (StoreFunctionParameter prop in _parameters.Values)
                        {
                            yield return prop;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void prop_Removed(object sender, EventArgs e)
        {
            _parameters.Remove(((StoreFunctionParameter)sender).Name);
        }

        void prop_NameChanged(object sender, NameChangeArgs e)
        {
            if (_parameters.ContainsKey(e.OldName))
            {
                _parameters.Remove(e.OldName);
                _parameters.Add(e.NewName, (StoreFunctionParameter)sender);
            }
        }

        /// <summary>
        /// Adds a parameter to the function definition.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <returns>A StoreFunctionParameter object.</returns>
        public StoreFunctionParameter AddParameter(string name)
        {
            return AddParameter(name, string.Empty, -1);
        }

        /// <summary>
        /// Adds a parameter to the function definition.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="dataType">Data type</param>
        /// <param name="ordinal">Ordinal position in relation to other parameters. (Zero-based)</param>
        /// <returns>A StoreFunctionParameter object.</returns>
        public StoreFunctionParameter AddParameter(string name, string dataType, int ordinal)
        {
            try
            {
                if (!Parameters.Where(mp => mp.Name == name).Any())
                {
                    StoreFunctionParameter mp = new StoreFunctionParameter(base.ParentFile, this, name, ordinal, _functionElement);
                    mp.DataType = dataType;
                    _parameters.Add(name, mp);
                    mp.NameChanged += new EventHandler<NameChangeArgs>(prop_NameChanged);
                    mp.Removed += new EventHandler(prop_Removed);
                    return mp;
                }
                else
                {
                    throw new ArgumentException("A parameter with the name " + name + " already exist in the function " + this.Name);
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
        /// Removes all parameters.
        /// </summary>
        public void ClearParameters()
        {
            try
            {
                foreach (XmlElement parameter in _functionElement.SelectNodes("ssdl:Parameter", NSM))
                {
                    _functionElement.RemoveChild(parameter);
                }

                _parameterElements = null;
                _parameters.Clear();
                _parametersEnumerated = false;
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

        private FunctionImportMapping _mapping = null;

        /// <summary>
        /// Function import mapping that maps this storage model function to a conceptual model function import.
        /// </summary>
        public FunctionImportMapping Mapping
        {
            get
            {
                try
                {
                    if (_mapping == null)
                    {
                        _mapping = ParentFile.CSMapping.FunctionImportMappings.FirstOrDefault(fm => fm.StoreFunction == this);
                        if (_mapping != null)
                        {
                            _mapping.Removed += new EventHandler(Mapping_Removed);
                        }
                    }
                    return _mapping;
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

        void Mapping_Removed(object sender, EventArgs e)
        {
            _mapping = null;
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _functionElement.GetOrCreateElement("ssdl", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_functionElement.SelectSingleNode("ssdl:Documentation/ssdl:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_functionElement.SelectSingleNode("ssdl:Documentation/ssdl:LongDescription", NSM);
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
