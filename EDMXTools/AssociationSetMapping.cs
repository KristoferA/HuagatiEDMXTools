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
    /// AssociationSetMapping represents a MSL AssociationSetMapping entry. These are typically used for many-to-many relationships and for independent associations where the scalar members are part of the SSDL entity but not part of the CSDL entity.
    /// </summary>
    public class AssociationSetMapping : EDMXMember, IEDMXRemovableMember, IEDMXNamedMember
    {
        private CSMapping _csMapping = null;
        private XmlElement _asmElement = null;
        private ModelAssociationSet _modelAssociationSet = null;
        private StoreEntitySet _storeEntitySet = null;

        internal AssociationSetMapping(EDMXFile parentFile, CSMapping csMapping, XmlElement asmElement)
            : base(parentFile)
        {
            _csMapping = csMapping;
            _asmElement = asmElement;
        }

        internal AssociationSetMapping(EDMXFile parentFile, XmlElement entityContainerMappingElement, CSMapping csMapping, string name, ModelAssociationSet modelAssocSet, StoreEntitySet storeEntitySet, StoreAssociationSet fromStoreAssocSet, StoreAssociationSet toStoreAssocSet)
            : base(parentFile)
        {
            _csMapping = csMapping;
            _modelAssociationSet = modelAssocSet;

            //create mapping xml elements
            _asmElement = EDMXDocument.CreateElement("AssociationSetMapping", NameSpaceURImap);
            entityContainerMappingElement.AppendChild(_asmElement);

            XmlElement fromEndProp = EDMXDocument.CreateElement("EndProperty", NameSpaceURImap);
            fromEndProp.SetAttribute("Name", modelAssocSet.FromRoleName);
            _asmElement.AppendChild(fromEndProp);

            XmlElement toEndProp = EDMXDocument.CreateElement("EndProperty", NameSpaceURImap);
            toEndProp.SetAttribute("Name", modelAssocSet.ToRoleName);
            _asmElement.AppendChild(toEndProp);

            List<Tuple<ModelMemberProperty, StoreMemberProperty, string>> fromKeys = (
                from key in fromStoreAssocSet.Keys
                select new Tuple<ModelMemberProperty, StoreMemberProperty, string>(
                    key.Item2.ModelMembers.FirstOrDefault(mm => mm.EntityType == modelAssocSet.FromEntityType),
                    key.Item1,
                    key.Item2.Name
                    )
                ).ToList();
            foreach (var key in fromKeys)
            {
                XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                scalarProperty.SetAttribute("Name", (key.Item1 != null ? key.Item1.Name : key.Item3));
                scalarProperty.SetAttribute("ColumnName", key.Item2.Name);
                fromEndProp.AppendChild(scalarProperty);
            }

            List<Tuple<ModelMemberProperty, StoreMemberProperty, string>> toKeys =
                (
                from key in toStoreAssocSet.Keys
                select new Tuple<ModelMemberProperty, StoreMemberProperty, string>(
                    key.Item2.ModelMembers.FirstOrDefault(mm => mm.EntityType == modelAssocSet.ToEntityType),
                    key.Item1,
                    key.Item2.Name
                    )
                ).ToList();
            foreach (var key in toKeys)
            {
                XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                scalarProperty.SetAttribute("Name", (key.Item1 != null ? key.Item1.Name : key.Item3));
                scalarProperty.SetAttribute("ColumnName", key.Item2.Name);
                toEndProp.AppendChild(scalarProperty);
            }

            Name = name;
            StoreEntitySetName = storeEntitySet.Name;
            TypeName = modelAssocSet.FullName;
        }

        internal AssociationSetMapping(EDMXFile parentFile, XmlElement entityContainerMappingElement, CSMapping csMapping, string name, HuagatiEDMXTools.ModelAssociationSet modelAssociationSet, StoreAssociationSet storeAssociationSet)
            : base(parentFile)
        {
            _csMapping = csMapping;
            _modelAssociationSet = modelAssociationSet;

            //create mapping xml elements
            _asmElement = EDMXDocument.CreateElement("AssociationSetMapping", NameSpaceURImap);
            entityContainerMappingElement.AppendChild(_asmElement);

            XmlElement fromEndProp = EDMXDocument.CreateElement("EndProperty", NameSpaceURImap);
            fromEndProp.SetAttribute("Name", modelAssociationSet.FromRoleName);
            _asmElement.AppendChild(fromEndProp);

            XmlElement toEndProp = EDMXDocument.CreateElement("EndProperty", NameSpaceURImap);
            toEndProp.SetAttribute("Name", modelAssociationSet.ToRoleName);
            _asmElement.AppendChild(toEndProp);

            List<Tuple<ModelMemberProperty, StoreMemberProperty, string>> fromKeys = (
                from key in storeAssociationSet.Keys
                select new Tuple<ModelMemberProperty, StoreMemberProperty, string>(
                    key.Item2.ModelMembers.FirstOrDefault(mm => mm.EntityType == modelAssociationSet.FromEntityType),
                    key.Item2,
                    key.Item2.Name
                    )
                ).ToList();
            foreach (var key in fromKeys)
            {
                XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                scalarProperty.SetAttribute("Name", (key.Item1 != null ? key.Item1.Name : key.Item3));
                scalarProperty.SetAttribute("ColumnName", key.Item2.Name);
                fromEndProp.AppendChild(scalarProperty);
            }

            List<Tuple<ModelMemberProperty, StoreMemberProperty, string>> toKeys =
                (
                from key in storeAssociationSet.Keys
                select new Tuple<ModelMemberProperty, StoreMemberProperty, string>(
                    key.Item1.ModelMembers.FirstOrDefault(mm => mm.EntityType == modelAssociationSet.ToEntityType),
                    key.Item1,
                    key.Item2.Name
                    )
                ).ToList();
            foreach (var key in toKeys)
            {
                XmlElement scalarProperty = EDMXDocument.CreateElement("ScalarProperty", NameSpaceURImap);
                scalarProperty.SetAttribute("Name", (key.Item1 != null ? key.Item1.Name : key.Item3));
                scalarProperty.SetAttribute("ColumnName", key.Item2.Name);
                toEndProp.AppendChild(scalarProperty);
            }

            Name = name;
            StoreEntitySetName = storeAssociationSet.FromEntitySet.Name;
            TypeName = modelAssociationSet.FullName;
        }

        /// <summary>
        /// Event fired when the AssociationSetMapping has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        /// Method to remove the AssociationSetMapping from the model
        /// </summary>
        public void Remove()
        {
            try
            {
                if (_asmElement.ParentNode != null)
                {
                    _asmElement.ParentNode.RemoveChild(_asmElement);

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
        /// Event fired when the AssociationSetMapping has changed name
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        /// <summary>
        /// AssociationSetMapping name, corresponding to the Name attribute in the MSL AssociationSetMapping element
        /// </summary>
        public string Name
        {
            get
            {
                return _asmElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _asmElement.GetAttribute("Name");
                _asmElement.SetAttribute("Name", value);

                if (NameChanged != null)
                {
                    NameChanged(this, new NameChangeArgs { OldName = oldName, NewName = value });
                }
            }
        }

        /// <summary>
        /// Fully qualified name. For AssociationSetMapping, this is the same as the Name property.
        /// </summary>
        public string FullName
        {
            get
            {
                return this.Name;
            }
        }

        /// <summary>
        /// Alias name. For AssociationSetMapping, this is the same as the Name property.
        /// </summary>
        public string AliasName
        {
            get
            {
                return FullName;
            }
        }

        /// <summary>
        /// Returns the ModelAssociationSet mapped with this AssociationSetMapping
        /// </summary>
        public ModelAssociationSet ModelAssociationSet
        {
            get
            {
                try
                {
                    if (_modelAssociationSet == null)
                    {
                        _modelAssociationSet = ParentFile.ConceptualModel.AssociationSets.FirstOrDefault(aset => aset.FullName.Equals(TypeName, StringComparison.InvariantCultureIgnoreCase) || aset.AliasName.Equals(TypeName, StringComparison.InvariantCultureIgnoreCase));
                        if (_modelAssociationSet != null)
                        {
                            _modelAssociationSet.Removed += new EventHandler(ModelAssociationSet_Removed);
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

        void ModelAssociationSet_Removed(object sender, EventArgs e)
        {
            this.Remove();
            _modelAssociationSet = null;
        }

        /// <summary>
        /// Returns the StoreEntitySet mapped with this AssociationSetMapping
        /// </summary>
        public StoreEntitySet StoreEntitySet
        {
            get
            {
                try
                {
                    if (_storeEntitySet == null)
                    {
                        _storeEntitySet = ParentFile.StorageModel.EntitySets.FirstOrDefault(ses => ses.Name.Equals(StoreEntitySetName, StringComparison.InvariantCultureIgnoreCase));
                        if (_storeEntitySet != null)
                        {
                            _storeEntitySet.Removed += new EventHandler(StoreEntitySet_Removed);
                        }
                    }
                    return _storeEntitySet;
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

        void StoreEntitySet_Removed(object sender, EventArgs e)
        {
            this.Remove();
            _storeEntitySet = null;
        }

        /// <summary>
        /// The AssociationSetMapping's conceptual model association
        /// </summary>
        public string TypeName
        {
            get
            {
                return _asmElement.GetAttribute("TypeName");
            }
            protected set
            {
                _asmElement.SetAttribute("TypeName", value);
            }
        }

        /// <summary>
        /// Name of the mapped store entity set
        /// </summary>
        public string StoreEntitySetName
        {
            get
            {
                return _asmElement.GetAttribute("StoreEntitySet");
            }
            protected set
            {
                _asmElement.SetAttribute("StoreEntitySet", value);
            }
        }
    }
}
