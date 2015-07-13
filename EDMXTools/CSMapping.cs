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
    /// Wrapper for all things related to mapping between the conceptual model and storage model. Access through the CSMapping property in the EDMXFile class.
    /// </summary>
    public class CSMapping : EDMXMember
    {
        private XmlElement _mappingsElement = null;
        private XmlElement _mappingElement = null;
        private XmlElement _entityContainerMapping = null;

        internal CSMapping(EDMXFile parentFile)
            : base(parentFile)
        {
            _mappingsElement = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:Mappings", NSM);
            _mappingElement = (XmlElement)_mappingsElement.SelectSingleNode("map:Mapping", NSM);
            if (_mappingElement == null)
            {
                _mappingElement = EDMXDocument.CreateElement("Mapping", NameSpaceURImap);
                _mappingElement.SetAttribute("Space", "C-S");
                _mappingsElement.AppendChild(_mappingElement);
            }

            _entityContainerMapping = (XmlElement)_mappingElement.SelectSingleNode("map:EntityContainerMapping", NSM);
            if (_entityContainerMapping == null)
            {
                _entityContainerMapping = EDMXDocument.CreateElement("EntityContainerMapping", NameSpaceURImap);
                _mappingElement.AppendChild(_entityContainerMapping);
            }
        }

        /// <summary>
        /// Store model entity container name.
        /// </summary>
        public string StorageEntityContainer
        {
            get
            {
                return _entityContainerMapping.GetAttribute("StorageEntityContainer");
            }
            set
            {
                _entityContainerMapping.SetAttribute("StorageEntityContainer", value);
            }
        }

        /// <summary>
        /// Conceptual model entity container name.
        /// </summary>
        public string ConceptualEntityContainer
        {
            get
            {
                return _entityContainerMapping.GetAttribute("CdmEntityContainer");
            }
            set
            {
                _entityContainerMapping.SetAttribute("CdmEntityContainer", value);
            }
        }

        private XmlNodeList _entitySetMappingElements = null;
        internal XmlNodeList EntitySetMappingElements
        {
            get
            {
                if (_entitySetMappingElements == null)
                {
                    _entitySetMappingElements = _entityContainerMapping.SelectNodes("map:EntitySetMapping", NSM);
                }
                return _entitySetMappingElements;
            }
        }

        private bool _entitySetMappingsEnumerated = false;
        private Dictionary<string, EntitySetMapping> _entitySetMappings = new Dictionary<string, EntitySetMapping>();

        /// <summary>
        /// Enumeration of all entity set mappings; mappings between store entity sets and model entity sets.
        /// </summary>
        public IEnumerable<EntitySetMapping> EntitySetMappings
        {
            get
            {
                if (_entitySetMappingsEnumerated == false)
                {
                    foreach (XmlElement esmElement in EntitySetMappingElements)
                    {
                        EntitySetMapping esm = null;
                        string esmName = esmElement.GetAttribute("Name");
                        if (_entitySetMappings.ContainsKey(esmName))
                        {
                            esm = _entitySetMappings[esmName];
                        }
                        else
                        {
                            esm = new EntitySetMapping(ParentFile, this, esmElement);
                            esm.ModelEntitySet.NameChanged += new EventHandler<NameChangeArgs>(ModelEntitySet_NameChanged);
                            esm.Removed += new EventHandler(esm_Removed);
                            _entitySetMappings.Add(esmName, esm);
                        }
                        yield return esm;
                    }
                    _entitySetMappingsEnumerated = true;
                    _entitySetMappingElements = null;
                }
                else
                {
                    foreach (EntitySetMapping esm in _entitySetMappings.Values)
                    {
                        yield return esm;
                    }
                }
            }
        }

        void esm_Removed(object sender, EventArgs e)
        {
            _entitySetMappings.Remove(((EntitySetMapping)sender).ModelEntitySet.Name);
        }

        void ModelEntitySet_NameChanged(object sender, NameChangeArgs e)
        {
            if (_entitySetMappings.ContainsKey(e.OldName))
            {
                EntitySetMapping esm = _entitySetMappings[e.OldName];
                _entitySetMappings.Remove(e.OldName);
                _entitySetMappings.Add(e.NewName, esm);
            }
        }

        /// <summary>
        /// Adds a mapping between a model entity set and one or more store entitysets.
        /// </summary>
        /// <param name="modelEntitySet">Model entityset to add mapping for.</param>
        /// <param name="storeEntitySets">One or several store entitysets mapped to the model entity set.</param>
        /// <returns>An EntitySetMapping object.</returns>
        public EntitySetMapping AddMapping(ModelEntitySet modelEntitySet, params StoreEntitySet[] storeEntitySets)
        {
            EntitySetMapping esm = new EntitySetMapping(base.ParentFile, _entityContainerMapping, this, modelEntitySet, storeEntitySets);
            _entitySetMappings.Add(esm.ModelEntitySet.Name, esm);
            esm.Removed += new EventHandler(esm_Removed);
            esm.ModelEntitySet.NameChanged += new EventHandler<NameChangeArgs>(ModelEntitySet_NameChanged);
            return esm;
        }

        /// <summary>
        /// Adds an association set mapping for a many-to-many relationship.
        /// </summary>
        /// <param name="name">Name of the association mapping.</param>
        /// <param name="modelAssociationSet">Model association set.</param>
        /// <param name="storeJunctionEntitySet">Store entity set acting as the junction table.</param>
        /// <param name="fromStoreAssocSet">First store association set making up the many-to-many mapping in the storage model.</param>
        /// <param name="toStoreAssocSet">Second store association set making up the many-to-many mapping in the storage model.</param>
        /// <returns>An AssociationSetMapping object.</returns>
        public AssociationSetMapping AddAssociationMapping(string name, ModelAssociationSet modelAssociationSet, StoreEntitySet storeJunctionEntitySet, StoreAssociationSet fromStoreAssocSet, StoreAssociationSet toStoreAssocSet)
        {
            AssociationSetMapping asm = new AssociationSetMapping(base.ParentFile, _entityContainerMapping, this, name, modelAssociationSet, storeJunctionEntitySet, fromStoreAssocSet, toStoreAssocSet);
            _associationSetMappings.Add(asm.Name, asm);
            asm.NameChanged += new EventHandler<NameChangeArgs>(asm_NameChanged);
            asm.Removed += new EventHandler(asm_Removed);
            return asm;
        }

        /// <summary>
        /// Adds an association set mapping between a model association set and store association set.
        /// </summary>
        /// <param name="name">Mapping name</param>
        /// <param name="modelAssociationSet">Model association set.</param>
        /// <param name="storeAssociationSet">Store association set.</param>
        /// <returns></returns>
        public AssociationSetMapping AddAssociationMapping(string name, ModelAssociationSet modelAssociationSet, StoreAssociationSet storeAssociationSet)
        {
            AssociationSetMapping asm = new AssociationSetMapping(base.ParentFile, _entityContainerMapping, this, name, modelAssociationSet, storeAssociationSet);
            _associationSetMappings.Add(asm.Name, asm);
            asm.NameChanged += new EventHandler<NameChangeArgs>(asm_NameChanged);
            asm.Removed += new EventHandler(asm_Removed);
            return asm;
        }

        void asm_NameChanged(object sender, NameChangeArgs e)
        {
            if (_associationSetMappings.ContainsKey(e.OldName))
            {
                _associationSetMappings.Remove(e.OldName);
                _associationSetMappings.Add(e.NewName, (AssociationSetMapping)sender);
            }
        }

        private XmlNodeList _associationSetMappingElements = null;
        internal XmlNodeList AssociationSetMappingElements
        {
            get
            {
                if (_associationSetMappingElements == null)
                {
                    _associationSetMappingElements = _entityContainerMapping.SelectNodes("map:AssociationSetMapping", NSM);
                }
                return _associationSetMappingElements;
            }
        }

        private bool _associationSetMappingsEnumerated = false;
        private Dictionary<string, AssociationSetMapping> _associationSetMappings = new Dictionary<string, AssociationSetMapping>();

        /// <summary>
        /// Enumeration of all associationsetmappings in the model.
        /// </summary>
        public IEnumerable<AssociationSetMapping> AssociationSetMappings
        {
            get
            {
                if (_associationSetMappingsEnumerated == false)
                {
                    foreach (XmlElement asmElement in AssociationSetMappingElements)
                    {
                        AssociationSetMapping asm = null;
                        string asmName = asmElement.GetAttribute("Name");
                        if (_associationSetMappings.ContainsKey(asmName))
                        {
                            asm = _associationSetMappings[asmName];
                        }
                        else
                        {
                            asm = new AssociationSetMapping(ParentFile, this, asmElement);
                            asm.Removed += new EventHandler(asm_Removed);
                            _associationSetMappings.Add(asmName, asm);
                        }
                        yield return asm;
                    }
                    _associationSetMappingsEnumerated = true;
                    _associationSetMappingElements = null;
                }
                else
                {
                    foreach (AssociationSetMapping asm in _associationSetMappings.Values)
                    {
                        yield return asm;
                    }
                }
            }
        }

        void asm_Removed(object sender, EventArgs e)
        {
            _associationSetMappings.Remove(((AssociationSetMapping)sender).Name);
        }



        private XmlNodeList _functionImportMappingElements = null;
        internal XmlNodeList FunctionImportMappingElements
        {
            get
            {
                if (_functionImportMappingElements == null)
                {
                    _functionImportMappingElements = _entityContainerMapping.SelectNodes("map:FunctionImportMapping", NSM);
                }
                return _functionImportMappingElements;
            }
        }

        private bool _functionImportMappingsEnumerated = false;
        private Dictionary<string, FunctionImportMapping> _functionImportMappings = new Dictionary<string, FunctionImportMapping>();

        /// <summary>
        /// Enumeration of all function import mappings.
        /// </summary>
        public IEnumerable<FunctionImportMapping> FunctionImportMappings
        {
            get
            {
                if (_functionImportMappingsEnumerated == false)
                {
                    foreach (XmlElement fimElement in FunctionImportMappingElements)
                    {
                        FunctionImportMapping fim = null;
                        string fimName = fimElement.GetAttribute("FunctionImportName");
                        if (_functionImportMappings.ContainsKey(fimName))
                        {
                            fim = _functionImportMappings[fimName];
                        }
                        else
                        {
                            fim = new FunctionImportMapping(ParentFile, this, fimElement);
                            fim.NameChanged += new EventHandler<NameChangeArgs>(ModelFunction_NameChanged);
                            fim.Removed += new EventHandler(fim_Removed);
                            _functionImportMappings.Add(fimName, fim);
                        }
                        yield return fim;
                    }
                    _functionImportMappingsEnumerated = true;
                    _functionImportMappingElements = null;
                }
                else
                {
                    foreach (FunctionImportMapping fim in _functionImportMappings.Values)
                    {
                        yield return fim;
                    }
                }
            }
        }

        void fim_Removed(object sender, EventArgs e)
        {
            string fimName = ((FunctionImportMapping)sender).Name;
            if (_functionImportMappings.ContainsKey(fimName))
            {
                _functionImportMappings.Remove(fimName);
            }
        }

        void ModelFunction_NameChanged(object sender, NameChangeArgs e)
        {
            if (_functionImportMappings.ContainsKey(e.OldName))
            {
                FunctionImportMapping fim = _functionImportMappings[e.OldName];
                _functionImportMappings.Remove(e.OldName);
                _functionImportMappings.Add(e.NewName, fim);
            }
        }

        /// <summary>
        /// Adds a mapping between a model function and a store function.
        /// </summary>
        /// <param name="modelFunction">Model function import to map.</param>
        /// <param name="storeFunction">Store function to map the model function to.</param>
        /// <returns>A FunctionImportMapping object representing the new mapping.</returns>
        public FunctionImportMapping AddMapping(ModelFunction modelFunction, StoreFunction storeFunction)
        {
            FunctionImportMapping fim = new FunctionImportMapping(base.ParentFile, _entityContainerMapping, this, modelFunction, storeFunction);
            _functionImportMappings.Add(fim.ModelFunction.Name, fim);
            fim.Removed += new EventHandler(fim_Removed);
            fim.ModelFunction.NameChanged += new EventHandler<NameChangeArgs>(ModelFunction_NameChanged);
            return fim;
        }

        /// <summary>
        /// Removes all mappings; association mappings, entity set mappings, and function import mappings.
        /// </summary>
        public void Clear()
        {
            foreach (AssociationSetMapping asm in AssociationSetMappings.ToList())
            {
                asm.Remove();
            }
            foreach (EntitySetMapping esm in EntitySetMappings.ToList())
            {
                esm.Remove();
            }
            foreach (FunctionImportMapping fim in FunctionImportMappings.ToList())
            {
                fim.Remove();
            }
        }
    }
}
