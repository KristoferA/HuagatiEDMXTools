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
    /// Class representing an EDMX file. This class is the entry point when working with an existing or creating a new EDMX file.
    /// </summary>
    public class EDMXFile
    {
        private XmlDocument _edmxDocument = null;
        private XmlDocument _diagramDocument = null;
        private XmlNamespaceManager _nsm = null;

        private EDMXVersionEnum _edmxVersion = EDMXVersionEnum.Unknown;

        private StorageModel _storageModel = null;
        private ConceptualModel _conceptualModel = null;
        private CSMapping _csMapping = null;
        private Designer _designer = null;

        internal XmlDocument EDMXDocument
        {
            get
            {
                return _edmxDocument;
            }
        }

        internal XmlDocument EDMXDiagramDocument
        {
            get
            {
                return _diagramDocument;
            }
        }

        internal XmlNamespaceManager NSM
        {
            get
            {
                if (_nsm == null)
                {
                    _nsm = EDMXUtils.GetNamespaceManager(_edmxDocument, out _edmxVersion);
                }
                return _nsm;
            }
        }

        public EDMXVersionEnum EDMXVersion
        {
            get
            {
                return _edmxVersion;
            }
        }

        /// <summary>
        /// Creates a new EDMX file with the given model name, provider and provider manifest token.
        /// </summary>
        /// <param name="modelName">Name of the new model, e.g. "AdventureWorks".</param>
        /// <param name="dbProvider">Database provider name, e.g. "System.Data.SqlClient"</param>
        /// <param name="providerManifestToken">Provider manifest token, e.g. "2008"</param>
        public EDMXFile(string modelName, string dbProvider, string providerManifestToken) : this(modelName, dbProvider, providerManifestToken, EDMXVersionEnum.EDMX2010) { }

        /// <summary>
        /// Creates a new EDMX file with the given model name, provider and provider manifest token.
        /// </summary>
        /// <param name="modelName">Name of the new model, e.g. "AdventureWorks".</param>
        /// <param name="dbProvider">Database provider name, e.g. "System.Data.SqlClient"</param>
        /// <param name="providerManifestToken">Provider manifest token, e.g. "2008"</param>
        /// <param name="version">EDMX file version</param>
        public EDMXFile(string modelName, string dbProvider, string providerManifestToken, EDMXVersionEnum version)
        {
            //new blank EDMX File
            _edmxDocument = new XmlDocument();

            string schemaURI = string.Empty;
            string versionNumber = string.Empty;
            switch (version)
            {
                case EDMXVersionEnum.EDMX2008:
                    schemaURI = "http://schemas.microsoft.com/ado/2007/06/edmx";
                    versionNumber = "1.0";
                    break;
                case EDMXVersionEnum.EDMX2010:
                    schemaURI = "http://schemas.microsoft.com/ado/2008/10/edmx";
                    versionNumber = "2.0";
                    break;
                case EDMXVersionEnum.EDMX2012:
                    schemaURI = "http://schemas.microsoft.com/ado/2009/11/edmx";
                    versionNumber = "3.0";
                    break;
            }

            //create document element
            XmlElement docElement = _edmxDocument.CreateElement("Edmx", schemaURI);
            docElement.SetAttribute("Version", versionNumber);
            _edmxDocument.AppendChild(docElement);

            //get hold of the namespace manager
            _nsm = EDMXUtils.GetNamespaceManager(_edmxDocument, out _edmxVersion);

            CreateEmptyEDMX();

            //set required storage model properties
            this.StorageModel.Namespace = modelName + ".Store";
            this.StorageModel.Provider = dbProvider;
            this.StorageModel.ProviderManifestToken = providerManifestToken;
            this.StorageModel.ContainerName = modelName + "StoreContainer";

            //set required conceptial model properties
            this.ConceptualModel.Namespace = modelName;
            this.ConceptualModel.ContainerName = modelName + "Entities";

            //set required mapping properties
            this.CSMapping.StorageEntityContainer = this.StorageModel.ContainerName;
            this.CSMapping.ConceptualEntityContainer = this.ConceptualModel.ContainerName;

            //set required designer properties
            this.Designer.DiagramName = modelName;
        }

        private void CreateEmptyEDMX()
        {
            //get namespace URIs
            string edmxNamespaceURI = NSM.LookupNamespace("edmx");
            string ssdlNamespaceURI = NSM.LookupNamespace("ssdl");
            string csdlNamespaceURI = NSM.LookupNamespace("edm");

            //create the basic structure of the EDMX file wrapper
            XmlElement runtimeElement = _edmxDocument.CreateElement("Runtime", edmxNamespaceURI);
            EDMXDocument.DocumentElement.AppendChild(runtimeElement);

            XmlElement storageModels = _edmxDocument.CreateElement("StorageModels", edmxNamespaceURI);
            runtimeElement.AppendChild(storageModels);

            XmlElement conceptualModels = _edmxDocument.CreateElement("ConceptualModels", edmxNamespaceURI);
            runtimeElement.AppendChild(conceptualModels);

            XmlElement mappingsElement = _edmxDocument.CreateElement("Mappings", edmxNamespaceURI);
            runtimeElement.AppendChild(mappingsElement);

            XmlElement designerElement = _edmxDocument.CreateElement("Designer", edmxNamespaceURI);
            EDMXDocument.DocumentElement.AppendChild(designerElement);
            designerElement.GetOrCreateElement("edmx", "Diagrams", NSM);
        }

        /// <summary>
        /// Opens an existing EDMX file
        /// </summary>
        /// <param name="fileName">File name of the EDMX file to open.</param>
        public EDMXFile(string fileName)
        {
            //load EDMX file
            _edmxDocument = new XmlDocument();
            _edmxDocument.Load(fileName);

            if (System.IO.File.Exists(fileName + ".diagram"))
            {
                _diagramDocument = new XmlDocument();
                _diagramDocument.Load(fileName + ".diagram");
            }

            //get namespace manager
            _nsm = EDMXUtils.GetNamespaceManager(_edmxDocument, out _edmxVersion);

            //ensure the file is in the expected format...
            switch (_edmxVersion)
            {
                case EDMXVersionEnum.EDMX2012:
                case EDMXVersionEnum.EDMX2010:
                    //expected format
                    break;
                case EDMXVersionEnum.EDMX2008:
                    throw new NotSupportedException("The EDMX file is in VS2008 / EFv1 format. This tool only support VS2010/EFv4 EDMX files");
                //break;
                default:
                    throw new NotSupportedException("Unknown EDMX file format.");
            }
        }

        /// <summary>
        /// Reads an EDMX file from a stream.
        /// </summary>
        /// <param name="edmxStream">Stream to read the EDMX data from.</param>
        public EDMXFile(System.IO.Stream edmxStream)
        {
            //load edmx stream
            _edmxDocument = new XmlDocument();
            _edmxDocument.Load(edmxStream);

            //get namespace manager
            _nsm = EDMXUtils.GetNamespaceManager(_edmxDocument, out _edmxVersion);

            //ensure the file is in the expected format...
            switch (_edmxVersion)
            {
                case EDMXVersionEnum.EDMX2012:
                case EDMXVersionEnum.EDMX2010:
                    //expected format
                    break;
                case EDMXVersionEnum.EDMX2008:
                    throw new NotSupportedException("The EDMX file is in VS2008 / EFv1 format. This tool only support VS2010/EFv4 EDMX files");
                //break;
                default:
                    throw new NotSupportedException("Unknown EDMX file format.");
            }
        }

        /// <summary>
        /// Saves the EDMX to a file.
        /// </summary>
        /// <param name="fileName">File name and path.</param>
        public void Save(string fileName)
        {
            AddSaveComment();

            _edmxDocument.Save(fileName);

            if (_diagramDocument != null)
            {
                _diagramDocument.Save(fileName + ".diagram");
            }
        }

        /// <summary>
        /// Saves the EDMX to a stream.
        /// </summary>
        /// <param name="stream">Stream to save the EDMX to.</param>
        public void Save(System.IO.Stream stream)
        {
            AddSaveComment();

            _edmxDocument.Save(stream);
        }

        private void AddSaveComment()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            string version = asm.GetName().Version.ToString();

            //remove old update comments
            foreach (XmlComment xc in EDMXDocument.DocumentElement.ChildNodes.OfType<XmlComment>().ToList())
            {
                string value = xc.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Trim().StartsWith("Updated by Huagati EDMX Tools"))
                    {
                        xc.ParentNode.RemoveChild(xc);
                    }
                }
            }

            //add a new update comment
            XmlComment comment = EDMXDocument.CreateComment("Updated by Huagati EDMX Tools version " + version + " on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", new System.Globalization.CultureInfo("en-us").DateTimeFormat));
            EDMXDocument.DocumentElement.InsertBefore(comment, EDMXDocument.DocumentElement.ChildNodes[0]);
        }

        /// <summary>
        /// Reference to the storage model.
        /// </summary>
        public StorageModel StorageModel
        {
            get
            {
                if (_storageModel == null)
                {
                    _storageModel = new StorageModel(this);
                }
                return _storageModel;
            }
        }

        /// <summary>
        /// Reference to the conceptual model.
        /// </summary>
        public ConceptualModel ConceptualModel
        {
            get
            {
                if (_conceptualModel == null)
                {
                    _conceptualModel = new ConceptualModel(this);
                }
                return _conceptualModel;
            }
        }

        /// <summary>
        /// Reference to the conceptual-storage model mappings.
        /// </summary>
        public CSMapping CSMapping
        {
            get
            {
                if (_csMapping == null)
                {
                    _csMapping = new CSMapping(this);
                }
                return _csMapping;
            }
        }

        /// <summary>
        /// Reference to designer / diagram data.
        /// </summary>
        public Designer Designer
        {
            get
            {
                if (_designer == null)
                {
                    _designer = new Designer(this);
                }
                return _designer;
            }
        }

        /// <summary>
        /// Contains exceptions encountered during model parsing
        /// </summary>
        public IEnumerable<Exception> ModelErrors
        {
            get
            {
                return StorageModel.ModelErrors.Concat(ConceptualModel.ModelErrors);
            }
        }
    }
}
