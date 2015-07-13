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
    /// Represents a mapping between a conceptual model function import and a storage model function.
    /// </summary>
    public class FunctionImportMapping : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember
    {
        private CSMapping _csMapping = null;
        private XmlElement _fimElement = null;
        private ModelFunction _modelFunction = null;
        private StoreFunction _storeFunction = null;

        internal FunctionImportMapping(EDMXFile parentFile, CSMapping csMapping, XmlElement fimElement)
            : base(parentFile)
        {
            _csMapping = csMapping;
            _fimElement = fimElement;
            ModelFunction mf = this.ModelFunction;
        }

        internal FunctionImportMapping(EDMXFile parentFile, XmlElement entityContainerMappingElement, CSMapping csMapping, ModelFunction modelFunction, StoreFunction storeFunction)
            : base(parentFile)
        {
            _csMapping = csMapping;

            _modelFunction = modelFunction;
            _modelFunction.NameChanged += new EventHandler<NameChangeArgs>(ModelFunction_NameChanged);
            _modelFunction.Removed += new EventHandler(ModelFunction_Removed);

            _storeFunction = storeFunction;
            _storeFunction.Removed += new EventHandler(StoreFunction_Removed);
        }

        #region IEDMXNamedMember Members

        /// <summary>
        /// Name of the model object
        /// </summary>
        public string Name
        {
            get
            {
                return _fimElement.GetAttribute("FunctionImportName");
            }
            set
            {
                string oldName = Name;
                if (value == null) { throw new ArgumentNullException(); }
                _fimElement.SetAttribute("FunctionImportName", value);
                if (NameChanged != null)
                {
                    NameChanged(this, new NameChangeArgs { NewName = value, OldName = oldName });
                }
            }
        }

        /// <summary>
        /// Fully qualified name, including parent object names.
        /// </summary>
        public string FullName
        {
            get { return Name; }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get { return Name; }
        }

        /// <summary>
        /// Event fired when the object changes name
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        #endregion

        #region IEDMXRemovableMember Members

        /// <summary>
        /// Removes the object from the model.
        /// </summary>
        public void Remove()
        {
            try
            {
                if (_fimElement.ParentNode != null)
                {
                    _fimElement.ParentNode.RemoveChild(_fimElement);

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
        /// Event fired when the object has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        #endregion

        /// <summary>
        /// Conceptual model function import mapped by this FunctionImportMapping.
        /// </summary>
        public ModelFunction ModelFunction
        {
            get
            {
                try
                {
                    if (_modelFunction == null)
                    {
                        _modelFunction = ParentFile.ConceptualModel.FunctionImports.FirstOrDefault(fi => fi.Name == this.Name);
                        if (_modelFunction != null)
                        {
                            _modelFunction.NameChanged += new EventHandler<NameChangeArgs>(ModelFunction_NameChanged);
                            _modelFunction.Removed += new EventHandler(ModelFunction_Removed);
                        }
                    }
                    return _modelFunction;
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
                _modelFunction = value;
                if (_modelFunction != null)
                {
                    _modelFunction.Removed += new EventHandler(ModelFunction_Removed);
                    this.Name = _modelFunction.Name;
                }
            }
        }

        void ModelFunction_NameChanged(object sender, NameChangeArgs e)
        {
            this.Name = e.NewName;
        }

        void ModelFunction_Removed(object sender, EventArgs e)
        {
            this.Remove();
            _modelFunction = null;
        }

        /// <summary>
        /// Name of the store function mapped by this FunctionImportMapping.
        /// </summary>
        public string StoreFunctionName
        {
            get
            {
                return _fimElement.GetAttribute("FunctionName");
            }
            private set
            {
                _fimElement.SetAttribute("FunctionName", value);
            }
        }

        /// <summary>
        /// Store function mapped by this FunctionImportMapping.
        /// </summary>
        public StoreFunction StoreFunction
        {
            get
            {
                try
                {
                    if (_storeFunction == null)
                    {
                        string functionName = StoreFunctionName;
                        _storeFunction = ParentFile.StorageModel.Functions.FirstOrDefault(sf => sf.FullName.Equals(functionName, StringComparison.InvariantCultureIgnoreCase) || sf.AliasName.Equals(functionName, StringComparison.InvariantCultureIgnoreCase));
                        if (_storeFunction != null)
                        {
                            _storeFunction.Removed += new EventHandler(StoreFunction_Removed);
                        }
                    }
                    return _storeFunction;
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
                _storeFunction = value;
                if (_storeFunction != null)
                {
                    _storeFunction.Removed += new EventHandler(StoreFunction_Removed);
                    StoreFunctionName = _storeFunction.FullName;
                }
            }
        }

        void StoreFunction_Removed(object sender, EventArgs e)
        {
            this.Remove();
            _storeFunction = null;
        }
    }
}
