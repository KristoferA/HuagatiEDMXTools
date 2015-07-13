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
    /// Represents the conceptual model (CSDL) within an EDMX file, and enumerations and methods for exploring and modifying the conceptual model. 
    /// </summary>
    public class ConceptualModel : EDMXMember
    {
        private XmlElement _conceptualModelElement = null;
        private XmlElement _schemaElement = null;
        private XmlElement _entityContainerElement = null;
        private List<Exception> _modelErrors = new List<Exception>();

        internal ConceptualModel(EDMXFile parentFile)
            : base(parentFile)
        {
            _conceptualModelElement = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels", NSM);
            _schemaElement = (XmlElement)_conceptualModelElement.SelectSingleNode("edm:Schema", NSM);

            if (_schemaElement == null)
            {
                _schemaElement = EDMXDocument.CreateElement("Schema", NameSpaceURIcsdl);
                _conceptualModelElement.AppendChild(_schemaElement);
            }

            _entityContainerElement = (XmlElement)_schemaElement.SelectSingleNode("edm:EntityContainer", NSM);
            if (_entityContainerElement == null)
            {
                _entityContainerElement = EDMXDocument.CreateElement("EntityContainer", NameSpaceURIcsdl);
                _schemaElement.AppendChild(_entityContainerElement);
            }
        }

        /// <summary>
        /// Conceptual model namespace name
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
        /// Conceptual model alias name / short name
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
        /// Controls whether the generated object context supports lazy loading or not
        /// </summary>
        public bool LazyLoadingEnabled
        {
            get
            {
                return (_entityContainerElement.GetAttribute("LazyLoadingEnabled", NameSpaceURIannotation).Equals("true", StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                _entityContainerElement.SetAttribute("LazyLoadingEnabled", NameSpaceURIannotation, value.ToLString());
            }
        }

        /// <summary>
        /// Creates and adds a new conceptual model entity set.
        /// </summary>
        /// <param name="name">Entity set name for the new entity</param>
        /// <returns>A ModelEntitySet instance corresponding to the new entity set.</returns>
        public ModelEntitySet AddEntitySet(string name)
        {
            try
            {
                if (!EntitySets.Where(es => es.Name == name).Any())
                {
                    ModelEntitySet es = new ModelEntitySet(ParentFile, this, name);
                    _modelEntitySets.Add(name, es);
                    es.NameChanged += new EventHandler<NameChangeArgs>(es_NameChanged);
                    es.Removed += new EventHandler(es_Removed);
                    return es;
                }
                else
                {
                    throw new ArgumentException("An entity set with the name " + name + " already exist in the model.");
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }

        }

        /// <summary>
        /// Creates and adds a new conceptual model entity type.
        /// </summary>
        /// <param name="name">Entity type name for the new entity type.</param>
        /// <returns>A ModelEntityType instance corresponding to the new entity type.</returns>
        public ModelEntityType AddEntityType(string name)
        {
            return AddEntityType(name, null);
        }

        /// <summary>
        /// Creates and adds a new conceptual model entity type inheriting from an existing entity type.
        /// </summary>
        /// <param name="name">Entity type name for the new entity type.</param>
        /// <param name="baseType">Base type that this entity type inherits from.</param>
        /// <returns>A ModelEntityType instance corresponding to the new entity type.</returns>
        public ModelEntityType AddEntityType(string name, ModelEntityType baseType)
        {
            try
            {
                if (!EntityTypes.Any(et => et.Name == name)
                    && !ComplexTypes.Any(ct => ct.Name == name))
                {
                    ModelEntityType et = new ModelEntityType(ParentFile, this, name, baseType);
                    _modelEntityTypes.Add(name, et);
                    et.NameChanged += new EventHandler<NameChangeArgs>(et_NameChanged);
                    et.Removed += new EventHandler(et_Removed);
                    return et;
                }
                else
                {
                    throw new ArgumentException("A type with the name " + name + " already exist in the model.");
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Creates and adds a new complex type to the conceptual model.
        /// </summary>
        /// <param name="name">Type name for the new complex type.</param>
        /// <returns>A ModelComplexType corresponding to the new complex type.</returns>
        public ModelComplexType AddComplexType(string name)
        {
            try
            {
                if (!EntityTypes.Any(et => et.Name == name)
                    && !ComplexTypes.Any(ct => ct.Name == name))
                {
                    ModelComplexType ct = new ModelComplexType(ParentFile, this, name);
                    _modelComplexTypes.Add(name, ct);
                    ct.NameChanged += new EventHandler<NameChangeArgs>(ct_NameChanged);
                    ct.Removed += new EventHandler(ct_Removed);
                    return ct;
                }
                else
                {
                    throw new ArgumentException("A type with the name " + name + " already exist in the model.");
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Adds a new association set and association between two conceptual model entities.
        /// </summary>
        /// <param name="name">Name of the association and association set.</param>
        /// <param name="fromEntitySet">Entity set where the entity set originates from. For one-to-many associations, this is typically the many-side of the association.</param>
        /// <param name="toEntitySet">Entity set that the entity set references.</param>
        /// <param name="fromEntityType">Entity type that the association originates from. This should be the entity type or a descendant of the entity type for the entity set passed in the fromEntitySet parameter.</param>
        /// <param name="toEntityType">Entity type that the association references. This should be the entity type or a descendant of the entity type that the entity set passed in the toEntitySet parameter.</param>
        /// <param name="fromMultiplicity">Multiplicity for the from entity set.</param>
        /// <param name="toMultiplicity">Multiplicity for the to entity set.</param>
        /// <param name="fromNavigationProperty">Name for the conceptual model navigation property in the fromEntityType for the association.</param>
        /// <param name="toNavigationProperty">Name for the conceptual model navigation property in the toEntityType for the association.</param>
        /// <param name="keys">A list of the entity key pairs for the association. This is a list containing pairs of ModelMemberProperty instances from the From and To entity types.</param>
        /// <returns></returns>
        public ModelAssociationSet AddAssociation(string name, ModelEntitySet fromEntitySet, ModelEntitySet toEntitySet, ModelEntityType fromEntityType, ModelEntityType toEntityType, MultiplicityTypeEnum fromMultiplicity, MultiplicityTypeEnum toMultiplicity, string fromNavigationProperty, string toNavigationProperty, List<Tuple<ModelMemberProperty, ModelMemberProperty>> keys)
        {
            try
            {
                if (!AssociationSets.Where(et => et.Name == name).Any())
                {
                    ModelAssociationSet mas = new ModelAssociationSet(this.ParentFile, this, name, fromEntitySet, toEntitySet, fromEntityType, toEntityType, fromMultiplicity, toMultiplicity, fromNavigationProperty, toNavigationProperty, keys);
                    _modelAssociationSets.Add(mas.Name, mas);
                    mas.NameChanged += new EventHandler<NameChangeArgs>(aset_NameChanged);
                    mas.Removed += new EventHandler(aset_Removed);
                    return mas;
                }
                else
                {
                    throw new ArgumentException("An association named " + name + " already exists in the model.");
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        private XmlNodeList _entitySetElements = null;
        private XmlNodeList EntitySetElements
        {
            get
            {
                if (_entitySetElements == null)
                {
                    _entitySetElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer/edm:EntitySet", NSM);
                }
                return _entitySetElements;
            }
        }

        private bool _entitySetsEnumerated = false;
        private Dictionary<string, ModelEntitySet> _modelEntitySets = new Dictionary<string, ModelEntitySet>();

        /// <summary>
        /// Enumeration of all entity sets contained within the current conceptual model.
        /// </summary>
        public IEnumerable<ModelEntitySet> EntitySets
        {
            get
            {
                try
                {
                    if (_entitySetsEnumerated == false)
                    {
                        foreach (XmlElement entitySetElement in EntitySetElements)
                        {
                            string esName = entitySetElement.GetAttribute("Name");
                            ModelEntitySet es = null;
                            if (!_modelEntitySets.ContainsKey(esName))
                            {
                                es = new ModelEntitySet(ParentFile, this, entitySetElement);
                                es.NameChanged += new EventHandler<NameChangeArgs>(es_NameChanged);
                                es.Removed += new EventHandler(es_Removed);
                                _modelEntitySets.Add(esName, es);
                            }
                            else
                            {
                                es = _modelEntitySets[esName];
                            }
                            yield return es;
                        }
                        _entitySetsEnumerated = true;
                        _entitySetElements = null;
                    }
                    else
                    {
                        foreach (ModelEntitySet es in _modelEntitySets.Values)
                        {
                            yield return es;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void es_Removed(object sender, EventArgs e)
        {
            _modelEntitySets.Remove(((ModelEntitySet)sender).Name);
        }

        void es_NameChanged(object sender, NameChangeArgs e)
        {
            if (_modelEntitySets.ContainsKey(e.OldName))
            {
                _modelEntitySets.Remove(e.OldName);
                _modelEntitySets.Add(e.NewName, (ModelEntitySet)sender);
            }
        }

        private XmlNodeList _entityTypeElements = null;
        private XmlNodeList EntityTypeElements
        {
            get
            {
                if (_entityTypeElements == null)
                {
                    _entityTypeElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityType", NSM);
                }
                return _entityTypeElements;
            }
        }

        private bool _entityTypesEnumerated = false;
        private Dictionary<string, ModelEntityType> _modelEntityTypes = new Dictionary<string, ModelEntityType>();

        /// <summary>
        /// Enumeration of the all entity types contained within the current conceptual model.
        /// </summary>
        public IEnumerable<ModelEntityType> EntityTypes
        {
            get
            {
                try
                {
                    if (_entityTypesEnumerated == false)
                    {
                        foreach (XmlElement entityTypeElement in EntityTypeElements)
                        {
                            string etName = entityTypeElement.GetAttribute("Name");
                            ModelEntityType et = null;
                            if (!_modelEntityTypes.ContainsKey(etName))
                            {
                                et = new ModelEntityType(ParentFile, this, entityTypeElement);
                                et.NameChanged += new EventHandler<NameChangeArgs>(et_NameChanged);
                                et.Removed += new EventHandler(et_Removed);
                                _modelEntityTypes.Add(etName, et);
                            }
                            else
                            {
                                et = _modelEntityTypes[etName];
                            }
                            yield return et;
                        }
                        _entityTypesEnumerated = true;
                        _entityTypeElements = null;
                    }
                    else
                    {
                        foreach (ModelEntityType et in _modelEntityTypes.Values)
                        {
                            yield return et;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void et_Removed(object sender, EventArgs e)
        {
            _modelEntityTypes.Remove(((ModelEntityType)sender).Name);
        }

        void et_NameChanged(object sender, NameChangeArgs e)
        {
            if (_modelEntityTypes.ContainsKey(e.OldName))
            {
                _modelEntityTypes.Remove(e.OldName);
                _modelEntityTypes.Add(e.NewName, (ModelEntityType)sender);
            }
        }

        private XmlNodeList _associationSetElements = null;
        private XmlNodeList AssociationSetElements
        {
            get
            {
                if (_associationSetElements == null)
                {
                    _associationSetElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer/edm:AssociationSet", NSM);
                }
                return _associationSetElements;
            }
        }

        private bool _associationSetsEnumerated = false;
        private Dictionary<string, ModelAssociationSet> _modelAssociationSets = new Dictionary<string, ModelAssociationSet>();

        /// <summary>
        /// Enumeration of all association sets contained within the current conceptual model.
        /// </summary>
        public IEnumerable<ModelAssociationSet> AssociationSets
        {
            get
            {
                try
                {
                    if (_associationSetsEnumerated == false)
                    {
                        foreach (XmlElement associationSetElement in AssociationSetElements)
                        {
                            string asName = associationSetElement.GetAttribute("Name");
                            ModelAssociationSet aset = null;
                            if (!_modelAssociationSets.ContainsKey(asName))
                            {
                                try
                                {
                                    aset = new ModelAssociationSet(ParentFile, this, associationSetElement);
                                    aset.NameChanged += new EventHandler<NameChangeArgs>(aset_NameChanged);
                                    aset.Removed += new EventHandler(aset_Removed);
                                    _modelAssociationSets.Add(asName, aset);
                                }
                                catch (InvalidModelObjectException ex)
                                {
                                    this._modelErrors.Add(ex);
                                }
                            }
                            else
                            {
                                aset = _modelAssociationSets[asName];
                            }
                            yield return aset;
                        }
                        _associationSetsEnumerated = true;
                        _associationSetElements = null;
                    }
                    else
                    {
                        foreach (ModelAssociationSet aset in _modelAssociationSets.Values)
                        {
                            yield return aset;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void aset_Removed(object sender, EventArgs e)
        {
            _modelAssociationSets.Remove(((ModelAssociationSet)sender).Name);
        }

        void aset_NameChanged(object sender, NameChangeArgs e)
        {
            if (_modelAssociationSets.ContainsKey(e.OldName))
            {
                _modelAssociationSets.Remove(e.OldName);
                _modelAssociationSets.Add(e.NewName, (ModelAssociationSet)sender);
            }
        }

        private XmlNodeList _complexTypeElements = null;
        private XmlNodeList ComplexTypeElements
        {
            get
            {
                if (_complexTypeElements == null)
                {
                    _complexTypeElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:ComplexType", NSM);
                }
                return _complexTypeElements;
            }
        }

        private bool _complexTypesEnumerated = false;
        private Dictionary<string, ModelComplexType> _modelComplexTypes = new Dictionary<string, ModelComplexType>();

        /// <summary>
        /// Enumeration of all complex types contained within the current conceptual model.
        /// </summary>
        public IEnumerable<ModelComplexType> ComplexTypes
        {
            get
            {
                try
                {
                    if (_complexTypesEnumerated == false)
                    {
                        foreach (XmlElement complexTypeElement in ComplexTypeElements)
                        {
                            string ctName = complexTypeElement.GetAttribute("Name");
                            ModelComplexType ct = null;
                            if (!_modelComplexTypes.ContainsKey(ctName))
                            {
                                ct = new ModelComplexType(ParentFile, this, complexTypeElement);
                                ct.NameChanged += new EventHandler<NameChangeArgs>(ct_NameChanged);
                                ct.Removed += new EventHandler(ct_Removed);
                                _modelComplexTypes.Add(ctName, ct);
                            }
                            else
                            {
                                ct = _modelComplexTypes[ctName];
                            }
                            yield return ct;
                        }
                        _complexTypesEnumerated = true;
                        _complexTypeElements = null;
                    }
                    else
                    {
                        foreach (ModelComplexType ct in _modelComplexTypes.Values)
                        {
                            yield return ct;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void ct_Removed(object sender, EventArgs e)
        {
            _modelComplexTypes.Remove(((ModelComplexType)sender).Name);
        }

        void ct_NameChanged(object sender, NameChangeArgs e)
        {
            if (_modelComplexTypes.ContainsKey(e.OldName))
            {
                _modelComplexTypes.Remove(e.OldName);
                _modelComplexTypes.Add(e.NewName, (ModelComplexType)sender);
            }

            //update all entity type member properties referencing the complex type...
            foreach (ModelMemberProperty mmp in EntityTypes.SelectMany(et => et.MemberProperties.Where(mp => mp.TypeName == this.Namespace + "." + e.OldName)))
            {
                mmp.TypeName = e.NewName;
            }

            //update all complex type member properties referencing the complex type...
            foreach (ModelMemberProperty mmp in ComplexTypes.SelectMany(ct => ct.MemberProperties.Where(mp => mp.TypeName == this.Namespace + "." + e.OldName)))
            {
                mmp.TypeName = e.NewName;
            }
        }

        private XmlNodeList _functionImportElements = null;
        private XmlNodeList FunctionImportElements
        {
            get
            {
                if (_functionImportElements == null)
                {
                    _functionImportElements = EDMXDocument.DocumentElement.SelectNodes("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer/edm:FunctionImport", NSM);
                }
                return _functionImportElements;
            }
        }

        private bool _functionImportsEnumerated = false;
        private Dictionary<string, ModelFunction> _modelFunctionImports = new Dictionary<string, ModelFunction>();

        /// <summary>
        /// Enumeration of all complex types contained within the current conceptual model.
        /// </summary>
        public IEnumerable<ModelFunction> FunctionImports
        {
            get
            {
                try
                {
                    if (_functionImportsEnumerated == false)
                    {
                        foreach (XmlElement functionImportElement in FunctionImportElements)
                        {
                            string mfName = functionImportElement.GetAttribute("Name");
                            ModelFunction mf = null;
                            if (!_modelFunctionImports.ContainsKey(mfName))
                            {
                                mf = new ModelFunction(ParentFile, this, functionImportElement);
                                mf.NameChanged += new EventHandler<NameChangeArgs>(mf_NameChanged);
                                mf.Removed += new EventHandler(mf_Removed);
                                _modelFunctionImports.Add(mfName, mf);
                            }
                            else
                            {
                                mf = _modelFunctionImports[mfName];
                            }
                            yield return mf;
                        }
                        _functionImportsEnumerated = true;
                        _functionImportElements = null;
                    }
                    else
                    {
                        foreach (ModelFunction mf in _modelFunctionImports.Values)
                        {
                            yield return mf;
                        }
                    }
                }
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
                }
            }
        }

        void mf_Removed(object sender, EventArgs e)
        {
            _modelFunctionImports.Remove(((ModelFunction)sender).Name);
        }

        void mf_NameChanged(object sender, NameChangeArgs e)
        {
            if (_modelFunctionImports.ContainsKey(e.OldName))
            {
                _modelFunctionImports.Remove(e.OldName);
                _modelFunctionImports.Add(e.NewName, (ModelFunction)sender);
            }
        }

        /// <summary>
        /// Adds a function import to the model.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ModelFunction AddFunctionImport(string name)
        {
            try
            {
                if (!EntityTypes.Any(et => et.Name == name)
                    && !FunctionImports.Any(mf => mf.Name == name))
                {
                    ModelFunction mf = new ModelFunction(ParentFile, this, name);
                    _modelFunctionImports.Add(name, mf);
                    mf.NameChanged += new EventHandler<NameChangeArgs>(mf_NameChanged);
                    mf.Removed += new EventHandler(mf_Removed);
                    return mf;
                }
                else
                {
                    throw new ArgumentException("A function with the name " + name + " already exist in the model.");
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        private ConceptualModelQueries _queries = null;

        /// <summary>
        /// Exposes a number of common IQueryable queries based on conceptual model data.
        /// </summary>
        public ConceptualModelQueries Queries
        {
            get
            {
                try
                {
                    if (_queries == null)
                    {
                        _queries = new ConceptualModelQueries(this);
                    }
                    return _queries;
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
                            ex.Data.Add("EDMXObjectName", this.ContainerName);
                        }
                    }
                    catch { }
                    throw;
                }
            }
        }

        /// <summary>
        /// Retrieves an existing entity set, or creates a new one if no matching entityset is found.
        /// </summary>
        /// <param name="entitySetName">Entity set name.</param>
        /// <returns>A ModelEntitySet object.</returns>
        public ModelEntitySet GetOrCreateEntitySet(string entitySetName)
        {
            try
            {
                ModelEntitySet modelEntitySet = EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName));
                if (modelEntitySet == null)
                {
                    modelEntitySet = AddEntitySet(entitySetName);
                }
                return modelEntitySet;
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Retrieves an existing entity type, or creates a new one if no matching entity type is found.
        /// </summary>
        /// <param name="entityTypeName">Entity type name.</param>
        /// <returns>A ModelEntityType object.</returns>
        public ModelEntityType GetOrCreateEntityType(string entityTypeName)
        {
            try
            {
                ModelEntityType modelEntityType = EntityTypes.FirstOrDefault(et => et.Name.Equals(entityTypeName));
                if (modelEntityType == null)
                {
                    modelEntityType = AddEntityType(entityTypeName);
                }
                return modelEntityType;
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Retrieves an existing entity type, or creates a new one if no matching entity type is found.
        /// </summary>
        /// <param name="entityTypeName">Entity type name.</param>
        /// <param name="baseType">Base type that this entity type will inherit from if creating a new type.</param>
        /// <returns>A ModelEntityType object.</returns>
        public ModelEntityType GetOrCreateEntityType(string entityTypeName, ModelEntityType baseType)
        {
            try
            {
                ModelEntityType modelEntityType = EntityTypes.FirstOrDefault(et => et.Name.Equals(entityTypeName));
                if (modelEntityType == null)
                {
                    modelEntityType = AddEntityType(entityTypeName, baseType);
                }
                return modelEntityType;
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Removes all members from the conceptual model.
        /// </summary>
        public void Clear()
        {
            try
            {
                foreach (ModelAssociationSet mas in AssociationSets.ToList())
                {
                    mas.Remove();
                }
                foreach (ModelComplexType mct in ComplexTypes.ToList())
                {
                    mct.Remove();
                }
                foreach (ModelEntityType met in EntityTypes.ToList())
                {
                    met.Remove();
                }
                foreach (ModelEntitySet mes in EntitySets.ToList())
                {
                    mes.Remove();
                }
                foreach (ModelFunction mf in FunctionImports.ToList())
                {
                    mf.Remove();
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
                        ex.Data.Add("EDMXObjectName", this.ContainerName);
                    }
                }
                catch { }
                throw;
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
