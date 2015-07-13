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
    /// Represents an entity type in the storage model.
    /// </summary>
    public class StoreEntityType : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation 
    {
        private StorageModel _storageModel;
        private XmlElement _entityTypeElement;

        internal StoreEntityType(EDMXFile parentFile, StorageModel storageModel, XmlElement entityTypeElement) : base(parentFile)
        {
            _storageModel = storageModel;
            _entityTypeElement = entityTypeElement;
        }

        internal StoreEntityType(EDMXFile parentFile, StorageModel storageModel, string name) : base(parentFile)
        {
            _storageModel = storageModel;

            //create the entity type element
            XmlElement schemaContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels/ssdl:Schema", NSM);
            _entityTypeElement = EDMXDocument.CreateElement("EntityType", NameSpaceURIssdl);
            schemaContainer.AppendChild(_entityTypeElement);

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
                if (_entityTypeElement.ParentNode != null)
                {
                    _entityTypeElement.ParentNode.RemoveChild(_entityTypeElement);

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
                return _entityTypeElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _entityTypeElement.GetAttribute("Name");
                _entityTypeElement.SetAttribute("Name", value);

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
                if (_entityTypeElement.ParentNode != null)
                {
                    return ((XmlElement)_entityTypeElement.ParentNode).GetAttribute("Namespace") + "." + Name;
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
                if (_entityTypeElement.ParentNode != null)
                {
                    return ((XmlElement)_entityTypeElement.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
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
                        _memberPropertyElements = _entityTypeElement.SelectNodes("ssdl:Property", NSM);
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
        private Dictionary<string, StoreMemberProperty> _memberProperties = new Dictionary<string, StoreMemberProperty>();

        /// <summary>
        /// Enumeration of all scalar members in the entity type.
        /// </summary>
        public IEnumerable<StoreMemberProperty> MemberProperties
        {
            get
            {
                try
                {
                    if (_memberPropertiesEnumerated == false)
                    {
                        foreach (XmlElement memberPropertyElement in MemberPropertyElements)
                        {
                            StoreMemberProperty prop = null;
                            string propName = memberPropertyElement.GetAttribute("Name");
                            if (!_memberProperties.ContainsKey(propName))
                            {
                                prop = new StoreMemberProperty(ParentFile, this, memberPropertyElement);
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
                        foreach (StoreMemberProperty prop in _memberProperties.Values)
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
            _memberProperties.Remove(((StoreMemberProperty)sender).Name);
        }

        void prop_NameChanged(object sender, NameChangeArgs e)
        {
            if (_memberProperties.ContainsKey(e.OldName))
            {
                _memberProperties.Remove(e.OldName);
                _memberProperties.Add(e.NewName, (StoreMemberProperty)sender);
            }
        }

        /// <summary>
        /// Adds a scalar member to the entity type.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty AddMember(string name)
        {
            return AddMember(name, string.Empty);
        }

        /// <summary>
        /// Retrieves an existing scalar member to the entity type, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty GetOrCreateMember(string name)
        {
            return GetOrCreateMember(name, string.Empty);
        }

        /// <summary>
        /// Adds a scalar member to the entity type.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <param name="dataType">Data type name</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty AddMember(string name, string dataType)
        {
            return AddMember(name, dataType, -1);
        }

        /// <summary>
        /// Retrieves an existing scalar member to the entity type, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <param name="dataType">Data type name</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty GetOrCreateMember(string name, string dataType)
        {
            return GetOrCreateMember(name, dataType, -1);
        }

        /// <summary>
        /// Adds a scalar member to the entity type.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <param name="dataType">Data type name</param>
        /// <param name="ordinal">Ordinal position of the member within the type. (zero-based)</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty AddMember(string name, string dataType, int ordinal)
        {
            try
            {
                if (!MemberProperties.Where(mp => mp.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    StoreMemberProperty mp = new StoreMemberProperty(base.ParentFile, this, name, ordinal, _entityTypeElement);
                    mp.DataType = dataType;
                    _memberProperties.Add(name, mp);
                    mp.NameChanged += new EventHandler<NameChangeArgs>(prop_NameChanged);
                    mp.Removed += new EventHandler(prop_Removed);
                    return mp;
                }
                else
                {
                    throw new ArgumentException("A member property with the name " + name + " already exist in the type " + this.Name);
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
        /// Retrieves an existing scalar member to the entity type, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="name">Scalar member name.</param>
        /// <param name="dataType">Data type name</param>
        /// <param name="ordinal">Ordinal position of the member within the type. (zero-based)</param>
        /// <returns>A StoreMemberProperty object.</returns>
        public StoreMemberProperty GetOrCreateMember(string name, string dataType, int ordinal)
        {
            try
            {
                StoreMemberProperty storeMemberProperty = MemberProperties.FirstOrDefault(mp => mp.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (storeMemberProperty == null)
                {
                    storeMemberProperty = AddMember(name, dataType, ordinal);
                }
                return storeMemberProperty;
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

        private StoreEntitySet _entitySet = null;

        /// <summary>
        /// The store entityset that this entity type belongs to.
        /// </summary>
        public StoreEntitySet EntitySet
        {
            get
            {
                try
                {
                    if (_entitySet == null)
                    {
                        _entitySet = _storageModel.EntitySets.FirstOrDefault(es => es.EntityTypeName.Equals(this.FullName, StringComparison.InvariantCultureIgnoreCase) || es.EntityTypeName.Equals(this.AliasName, StringComparison.InvariantCultureIgnoreCase));
                        if (_entitySet != null)
                        {
                            _entitySet.Removed += new EventHandler(entitySet_Removed);
                        }
                    }
                    return _entitySet;
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

        void entitySet_Removed(object sender, EventArgs e)
        {
            _entitySet = null;
        }

        /// <summary>
        /// Enumeration of all associations originating from this entity type.
        /// </summary>
        public IEnumerable<StoreAssociationSet> AssociationsFrom
        {
            get
            {
                try
                {
                    foreach (StoreAssociationSet assoc in _storageModel.AssociationSets.Where(aset => aset.FromEntitySet == EntitySet))
                    {
                        yield return assoc;
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        /// <summary>
        /// Enumeration of all associations referencing this entity type.
        /// </summary>
        public IEnumerable<StoreAssociationSet> AssociationsTo
        {
            get
            {
                try
                {
                    foreach (StoreAssociationSet assoc in _storageModel.AssociationSets.Where(aset => aset.ToEntitySet == EntitySet))
                    {
                        yield return assoc;
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        #region "Documentation"
        internal XmlElement DocumentationElement
        {
            get
            {
                return _entityTypeElement.GetOrCreateElement("ssdl", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_entityTypeElement.SelectSingleNode("ssdl:Documentation/ssdl:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_entityTypeElement.SelectSingleNode("ssdl:Documentation/ssdl:LongDescription", NSM);
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

        /// <summary>
        /// Adds an xml comment to the entity type.
        /// </summary>
        /// <param name="commentText">Text contained in the comment.</param>
        public void AddComment(string commentText)
        {
            if (_entityTypeElement != null && _entityTypeElement.ParentNode != null)
            {
                XmlComment commentElement = EDMXDocument.CreateComment(commentText);
                _entityTypeElement.AppendChild(commentElement);
            }
        }
    }
}
