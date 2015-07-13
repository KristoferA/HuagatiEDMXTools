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
    /// Class representing the storage model (SSDL)
    /// </summary>
    public class StorageModel : EDMXMember
    {
        private XmlElement _storageModelElement = null;
        private XmlElement _schemaElement = null;
        private XmlElement _entityContainerElement = null;
        private List<Exception> _modelErrors = new List<Exception>();

        internal StorageModel(EDMXFile parentFile) : base(parentFile)
        {
            _storageModelElement = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels", NSM);
            _schemaElement = (XmlElement)_storageModelElement.SelectSingleNode("ssdl:Schema", NSM);

            if (_schemaElement == null)
            {
                _schemaElement = EDMXDocument.CreateElement("Schema", NameSpaceURIssdl);
                _storageModelElement.AppendChild(_schemaElement);
            }

            _entityContainerElement = (XmlElement)_schemaElement.SelectSingleNode("ssdl:EntityContainer", NSM);
            if (_entityContainerElement == null)
            {
                _entityContainerElement = EDMXDocument.CreateElement("EntityContainer", NameSpaceURIssdl);
                _schemaElement.AppendChild(_entityContainerElement);
            }
        }

        /// <summary>
        /// Namespace name for the storage model.
        /// </summary>
        public string Namespace
        {
            get
            {
                return _schemaElement.GetAttribute("Namespace");
            }
            set
            {
                _schemaElement.SetAttribute("Namespace", value);
            }
        }

        /// <summary>
        /// Alias name for the storage model.
        /// </summary>
        public string Alias
        {
            get
            {
                return _schemaElement.GetAttribute("Alias");
            }
            set
            {
                _schemaElement.SetAttribute("Alias", value);
            }
        }

        /// <summary>
        /// Provider name
        /// </summary>
        public string Provider
        {
            get
            {
                return _schemaElement.GetAttribute("Provider");
            }
            set
            {
                _schemaElement.SetAttribute("Provider", value);
            }
        }

        /// <summary>
        /// Provider manifest token
        /// </summary>
        public string ProviderManifestToken
        {
            get
            {
                return _schemaElement.GetAttribute("ProviderManifestToken");
            }
            set
            {
                _schemaElement.SetAttribute("ProviderManifestToken", value);
            }
        }

        /// <summary>
        /// Entity container name
        /// </summary>
        public string ContainerName
        {
            get
            {
                return _entityContainerElement.GetAttribute("Name");
            }
            set
            {
                _entityContainerElement.SetAttribute("Name", value);
            }
        }

        /// <summary>
        /// Adds a new entity set to the storage model.
        /// </summary>
        /// <param name="name">Entity set name for the new entityset.</param>
        /// <returns>A new StoreEntitySet object.</returns>
        public StoreEntitySet AddEntitySet(string name)
        {
            if (!EntitySets.Where(es => es.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                StoreEntitySet es = new StoreEntitySet(ParentFile, this, name);
                _storeEntitySets.Add(name, es);
                es.NameChanged += new EventHandler<NameChangeArgs>(es_NameChanged);
                es.Removed += new EventHandler(es_Removed);
                return es;
            }
            else
            {
                throw new ArgumentException("An entity set with the name " + name + " already exist in the model.");
            }
        }

        /// <summary>
        /// Adds a new entity type to the storage model.
        /// </summary>
        /// <param name="name">Entity type name from the new entitytype.</param>
        /// <returns>A new StoreEntityType object.</returns>
        public StoreEntityType AddEntityType(string name)
        {
            if (!EntityTypes.Where(et => et.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                StoreEntityType et = new StoreEntityType(ParentFile, this, name);
                _storeEntityTypes.Add(name, et);
                et.NameChanged += new EventHandler<NameChangeArgs>(et_NameChanged);
                et.Removed += new EventHandler(et_Removed);
                return et;
            }
            else
            {
                throw new ArgumentException("An entity type with the name " + name + " already exist in the model.");
            }
        }

        /// <summary>
        /// Adds a new association between two store entitysets.
        /// </summary>
        /// <param name="name">Name of the association, typically the foreign key name.</param>
        /// <param name="fromEntitySet">From-entityset.</param>
        /// <param name="toEntitySet">To-entityset.</param>
        /// <param name="fromMultiplicity">From-multiplicity.</param>
        /// <param name="toMultiplicity">To-multiplicity.</param>
        /// <param name="keys">Pairs of the foreign key / association scalar members enforcing the association/foreign key constraint.</param>
        /// <returns>A new StoreAssociationSet object.</returns>
        public StoreAssociationSet AddAssociation(string name, StoreEntitySet fromEntitySet, StoreEntitySet toEntitySet, MultiplicityTypeEnum fromMultiplicity, MultiplicityTypeEnum toMultiplicity, List<Tuple<StoreMemberProperty, StoreMemberProperty>> keys)
        {
            return AddAssociation(name, fromEntitySet, toEntitySet, fromEntitySet.EntityType, toEntitySet.EntityType, fromEntitySet.EntityType.Name, toEntitySet.EntityType.Name, fromMultiplicity, toMultiplicity, keys);
        }

        /// <summary>
        /// Adds a new association between two store entitysets.
        /// </summary>
        /// <param name="name">Name of the association, typically the foreign key name.</param>
        /// <param name="fromEntitySet">From-entityset.</param>
        /// <param name="toEntitySet">To-entityset.</param>
        /// <param name="fromEntityType">From-entitytype. This must be an entity type associated with the from-entityset, or part of the same inheritance structure.</param>
        /// <param name="toEntityType">To-entitytype. This must be an entity type associated with the to-entityset, or part of the same inheritance structure.</param>
        /// <param name="fromRoleName">From-role</param>
        /// <param name="toRoleName">To-role</param>
        /// <param name="fromMultiplicity">From-multiplicity.</param>
        /// <param name="toMultiplicity">To-multiplicity.</param>
        /// <param name="keys">Pairs of the foreign key / association scalar members enforcing the association/foreign key constraint.</param>
        /// <returns></returns>
        public StoreAssociationSet AddAssociation(string name, StoreEntitySet fromEntitySet, StoreEntitySet toEntitySet, StoreEntityType fromEntityType, StoreEntityType toEntityType, string fromRoleName, string toRoleName, MultiplicityTypeEnum fromMultiplicity, MultiplicityTypeEnum toMultiplicity, List<Tuple<StoreMemberProperty, StoreMemberProperty>> keys)
        {
            if (!AssociationSets.Where(et => et.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                StoreAssociationSet sas = new StoreAssociationSet(this.ParentFile, this, name, fromEntitySet, toEntitySet, fromEntityType, toEntityType, fromRoleName, toRoleName, fromMultiplicity, toMultiplicity, keys);
                _storeAssociationSets.Add(sas.Name, sas);
                sas.NameChanged += new EventHandler<NameChangeArgs>(aset_NameChanged);
                sas.Removed += new EventHandler(aset_Removed);
                return sas;
            }
            else
            {
                throw new ArgumentException("An association named " + name + " already exists in the model.");
            }
        }

        /// <summary>
        /// Adds a store function.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <returns>A new StoreFunction object.</returns>
        public StoreFunction AddFunction(string name)
        {
            if (!Functions.Where(sf => sf.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                StoreFunction sf = new StoreFunction(ParentFile, this, name);
                _storeFunctions.Add(name, sf);
                sf.NameChanged += new EventHandler<NameChangeArgs>(sfn_NameChanged);
                sf.Removed += new EventHandler(sfn_Removed);
                return sf;
            }
            else
            {
                throw new ArgumentException("A function with the name " + name + " already exist in the model.");
            }
        }

        private XmlNodeList _entitySetElements = null;
        private XmlNodeList EntitySetElements
        {
            get
            {
                if (_entitySetElements == null)
                {
                    _entitySetElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:EntityContainer/ssdl:EntitySet", NSM);
                }
                return _entitySetElements;
            }
        }

        private bool _entitySetsEnumerated = false;
        private Dictionary<string, StoreEntitySet> _storeEntitySets = new Dictionary<string, StoreEntitySet>();

        /// <summary>
        /// Enumeration of all entity sets in this store model.
        /// </summary>
        public IEnumerable<StoreEntitySet> EntitySets
        {
            get
            {
                if (_entitySetsEnumerated == false)
                {
                    foreach (XmlElement entitySetElement in EntitySetElements)
                    {
                        string esName = entitySetElement.GetAttribute("Name");
                        StoreEntitySet es = null;
                        if (!_storeEntitySets.ContainsKey(esName))
                        {
                            es = new StoreEntitySet(ParentFile, this, entitySetElement);
                            es.NameChanged += new EventHandler<NameChangeArgs>(es_NameChanged);
                            es.Removed += new EventHandler(es_Removed);
                            _storeEntitySets.Add(esName, es);
                        }
                        else
                        {
                            es = _storeEntitySets[esName];
                        }
                        yield return es;
                    }
                    _entitySetsEnumerated = true;
                    _entitySetElements = null;
                }
                else
                {
                    foreach (StoreEntitySet es in _storeEntitySets.Values)
                    {
                        yield return es;
                    }
                }
            }
        }

        void es_Removed(object sender, EventArgs e)
        {
            _storeEntitySets.Remove(((StoreEntitySet)sender).Name);
        }

        void es_NameChanged(object sender, NameChangeArgs e)
        {
            if (_storeEntitySets.ContainsKey(e.OldName))
            {
                _storeEntitySets.Remove(e.OldName);
                _storeEntitySets.Add(e.NewName, (StoreEntitySet)sender);
            }
        }

        private XmlNodeList _associationSetElements = null;
        private XmlNodeList AssociationSetElements
        {
            get
            {
                if (_associationSetElements == null)
                {
                    _associationSetElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:EntityContainer/ssdl:AssociationSet", NSM);
                }
                return _associationSetElements;
            }
        }

        private bool _associationSetsEnumerated = false;
        private Dictionary<string, StoreAssociationSet> _storeAssociationSets = new Dictionary<string, StoreAssociationSet>();

        /// <summary>
        /// Enumeration of all association sets (foreign key constraints) in the current storage model.
        /// </summary>
        public IEnumerable<StoreAssociationSet> AssociationSets
        {
            get
            {
                if (_associationSetsEnumerated == false)
                {
                    foreach (XmlElement associationSetElement in AssociationSetElements)
                    {
                        string asName = associationSetElement.GetAttribute("Name");
                        StoreAssociationSet aset = null;
                        if (!_storeAssociationSets.ContainsKey(asName))
                        {
                            try
                            {
                                aset = new StoreAssociationSet(ParentFile, this, associationSetElement);
                                aset.NameChanged += new EventHandler<NameChangeArgs>(aset_NameChanged);
                                aset.Removed += new EventHandler(aset_Removed);
                                _storeAssociationSets.Add(asName, aset);
                            }
                            catch (InvalidModelObjectException ex)
                            {
                                this._modelErrors.Add(ex);
                            }
                        }
                        else
                        {
                            aset = _storeAssociationSets[asName];
                        }
                        yield return aset;
                    }
                    _associationSetsEnumerated = true;
                    _associationSetElements = null;
                }
                else
                {
                    foreach (StoreAssociationSet aset in _storeAssociationSets.Values)
                    {
                        yield return aset;
                    }
                }
            }
        }

        void aset_Removed(object sender, EventArgs e)
        {
            _storeAssociationSets.Remove(((StoreAssociationSet)sender).Name);
        }

        void aset_NameChanged(object sender, NameChangeArgs e)
        {
            if (_storeAssociationSets.ContainsKey(e.OldName))            
            {
                _storeAssociationSets.Remove(e.OldName);
                _storeAssociationSets.Add(e.NewName, (StoreAssociationSet)sender);
            }
        }

        private XmlNodeList _entityTypeElements = null;
        private XmlNodeList EntityTypeElements
        {
            get
            {
                if (_entityTypeElements == null)
                {
                    _entityTypeElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:EntityType", NSM);
                }
                return _entityTypeElements;
            }
        }

        private bool _entityTypesEnumerated = false;
        private Dictionary<string, StoreEntityType> _storeEntityTypes = new Dictionary<string, StoreEntityType>();

        /// <summary>
        /// Enumeration of all entity types in the current storage model.
        /// </summary>
        public IEnumerable<StoreEntityType> EntityTypes
        {
            get
            {
                if (_entityTypesEnumerated == false)
                {
                    foreach (XmlElement entityTypeElement in EntityTypeElements)
                    {
                        string etName = entityTypeElement.GetAttribute("Name");
                        StoreEntityType et = null;
                        if (!_storeEntityTypes.ContainsKey(etName))
                        {
                            et = new StoreEntityType(ParentFile, this, entityTypeElement);
                            et.NameChanged += new EventHandler<NameChangeArgs>(et_NameChanged);
                            et.Removed += new EventHandler(et_Removed);
                            _storeEntityTypes.Add(etName, et);
                        }
                        else
                        {
                            et = _storeEntityTypes[etName];
                        }
                        yield return et;
                    }
                    _entityTypesEnumerated = true;
                    _entityTypeElements = null;
                }
                else
                {
                    foreach (StoreEntityType et in _storeEntityTypes.Values)
                    {
                        yield return et;
                    }
                }
            }
        }

        void et_Removed(object sender, EventArgs e)
        {
            _storeEntityTypes.Remove(((StoreEntityType)sender).Name);
        }

        void et_NameChanged(object sender, NameChangeArgs e)
        {
            if (_storeEntityTypes.ContainsKey(e.OldName))
            {
                _storeEntityTypes.Remove(e.OldName);
                _storeEntityTypes.Add(e.NewName, (StoreEntityType)sender);
            }
        }

        private XmlNodeList _functionElements = null;
        private XmlNodeList FunctionElements
        {
            get
            {
                if (_functionElements == null)
                {
                    _functionElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:Function", NSM);
                }
                return _functionElements;
            }
        }

        private bool _functionsEnumerated = false;
        private Dictionary<string, StoreFunction> _storeFunctions = new Dictionary<string, StoreFunction>();

        /// <summary>
        /// Enumeration of all store functions in the storage model.
        /// </summary>
        public IEnumerable<StoreFunction> Functions
        {
            get
            {
                if (_functionsEnumerated == false)
                {
                    foreach (XmlElement functionElement in FunctionElements)
                    {
                        string sfnName = functionElement.GetAttribute("Name");
                        StoreFunction sfn = null;
                        if (!_storeFunctions.ContainsKey(sfnName))
                        {
                            sfn = new StoreFunction(ParentFile, this, functionElement);
                            sfn.NameChanged += new EventHandler<NameChangeArgs>(sfn_NameChanged);
                            sfn.Removed += new EventHandler(sfn_Removed);
                            _storeFunctions.Add(sfnName, sfn);
                        }
                        else
                        {
                            sfn = _storeFunctions[sfnName];
                        }
                        yield return sfn;
                    }
                    _functionsEnumerated = true;
                    _functionElements = null;
                }
                else
                {
                    foreach (StoreFunction sfn in _storeFunctions.Values)
                    {
                        yield return sfn;
                    }
                }
            }
        }

        void sfn_Removed(object sender, EventArgs e)
        {
            _storeFunctions.Remove(((StoreFunction)sender).Name);
        }

        void sfn_NameChanged(object sender, NameChangeArgs e)
        {
            if (_storeFunctions.ContainsKey(e.OldName))
            {
                _storeFunctions.Remove(e.OldName);
                _storeFunctions.Add(e.NewName, (StoreFunction)sender);
            }
        }

        private StorageModelQueries _queries = null;

        /// <summary>
        /// Commonly used queries against the storage model.
        /// </summary>
        public StorageModelQueries Queries
        {
            get
            {
                if (_queries == null)
                {
                    _queries = new StorageModelQueries(this);
                }
                return _queries;
            }
        }

        /// <summary>
        /// Adds a store entityset to the model, based on an existing conceptual model entity set and entity type.
        /// </summary>
        /// <param name="modelEntitySet">Conceptual model entity set</param>
        /// <param name="modelEntityType">Conceptual model entity type</param>
        /// <returns>A new StoreEntitySet object.</returns>
        public StoreEntitySet AddEntitySet(ModelEntitySet modelEntitySet, ModelEntityType modelEntityType)
        {
            return AddEntitySet(modelEntitySet, modelEntityType, "dbo", modelEntitySet.Name);
        }

        /// <summary>
        /// Adds a store entityset to the model, based on an existing conceptual model entity set and entity type.
        /// </summary>
        /// <param name="modelEntitySet">Conceptual model entity set</param>
        /// <param name="modelEntityType">Conceptual model entity type</param>
        /// <param name="schemaName">Database schemaname for the new entity set.</param>
        /// <param name="tableName">Tablename for the new entityset.</param>
        /// <returns>A new StoreEntitySet object.</returns>
        public StoreEntitySet AddEntitySet(ModelEntitySet modelEntitySet, ModelEntityType modelEntityType, string schemaName, string tableName)
        {
            string entitySetName = null;
            if (!modelEntityType.HasBaseType)
            {
                entitySetName = modelEntitySet.Name;
            }
            else
            {
                entitySetName = modelEntityType.TopLevelBaseType.EntitySet.Name + "_" + modelEntityType.Name;// string.Join("_", modelEntityType.BaseTypes.Select(tn => tn.Name));
            }

            StoreEntitySet storeEntitySet = this.AddEntitySet(entitySetName);
            storeEntitySet.StoreType = StoreTypeEnum.Table;
            storeEntitySet.Schema = schemaName;
            storeEntitySet.TableName = tableName;
            if (!string.IsNullOrEmpty(modelEntitySet.ShortDescription))
            {
                storeEntitySet.ShortDescription = modelEntitySet.ShortDescription;
            }
            if (!string.IsNullOrEmpty(modelEntitySet.LongDescription))
            {
                storeEntitySet.LongDescription = modelEntitySet.LongDescription;
            }
            return storeEntitySet;
        }

        /// <summary>
        /// Adds a new entity type to the storage model, based on an existing conceptual model entity type.
        /// </summary>
        /// <param name="modelEntityType">Conceptual model entity type to use as a base.</param>
        /// <returns>A new StoreEntityType object.</returns>
        public StoreEntityType AddEntityType(ModelEntityType modelEntityType)
        {
            StoreEntityType storeEntityType = this.AddEntityType(modelEntityType.Name);
            if (!string.IsNullOrEmpty(modelEntityType.ShortDescription))
            {
                storeEntityType.ShortDescription = modelEntityType.ShortDescription;
            }
            if (!string.IsNullOrEmpty(modelEntityType.LongDescription))
            {
                storeEntityType.LongDescription = modelEntityType.LongDescription;
            }
            return storeEntityType;
        }

        /// <summary>
        /// Retrieves an existing entityset by name, or creates a new one if no matching entity set exist in the storage model.
        /// </summary>
        /// <param name="entitySetName">Name of the entity set to get or create.</param>
        /// <returns>A StoreEntitySet object.</returns>
        public StoreEntitySet GetOrCreateEntitySet(string entitySetName)
        {
            StoreEntitySet storeEntitySet = EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName, StringComparison.InvariantCultureIgnoreCase));
            if (storeEntitySet == null)
            {
                storeEntitySet = AddEntitySet(entitySetName);
            }
            return storeEntitySet;
        }

        /// <summary>
        /// Retrieves an existing entity type by name, or creates a new one if no matching entity type exist in the storage model.
        /// </summary>
        /// <param name="entityTypeName">Name of the entity type to get or create.</param>
        /// <returns>A StoreEntityType object.</returns>
        public StoreEntityType GetOrCreateEntityType(string entityTypeName)
        {
            StoreEntityType storeEntityType = EntityTypes.FirstOrDefault(et => et.Name.Equals(entityTypeName, StringComparison.InvariantCultureIgnoreCase));
            if (storeEntityType == null)
            {
                storeEntityType = AddEntityType(entityTypeName);
            }
            return storeEntityType;
        }

        /// <summary>
        /// Retrieves an existing entityset based on model entityset/type, or creates a new one if a match can not be found.
        /// </summary>
        /// <param name="modelEntitySet">Conceptual model entity set to match.</param>
        /// <param name="modelEntityType">Conceptual model entity type to match.</param>
        /// <returns>A StoreEntitySet object.</returns>
        public StoreEntitySet GetOrCreateEntitySet(ModelEntitySet modelEntitySet, ModelEntityType modelEntityType)
        {
            return GetOrCreateEntitySet(modelEntitySet, modelEntityType, "dbo", modelEntitySet.Name);
        }

        /// <summary>
        /// Retrieves an existing entityset based on a conceptual model entityset/type, or creates a new one if a match can not be found.
        /// </summary>
        /// <param name="modelEntitySet">Conceptual model entity set to match.</param>
        /// <param name="modelEntityType">Conceptual model entity type to match.</param>
        /// <param name="schemaName">Database schemaname for the new entity set.</param>
        /// <param name="tableName">Tablename for the new entityset.</param>
        /// <returns>A StoreEntitySet object.</returns>
        public StoreEntitySet GetOrCreateEntitySet(ModelEntitySet modelEntitySet, ModelEntityType modelEntityType, string schemaName, string tableName)
        {
            string entitySetName = null;
            if (!modelEntityType.HasBaseType)
            {
                entitySetName = modelEntitySet.Name;
            }
            else
            {
                entitySetName = modelEntityType.TopLevelBaseType.EntitySet.Name + "_" + modelEntityType.Name;
            }

            StoreEntitySet storeEntitySet = EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName, StringComparison.InvariantCultureIgnoreCase));
            if (storeEntitySet == null)
            {
                storeEntitySet = AddEntitySet(modelEntitySet, modelEntityType, schemaName, tableName);
            }
            return storeEntitySet;
        }

        /// <summary>
        /// Retrieves an existing entitytype based on a conceptual model entittype, or creates a new one if a match can not be found.
        /// </summary>
        /// <param name="modelEntityType">Conceptual model entity type to match.</param>
        /// <returns>A StoreEntityType object.</returns>
        public StoreEntityType GetOrCreateEntityType(ModelEntityType modelEntityType)
        {
            StoreEntityType storeEntityType = EntityTypes.FirstOrDefault(et => et.Name.Equals(modelEntityType.Name, StringComparison.InvariantCultureIgnoreCase));
            if (storeEntityType == null)
            {
                storeEntityType = AddEntityType(modelEntityType);
            }
            return storeEntityType;
        }

        /// <summary>
        /// Removes all model objects from the storage model.
        /// </summary>
        public void Clear()
        {
            foreach (StoreAssociationSet sas in this.AssociationSets.ToList())
            {
                sas.Remove();
            }
            foreach (StoreFunction sf in this.Functions.ToList())
            {
                sf.Remove();
            }
            foreach (StoreEntityType set in this.EntityTypes.ToList())
            {
                set.Remove();
            }
            foreach (StoreEntitySet ses in this.EntitySets.ToList())
            {
                ses.Remove();
            }
        }

        /// <summary>
        /// Contains exceptions encountered during model parsing
        /// </summary>
        public IEnumerable<Exception> ModelErrors
        {
            get
            {
                return _modelErrors.AsEnumerable();
            }
        }
    }
}
