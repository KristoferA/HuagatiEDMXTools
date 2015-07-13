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
    /// Represents a conceptual model association between two conceptual model entitysets
    /// </summary>
    public class ModelAssociationSet : EDMXMember, IEDMXRemovableMember, IEDMXNamedMember
    {
        private ConceptualModel _conceptualModel = null;
        private XmlElement _associationSetElement = null;
        private XmlElement _associationElement = null;

        internal ModelAssociationSet(EDMXFile parentFile, ConceptualModel conceptualModel, XmlElement associationSetElement)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;
            _associationSetElement = associationSetElement;

            string associationName = _associationSetElement.GetAttribute("Association");
            if (!string.IsNullOrEmpty(associationName))
            {
                if (_associationSetElement.ParentNode != null && _associationSetElement.ParentNode.ParentNode != null)
                {
                    XmlElement schemaElement = (XmlElement)_associationSetElement.ParentNode.ParentNode;
                    string schemaNamespace = schemaElement.GetAttribute("Namespace");
                    string schemaAlias = schemaElement.GetAttribute("Alias");
                    if (!string.IsNullOrEmpty(schemaNamespace) && associationName.StartsWith(schemaNamespace))
                    {
                        associationName = associationName.Substring(schemaNamespace.Length + 1);
                    }
                    else if (!string.IsNullOrEmpty(schemaAlias) && associationName.StartsWith(schemaAlias))
                    {
                        associationName = associationName.Substring(schemaAlias.Length + 1);
                    }
                    _associationElement = (XmlElement)_associationSetElement.ParentNode.ParentNode.SelectSingleNode("edm:Association[@Name=" + XmlHelpers.XPathLiteral(associationName) + "]", NSM);
                }
            }

            if (_associationElement == null)
            {
                throw new InvalidModelObjectException("The ModelAssociationSet " + (associationName ?? "[unknown]") + " has no corresponding association element.");
            }
        }

        internal ModelAssociationSet(EDMXFile parentFile, ConceptualModel conceptualModel, string name, ModelEntitySet fromES, ModelEntitySet toES, ModelEntityType fromET, ModelEntityType toET, MultiplicityTypeEnum fromMultiplicity, MultiplicityTypeEnum toMultiplicity, string fromNavProperty, string toNavProperty, List<Tuple<ModelMemberProperty, ModelMemberProperty>> keys)
            : base(parentFile)
        {
            _conceptualModel = conceptualModel;

            bool manyToMany = (fromMultiplicity == MultiplicityTypeEnum.Many && toMultiplicity == MultiplicityTypeEnum.Many);

            _associationSetElement = CreateAssociationSet();
            _associationElement = CreateAssociation(name, manyToMany);

            Name = name;

            //set from/to sets and multiplicity
            FromEntitySet = fromES;
            FromEntityType = fromET;
            FromMultiplicity = fromMultiplicity;
            ToEntitySet = toES;
            ToEntityType = toET;
            ToMultiplicity = toMultiplicity;

            //add navigation properties
            if (!string.IsNullOrEmpty(fromNavProperty))
            {
                fromET.AddNavigationMember(fromNavProperty, this, FromRoleName, ToRoleName);
            }
            if (!string.IsNullOrEmpty(toNavProperty))
            {
                toET.AddNavigationMember(toNavProperty, this, ToRoleName, FromRoleName);
            }

            if (keys != null)
            {
                foreach (Tuple<ModelMemberProperty, ModelMemberProperty> key in keys)
                {
                    AddKey(key.Item1, key.Item2);
                }
            }

            _keysEnumerated = true;
        }

        private XmlElement CreateAssociation(string name, bool manyToMany)
        {
            XmlElement assocParent = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema", NSM);
            XmlElement assoc = null;
            if (assocParent != null)
            {
                assoc = EDMXDocument.CreateElement("Association", NameSpaceURIcsdl);
                assocParent.AppendChild(assoc);

                XmlElement toEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIcsdl);
                XmlElement fromEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIcsdl);
                assoc.AppendChild(toEndPoint);
                assoc.AppendChild(fromEndPoint);

                if (manyToMany == false)
                {
                    AddRefConstraint(assoc);
                }
            }
            return assoc;
        }

        private XmlElement AddRefConstraint(XmlElement associationElement)
        {
            XmlElement refConstraint = EDMXDocument.CreateElement("ReferentialConstraint", NameSpaceURIcsdl);
            associationElement.AppendChild(refConstraint);

            XmlElement toKeysContainer = EDMXDocument.CreateElement("Principal", NameSpaceURIcsdl);
            refConstraint.AppendChild(toKeysContainer);

            XmlElement fromKeysContainer = EDMXDocument.CreateElement("Dependent", NameSpaceURIcsdl);
            refConstraint.AppendChild(fromKeysContainer);

            return refConstraint;
        }

        private XmlElement CreateAssociationSet()
        {
            XmlElement assocSetParent = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer", NSM);
            XmlElement assocSet = null;
            if (assocSetParent != null)
            {
                assocSet = EDMXDocument.CreateElement("AssociationSet", NameSpaceURIcsdl);
                assocSetParent.AppendChild(assocSet);
                XmlElement toEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIcsdl);
                XmlElement fromEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIcsdl);
                assocSet.AppendChild(toEndPoint);
                assocSet.AppendChild(fromEndPoint);
            }
            return assocSet;
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
                NavigationProperty navPropFrom = NavigationPropertyFrom;
                NavigationProperty navPropTo = NavigationPropertyTo;
                try
                {
                    if (navPropFrom != null)
                    {
                        navPropFrom.Remove();
                    }
                }
                catch { }
                try
                {
                    if (navPropTo != null)
                    {
                        navPropTo.Remove();
                    }
                }
                catch { }

                if (this.AssociationSetMapping != null)
                {
                    this.AssociationSetMapping.Remove();
                }

                if (_associationElement.ParentNode != null)
                {
                    _associationElement.ParentNode.RemoveChild(_associationElement);
                    if (_associationSetElement.ParentNode != null)
                    {
                        _associationSetElement.ParentNode.RemoveChild(_associationSetElement);
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
                return _associationSetElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _associationSetElement.GetAttribute("Name");

                _associationSetElement.SetAttribute("Name", value);
                _associationSetElement.SetAttribute("Association", ((XmlElement)_associationSetElement.ParentNode.ParentNode).GetAttribute("Namespace") + "." + value);
                _associationElement.SetAttribute("Name", value);

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
                if (_associationSetElement.ParentNode != null && _associationSetElement.ParentNode.ParentNode != null)
                {
                    string namespaceName = ((XmlElement)_associationSetElement.ParentNode.ParentNode).GetAttribute("Namespace");
                    return namespaceName + "." + Name;
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
                if (_associationSetElement.ParentNode != null && _associationSetElement.ParentNode.ParentNode != null)
                {
                    return ((XmlElement)_associationSetElement.ParentNode.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// First entity type involved in the association; dependent entity type
        /// </summary>
        public ModelEntityType FromEntityType
        {
            get
            {
                try
                {
                    string typeName = AssocFromEnd.GetAttribute("Type");
                    ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                    return entityType;
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
                XmlElement endElement = AssocFromEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("Type", value.FullName);
                }
            }
        }

        /// <summary>
        /// Second entity type involved in the association; principal entity type
        /// </summary>
        public ModelEntityType ToEntityType
        {
            get
            {
                try
                {
                    string typeName = AssocToEnd.GetAttribute("Type");
                    ModelEntityType entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                    return entityType;
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
                XmlElement endElement = AssocToEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("Type", value.FullName);
                }
            }
        }

        private ModelEntitySet _fromEntitySet = null;

        /// <summary>
        /// First entity set involved in the association; dependent entity type
        /// </summary>
        public ModelEntitySet FromEntitySet
        {
            get
            {
                try
                {
                    if (_fromEntitySet == null)
                    {
                        XmlElement endElement = AssocSetFromEnd;
                        if (endElement != null)
                        {
                            string entitySetName = endElement.GetAttribute("EntitySet");
                            _fromEntitySet = _conceptualModel.EntitySets.FirstOrDefault(es => es.Name == entitySetName);
                            if (_fromEntitySet != null)
                            {
                                _fromEntitySet.Removed += new EventHandler(fromEntitySet_Removed);
                            }
                        }
                    }
                    return _fromEntitySet;
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
                _fromEntitySet = value;

                string roleName = value.Name;
                if (roleName.Equals(ToRoleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    roleName = roleName + "1";
                }

                //update the association set's end element
                XmlElement endElement = AssocSetFromEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("EntitySet", value.Name);
                    endElement.SetAttribute("Role", roleName);
                }

                //update the associations end element
                endElement = AssocFromEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("Role", roleName);
                    if (!endElement.HasAttribute("Type"))
                    {
                        endElement.SetAttribute("Type", value.EntityType.FullName);
                    }
                }

                //update the key wrapper's role name
                XmlElement keyWrapper = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Dependent", NSM);
                if (keyWrapper != null)
                {
                    keyWrapper.SetAttribute("Role", roleName);
                }
            }
        }

        void fromEntitySet_Removed(object sender, EventArgs e)
        {
            _fromEntitySet = null;
        }

        private ModelEntitySet _toEntitySet = null;

        /// <summary>
        /// Second entity type involved in the association; principal entity type
        /// </summary>
        public ModelEntitySet ToEntitySet
        {
            get
            {
                try
                {
                    if (_toEntitySet == null)
                    {
                        XmlElement endElement = AssocSetToEnd;
                        if (endElement != null)
                        {
                            string entitySetName = endElement.GetAttribute("EntitySet");
                            _toEntitySet = _conceptualModel.EntitySets.FirstOrDefault(es => es.Name == entitySetName);
                            if (_toEntitySet != null)
                            {
                                _toEntitySet.Removed += new EventHandler(toEntitySet_Removed);
                            }
                        }
                    }
                    return _toEntitySet;
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
                _toEntitySet = value;

                string roleName = value.Name;
                if (roleName.Equals(FromRoleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    roleName = roleName + "1";
                }

                //update the association set's end element
                XmlElement endElement = AssocSetToEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("EntitySet", value.Name);
                    endElement.SetAttribute("Role", roleName);
                }

                //update the associations end element
                endElement = AssocToEnd;
                if (endElement != null)
                {
                    endElement.SetAttribute("Role", roleName);
                    if (!endElement.HasAttribute("Type"))
                    {
                        endElement.SetAttribute("Type", value.EntityType.FullName);
                    }
                }

                //update the key wrapper's role name
                XmlElement keyWrapper = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Principal", NSM);
                if (keyWrapper != null)
                {
                    keyWrapper.SetAttribute("Role", roleName);
                }
            }
        }

        void toEntitySet_Removed(object sender, EventArgs e)
        {
            _toEntitySet = null;
        }

        /// <summary>
        /// Multiplicity on the dependent side of the association
        /// </summary>
        public MultiplicityTypeEnum FromMultiplicity
        {
            get
            {
                try
                {
                    XmlElement endElement = AssocFromEnd;
                    return EDMXUtils.GetMultiplicity(endElement);
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
                XmlElement endElement = AssocFromEnd;
                EDMXUtils.SetMultiplicity(endElement, value);
            }
        }

        /// <summary>
        /// Multiplicity on the principal side of the association
        /// </summary>
        public MultiplicityTypeEnum ToMultiplicity
        {
            get
            {
                try
                {
                    XmlElement endElement = AssocToEnd;
                    return EDMXUtils.GetMultiplicity(endElement);
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
                XmlElement endElement = AssocToEnd;
                EDMXUtils.SetMultiplicity(endElement, value);
            }
        }

        /// <summary>
        /// Adds a key pair to the association. This should involve a member in the principal entity type and the corresponding member in the dependent entity type
        /// </summary>
        /// <param name="fromKey">A member of the principal entity type</param>
        /// <param name="toKey">A member of the dependent entity type</param>
        /// <returns></returns>
        public Tuple<ModelMemberProperty, ModelMemberProperty> AddKey(ModelMemberProperty fromKey, ModelMemberProperty toKey)
        {
            if (fromKey == null) { throw new ArgumentNullException("fromKey"); }
            if (toKey == null) { throw new ArgumentNullException("toKey"); }

            Tuple<ModelMemberProperty, ModelMemberProperty> newKey = new Tuple<ModelMemberProperty, ModelMemberProperty>(fromKey, toKey);

            fromKey.Removed += new EventHandler(fromKey_Removed);
            toKey.Removed += new EventHandler(toKey_Removed);

            _keys.Add(newKey);

            XmlElement fromKeyContainer = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Dependent", NSM);
            if (fromKeyContainer != null)
            {
                XmlElement fromKeyElement = EDMXDocument.CreateElement("PropertyRef", NameSpaceURIcsdl);
                fromKeyElement.SetAttribute("Name", fromKey.Name);
                fromKeyContainer.AppendChild(fromKeyElement);
            }

            XmlElement toKeyContainer = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Principal", NSM);
            if (toKeyContainer != null)
            {
                XmlElement toKeyElement = EDMXDocument.CreateElement("PropertyRef", NameSpaceURIcsdl);
                toKeyElement.SetAttribute("Name", toKey.Name);
                toKeyContainer.AppendChild(toKeyElement);
            }

            return newKey;
        }

        /// <summary>
        /// Removes a key pair from the association
        /// </summary>
        /// <param name="key">A tuple containing the dependent and principal key member to remove</param>
        public void RemoveKey(Tuple<ModelMemberProperty, ModelMemberProperty> key)
        {
            try
            {
                if (_keys.Contains(key))
                {
                    _keys.Remove(key);
                }

                XmlElement fromKeyElement = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Dependent/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(key.Item1.Name) + "]", NSM);
                if (fromKeyElement != null)
                {
                    if (fromKeyElement.ParentNode != null)
                    {
                        if (fromKeyElement.ParentNode != null)
                        {
                            fromKeyElement.ParentNode.RemoveChild(fromKeyElement);
                        }
                    }
                }

                XmlElement toKeyElement = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Principal/edm:PropertyRef", NSM);
                if (toKeyElement != null)
                {
                    if (toKeyElement.ParentNode != null)
                    {
                        if (toKeyElement.ParentNode != null)
                        {
                            toKeyElement.ParentNode.RemoveChild(toKeyElement);
                        }
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
        /// Removes a key pair based on the principal key member
        /// </summary>
        /// <param name="modelMemberProperty">Principal key member</param>
        public void RemoveKeyTo(ModelMemberProperty modelMemberProperty)
        {
            try
            {
                foreach (Tuple<ModelMemberProperty, ModelMemberProperty> key in Keys.Where(k => k.Item2 == modelMemberProperty))
                {
                    RemoveKey(key);
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
        /// Removes a key pair based on the dependent key member
        /// </summary>
        /// <param name="modelMemberProperty">Dependent key member</param>
        public void RemoveKeyFrom(ModelMemberProperty modelMemberProperty)
        {
            try
            {
                foreach (Tuple<ModelMemberProperty, ModelMemberProperty> key in Keys.Where(k => k.Item1 == modelMemberProperty))
                {
                    RemoveKey(key);
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

        private bool _keysEnumerated = false;
        private List<Tuple<ModelMemberProperty, ModelMemberProperty>> _keys = new List<Tuple<ModelMemberProperty, ModelMemberProperty>>();

        /// <summary>
        /// Enumeration of entity member pairs that make up the keys for this association
        /// </summary>
        public IEnumerable<Tuple<ModelMemberProperty, ModelMemberProperty>> Keys
        {
            get
            {
                if (_keysEnumerated == false)
                {
                    EnumerateKeys();
                }

                return _keys.AsEnumerable();
            }
        }

        private void EnumerateKeys()
        {
            try
            {
                //get hold of key propertyrefs
                XmlNodeList fromKeys = _associationElement.SelectNodes("edm:ReferentialConstraint/edm:Dependent/edm:PropertyRef", NSM);
                XmlNodeList toKeys = _associationElement.SelectNodes("edm:ReferentialConstraint/edm:Principal/edm:PropertyRef", NSM);

                //number of keys?
                int keyCount = Math.Max(fromKeys.Count, toKeys.Count);
                int keyNo = 0;
                while (keyNo < keyCount)
                {
                    //get the from entity type member
                    ModelMemberProperty fromKey = null;
                    if (fromKeys.Count > keyNo)
                    {
                        XmlElement fromElement = (XmlElement)fromKeys[keyNo];
                        if (fromElement != null)
                        {
                            string fromName = fromElement.GetAttribute("Name");
                            fromKey = FromEntitySet.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name == fromName);
                            if (fromKey == null && FromEntitySet.EntityType.HasSubTypes)
                            {
                                //check if it is a subtype member
                                fromKey = FromEntitySet.EntityType.SubTypes.SelectMany(t => t.MemberProperties).FirstOrDefault(mp => mp.Name == fromName);
                            }
                        }
                    }

                    //get the to entity type member
                    ModelMemberProperty toKey = null;
                    if (toKeys.Count > keyNo)
                    {
                        XmlElement toElement = (XmlElement)toKeys[keyNo];
                        if (toElement != null)
                        {
                            string toName = toElement.GetAttribute("Name");
                            toKey = ToEntitySet.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name == toName);
                            if (toKey == null && ToEntitySet.EntityType.HasSubTypes)
                            {
                                //check if it is a subtype member
                                toKey = ToEntitySet.EntityType.SubTypes.SelectMany(t => t.MemberProperties).FirstOrDefault(mp => mp.Name == toName);
                            }
                        }
                    }

                    Tuple<ModelMemberProperty, ModelMemberProperty> key = null;
                    if (!_keys.Any(k => k.Item1.Equals(fromKey) && k.Item2.Equals(toKey)))
                    {
                        key = new Tuple<ModelMemberProperty, ModelMemberProperty>(fromKey, toKey);
                        _keys.Add(key);
                    }
                    else
                    {
                        key = _keys.FirstOrDefault(k => k.Item1.Equals(fromKey) && k.Item2.Equals(toKey));
                    }

                    if (fromKey != null)
                    {
                        fromKey.Removed += new EventHandler(fromKey_Removed);
                    }
                    if (toKey != null)
                    {
                        toKey.Removed += new EventHandler(toKey_Removed);
                    }

                    keyNo++;
                }

                _keysEnumerated = true;
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

        void toKey_Removed(object sender, EventArgs e)
        {
            ModelMemberProperty modelMemberProperty = (ModelMemberProperty)sender;
            _keys.RemoveAll(k => k.Item2 == modelMemberProperty);
            RemoveKeyTo(modelMemberProperty);
        }

        void fromKey_Removed(object sender, EventArgs e)
        {
            ModelMemberProperty modelMemberProperty = (ModelMemberProperty)sender;
            _keys.RemoveAll(k => k.Item1 == modelMemberProperty);
            RemoveKeyFrom(modelMemberProperty);
        }

        internal void UpdateKeyName(ModelEntityType entityType, ModelMemberProperty memberProperty, string oldName, string newName)
        {
            try
            {
                if (entityType == FromEntitySet.EntityType)
                {
                    foreach (XmlElement key in _associationElement.SelectNodes("edm:ReferentialConstraint/edm:Dependent/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(oldName) + "]", NSM))
                    {
                        key.SetAttribute("Name", newName);
                    }
                }
                else if (entityType == ToEntitySet.EntityType)
                {
                    foreach (XmlElement key in _associationElement.SelectNodes("edm:ReferentialConstraint/edm:Principal/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(oldName) + "]", NSM))
                    {
                        key.SetAttribute("Name", newName);
                    }
                }
                else
                {
                    throw new ArgumentException("The entity type " + entityType.Name + " does not participate in the association " + this.Name);
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

        private NavigationProperty _navigationPropertyFrom = null;

        /// <summary>
        /// Navigation property corresponding to this association in the dependent (from) entity type
        /// </summary>
        public NavigationProperty NavigationPropertyFrom
        {
            get
            {
                try
                {
                    if (_navigationPropertyFrom == null)
                    {
                        XmlElement endElement = AssocSetFromEnd;
                        if (endElement != null && FromEntityType != null)
                        {
                            _navigationPropertyFrom = FromEntityType.NavigationProperties.FirstOrDefault(rn => (rn.AssociationName == this.FullName || rn.AssociationName == this.AliasName) && rn.FromRoleName == endElement.GetAttribute("Role"));
                            if (_navigationPropertyFrom != null)
                            {
                                _navigationPropertyFrom.Removed += new EventHandler(NavigationPropertyFrom_Removed);
                            }
                        }
                    }
                    return _navigationPropertyFrom;
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

        void NavigationPropertyFrom_Removed(object sender, EventArgs e)
        {
            _navigationPropertyFrom = null;
        }

        private NavigationProperty _navigationPropertyTo = null;

        /// <summary>
        /// Navigation property corresponding to this association in the principal entity type 
        /// </summary>
        public NavigationProperty NavigationPropertyTo
        {
            get
            {
                try
                {
                    if (_navigationPropertyTo == null)
                    {
                        XmlElement endElement = AssocSetToEnd;
                        if (endElement != null && ToEntityType != null)
                        {
                            _navigationPropertyTo = ToEntityType.NavigationProperties.FirstOrDefault(rn => (rn.AssociationName == this.FullName || rn.AssociationName == this.AliasName) && rn.FromRoleName == endElement.GetAttribute("Role"));
                            if (_navigationPropertyTo != null)
                            {
                                _navigationPropertyTo.Removed += new EventHandler(NavigationPropertyTo_Removed);
                            }
                        }
                    }
                    return _navigationPropertyTo;
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

        void NavigationPropertyTo_Removed(object sender, EventArgs e)
        {
            _navigationPropertyTo = null;
        }

        private string _fromRoleName = null;

        /// <summary>
        /// Dependent (from) role name
        /// </summary>
        public string FromRoleName
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_fromRoleName))
                    {
                        XmlElement dependentElement = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Dependent", NSM);
                        if (dependentElement != null)
                        {
                            _fromRoleName = dependentElement.GetAttribute("Role");
                        }
                        if (string.IsNullOrEmpty(_fromRoleName) && _associationElement.SelectSingleNode("edm:End[@Multiplicity='1']", NSM) != null)
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("edm:End[@Multiplicity='*' or @Multiplicity='0..1']", NSM);
                            if (endElement != null)
                            {
                                _fromRoleName = endElement.GetAttribute("Role");
                            }
                        }
                        if (string.IsNullOrEmpty(_fromRoleName))
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("edm:End[position()=2]", NSM);
                            if (endElement != null)
                            {
                                _fromRoleName = endElement.GetAttribute("Role");
                            }
                        }
                    }
                    return _fromRoleName;
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

        private string _toRoleName = null;

        /// <summary>
        /// Principal role name
        /// </summary>
        public string ToRoleName
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_toRoleName))
                    {
                        XmlElement principalElement = (XmlElement)_associationElement.SelectSingleNode("edm:ReferentialConstraint/edm:Principal", NSM);
                        if (principalElement != null)
                        {
                            _toRoleName = principalElement.GetAttribute("Role");
                        }
                        if (string.IsNullOrEmpty(_toRoleName) && _associationElement.SelectSingleNode("edm:End[@Multiplicity='*']", NSM) != null)
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("edm:End[@Multiplicity!='*']", NSM);
                            if (endElement != null)
                            {
                                _toRoleName = endElement.GetAttribute("Role");
                            }
                        }
                        if (string.IsNullOrEmpty(_toRoleName))
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("edm:End[position()=1]", NSM);
                            if (endElement != null)
                            {
                                _toRoleName = endElement.GetAttribute("Role");
                            }
                        }
                    }
                    return _toRoleName;
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

        private StoreAssociationSet _storeAssociationSet = null;

        /// <summary>
        /// Store association set corresponding to this model association set. Not valid for many-to-many associations that has more than one underlying store association.
        /// </summary>
        public StoreAssociationSet StoreAssociationSet
        {
            get
            {
                try
                {
                    if (_storeAssociationSet == null)
                    {
                        _storeAssociationSet = ParentFile.StorageModel.AssociationSets.FirstOrDefault(assoc => assoc.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase));
                        if (_storeAssociationSet == null)
                        {
                            _storeAssociationSet = ParentFile.StorageModel.AssociationSets.FirstOrDefault(assoc => assoc.Name.Replace(" ", "_").Equals(this.Name, StringComparison.InvariantCultureIgnoreCase));
                        }
                        if (_storeAssociationSet != null)
                        {
                            _storeAssociationSet.Removed += new EventHandler(StoreAssociationSet_Removed);
                        }
                    }
                    return _storeAssociationSet;
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

        void StoreAssociationSet_Removed(object sender, EventArgs e)
        {
            _storeAssociationSet = null;
        }

        private StoreEntitySet _storeEntitySetJunction = null;

        /// <summary>
        /// Storage model junction entity set for many-to-many associations
        /// </summary>
        public StoreEntitySet StoreEntitySetJunction
        {
            get
            {
                try
                {
                    if (_storeEntitySetJunction == null)
                    {
                        AssociationSetMapping asetmap = ParentFile.CSMapping.AssociationSetMappings.FirstOrDefault(asm => asm.TypeName.Equals(this.FullName, StringComparison.InvariantCultureIgnoreCase));
                        if (asetmap != null)
                        {
                            _storeEntitySetJunction = asetmap.StoreEntitySet;
                            if (_storeEntitySetJunction != null)
                            {
                                _storeEntitySetJunction.Removed += new EventHandler(StoreEntitySetJunction_Removed);
                            }
                        }
                    }
                    return _storeEntitySetJunction;
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

        void StoreEntitySetJunction_Removed(object sender, EventArgs e)
        {
            _storeEntitySetJunction = null;
        }

        private XmlElement AssocFromEnd
        {
            get
            {
                XmlElement assocFromEnd = null;

                string fromRoleName = FromRoleName;
                if (!string.IsNullOrEmpty(fromRoleName))
                {
                    assocFromEnd = (XmlElement)_associationElement.SelectSingleNode("edm:End[@Role=" + XmlHelpers.XPathLiteral(fromRoleName) + "]", NSM);
                }
                if (assocFromEnd == null)
                {
                    assocFromEnd = (XmlElement)_associationElement.SelectSingleNode("edm:End[position()=2]", NSM);
                }
                return assocFromEnd;
            }
        }

        private XmlElement AssocToEnd
        {
            get
            {
                XmlElement assocToEnd = null;

                string toRoleName = ToRoleName;
                if (!string.IsNullOrEmpty(toRoleName))
                {
                    assocToEnd = (XmlElement)_associationElement.SelectSingleNode("edm:End[@Role=" + XmlHelpers.XPathLiteral(toRoleName) + "]", NSM);
                }
                if (assocToEnd == null)
                {
                    assocToEnd = (XmlElement)_associationElement.SelectSingleNode("edm:End[position()=1]", NSM);
                }
                return assocToEnd;
            }
        }

        private XmlElement AssocSetFromEnd
        {
            get
            {
                XmlElement assocFromEnd = null;

                string fromRoleName = FromRoleName;
                if (!string.IsNullOrEmpty(fromRoleName))
                {
                    assocFromEnd = (XmlElement)_associationSetElement.SelectSingleNode("edm:End[@Role=" + XmlHelpers.XPathLiteral(fromRoleName) + "]", NSM);
                }
                if (assocFromEnd == null)
                {
                    assocFromEnd = (XmlElement)_associationSetElement.SelectSingleNode("edm:End[position()=2]", NSM);
                }
                return assocFromEnd;
            }
        }

        private XmlElement AssocSetToEnd
        {
            get
            {
                XmlElement assocToEnd = null;

                string toRoleName = ToRoleName;
                if (!string.IsNullOrEmpty(toRoleName))
                {
                    assocToEnd = (XmlElement)_associationSetElement.SelectSingleNode("edm:End[@Role=" + XmlHelpers.XPathLiteral(toRoleName) + "]", NSM);
                }
                if (assocToEnd == null)
                {
                    assocToEnd = (XmlElement)_associationSetElement.SelectSingleNode("edm:End[position()=1]", NSM);
                }
                return assocToEnd;
            }
        }

        private AssociationSetMapping _associationSetMapping = null;

        /// <summary>
        /// Association set mapping (if any) mapping this conceptual model association to the underlying store associations. Normally only set for many-to-many and/or independent associations.
        /// </summary>
        public AssociationSetMapping AssociationSetMapping
        {
            get
            {
                try
                {
                    if (_associationSetMapping == null)
                    {
                        _associationSetMapping = ParentFile.CSMapping.AssociationSetMappings.FirstOrDefault(asm => asm.ModelAssociationSet == this);
                        if (_associationSetMapping != null)
                        {
                            _associationSetMapping.Removed += new EventHandler(AssociationSetMapping_Removed);
                        }
                    }
                    return _associationSetMapping;
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

        void AssociationSetMapping_Removed(object sender, EventArgs e)
        {
            _associationSetMapping = null;
        }
    }
}
