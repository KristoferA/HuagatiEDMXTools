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
    /// Represent a storage model associationset.
    /// </summary>
    public class StoreAssociationSet : EDMXMember, IEDMXRemovableMember, IEDMXNamedMember
    {
        private StorageModel _storageModel = null;
        private XmlElement _associationSetElement = null;
        private XmlElement _associationElement = null;

        internal StoreAssociationSet(EDMXFile parentFile, StorageModel storageModel, XmlElement associationSetElement)
            : base(parentFile)
        {
            _storageModel = storageModel;
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
                    _associationElement = (XmlElement)_associationSetElement.ParentNode.ParentNode.SelectSingleNode("ssdl:Association[@Name=" + XmlHelpers.XPathLiteral(associationName) + "]", NSM);
                }
            }

            if (_associationElement == null)
            {
                throw new InvalidModelObjectException("The StoreAssociationSet " + (associationName ?? "[unknown]") + " has no corresponding association element.");
            }
        }

        internal StoreAssociationSet(EDMXFile parentFile, StorageModel storageModel, string name, StoreEntitySet fromES, StoreEntitySet toES, StoreEntityType fromET, StoreEntityType toET, string fromRoleName, string toRoleName, MultiplicityTypeEnum fromMultiplicity, MultiplicityTypeEnum toMultiplicity, List<Tuple<StoreMemberProperty, StoreMemberProperty>> keys)
            : base(parentFile)
        {
            _storageModel = storageModel;

            _associationSetElement = CreateAssociationSet();
            _associationElement = CreateAssociation(name);

            Name = name;

            FromRoleName = fromRoleName;
            ToRoleName = toRoleName;

            FromEntitySet = fromES;
            FromEntityType = fromET;
            FromMultiplicity = fromMultiplicity;

            ToEntitySet = toES;
            ToEntityType = toET;
            ToMultiplicity = toMultiplicity;

            foreach (Tuple<StoreMemberProperty, StoreMemberProperty> key in keys)
            {
                AddKey(key.Item1, key.Item2);
            }

            _keysEnumerated = true;
        }

        private XmlElement CreateAssociation(string name)
        {
            XmlElement assocParent = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels/ssdl:Schema", NSM);
            XmlElement assoc = null;
            if (assocParent != null)
            {
                assoc = EDMXDocument.CreateElement("Association", NameSpaceURIssdl);
                assocParent.AppendChild(assoc);

                XmlElement toEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIssdl);
                toEndPoint.SetAttribute("Multiplicity", "1");
                XmlElement fromEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIssdl);
                assoc.AppendChild(toEndPoint);
                fromEndPoint.SetAttribute("Multiplicity", "*");
                assoc.AppendChild(fromEndPoint);

                XmlElement constraint = EDMXDocument.CreateElement("ReferentialConstraint", NameSpaceURIssdl);
                assoc.AppendChild(constraint);

                XmlElement toKeysContainer = EDMXDocument.CreateElement("Principal", NameSpaceURIssdl);
                constraint.AppendChild(toKeysContainer);

                XmlElement fromKeysContainer = EDMXDocument.CreateElement("Dependent", NameSpaceURIssdl);
                constraint.AppendChild(fromKeysContainer);
            }
            return assoc;
        }

        private XmlElement CreateAssociationSet()
        {
            XmlElement assocSetParent = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:StorageModels/ssdl:Schema/ssdl:EntityContainer", NSM);
            XmlElement assocSet = null;
            if (assocSetParent != null)
            {
                assocSet = EDMXDocument.CreateElement("AssociationSet", NameSpaceURIssdl);
                assocSetParent.AppendChild(assocSet);
                XmlElement toEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIssdl);
                XmlElement fromEndPoint = EDMXDocument.CreateElement("End", NameSpaceURIssdl);
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
                if (FromEntitySet != null)
                {
                    foreach (AssociationSetMapping asm in FromEntitySet.AssociationSetMappings.ToList())
                    {
                        if (asm != null)
                        {
                            asm.Remove();
                        }
                    }
                }

                if (ToEntitySet != null)
                {
                    foreach (AssociationSetMapping asm in ToEntitySet.AssociationSetMappings.ToList())
                    {
                        if (asm != null)
                        {
                            asm.Remove();
                        }
                    }
                }

                if (_associationElement != null && _associationElement.ParentNode != null)
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
                if (_associationElement != null)
                {
                    _associationElement.SetAttribute("Name", value);
                }

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
                if (_associationElement != null && _associationElement.ParentNode != null)
                {
                    return ((XmlElement)_associationElement.ParentNode).GetAttribute("Namespace") + "." + Name;
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
                if (_associationElement != null && _associationElement.ParentNode != null)
                {
                    return ((XmlElement)_associationElement.ParentNode).GetAttribute("Alias") + "." + Name;
                }
                else
                {
                    return null;
                }
            }
        }

        private StoreEntitySet _fromEntitySet = null;

        /// <summary>
        /// Dependent entityset. This is normally the entityset corresponding to the table that the underlying foreign key constraint is defined on.
        /// </summary>
        public StoreEntitySet FromEntitySet
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
                            _fromEntitySet = _storageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName, StringComparison.InvariantCultureIgnoreCase));
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
                if (_associationElement == null) { throw new InvalidOperationException("The associationset doesn't have a corresponding association."); }

                XmlElement dependentElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Dependent", NSM);
                if (dependentElement != null)
                {
                    _fromEntitySet = value;

                    if (_fromEntitySet != null)
                    {
                        _fromEntitySet.Removed += new EventHandler(fromEntitySet_Removed);

                        //update the association set's end element
                        XmlElement endElement = AssocSetFromEnd;
                        if (endElement != null)
                        {
                            endElement.SetAttribute("EntitySet", value.Name);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Association already has a 'From' EntitySet or can not be assigned one");
                }
            }
        }

        void fromEntitySet_Removed(object sender, EventArgs e)
        {
            _fromEntitySet = null;
        }

        private StoreEntitySet _toEntitySet = null;

        /// <summary>
        /// Principal entityset.
        /// </summary>
        public StoreEntitySet ToEntitySet
        {
            get
            {
                try
                {
                    if (_toEntitySet == null)
                    {
                        XmlElement endElement = AssocSetToEnd; ;
                        if (endElement != null)
                        {
                            string entitySetName = endElement.GetAttribute("EntitySet");
                            _toEntitySet = _storageModel.EntitySets.FirstOrDefault(es => es.Name.Equals(entitySetName, StringComparison.InvariantCultureIgnoreCase));
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
                if (_associationElement == null) { throw new InvalidOperationException("The association set doesn't have a corresponding association."); }

                XmlElement principalElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Principal", NSM);
                if (principalElement != null)
                {
                    _toEntitySet = value;

                    if (_toEntitySet != null)
                    {
                        _toEntitySet.Removed += new EventHandler(toEntitySet_Removed);

                        //update the association set's end element
                        XmlElement endElement = AssocSetToEnd;
                        if (endElement != null)
                        {
                            endElement.SetAttribute("EntitySet", value.Name);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Association already has a 'To' EntitySet, or can not be assigned one.");
                }
            }
        }

        void toEntitySet_Removed(object sender, EventArgs e)
        {
            _toEntitySet = null;
        }

        /// <summary>
        /// Dependent entity type. This is normally the entity corresponding to the table where the underlying foreign key constraint is defined.
        /// </summary>
        public StoreEntityType FromEntityType
        {
            get
            {
                try
                {
                    string typeName = AssocFromEnd.GetAttribute("Type");
                    StoreEntityType entityType = ParentFile.StorageModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
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
        /// Principal entity type.
        /// </summary>
        public StoreEntityType ToEntityType
        {
            get
            {
                try
                {
                    string typeName = AssocToEnd.GetAttribute("Type");
                    StoreEntityType entityType = ParentFile.StorageModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
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

        /// <summary>
        /// Multiplicity on the from (dependent) entityset.
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
        /// Multiplicity on the to (principal) entityset.
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

        private string _fromRoleName = null;

        /// <summary>
        /// Dependent role.
        /// </summary>
        public string FromRoleName
        {
            get
            {
                try
                {
                    if (_associationElement != null && string.IsNullOrEmpty(_fromRoleName))
                    {
                        XmlElement dependentElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Dependent", NSM);
                        if (dependentElement != null)
                        {
                            _fromRoleName = dependentElement.GetAttribute("Role");
                        }
                        if (string.IsNullOrEmpty(_fromRoleName) && _associationElement.SelectSingleNode("ssdl:End[@Multiplicity='1']", NSM) != null)
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[@Multiplicity='*' or @Multiplicity='0..1']", NSM);
                            if (endElement != null)
                            {
                                _fromRoleName = endElement.GetAttribute("Role");
                            }
                        }
                        if (string.IsNullOrEmpty(_fromRoleName))
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[position()=2]", NSM);
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
            set
            {
                string roleName = value;
                if (roleName.Equals(ToRoleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    roleName = roleName + "1";
                }
                _fromRoleName = roleName;

                XmlElement endElementSet = AssocSetFromEnd;
                XmlElement endElement = AssocFromEnd;

                if (endElementSet != null)
                {
                    endElementSet.SetAttribute("Role", roleName);
                }

                //update the associations end element
                if (endElement != null)
                {
                    endElement.SetAttribute("Role", roleName);
                }

                //update the key wrapper's role name
                if (_associationElement != null)
                {
                    XmlElement dependentElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Dependent", NSM);
                    if (dependentElement != null)
                    {
                        dependentElement.SetAttribute("Role", roleName);
                    }
                }
            }
        }

        private string _toRoleName = null;

        /// <summary>
        /// Principal role.
        /// </summary>
        public string ToRoleName
        {
            get
            {
                try
                {
                    if (_associationElement != null && string.IsNullOrEmpty(_toRoleName))
                    {
                        XmlElement principalElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Principal", NSM);
                        if (principalElement != null)
                        {
                            _toRoleName = principalElement.GetAttribute("Role");
                        }
                        if (string.IsNullOrEmpty(_toRoleName) && _associationElement.SelectSingleNode("ssdl:End[@Multiplicity='*']", NSM) != null)
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[@Multiplicity!='*']", NSM);
                            if (endElement != null)
                            {
                                _toRoleName = endElement.GetAttribute("Role");
                            }
                        }
                        if (string.IsNullOrEmpty(_toRoleName))
                        {
                            XmlElement endElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[position()=1]", NSM);
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
            set
            {
                string roleName = value;
                if (roleName.Equals(FromRoleName, StringComparison.InvariantCultureIgnoreCase))
                {
                    roleName = roleName + "1";
                }
                _toRoleName = roleName;

                XmlElement endElementSet = AssocSetToEnd;
                XmlElement endElement = AssocToEnd;

                if (endElementSet != null)
                {
                    endElementSet.SetAttribute("Role", roleName);
                }

                //update the associations end element
                if (endElement != null)
                {
                    endElement.SetAttribute("Role", roleName);
                }

                //update the key wrapper's role name
                if (_associationElement != null)
                {
                    XmlElement principalElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Principal", NSM);
                    if (principalElement != null)
                    {
                        principalElement.SetAttribute("Role", roleName);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a key pair to the association.
        /// </summary>
        /// <param name="fromKey">Dependent (foreign key owner table) key member.</param>
        /// <param name="toKey">Principal (foreign key referenced table) key member.</param>
        /// <returns></returns>
        public Tuple<StoreMemberProperty, StoreMemberProperty> AddKey(StoreMemberProperty fromKey, StoreMemberProperty toKey)
        {
            if (fromKey == null) { throw new ArgumentNullException("fromKey"); }
            if (toKey == null) { throw new ArgumentNullException("toKey"); }
            if (_associationElement == null) { throw new InvalidOperationException("The association set doesn't have a corresponding association."); }

            Tuple<StoreMemberProperty, StoreMemberProperty> newKey = new Tuple<StoreMemberProperty, StoreMemberProperty>(fromKey, toKey);

            fromKey.Removed += new EventHandler(fromKey_Removed);
            toKey.Removed += new EventHandler(toKey_Removed);

            _keys.Add(newKey);

            XmlElement fromKeyContainer = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Dependent", NSM);
            if (fromKeyContainer != null)
            {
                XmlElement fromKeyElement = EDMXDocument.CreateElement("PropertyRef", NameSpaceURIssdl);
                fromKeyElement.SetAttribute("Name", fromKey.Name);
                fromKeyContainer.AppendChild(fromKeyElement);
            }

            XmlElement toKeyContainer = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Principal", NSM);
            if (toKeyContainer != null)
            {
                XmlElement toKeyElement = EDMXDocument.CreateElement("PropertyRef", NameSpaceURIssdl);
                toKeyElement.SetAttribute("Name", toKey.Name);
                toKeyContainer.AppendChild(toKeyElement);
            }

            return newKey;
        }

        /// <summary>
        /// Removes a pair of key members from the association.
        /// </summary>
        /// <param name="key">Pair of store member properties to remove from the association.</param>
        public void RemoveKey(Tuple<StoreMemberProperty, StoreMemberProperty> key)
        {
            if (_keys.Contains(key))
            {
                _keys.Remove(key);
            }

            if (_associationElement != null)
            {
                XmlElement fromKeyElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Dependent/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(key.Item1.Name) + "]", NSM);
                if (fromKeyElement != null)
                {
                    if (fromKeyElement.ParentNode != null)
                    {
                        fromKeyElement.ParentNode.RemoveChild(fromKeyElement);
                    }
                }

                XmlElement toKeyElement = (XmlElement)_associationElement.SelectSingleNode("ssdl:ReferentialConstraint/ssdl:Principal/ssdl:PropertyRef", NSM);
                if (toKeyElement != null)
                {
                    if (toKeyElement.ParentNode != null)
                    {
                        toKeyElement.ParentNode.RemoveChild(toKeyElement);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a key pair based on the 'To'-side key
        /// </summary>
        /// <param name="storeMemberProperty">To-key to remove the key pair for</param>
        public void RemoveKeyTo(StoreMemberProperty storeMemberProperty)
        {
            foreach (Tuple<StoreMemberProperty, StoreMemberProperty> key in Keys.Where(k => k.Item2 == storeMemberProperty))
            {
                RemoveKey(key);
            }
        }

        /// <summary>
        /// Removes a key pair based on the 'From'-side key
        /// </summary>
        /// <param name="storeMemberProperty">From-key to remove the key pair for</param>
        public void RemoveKeyFrom(StoreMemberProperty storeMemberProperty)
        {
            foreach (Tuple<StoreMemberProperty, StoreMemberProperty> key in Keys.Where(k => k.Item1 == storeMemberProperty))
            {
                RemoveKey(key);
            }
        }

        private bool _keysEnumerated = false;
        private List<Tuple<StoreMemberProperty, StoreMemberProperty>> _keys = new List<Tuple<StoreMemberProperty, StoreMemberProperty>>();

        /// <summary>
        /// Enumeration of key members for this association set.
        /// </summary>
        public IEnumerable<Tuple<StoreMemberProperty, StoreMemberProperty>> Keys
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
                if (_associationElement == null) { throw new InvalidOperationException("The association set doesn't have a corresponding association."); }

                //get hold of key propertyrefs
                XmlNodeList fromKeys = _associationElement.SelectNodes("ssdl:ReferentialConstraint/ssdl:Dependent/ssdl:PropertyRef", NSM);
                XmlNodeList toKeys = _associationElement.SelectNodes("ssdl:ReferentialConstraint/ssdl:Principal/ssdl:PropertyRef", NSM);

                //number of keys?
                int keyCount = Math.Max(fromKeys.Count, toKeys.Count);
                int keyNo = 0;
                while (keyNo < keyCount)
                {
                    //get the from entity type member
                    StoreMemberProperty fromKey = null;
                    if (fromKeys.Count > keyNo)
                    {
                        string fromName = ((XmlElement)fromKeys[keyNo]).GetAttribute("Name");
                        fromKey = FromEntitySet.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name.Equals(fromName, StringComparison.InvariantCultureIgnoreCase));
                    }

                    //get the to entity type member
                    StoreMemberProperty toKey = null;
                    if (toKeys.Count > keyNo)
                    {
                        string toName = ((XmlElement)toKeys[keyNo]).GetAttribute("Name");
                        toKey = ToEntitySet.EntityType.MemberProperties.FirstOrDefault(mp => mp.Name.Equals(toName, StringComparison.InvariantCultureIgnoreCase));
                    }

                    Tuple<StoreMemberProperty, StoreMemberProperty> key = null;
                    if (!_keys.Any(k => k.Item1.Equals(fromKey) && k.Item2.Equals(toKey)))
                    {
                        key = new Tuple<StoreMemberProperty, StoreMemberProperty>(fromKey, toKey);
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
            StoreMemberProperty storeMemberProperty = (StoreMemberProperty)sender;
            _keys.RemoveAll(k => k.Item2 == storeMemberProperty);
            RemoveKeyTo(storeMemberProperty);
        }

        void fromKey_Removed(object sender, EventArgs e)
        {
            StoreMemberProperty storeMemberProperty = (StoreMemberProperty)sender;
            _keys.RemoveAll(k => k.Item1 == storeMemberProperty);
            RemoveKeyFrom(storeMemberProperty);
        }

        internal void UpdateKeyName(StoreEntityType entityType, StoreMemberProperty memberProperty, string oldName, string newName)
        {
            if (_associationElement == null) { throw new InvalidOperationException("The association set doesn't have a corresponding association."); }

            if (entityType == FromEntitySet.EntityType)
            {
                foreach (XmlElement key in _associationElement.SelectNodes("ssdl:ReferentialConstraint/ssdl:Dependent/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(oldName) + "]", NSM))
                {
                    key.SetAttribute("Name", newName);
                }
            }
            else if (entityType == ToEntitySet.EntityType)
            {
                foreach (XmlElement key in _associationElement.SelectNodes("ssdl:ReferentialConstraint/ssdl:Principal/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(oldName) + "]", NSM))
                {
                    key.SetAttribute("Name", newName);
                }
            }
            else
            {
                throw new ArgumentException("The entity type " + entityType.Name + " does not participate in the association " + this.Name);
            }
        }

        private ModelAssociationSet _modelAssociationSet = null;

        /// <summary>
        /// Conceptual model association set mapped to this storage model association set.
        /// </summary>
        public ModelAssociationSet ModelAssociationSet
        {
            get
            {
                try
                {
                    if (_modelAssociationSet == null)
                    {
                        _modelAssociationSet = ParentFile.ConceptualModel.AssociationSets.FirstOrDefault(assoc => assoc.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase));
                        if (_modelAssociationSet == null)
                        {
                            _modelAssociationSet = ParentFile.ConceptualModel.AssociationSets.FirstOrDefault(assoc => assoc.Name.Equals(this.Name.Replace(" ", "_"), StringComparison.InvariantCultureIgnoreCase));
                        }
                        if (_modelAssociationSet != null)
                        {
                            _modelAssociationSet.Removed += new EventHandler(modelAssociationSet_Removed);
                        }
                    }
                    if (_modelAssociationSet == null)
                    {
                        AssociationSetMapping asetMapping = ParentFile.CSMapping.AssociationSetMappings.FirstOrDefault(asm => asm.StoreEntitySet == this.FromEntitySet);
                        if (asetMapping != null)
                        {
                            _modelAssociationSet = asetMapping.ModelAssociationSet;
                            if (_modelAssociationSet != null)
                            {
                                _modelAssociationSet.Removed += new EventHandler(modelAssociationSet_Removed);
                            }
                        }
                    }
                    return _modelAssociationSet;
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

        void modelAssociationSet_Removed(object sender, EventArgs e)
        {
            _modelAssociationSet = null;
        }

        internal bool IsInheritanceConstraint()
        {
            try
            {
                var q =
                    from esm in this.FromEntitySet.EntitySetMappings
                    join esm2 in this.ToEntitySet.EntitySetMappings on esm equals esm2
                    where esm.EntityTypeFor(this.FromEntitySet) != esm.EntityTypeFor(this.ToEntitySet)
                    select esm;
                return q.Any();
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

        private XmlElement AssocFromEnd
        {
            get
            {
                XmlElement assocFromEnd = null;

                string fromRoleName = FromRoleName;
                if (!string.IsNullOrEmpty(fromRoleName))
                {
                    assocFromEnd = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[@Role=" + XmlHelpers.XPathLiteral(fromRoleName) + "]", NSM);
                }
                if (assocFromEnd == null)
                {
                    assocFromEnd = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[position()=2]", NSM);
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
                    assocToEnd = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[@Role=" + XmlHelpers.XPathLiteral(toRoleName) + "]", NSM);
                }
                if (assocToEnd == null)
                {
                    assocToEnd = (XmlElement)_associationElement.SelectSingleNode("ssdl:End[position()=1]", NSM);
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
                    assocFromEnd = (XmlElement)_associationSetElement.SelectSingleNode("ssdl:End[@Role=" + XmlHelpers.XPathLiteral(fromRoleName) + "]", NSM);
                }
                if (assocFromEnd == null)
                {
                    assocFromEnd = (XmlElement)_associationSetElement.SelectSingleNode("ssdl:End[position()=2]", NSM);
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
                    assocToEnd = (XmlElement)_associationSetElement.SelectSingleNode("ssdl:End[@Role=" + XmlHelpers.XPathLiteral(toRoleName) + "]", NSM);
                }
                if (assocToEnd == null)
                {
                    assocToEnd = (XmlElement)_associationSetElement.SelectSingleNode("ssdl:End[position()=1]", NSM);
                }
                return assocToEnd;
            }
        }
    }
}
