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
    /// Represents a mapping between a conceptual model entity set and a storage model entity set
    /// </summary>
    public class EntitySetMapping : EDMXMember, IEDMXRemovableMember
    {
        private CSMapping _csMapping = null;
        private XmlElement _esmElement = null;

        internal EntitySetMapping(EDMXFile parentFile, CSMapping csMapping, XmlElement esmElement)
            : base(parentFile)
        {
            _csMapping = csMapping;
            _esmElement = esmElement;
        }

        internal EntitySetMapping(EDMXFile parentFile, XmlElement entityContainerMappingElement, CSMapping csMapping, ModelEntitySet modelEntitySet, StoreEntitySet[] storeEntitySets)
            : base(parentFile)
        {
            _csMapping = csMapping;

            //create mapping xml elements
            _esmElement = EDMXDocument.CreateElement("EntitySetMapping", NameSpaceURImap);
            _esmElement.SetAttribute("Name", modelEntitySet.Name);
            entityContainerMappingElement.AppendChild(_esmElement);

            //entity type mapping
            XmlElement entityTypeMapping = EDMXDocument.CreateElement("EntityTypeMapping", NameSpaceURImap);
            if ((modelEntitySet.EntityType.HasBaseType || modelEntitySet.EntityType.HasSubTypes) && modelEntitySet.InheritanceStrategy != EDMXInheritanceStrategyEnum.TPC)
            {
                entityTypeMapping.SetAttribute("TypeName", "IsTypeOf(" + modelEntitySet.EntityType.FullName + ")");
            }
            else
            {
                entityTypeMapping.SetAttribute("TypeName", modelEntitySet.EntityType.FullName);
            }
            _esmElement.AppendChild(entityTypeMapping);

            foreach (StoreEntitySet ses in storeEntitySets)
            {
                //mapping fragment wrapper
                XmlElement mappingFragment = EDMXDocument.CreateElement("MappingFragment", NameSpaceURImap);
                mappingFragment.SetAttribute("StoreEntitySet", ses.Name);
                entityTypeMapping.AppendChild(mappingFragment);
            }

            //keep references to entity sets...
            _modelEntitySet = modelEntitySet;
            _storeEntitySets.AddRange(storeEntitySets);

            //let the involved entity sets know about the change in mappings...
            modelEntitySet.CSMappingsUpdated();
            storeEntitySets.ToList().ForEach(es => es.CSMappingsUpdated());

            //hook up remove events
            _modelEntitySet.Removed += new EventHandler(modelEntitySet_Removed);
            foreach (StoreEntitySet ses in _storeEntitySets)
            {
                ses.Removed += new EventHandler(storeEntitySet_Removed);
            }
        }

        void modelEntitySet_Removed(object sender, EventArgs e)
        {
            this.Remove();
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
                if (_esmElement.ParentNode != null)
                {
                    _esmElement.ParentNode.RemoveChild(_esmElement);

                    if (_modelEntitySet != null)
                    {
                        _modelEntitySet.CSMappingsUpdated();
                    }
                    if (_storeEntitySets != null)
                    {
                        _storeEntitySets.ForEach(ses => ses.CSMappingsUpdated());
                    }

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
                    if (!ex.Data.Contains("EDMXType"))
                    {
                        ex.Data.Add("EDMXType", this.GetType().Name);
                    }
                    if (!ex.Data.Contains("EDMXObjectName"))
                    {
                        ex.Data.Add("EDMXObjectName", this.ModelEntitySet.FullName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Event fired for each suggested mapping between a conceptual model scalar member and storage model scalar member, when using automapping. See the AutoMapMembers method.
        /// </summary>
        public event EventHandler<AutoMapArgs> OnColumnMapping;

        /// <summary>
        /// Initiates auto-mapping of entity set members. The OnColumnMapping event will be fired for every suggested mapping between a conceptual model member property and a storage model member property.
        /// </summary>
        public void AutoMapMembers()
        {
            AutoMapMembers(_modelEntitySet.EntityType);
            foreach (ModelEntityType et in _modelEntitySet.EntityType.SubTypes)
            {
                AutoMapMembers(et);
            }
        }

        /// <summary>
        /// Initiates auto-mapping of entity set members. The OnColumnMapping event will be fired for every suggested mapping between a conceptual model member property and a storage model member property.
        /// </summary>
        public void AutoMapMembers(ModelEntityType entityType)
        {
            //go through model entity set members and attempt to map to storage set(s) members.
            //  ...raise BeforeColumnMapping for each mapping suggestion, to give caller a chance to veto...
            foreach (ModelMemberProperty modelMemberProperty in entityType.MemberProperties)
            {
                List<StoreMemberProperty> mappingCandidates = _storeEntitySets.SelectMany(mp => mp.EntityType.MemberProperties.Where(mpn => mpn.Name.Equals(modelMemberProperty.Name))).ToList();
                if (mappingCandidates.Count > 0)
                {
                    //match(es) found
                    foreach (StoreMemberProperty storeMemberProperty in mappingCandidates)
                    {
                        AutoMapArgs args = new AutoMapArgs()
                        {
                            ModelMemberProperty = modelMemberProperty,
                            StoreMemberProperty = storeMemberProperty,
                            UseMapping = true
                        };
                        if (OnColumnMapping != null)
                        {
                            OnColumnMapping(this, args);
                        }
                        if (args.UseMapping)
                        {
                            AddMemberMapping(modelMemberProperty, storeMemberProperty);
                        }
                    }
                }
                else
                {
                    //no match found, raise event to request mapping...
                    AutoMapArgs args = new AutoMapArgs()
                    {
                        ModelMemberProperty = modelMemberProperty,
                        StoreMemberProperty = null,
                        UseMapping = false
                    };

                    if (OnColumnMapping != null)
                    {
                        OnColumnMapping(this, args);
                    }
                    if (args.UseMapping == true && args.StoreMemberProperty != null)
                    {
                        AddMemberMapping(modelMemberProperty, args.StoreMemberProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a member mapping from a conceptual model scalar member to a storage model scalar member.
        /// </summary>
        /// <param name="modelMemberProperty">Conceptual model scalar member to map</param>
        /// <param name="storeMemberProperty">Storage model scalar member to map to</param>
        public void AddMemberMapping(ModelMemberProperty modelMemberProperty, StoreMemberProperty storeMemberProperty)
        {
            AddMemberMapping(modelMemberProperty, storeMemberProperty, modelMemberProperty.EntityType);
        }

        /// <summary>
        /// Adds a member mapping from a conceptual model scalar member to a storage model scalar member, with a entity type specified
        /// </summary>
        /// <param name="modelMemberProperty">Conceptual model scalar member to map</param>
        /// <param name="storeMemberProperty">Storage model scalar member to map to</param>
        /// <param name="modelEntityType">Model entity type to specify in the EntityTypeMapping for this member mapping.</param>
        public void AddMemberMapping(ModelMemberProperty modelMemberProperty, StoreMemberProperty storeMemberProperty, ModelEntityType modelEntityType)
        {
            if (modelEntityType != _modelEntitySet.EntityType && !modelEntityType.IsSubtypeOf(_modelEntitySet.EntityType))
            {
                throw new ArgumentException("The model member does not belong to the mapped entity type or a subclass of the mapped entity type.");
            }

            if (storeMemberProperty.EntityType.EntitySet != null)
            {
                //find the appropriate mapping fragment
                string storeEntitySetName = storeMemberProperty.EntityType.EntitySet.Name;

                //get hold of the type mapping
                XmlElement entityTypeMapping = (XmlElement)_esmElement.SelectSingleNode("map:EntityTypeMapping[@TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.FullName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.FullName + ")") + " or @TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.AliasName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.AliasName + ")") + "]", NSM);
                if (entityTypeMapping == null)
                {
                    //not found - create
                    entityTypeMapping = EDMXDocument.CreateElement("EntityTypeMapping", NameSpaceURImap);
                    _esmElement.AppendChild(entityTypeMapping);

                    entityTypeMapping.SetAttribute("TypeName", modelEntityType.FullName);
                }

                XmlElement mappingFragment = (XmlElement)entityTypeMapping.SelectSingleNode("map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM);
                if (mappingFragment == null)
                {
                    mappingFragment = EDMXDocument.CreateElement("MappingFragment", NameSpaceURImap);
                    entityTypeMapping.AppendChild(mappingFragment);

                    mappingFragment.SetAttribute("StoreEntitySet", storeEntitySetName);

                    if (_storeEntitySetsEnumerated == true)
                    {
                        StoreEntitySet storeEntitySet = _csMapping.ParentFile.StorageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(storeEntitySetName, StringComparison.InvariantCultureIgnoreCase));
                        if (storeEntitySet != null)
                        {
                            storeEntitySet.Removed += new EventHandler(storeEntitySet_Removed);
                            _storeEntitySets.Add(storeEntitySet);
                        }
                    }
                }

                if (mappingFragment != null)
                {
                    if (mappingFragment.SelectSingleNode("map:ScalarProperty[@Name=" + XmlHelpers.XPathLiteral(modelMemberProperty.Name) + "][@ColumnName=" + XmlHelpers.XPathLiteral(storeMemberProperty.Name) + "]", NSM) == null)
                    {
                        XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                        scalarProperty.SetAttribute("Name", modelMemberProperty.Name);
                        scalarProperty.SetAttribute("ColumnName", storeMemberProperty.Name);
                        mappingFragment.AppendChild(scalarProperty);

                        _memberMappings.Add(new Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>(storeMemberProperty, modelMemberProperty, modelEntityType));

                        storeMemberProperty.Removed += new EventHandler(smp_Removed);
                        modelMemberProperty.Removed += new EventHandler(mmp_Removed);

                        storeMemberProperty.CSMappingsUpdated();
                        modelMemberProperty.CSMappingsUpdated();
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
                else
                {
                    throw new ArgumentException("The store entity set " + storeEntitySetName + " is not associated with the model entity set " + this.ModelEntitySet.Name);
                }
            }
            else
            {
                throw new InvalidOperationException("The store entity type " + (storeMemberProperty.EntityType != null ? storeMemberProperty.EntityType.Name : "[unknown]") + " is not associated with an entity set.");
            }
        }

        /// <summary>
        /// Removes all scalar member mappings for a specific conceptual model member.
        /// </summary>
        /// <param name="modelMemberProperty">Conceptual model member to remove mappings for.</param>
        public void RemoveMemberMapping(ModelMemberProperty modelMemberProperty)
        {
            _memberMappings.RemoveAll(mm => mm.Item2 == modelMemberProperty);

            foreach (XmlElement scalarProperty in _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment//map:ScalarProperty[@Name=" + XmlHelpers.XPathLiteral(modelMemberProperty.Name) + "]", NSM))
            {
                XmlNode parentNode = scalarProperty.ParentNode;
                if (parentNode != null)
                {
                    parentNode.RemoveChild(scalarProperty);

                    //if this was the last child node, remove the wrapper
                    if (!parentNode.HasChildNodes)
                    {
                        parentNode.ParentNode.RemoveChild(parentNode);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all scalar member mappings for a specific storage model member.
        /// </summary>
        /// <param name="storeMemberProperty">Storage model member to remove mappings for.</param>
        public void RemoveMemberMapping(StoreMemberProperty storeMemberProperty)
        {
            _memberMappings.RemoveAll(mm => mm.Item1 == storeMemberProperty);

            string storeEntitySetName = storeMemberProperty.EntityType.EntitySet.Name;
            XmlElement mappingFragment = (XmlElement)_esmElement.SelectSingleNode("map:EntityTypeMapping/map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM);
            if (mappingFragment != null)
            {
                XmlElement scalarProperty = (XmlElement)mappingFragment.SelectSingleNode(".//map:ScalarProperty[@ColumnName=" + XmlHelpers.XPathLiteral(storeMemberProperty.Name) + "]", NSM);
                if (scalarProperty != null)
                {
                    XmlNode parentNode = scalarProperty.ParentNode;
                    if (parentNode != null)
                    {
                        parentNode.RemoveChild(scalarProperty);

                        //if this was the last child node, remove the wrapper
                        if (!parentNode.HasChildNodes)
                        {
                            parentNode.ParentNode.RemoveChild(parentNode);
                        }
                    }
                }
            }
        }

        private ModelEntitySet _modelEntitySet = null;

        /// <summary>
        /// Conceptual model entity set mapped with this EntitySetMapping
        /// </summary>
        public ModelEntitySet ModelEntitySet
        {
            get
            {
                if (_modelEntitySet == null)
                {
                    string esmName = _esmElement.GetAttribute("Name");
                    _modelEntitySet = _csMapping.ParentFile.ConceptualModel.EntitySets.FirstOrDefault(es => es.Name.Equals(esmName, StringComparison.InvariantCultureIgnoreCase));
                    if (_modelEntitySet != null)
                    {
                        _modelEntitySet.Removed += new EventHandler(modelEntitySet_Removed);
                    }
                }
                return _modelEntitySet;
            }
        }

        private bool _storeEntitySetsEnumerated = false;
        private List<StoreEntitySet> _storeEntitySets = new List<StoreEntitySet>();

        /// <summary>
        /// Store entity sets mapped in this EntitySetMapping
        /// </summary>
        public IEnumerable<StoreEntitySet> StoreEntitySets
        {
            get
            {
                if (_storeEntitySetsEnumerated == false)
                {
                    EnumerateStoreEntitySets();
                }
                return _storeEntitySets.AsEnumerable();
            }
        }

        private void EnumerateStoreEntitySets()
        {
            XmlNodeList mappingFragmentElements = _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment", NSM);
            foreach (XmlElement mappingFragmentElement in mappingFragmentElements)
            {
                string storeEntitySetName = mappingFragmentElement.GetAttribute("StoreEntitySet");
                StoreEntitySet storeEntitySet = _csMapping.ParentFile.StorageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(storeEntitySetName, StringComparison.InvariantCultureIgnoreCase));
                if (storeEntitySet != null)
                {
                    storeEntitySet.Removed += new EventHandler(storeEntitySet_Removed);
                    _storeEntitySets.Add(storeEntitySet);
                }
            }
            _storeEntitySetsEnumerated = true;
        }

        void storeEntitySet_Removed(object sender, EventArgs e)
        {
            _storeEntitySets.Remove((StoreEntitySet)sender);
            if (!_storeEntitySets.Any())
            {
                this.Remove();
            }
        }

        private bool _memberMappingsEnumerated = false;
        private List<Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>> _memberMappings = new List<Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>>();

        /// <summary>
        /// Member mappings between the conceptual model and storage model
        /// </summary>
        public IEnumerable<Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>> MemberMappings
        {
            get
            {
                if (_memberMappingsEnumerated == false)
                {
                    EnumerateMemberMappings();
                }
                return _memberMappings.AsEnumerable();
            }
        }

        /// <summary>
        /// Returns the entity type mapped to the specified store entity set
        /// </summary>
        /// <param name="storeEntitySet">A store entityset that is mapped with this EntitySetMapping</param>
        /// <returns>An entity type object for the entity type mapped to the specified store entityset</returns>
        public ModelEntityType EntityTypeFor(StoreEntitySet storeEntitySet)
        {
            string storeEntitySetName = storeEntitySet.Name;
            XmlElement mappingFragment = (XmlElement)_esmElement.SelectSingleNode("map:EntityTypeMapping/map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM);
            if (mappingFragment != null)
            {
                string entityTypeName = EDMXUtils.StripTypeOf(((XmlElement)mappingFragment.ParentNode).GetAttribute("TypeName"));

                ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == entityTypeName || et.AliasName == entityTypeName);
                return entityType;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// All model entity types that take part in this EntitySetMapping
        /// </summary>
        public IEnumerable<ModelEntityType> EntityTypes
        {
            get
            {
                foreach (XmlElement etm in _esmElement.SelectNodes("map:EntityTypeMapping", NSM))
                {
                    string entityTypeName = EDMXUtils.StripTypeOf(etm.GetAttribute("TypeName"));

                    ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == entityTypeName || et.AliasName == entityTypeName);
                    yield return entityType;
                }
            }
        }

        private void EnumerateMemberMappings()
        {
            foreach (XmlElement sp in _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment/map:ScalarProperty", NSM))
            {
                string modelPropertyName = sp.GetAttribute("Name");
                string entityTypeName = EDMXUtils.StripTypeOf(((XmlElement)sp.ParentNode.ParentNode).GetAttribute("TypeName"));

                ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == entityTypeName || et.AliasName == entityTypeName);
                ModelMemberProperty mmp = entityType.MemberProperties.FirstOrDefault(mp => mp.Name == modelPropertyName);

                if (mmp != null)
                {
                    string storeEntitySetName = ((XmlElement)sp.ParentNode).GetAttribute("StoreEntitySet");
                    StoreEntitySet ses = ParentFile.StorageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(storeEntitySetName, StringComparison.InvariantCultureIgnoreCase));

                    if (ses != null)
                    {
                        string storePropertyName = sp.GetAttribute("ColumnName");
                        StoreMemberProperty smp = ses.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name.Equals(storePropertyName, StringComparison.InvariantCultureIgnoreCase));

                        if (smp != null)
                        {
                            _memberMappings.Add(new Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>(smp, mmp, entityType));

                            smp.Removed += new EventHandler(smp_Removed);
                            mmp.Removed += new EventHandler(mmp_Removed);
                        }
                    }
                }
            }
            foreach (XmlElement sp in _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment/map:ComplexProperty/map:ScalarProperty", NSM))
            {
                string modelPropertyName = sp.GetAttribute("Name");

                string complexTypeName = EDMXUtils.StripTypeOf(((XmlElement)sp.ParentNode).GetAttribute("TypeName"));
                ModelComplexType complexType = ParentFile.ConceptualModel.ComplexTypes.FirstOrDefault(ct => ct.FullName == complexTypeName || ct.AliasName == complexTypeName);

                string entityTypeName = EDMXUtils.StripTypeOf(((XmlElement)sp.ParentNode.ParentNode.ParentNode).GetAttribute("TypeName"));
                ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == entityTypeName || et.AliasName == entityTypeName);

                ModelMemberProperty mmp = null;
                if (complexType != null)
                {
                    mmp = complexType.MemberProperties.FirstOrDefault(mp => mp.Name == modelPropertyName);

                    if (mmp != null)
                    {
                        string storeEntitySetName = ((XmlElement)sp.ParentNode.ParentNode).GetAttribute("StoreEntitySet");
                        StoreEntitySet ses = ParentFile.StorageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(storeEntitySetName, StringComparison.InvariantCultureIgnoreCase));

                        if (ses != null)
                        {
                            string storePropertyName = sp.GetAttribute("ColumnName");
                            StoreMemberProperty smp = ses.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name.Equals(storePropertyName, StringComparison.InvariantCultureIgnoreCase));

                            if (smp != null)
                            {
                                _memberMappings.Add(new Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>(smp, mmp, entityType));

                                smp.Removed += new EventHandler(smp_Removed);
                                mmp.Removed += new EventHandler(mmp_Removed);
                            }
                        }
                    }
                }
            }
            _memberMappingsEnumerated = true;
        }

        void mmp_Removed(object sender, EventArgs e)
        {
            ModelMemberProperty modelMemberProperty = (ModelMemberProperty)sender;
            _memberMappings.RemoveAll(mmp => mmp.Item2 == modelMemberProperty);
            RemoveMemberMapping(modelMemberProperty);
        }

        void smp_Removed(object sender, EventArgs e)
        {
            StoreMemberProperty storeMemberProperty = (StoreMemberProperty)sender;
            _memberMappings.RemoveAll(mmp => mmp.Item1 == storeMemberProperty);
            RemoveMemberMapping(storeMemberProperty);
        }

        /// <summary>
        /// Adds a complex type mapping
        /// </summary>
        /// <param name="complexTypeReference">Model member property referencing the complex type property</param>
        /// <param name="memberProperty">Model member property</param>
        /// <param name="storeMemberProperty">Store member property</param>
        public void AddComplexMapping(ModelMemberProperty complexTypeReference, ModelMemberProperty memberProperty, StoreMemberProperty storeMemberProperty)
        {
            //find the appropriate mapping fragment
            string storeEntitySetName = storeMemberProperty.EntityType.EntitySet.Name;
            foreach (XmlElement mappingFragment in _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM))
            {
                if (mappingFragment != null)
                {
                    XmlElement complexProperty = (XmlElement)mappingFragment.SelectSingleNode("map:ComplexProperty[@Name=" + XmlHelpers.XPathLiteral(complexTypeReference.Name) + "]", NSM);
                    if (complexProperty == null)
                    {
                        complexProperty = EDMXDocument.CreateElement("ComplexProperty", NameSpaceURImap);
                        complexProperty.SetAttribute("Name", complexTypeReference.Name);
                        complexProperty.SetAttribute("TypeName", complexTypeReference.TypeName);
                        mappingFragment.AppendChild(complexProperty);
                    }

                    string entityTypeName = EDMXUtils.StripTypeOf(((XmlElement)mappingFragment.ParentNode).GetAttribute("TypeName"));
                    ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == entityTypeName || et.AliasName == entityTypeName);

                    XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                    scalarProperty.SetAttribute("Name", memberProperty.Name);
                    scalarProperty.SetAttribute("ColumnName", storeMemberProperty.Name);
                    complexProperty.AppendChild(scalarProperty);

                    _memberMappings.Add(new Tuple<StoreMemberProperty, ModelMemberProperty, ModelEntityType>(storeMemberProperty, memberProperty, entityType));

                    storeMemberProperty.Removed += new EventHandler(smp_Removed);
                    memberProperty.Removed += new EventHandler(mmp_Removed);
                }
                else
                {
                    throw new ArgumentException("The store entity set " + storeEntitySetName + " is not associated with the model entity set " + this.ModelEntitySet.Name);
                }
            }
        }

        /// <summary>
        /// Returns all store entitysets mapped to the specified conceptual model entity type
        /// </summary>
        /// <param name="modelEntityType">A conceptual model entity type</param>
        /// <returns>An enumeration of the store entitysets mapped to the specified conceptual model entity type</returns>
        public IEnumerable<StoreEntitySet> StoreEntitySetsFor(ModelEntityType modelEntityType)
        {
            XmlElement entityTypeMapping = (XmlElement)_esmElement.SelectSingleNode("map:EntityTypeMapping[@TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.FullName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.FullName + ")") + " or @TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.AliasName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.AliasName + ")") + "]", NSM);
            if (entityTypeMapping != null)
            {
                foreach (XmlElement mappingFragment in entityTypeMapping.SelectNodes("map:MappingFragment", NSM))
                {
                    string storeEntitySetName = mappingFragment.GetAttribute("StoreEntitySet");
                    StoreEntitySet storeEntitySet = StoreEntitySets.FirstOrDefault(ses => ses.Name.Equals(storeEntitySetName, StringComparison.InvariantCultureIgnoreCase));
                    if (storeEntitySet != null)
                    {
                        yield return storeEntitySet;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new entity type mapping to this entity set mapping. Used for adding inherited sub-types to an entity set mapping
        /// </summary>
        /// <param name="modelEntityType">Conceptual model entity type to add to the mapping</param>
        /// <param name="storeEntityType">Store entity type mapped to the conceptual model entity type</param>
        public void AddEntityTypeMapping(ModelEntityType modelEntityType, StoreEntityType storeEntityType)
        {
            string storeEntitySetName = storeEntityType.EntitySet.Name;

            //get hold of the type mapping
            XmlElement entityTypeMapping = (XmlElement)_esmElement.SelectSingleNode("map:EntityTypeMapping[@TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.FullName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.FullName + ")") + " or @TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.AliasName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.AliasName + ")") + "]", NSM);
            if (entityTypeMapping == null)
            {
                //not found - create
                entityTypeMapping = EDMXDocument.CreateElement("EntityTypeMapping", NameSpaceURImap);
                _esmElement.AppendChild(entityTypeMapping);

                if ((modelEntityType.HasBaseType || modelEntityType.HasSubTypes)
                    && this.ModelEntitySet.InheritanceStrategy != EDMXInheritanceStrategyEnum.TPC)
                {
                    entityTypeMapping.SetAttribute("TypeName", "IsTypeOf(" + modelEntityType.FullName + ")");
                }
                else
                {
                    entityTypeMapping.SetAttribute("TypeName", modelEntityType.FullName);
                }
            }

            XmlElement mappingFragment = (XmlElement)entityTypeMapping.SelectSingleNode("map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM);
            if (mappingFragment == null)
            {
                mappingFragment = EDMXDocument.CreateElement("MappingFragment", NameSpaceURImap);
                entityTypeMapping.AppendChild(mappingFragment);

                mappingFragment.SetAttribute("StoreEntitySet", storeEntitySetName);

                if (_storeEntitySetsEnumerated == true)
                {
                    StoreEntitySet storeEntitySet = _csMapping.ParentFile.StorageModel.EntitySets.FirstOrDefault(es => es.Name == storeEntitySetName);
                    if (storeEntitySet != null)
                    {
                        storeEntitySet.Removed += new EventHandler(storeEntitySet_Removed);
                        _storeEntitySets.Add(storeEntitySet);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a mapping condition. Used for entity inheritance (e.g. TPH discriminators) or entity splitting.
        /// </summary>
        /// <param name="modelEntityType">Conceptual model entity type that this condition applies to.</param>
        /// <param name="discriminatorColumn">Store member that is used for the mapping condition.</param>
        /// <param name="discriminatorValue">Discriminator value that makes the mapping valid.</param>
        /// <returns>A MappingCondition object.</returns>
        public MappingCondition AddMappingCondition(ModelEntityType modelEntityType, StoreMemberProperty discriminatorColumn, string discriminatorValue)
        {
            MappingCondition mappingCondition = MappingConditions.FirstOrDefault(mc => mc.DiscriminatorColumn == discriminatorColumn && mc.ModelEntityType == modelEntityType);
            if (mappingCondition == null)
            {
                mappingCondition = new MappingCondition(this.ParentFile, this, _esmElement, modelEntityType, discriminatorColumn, discriminatorValue);
                if (_mappingConditions != null)
                {
                    _mappingConditions.Add(mappingCondition);
                    mappingCondition.Removed += new EventHandler(mappingCondition_Removed);
                }
            }
            return mappingCondition;
        }

        void mappingCondition_Removed(object sender, EventArgs e)
        {
            _mappingConditions.Remove((MappingCondition)sender);
        }

        private List<MappingCondition> _mappingConditions = null;

        /// <summary>
        /// Enumeration of all mapping conditions that exist within this EntitySetMapping
        /// </summary>
        public IEnumerable<MappingCondition> MappingConditions
        {
            get
            {
                if (_mappingConditions == null)
                {
                    EnumerateMappingConditions();
                }
                return _mappingConditions;
            }
        }

        private void EnumerateMappingConditions()
        {
            _mappingConditions = new List<MappingCondition>();
            foreach (XmlElement conditionElement in _esmElement.SelectNodes("map:EntityTypeMapping/map:MappingFragment/map:Condition", NSM))
            {
                MappingCondition mappingCondition = new MappingCondition(this.ParentFile, this, conditionElement);
                mappingCondition.Removed += new EventHandler(mappingCondition_Removed);
                _mappingConditions.Add(mappingCondition);
            }
        }
    }
}
