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
    /// Represents a function import in the conceptual model.
    /// </summary>
    public class ModelFunction : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private ConceptualModel _conceptualModel = null;
        private XmlElement _functionImportElement = null;

        internal ModelFunction(EDMXFile parentFile, ConceptualModel conceptualModel, System.Xml.XmlElement functionImportElement)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;
            _functionImportElement = functionImportElement;
        }

        internal ModelFunction(EDMXFile parentFile, ConceptualModel conceptualModel, string name)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;

            //create the entity type element
            XmlElement schemaContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer", NSM);
            _functionImportElement = EDMXDocument.CreateElement("FunctionImport", NameSpaceURIcsdl);
            schemaContainer.AppendChild(_functionImportElement);

            Name = name;
        }

        #region IEDMXNamedMember Members

        /// <summary>
        /// Name of the model object
        /// </summary>
        public string Name
        {
            get
            {
                return _functionImportElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _functionImportElement.GetAttribute("Name");
                _functionImportElement.SetAttribute("Name", value);

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
                if (_functionImportElement.ParentNode != null)
                {
                    return ((XmlElement)_functionImportElement.ParentNode).GetAttribute("Namespace") + "." + Name;
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
                if (_functionImportElement.ParentNode != null)
                {
                    return ((XmlElement)_functionImportElement.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Event fired when the object changes name
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        #endregion

        #region IEDMXRemovableMember Members

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

                if (_functionImportElement.ParentNode != null)
                {
                    _functionImportElement.ParentNode.RemoveChild(_functionImportElement);

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
        /// Event fired when the object has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        #endregion

        /// <summary>
        /// Return type name for the function
        /// </summary>
        public string ReturnType
        {
            get
            {
                return StripCollection(_functionImportElement.GetAttribute("ReturnType"));
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (_functionImportElement.HasAttribute("ReturnType"))
                    {
                        _functionImportElement.RemoveAttribute("ReturnType");
                    }
                }
                else
                {
                    _functionImportElement.SetAttribute("ReturnType", ReturnsCollection ? "Collection(" + value + ")" : value);
                }
            }
        }

        private string StripCollection(string typeName)
        {
            if (typeName.StartsWith("Collection(", StringComparison.InvariantCultureIgnoreCase) && typeName.EndsWith(")", StringComparison.InvariantCultureIgnoreCase))
            {
                typeName = typeName.Substring(11, typeName.Length - 12);
            }
            return typeName;
        }

        /// <summary>
        /// Complex type reference if the return type is defined as a complex type in the model.
        /// </summary>
        public ModelComplexType ReturnComplexType
        {
            get
            {
                try
                {
                    string returnType = ReturnType;
                    return ParentFile.ConceptualModel.ComplexTypes.FirstOrDefault(ct => ct.FullName.Equals(returnType, StringComparison.InvariantCulture) || ct.AliasName.Equals(returnType, StringComparison.InvariantCulture));
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
            set
            {
                ReturnType = value.Name;
            }
        }

        /// <summary>
        /// Entity type reference if the return type is defined as an entity type in the model.
        /// </summary>
        public ModelEntityType ReturnEntityType
        {
            get
            {
                try
                {
                    string returnType = ReturnType;
                    return ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(ct => ct.FullName.Equals(returnType, StringComparison.InvariantCulture) || ct.AliasName.Equals(returnType, StringComparison.InvariantCulture));
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
            set
            {
                ReturnType = value.Name;
            }
        }
        
        /// <summary>
        /// True if the returned result is a collection of ReturnType, false if not.
        /// </summary>
        public bool ReturnsCollection
        {
            get
            {
                string returnType = _functionImportElement.GetAttribute("ReturnType");
                return returnType.StartsWith("Collection(", StringComparison.InvariantCultureIgnoreCase) && returnType.EndsWith(")", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                bool returnsCollection = ReturnsCollection;
                if (value == false && returnsCollection == true)
                {
                    _functionImportElement.SetAttribute("ReturnType", StripCollection(_functionImportElement.GetAttribute("ReturnType")));
                }
                else if (value == true && returnsCollection == false)
                {
                    _functionImportElement.SetAttribute("ReturnType", "Collection(" + _functionImportElement.GetAttribute("ReturnType") + ")");
                }
            }
        }

        /// <summary>
        /// Entity set that this function is linked to, if linked to an entity set.
        /// </summary>
        public ModelEntitySet EntitySet
        {
            get
            {
                try
                {
                    string entitySetName = _functionImportElement.GetAttribute("EntitySet");
                    return ParentFile.ConceptualModel.EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName, StringComparison.InvariantCulture));
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
            set
            {
                if (value != null)
                {
                    _functionImportElement.SetAttribute("EntitySet", value.Name);
                }
                else
                {
                    if (_functionImportElement.HasAttribute("EntitySet"))
                    {
                        _functionImportElement.RemoveAttribute("EntitySet");
                    }
                }
            }
        }

        private FunctionImportMapping _mapping = null;

        /// <summary>
        /// Mapping to storage model functions.
        /// </summary>
        public FunctionImportMapping Mapping
        {
            get
            {
                try
                {
                    if (_mapping == null)
                    {
                        _mapping = ParentFile.CSMapping.FunctionImportMappings.FirstOrDefault(fm => fm.ModelFunction == this);
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

        private XmlNodeList _parameterElements = null;
        private XmlNodeList ParameterElements
        {
            get
            {
                try
                {
                    if (_parameterElements == null)
                    {
                        _parameterElements = _functionImportElement.SelectNodes("edm:Parameter", NSM);
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
        private Dictionary<string, ModelFunctionParameter> _parameters = new Dictionary<string, ModelFunctionParameter>();

        /// <summary>
        /// Enumeration of function parameters
        /// </summary>
        public IEnumerable<ModelFunctionParameter> Parameters
        {
            get
            {
                try
                {
                    if (_parametersEnumerated == false)
                    {
                        foreach (XmlElement parameterElement in ParameterElements)
                        {
                            ModelFunctionParameter param = null;
                            string paramName = parameterElement.GetAttribute("Name");
                            if (!_parameters.ContainsKey(paramName))
                            {
                                param = new ModelFunctionParameter(ParentFile, this, parameterElement);
                                param.NameChanged += new EventHandler<NameChangeArgs>(param_NameChanged);
                                param.Removed += new EventHandler(param_Removed);
                                _parameters.Add(paramName, param);
                            }
                            else
                            {
                                param = _parameters[paramName];
                            }
                            yield return param;
                        }
                        _parametersEnumerated = true;
                        _parameterElements = null;
                    }
                    else
                    {
                        foreach (ModelFunctionParameter param in _parameters.Values)
                        {
                            yield return param;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void param_Removed(object sender, EventArgs e)
        {
            _parameters.Remove(((ModelFunctionParameter)sender).Name);
        }

        void param_NameChanged(object sender, NameChangeArgs e)
        {
            if (_parameters.ContainsKey(e.OldName))
            {
                _parameters.Remove(e.OldName);
                _parameters.Add(e.NewName, (ModelFunctionParameter)sender);
            }
        }

        /// <summary>
        /// Adds a new parameter to the parameter collection.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <returns>A ModelFunctionParameter object</returns>
        public ModelFunctionParameter AddParameter(string name)
        {
            return AddParameter(name, string.Empty, -1);
        }

        /// <summary>
        /// Adds a new parameter to the parameter collection.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="typeName">Parameter type</param>
        /// <param name="ordinal">Ordinal position within the set of parameters</param>
        /// <returns>A ModelFunctionParameter object</returns>
        public ModelFunctionParameter AddParameter(string name, string typeName, int ordinal)
        {
            try
            {
                if (!Parameters.Where(mp => mp.Name == name).Any())
                {
                    ModelFunctionParameter mp = new ModelFunctionParameter(base.ParentFile, this, name, ordinal, _functionImportElement);
                    mp.TypeName = typeName;
                    _parameters.Add(name, mp);
                    mp.NameChanged += new EventHandler<NameChangeArgs>(param_NameChanged);
                    mp.Removed += new EventHandler(param_Removed);
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
        /// Removes all parameters
        /// </summary>
        public void ClearParameters()
        {
            try
            {
                foreach (XmlElement parameter in _functionImportElement.SelectNodes("edm:Parameter", NSM))
                {
                    _functionImportElement.RemoveChild(parameter);
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

        #region "Documentation"
        internal XmlElement DocumentationElement
        {
            get
            {
                return _functionImportElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_functionImportElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement summaryElement = DocumentationElement.GetOrCreateElement("edm", "Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_functionImportElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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
