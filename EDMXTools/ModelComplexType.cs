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
    /// Represents a complex type in the conceptual model
    /// </summary>
    public class ModelComplexType : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private ConceptualModel _conceptualModel = null;
        private XmlElement _complexTypeElement = null;

        internal ModelComplexType(EDMXFile parentFile, ConceptualModel conceptualModel, System.Xml.XmlElement entityTypeElement)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;
            _complexTypeElement = entityTypeElement;
        }

        internal ModelComplexType(EDMXFile parentFile, ConceptualModel conceptualModel, string name)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;

            //create the entity type element
            XmlElement schemaContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema", NSM);
            _complexTypeElement = EDMXDocument.CreateElement("ComplexType", NameSpaceURIcsdl);
            schemaContainer.AppendChild(_complexTypeElement);

            Name = name;
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
                if (_complexTypeElement.ParentNode != null)
                {
                    _complexTypeElement.ParentNode.RemoveChild(_complexTypeElement);

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
                return _complexTypeElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _complexTypeElement.GetAttribute("Name");
                _complexTypeElement.SetAttribute("Name", value);

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
                return ((XmlElement)_complexTypeElement.ParentNode).GetAttribute("Namespace") + "." + Name;
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get
            {
                return ((XmlElement)_complexTypeElement.ParentNode).GetAttribute("Alias") + "." + Name;
            }
        }

        private XmlNodeList _memberPropertyElements = null;
        private XmlNodeList MemberPropertyElements
        {
            get
            {
                try
                {
                    if (_memberPropertyElements == null)
                    {
                        _memberPropertyElements = _complexTypeElement.SelectNodes("edm:Property", NSM);
                    }
                    return _memberPropertyElements;
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

        private bool _memberPropertiesEnumerated = false;
        private Dictionary<string, ModelMemberProperty> _memberProperties = new Dictionary<string, ModelMemberProperty>();

        /// <summary>
        /// Enumeration of all members of this complex type
        /// </summary>
        public IEnumerable<ModelMemberProperty> MemberProperties
        {
            get
            {
                if (_memberPropertiesEnumerated == false)
                {
                    foreach (XmlElement memberPropertyElement in MemberPropertyElements)
                    {
                        ModelMemberProperty prop = null;
                        string propName = memberPropertyElement.GetAttribute("Name");
                        if (!_memberProperties.ContainsKey(propName))
                        {
                            prop = new ModelMemberProperty(ParentFile, this, memberPropertyElement);
                            prop.NameChanged += new EventHandler<NameChangeArgs>(prop_NameChanged);
                            prop.Removed += new EventHandler(prop_Removed);
                            _memberProperties.Add(propName, prop);
                        }
                        else
                        {
                            prop = _memberProperties[propName];
                        }
                        yield return prop;
                    }
                    _memberPropertiesEnumerated = true;
                    _memberPropertyElements = null;
                }
                else
                {
                    foreach (ModelMemberProperty prop in _memberProperties.Values)
                    {
                        yield return prop;
                    }
                }
            }
        }

        void prop_Removed(object sender, EventArgs e)
        {
            _memberProperties.Remove(((ModelMemberProperty)sender).Name);
        }

        void prop_NameChanged(object sender, NameChangeArgs e)
        {
            if (_memberProperties.ContainsKey(e.OldName))
            {
                _memberProperties.Remove(e.OldName);
                _memberProperties.Add(e.NewName, (ModelMemberProperty)sender);
            }
        }

        /// <summary>
        /// Adds a new scalar member to the type
        /// </summary>
        /// <param name="name">Member name</param>
        /// <returns>A ModelMemberProperty object corresponding to the new member.</returns>
        public ModelMemberProperty AddMember(string name)
        {
            return AddMember(name, typeof(string), true);
        }

        /// <summary>
        /// Adds a new scalar member to the type
        /// </summary>
        /// <param name="name">Member name</param>
        /// <param name="type">Member type. Must be a EDM compatible CLR type.</param>
        /// <param name="nullable">Nullable or non-nullable?</param>
        /// <returns>A ModelMemberProperty object corresponding to the new member.</returns>
        public ModelMemberProperty AddMember(string name, Type type, bool nullable)
        {
            try
            {
                if (!MemberProperties.Where(mp => mp.Name == name).Any()
                    && name != this.Name)
                {
                    ModelMemberProperty mp = new ModelMemberProperty(base.ParentFile, this, name, _complexTypeElement);
                    mp.Type = type;
                    mp.Nullable = nullable;
                    _memberProperties.Add(name, mp);
                    mp.NameChanged += new EventHandler<NameChangeArgs>(prop_NameChanged);
                    mp.Removed += new EventHandler(prop_Removed);
                    return mp;
                }
                else
                {
                    throw new ArgumentException("A property with the name " + name + " already exist in the type " + this.Name);
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

        #region "Documentation"
        internal XmlElement DocumentationElement
        {
            get
            {
                return _complexTypeElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_complexTypeElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_complexTypeElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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
