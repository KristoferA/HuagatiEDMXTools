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
    /// Represents an entity set in the storage model.
    /// </summary>
    public class StoreEntitySet : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation 
    {
        private XmlElement _entitySetElement = null;
        private StorageModel _storageModel = null;
        private StoreEntityType _entityType = null;

        internal StoreEntitySet(EDMXFile parentFile, StorageModel storageModel, XmlElement entitySetElement) : base(parentFile)
        {
            _storageModel = storageModel;
            _entitySetElement = entitySetElement;
        }

        internal StoreEntitySet(EDMXFile parentFile, StorageModel storageModel, string name) : base(parentFile)
        {
            _storageModel = storageModel;

            //create and add the entity set element
            XmlElement setContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:EntityContainer", NSM);
            _entitySetElement = EDMXDocument.CreateElement("EntitySet", NameSpaceURIssdl);
            setContainer.AppendChild(_entitySetElement);

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
                foreach (StoreAssociationSet sas in this.AssociationsFrom.Union(this.AssociationsTo).ToList())
                {
                    sas.Remove();
                }

                foreach (AssociationSetMapping asm in this.AssociationSetMappings.ToList())
                {
                    asm.Remove();
                }

                if (_entitySetElement.ParentNode != null)
                {
                    _entitySetElement.ParentNode.RemoveChild(_entitySetElement);

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
                return _entitySetElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _entitySetElement.GetAttribute("Name");
                _entitySetElement.SetAttribute("Name", value);

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
                if (_entitySetElement.ParentNode != null && _entitySetElement.ParentNode.ParentNode != null)
                {
                    return ((XmlElement)_entitySetElement.ParentNode.ParentNode).GetAttribute("Namespace") + "." + Name;
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
                if (_entitySetElement.ParentNode != null && _entitySetElement.ParentNode.ParentNode != null)
                {
                    return ((XmlElement)_entitySetElement.ParentNode.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Name of the entity type associated with this entity set.
        /// </summary>
        public string EntityTypeName
        {
            get
            {
                return _entitySetElement.GetAttribute("EntityType");
            }
        }

        /// <summary>
        /// Entity type defining the members of this entityset
        /// </summary>
        public StoreEntityType EntityType
        {
            get
            {
                try
                {
                    if (_entityType == null)
                    {
                        string etTypeName = EntityTypeName;
                        _entityType = _storageModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(etTypeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(etTypeName, StringComparison.InvariantCultureIgnoreCase));
                        if (_entityType != null)
                        {
                            _entityType.Removed += new EventHandler(entityType_Removed);
                        }
                    }
                    return _entityType;
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
                _entityType = value;
                _entitySetElement.SetAttribute("EntityType", value.FullName);
            }
        }

        void entityType_Removed(object sender, EventArgs e)
        {
            _entityType = null;
        }

        /// <summary>
        /// Type of underlying store object; table or view
        /// </summary>
        public StoreTypeEnum StoreType
        {
            get
            {
                try
                {
                    switch (_entitySetElement.GetAttribute("Type", NameSpaceURIstore))
                    {
                        case "Tables":
                            return StoreTypeEnum.Table;
                        case "Views":
                            return StoreTypeEnum.View;
                        default:
                            return StoreTypeEnum.Table;
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
            set
            {
                switch (value)
                {
                    case StoreTypeEnum.Table:
                        _entitySetElement.SetAttribute("Type", NameSpaceURIstore, "Tables");
                        break;
                    case StoreTypeEnum.View:
                        _entitySetElement.SetAttribute("Type", NameSpaceURIstore, "Views");
                        break;
                    default:
                        throw new ArgumentException("Invalid store type");
                }
            }
        }

        /// <summary>
        /// Schema name in the database for the underlying table or view
        /// </summary>
        public string Schema
        {
            get
            {
                try
                {
                    string schemaName = _entitySetElement.GetAttribute("Schema");
                    if (string.IsNullOrEmpty(schemaName))
                    {
                        schemaName = _entitySetElement.GetAttribute("Schema", NameSpaceURIstore);
                    }
                    return schemaName;
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
                _entitySetElement.SetAttribute("Schema", value);
                if (_entitySetElement.HasAttribute("Schema", NameSpaceURIstore))
                {
                    _entitySetElement.SetAttribute("Schema", NameSpaceURIstore, value);
                }
            }
        }

        /// <summary>
        /// Table or view name for the underlying table or view
        /// </summary>
        public string TableName
        {
            get
            {
                try
                {
                    string tableName = _entitySetElement.GetAttribute("Table");
                    if (string.IsNullOrEmpty(tableName))
                    {
                        tableName = _entitySetElement.GetAttribute("Name", NameSpaceURIstore);
                    }
                    if (string.IsNullOrEmpty(tableName))
                    {
                        string storeEntityTypeName = _entitySetElement.GetAttribute("EntityType");

                        XmlElement parentsParent = ((_entitySetElement.ParentNode != null && _entitySetElement.ParentNode.ParentNode != null) ? (XmlElement)_entitySetElement.ParentNode.ParentNode : null);
                        if (parentsParent != null)
                        {
                            string namespaceName = parentsParent.GetAttribute("Namespace");
                            string namespaceAlias = parentsParent.GetAttribute("Alias");
                            if (storeEntityTypeName.StartsWith(namespaceName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                tableName = storeEntityTypeName.Substring(namespaceName.Length + 1);
                            }
                            else if (storeEntityTypeName.StartsWith(namespaceAlias, StringComparison.InvariantCultureIgnoreCase))
                            {
                                tableName = storeEntityTypeName.Substring(namespaceAlias.Length + 1);
                            }
                        }
                    }
                    return tableName;
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
                _entitySetElement.SetAttribute("Table", value);
                _entitySetElement.SetAttribute("Name", NameSpaceURIstore, value);
            }
        }

        /// <summary>
        /// DefiningQuery for views
        /// </summary>
        public string DefiningQuery
        {
            get
            {
                XmlElement definingQueryElement = (XmlElement)_entitySetElement.SelectSingleNode("ssdl:DefiningQuery", NSM);
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
                XmlElement documentationElement = (XmlElement)_entitySetElement.SelectSingleNode("ssdl:Documentation", NSM);
                XmlElement definingQueryElement = null;
                if (documentationElement == null)
                {
                    definingQueryElement = XmlHelpers.GetOrCreateElement(_entitySetElement, "ssdl", "DefiningQuery", NSM, true);
                }
                else
                {
                    definingQueryElement = XmlHelpers.GetOrCreateElement(_entitySetElement, "ssdl", "DefiningQuery", NSM, false, documentationElement);
                }
                if (definingQueryElement != null)
                {
                    definingQueryElement.InnerText = value;
                }
            }
        }

        /// <summary>
        /// Entity set associations (foreign key constraints) originating from this entity set
        /// </summary>
        public IEnumerable<StoreAssociationSet> AssociationsFrom
        {
            get
            {
                try
                {
                    foreach (StoreAssociationSet assoc in _storageModel.AssociationSets.Where(aset => aset.FromEntitySet == this))
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
        /// Entity set associations (foreign key constraints) referencing this entity set
        /// </summary>
        public IEnumerable<StoreAssociationSet> AssociationsTo
        {
            get
            {
                try
                {
                    foreach (StoreAssociationSet assoc in _storageModel.AssociationSets.Where(aset => aset.ToEntitySet == this))
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
        /// True if this table is a candidate to act as a junction table behind a many-to-many association in the conceptual model.
        /// </summary>
        public bool IsJunctionCandidate
        {
            get
            {
                try
                {
                    bool isJunctionCandidate = false;
                    if (AssociationsFrom.Count() == 2)
                    {
                        List<StoreMemberProperty> assocMembers = AssociationsFrom.SelectMany(amp => amp.Keys.Select(k => k.Item1)).Distinct().ToList();
                        isJunctionCandidate = !EntityType.MemberProperties.Any(mp => !assocMembers.Contains(mp));
                    }
                    return isJunctionCandidate;
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

        private List<EntitySetMapping> _entitySetMappings = null;

        /// <summary>
        /// Mappings between this storage model entityset and conceptual model entityset(s) based on it.
        /// </summary>
        public IEnumerable<EntitySetMapping> EntitySetMappings
        {
            get
            {
                try
                {
                    if (_entitySetMappings == null)
                    {
                        _entitySetMappings = ParentFile.CSMapping.EntitySetMappings.Where(esm => esm.StoreEntitySets.Contains(this)).ToList();
                        foreach (EntitySetMapping esm in _entitySetMappings)
                        {
                            esm.Removed += new EventHandler(esm_Removed);
                        }
                    }
                    return _entitySetMappings.AsEnumerable();
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

        void esm_Removed(object sender, EventArgs e)
        {
            if (_entitySetMappings != null)
            {
                _entitySetMappings.Remove((EntitySetMapping)sender);
            }
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _entitySetElement.GetOrCreateElement("ssdl", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_entitySetElement.SelectSingleNode("ssdl:Documentation/ssdl:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_entitySetElement.SelectSingleNode("ssdl:Documentation/ssdl:LongDescription", NSM);
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

        internal void CSMappingsUpdated()
        {
            _entitySetMappings = null;
        }

        /// <summary>
        /// Enumeration of associationsetmappings (many-to-many associations) based on this entityset.
        /// </summary>
        public IEnumerable<AssociationSetMapping> AssociationSetMappings
        {
            get
            {
                try
                {
                    return ParentFile.CSMapping.AssociationSetMappings.Where(asm => asm.StoreEntitySet == this);
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
    }
}
