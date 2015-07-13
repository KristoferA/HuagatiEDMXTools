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
    /// Represents a conceptual model entity type.
    /// </summary>
    public class ModelEntityType : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private ConceptualModel _conceptualModel = null;
        private XmlElement _entityTypeElement = null;

        internal ModelEntityType(EDMXFile parentFile, ConceptualModel conceptualModel, System.Xml.XmlElement entityTypeElement)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;
            _entityTypeElement = entityTypeElement;
        }

        internal ModelEntityType(EDMXFile parentFile, ConceptualModel conceptualModel, string name, ModelEntityType baseType)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;

            //create the entity type element
            XmlElement schemaContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema", NSM);
            _entityTypeElement = EDMXDocument.CreateElement("EntityType", NameSpaceURIcsdl);
            schemaContainer.AppendChild(_entityTypeElement);

            BaseType = baseType;

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
                    if (DiagramShape != null)
                    {
                        DiagramShape.Remove();
                    }

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
                        _memberPropertyElements = _entityTypeElement.SelectNodes("edm:Property", NSM);
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
        /// Enumeration of scalar members in this entity type.
        /// </summary>
        public IEnumerable<ModelMemberProperty> MemberProperties
        {
            get
            {
                try
                {
                    if (_memberPropertiesEnumerated == false)
                    {
                        //get all base type scalar members
                        if (BaseType != null)
                        {
                            foreach (ModelMemberProperty mp in BaseType.MemberProperties)
                            {
                                _memberProperties.Add(mp.Name, mp);
                                yield return mp;
                            }
                        }

                        //get all member properties
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
                finally
                {
                    //if possible to get exception data: ExceptionTools.AddExceptionData(ex, this);
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

        private ModelEntityType _baseType = null;

        /// <summary>
        /// Immediate base type for this type, if inheriting from other model entity type. Null if this type has no base type.
        /// </summary>
        public ModelEntityType BaseType
        {
            get
            {
                try
                {
                    if (_baseType == null)
                    {
                        string baseTypeName = _entityTypeElement.GetAttribute("BaseType");
                        _baseType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == baseTypeName || et.AliasName == baseTypeName);
                    }
                    return _baseType;
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
            private set
            {
                _baseType = value;
                if (value != null)
                {
                    _entityTypeElement.SetAttribute("BaseType", value.FullName);
                }
                else
                {
                    if (_entityTypeElement.HasAttribute("BaseType"))
                    {
                        _entityTypeElement.RemoveAttribute("BaseType");
                    }
                }
            }
        }

        /// <summary>
        /// True if this entity type has a base type, false if not.
        /// </summary>
        public bool HasBaseType
        {
            get
            {
                return (BaseType != null);
            }
        }

        private List<ModelEntityType> _baseTypes = null;

        /// <summary>
        /// Enumeration returning all base types above this type, starting with the immediate base type.
        /// </summary>
        public IEnumerable<ModelEntityType> BaseTypes
        {
            get
            {
                try
                {
                    return BaseTypesInternal;
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

        private IEnumerable<ModelEntityType> BaseTypesInternal
        {
            get
            {
                if (_baseTypes == null)
                {
                    _baseTypes = new List<ModelEntityType>();
                    if (BaseType != null)
                    {
                        _baseTypes.Add(BaseType);
                        yield return BaseType;
                        foreach (ModelEntityType bt in BaseType.BaseTypes)
                        {
                            _baseTypes.Add(bt);
                            yield return bt;
                        }
                    }
                }
                else
                {
                    foreach (ModelEntityType bt in _baseTypes)
                    {
                        yield return bt;
                    }
                }
            }
        }

        /// <summary>
        /// The top-level base type for this entity; the entity type highest up in the inheritance chain.
        /// </summary>
        public ModelEntityType TopLevelBaseType
        {
            get
            {
                try
                {
                    return BaseTypes.FirstOrDefault(et => et.HasBaseType == false);
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
        /// True if the specified entity type is a parent somewhere in the inheritance chain for this entity type.
        /// </summary>
        /// <param name="entityType">Entity type to look for in the inheritance chain.</param>
        /// <returns>True if found, false if not.</returns>
        public bool IsSubtypeOf(ModelEntityType entityType)
        {
            try
            {
                return BaseTypes.Any(t => t == entityType);
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
        /// True if the specified entity type inherits from this entity type.
        /// </summary>
        /// <param name="entityType">Entity type to look for in the subtypes of this entity.</param>
        /// <returns>True if found, false if not.</returns>
        public bool InheritsFrom(ModelEntityType entityType)
        {
            try
            {
                return SubTypes.Any(t => t == entityType);
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
        /// Enumeration returning all sub types inheriting directly or indirectly from this type.
        /// </summary>
        public IEnumerable<ModelEntityType> SubTypes
        {
            get
            {
                try
                {
                    return SubTypesInternal;
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

        private IEnumerable<ModelEntityType> SubTypesInternal
        {
            get
            {
                foreach (ModelEntityType et in ParentFile.ConceptualModel.EntityTypes.Where(et => et.BaseType == this))
                {
                    yield return et;
                    foreach (ModelEntityType st in et.SubTypes)
                    {
                        yield return st;
                    }
                }
            }
        }

        private bool _hasSubTypes = false;

        /// <summary>
        /// True if this type has subtypes, false if not.
        /// </summary>
        public bool HasSubTypes
        {
            get
            {
                try
                {
                    if (_hasSubTypes == false)
                    {
                        _hasSubTypes = SubTypes.Any();
                    }
                    return _hasSubTypes;
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
        /// Adds a scalar member to this entity type.
        /// </summary>
        /// <param name="name">Name of the new member property.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty AddMember(string name)
        {
            return AddMember(name, typeof(string), true);
        }

        /// <summary>
        /// Retrieves a scalar member in this entity type, or creates it if not found
        /// </summary>
        /// <param name="name">Name of the member property.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty GetOrCreateMember(string name)
        {
            return GetOrCreateMember(name, typeof(string), true);
        }

        /// <summary>
        /// Adds a scalar member to this entity type.
        /// </summary>
        /// <param name="name">Name of the new member property.</param>
        /// <param name="type">Type of the new member property.</param>
        /// <param name="nullable">Nullable; true or false.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty AddMember(string name, Type type, bool nullable)
        {
            return AddMember(name, type, nullable, -1);
        }

        /// <summary>
        /// Retrieves a scalar member in this entity type, or creates it if not found
        /// </summary>
        /// <param name="name">Name of the member property.</param>
        /// <param name="type">Type of the new member property.</param>
        /// <param name="nullable">Nullable; true or false.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty GetOrCreateMember(string name, Type type, bool nullable)
        {
            return GetOrCreateMember(name, type, nullable, -1);
        }

        /// <summary>
        /// Adds a scalar member to this entity type.
        /// </summary>
        /// <param name="name">Name of the new member property.</param>
        /// <param name="type">Type of the new member property.</param>
        /// <param name="nullable">Nullable; true or false.</param>
        /// <param name="ordinal">Ordinal position within the entity type.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty AddMember(string name, Type type, bool nullable, int ordinal)
        {
            try
            {
                if (!MemberProperties.Where(mp => mp.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Any()
                    && !NavigationProperties.Any(np => np.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    && name != this.Name)
                {
                    ModelMemberProperty mp = new ModelMemberProperty(base.ParentFile, this, name, ordinal, _entityTypeElement);
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

        /// <summary>
        /// Retrieves a scalar member in this entity type, or creates it if not found
        /// </summary>
        /// <param name="name">Name of the member property.</param>
        /// <param name="type">Type of the new member property.</param>
        /// <param name="nullable">Nullable; true or false.</param>
        /// <param name="ordinal">Ordinal position within the entity type.</param>
        /// <returns>A ModelMemberProperty object.</returns>
        public ModelMemberProperty GetOrCreateMember(string name, Type type, bool nullable, int ordinal)
        {
            try
            {
                ModelMemberProperty modelMemberProperty = MemberProperties.FirstOrDefault(mp => mp.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (modelMemberProperty == null)
                {
                    modelMemberProperty = AddMember(name, type, nullable, ordinal);
                }
                return modelMemberProperty;
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

        private ModelEntitySet _entitySet = null;

        /// <summary>
        /// Entity set that this entity type is a member of.
        /// </summary>
        public ModelEntitySet EntitySet
        {
            get
            {
                try
                {
                    if (_entitySet == null)
                    {
                        _entitySet = _conceptualModel.EntitySets.FirstOrDefault(es => es.EntityTypeName == this.FullName || es.EntityTypeName == this.AliasName);
                        if (_entitySet == null && this.HasBaseType)
                        {
                            ModelEntityType baseType = this.TopLevelBaseType;
                            _entitySet = _conceptualModel.EntitySets.FirstOrDefault(es => es.EntityTypeName == baseType.FullName || es.EntityTypeName == baseType.AliasName);
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

        /// <summary>
        /// Enumeration of associations originating from this entity type.
        /// </summary>
        public IEnumerable<ModelAssociationSet> AssociationsFrom
        {
            get
            {
                try
                {
                    return AssociationsFromInternal;
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

        private IEnumerable<ModelAssociationSet> AssociationsFromInternal
        {
            get
            {
                foreach (ModelAssociationSet assoc in _conceptualModel.AssociationSets.Where(aset => aset.FromEntityType == this))
                {
                    yield return assoc;
                }
            }
        }

        /// <summary>
        /// Enumeration if associations referencing this entity type.
        /// </summary>
        public IEnumerable<ModelAssociationSet> AssociationsTo
        {
            get
            {
                try
                {
                    return AssociationsToInternal;
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

        private IEnumerable<ModelAssociationSet> AssociationsToInternal
        {
            get
            {
                try
                {
                    foreach (ModelAssociationSet assoc in _conceptualModel.AssociationSets.Where(aset => aset.ToEntityType == this))
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

        private EntityTypeShape _diagramShape = null;

        /// <summary>
        /// EntityTypeShape representing this entity type in the designer diagram.
        /// </summary>
        public EntityTypeShape DiagramShape
        {
            get
            {
                try
                {
                    if (_diagramShape == null)
                    {
                        _diagramShape = this.ParentFile.Designer.EntityTypeShapes.FirstOrDefault(ets => ets.EntityTypeName == this.FullName || ets.EntityTypeName == this.AliasName);
                        if (_diagramShape == null)
                        {
                            _diagramShape = this.ParentFile.Designer.AddEntityTypeShape(this);
                        }
                    }
                    return _diagramShape;
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
        /// Adds a navigation property to the entity type.
        /// </summary>
        /// <param name="navigationPropertyName">Name of the navigation property.</param>
        /// <param name="modelAssociationSet">Association set that this navigation property is based on.</param>
        /// <returns>A NavigationProperty object.</returns>
        public NavigationProperty AddNavigationMember(string navigationPropertyName, ModelAssociationSet modelAssociationSet)
        {
            return AddNavigationMember(navigationPropertyName, modelAssociationSet, null, null);
        }

        /// <summary>
        /// Adds a navigation property to the entity type.
        /// </summary>
        /// <param name="navigationPropertyName">Name of the navigation property.</param>
        /// <param name="modelAssociationSet">Association set that this navigation property is based on.</param>
        /// <param name="fromRoleName">From-role. Normally the same as the from-role for the associationset, but can be reversed for recursive associations.</param>
        /// <param name="fromRoleName">To-role. Normally the same as the To-role for the associationset, but can be reversed for recursive associations.</param>
        /// <returns>A NavigationProperty object.</returns>
        public NavigationProperty AddNavigationMember(string navigationPropertyName, ModelAssociationSet modelAssociationSet, string fromRoleName, string toRoleName)
        {
            try
            {
                if (!NavigationProperties.Any(np => np.Name.Equals(navigationPropertyName))
                    && !MemberProperties.Any(mp => mp.Name.Equals(navigationPropertyName))
                    && navigationPropertyName != this.Name)
                {
                    NavigationProperty navigationProperty = new NavigationProperty(ParentFile, this, navigationPropertyName, modelAssociationSet, _entityTypeElement, fromRoleName, toRoleName);
                    _navigationProperties.Add(navigationProperty.Name, navigationProperty);
                    navigationProperty.NameChanged += new EventHandler<NameChangeArgs>(navprop_NameChanged);
                    navigationProperty.Removed += new EventHandler(navprop_Removed);
                    return navigationProperty;
                }
                else
                {
                    throw new ArgumentException("A property named " + navigationPropertyName + " already exist in the type " + this.Name);
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

        private XmlNodeList _navigationPropertyElements = null;
        private XmlNodeList NavigationPropertyElements
        {
            get
            {
                try
                {
                    if (_navigationPropertyElements == null)
                    {
                        _navigationPropertyElements = _entityTypeElement.SelectNodes("edm:NavigationProperty", NSM);
                    }
                    return _navigationPropertyElements;
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

        private bool _navigationPropertiesEnumerated = false;
        private Dictionary<string, NavigationProperty> _navigationProperties = new Dictionary<string, NavigationProperty>();

        /// <summary>
        /// An enumeration of navigation properties on this entity type.
        /// </summary>
        public IEnumerable<NavigationProperty> NavigationProperties
        {
            get
            {
                if (_navigationPropertiesEnumerated == false)
                {
                    foreach (XmlElement navigationPropertyElement in NavigationPropertyElements)
                    {
                        NavigationProperty prop = null;
                        string propName = navigationPropertyElement.GetAttribute("Name");
                        if (!_navigationProperties.ContainsKey(propName))
                        {
                            prop = new NavigationProperty(ParentFile, this, navigationPropertyElement);
                            prop.NameChanged += new EventHandler<NameChangeArgs>(navprop_NameChanged);
                            prop.Removed += new EventHandler(navprop_Removed);
                            _navigationProperties.Add(propName, prop);
                        }
                        else
                        {
                            prop = _navigationProperties[propName];
                        }
                        yield return prop;
                    }
                    _navigationPropertiesEnumerated = true;
                    _navigationPropertyElements = null;
                }
                else
                {
                    foreach (NavigationProperty prop in _navigationProperties.Values)
                    {
                        yield return prop;
                    }
                }
            }
        }

        void navprop_Removed(object sender, EventArgs e)
        {
            _navigationProperties.Remove(((NavigationProperty)sender).Name);
        }

        void navprop_NameChanged(object sender, NameChangeArgs e)
        {
            if (_navigationProperties.ContainsKey(e.OldName))
            {
                _navigationProperties.Remove(e.OldName);
                _navigationProperties.Add(e.NewName, (NavigationProperty)sender);
            }
        }

        #region "Documentation"
        internal XmlElement DocumentationElement
        {
            get
            {
                return _entityTypeElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_entityTypeElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_entityTypeElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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
