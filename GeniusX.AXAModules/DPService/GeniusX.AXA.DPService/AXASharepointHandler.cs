using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.SharePoint.Client;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Configuration;
using Xiap.Framework.Logging;
using Xiap.Framework.Validation;
using Xiap.Framework.Common.DocumentProduction;
using System.Net;
using System.Collections;

namespace GeniusX.AXA.DPService
{
    /// <summary>
    /// This class is used to upload 'External' documents to the 
    /// site and manage the document set creation and metadata.
    /// </summary>
    public class AXASharepointHandler : IDocumentManagementHandler
    {
        private const string DOCUMENTSET_CONTENTTYPE = "Document Set";
        private const string CLAIM_REFERENCE_FIELDNAME = "Claim_Reference";
        private const string TITLE_FIELDNAME = "Title";
        private const string LOB_FIELDNAME = "LOB";
        private const string ENTITY_FIELDNAME = "Entity";
        private const string PRODUCT_CODE_FIELDNAME = "ProductCode";
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string redirectUrl;
        private Dictionary<string, string> contentTypes = new Dictionary<string, string>();
        private bool isCredentialsProvided = false;

        public AXASharepointHandler()
        {
            XmlNode processConfigs = ConfigurationFactory.ConfigurationManager.GetSection<XmlNode>("xiap/custom/sharepointConfiguration");
            if (processConfigs == null)
            {
                throw new ObjectNotFoundException("Can not find a sharepointConfiguration section in configuration file");
            }

            XmlNode locationNode = processConfigs.SelectSingleNode("//locationTemplate");
            if (locationNode == null)
            {
                throw new ObjectNotFoundException("Can not find a locationTemplate within the sharepointConfiguration section");
            }

            this.redirectUrl = locationNode == null ? string.Empty : locationNode.InnerText;
            XmlNode contentNode = processConfigs.SelectSingleNode("//contentTypes");
            XmlNode isCredentialsProvidedNode = processConfigs.SelectSingleNode("//isCredentialsProvided");
            if (isCredentialsProvidedNode != null && isCredentialsProvidedNode.InnerText.ToLower() == "true")
            {
                this.isCredentialsProvided = true;
            }

            foreach (XmlNode node in contentNode.ChildNodes)
            {
                this.contentTypes.Add(node.Attributes["DocumentLevel"].Value, node.InnerText);
            }
        }

        /// <summary>
        /// This method upload the document and set the redirectUrl.
        /// </summary>
        /// <param name="document">Document in bytes </param>
        /// <param name="destinationUrl"> Destination Url</param>
        /// <param name="contentType">Content type </param>
        /// <param name="metadata"> Meta data</param>
        /// <returns>Document URl </returns>
        public UploadDocumentResult UploadDocument(byte[] document, string destinationUrl, string contentType, Dictionary<string, object> metadata)
        {
            UploadDocumentResult uploadDocumentResult = new UploadDocumentResult();
            uploadDocumentResult.IsDocumentUploadDelayed = false;

            using (new PerfLogger(typeof(AXASharepointHandler), "UploadDocument"))
            {
                ArgumentCheck.ArgumentNullCheck(document, "document");
                ArgumentCheck.ArgumentNullCheck(destinationUrl, "destinationUrl");

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug("UploadDocument()");
                }

                string site;
                string docID = this.UploadFile(destinationUrl, contentType, metadata, document, out site);
                string documentURL = string.Format(this.redirectUrl, site, docID);

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug("UploadDocument():documentURL = " + documentURL);
                }

                uploadDocumentResult.Folder = documentURL;
                uploadDocumentResult.ExternalDocumentReference = docID;
                return uploadDocumentResult;
            }
        }

        /// <summary>
        /// This method upload the file in the document library/document set with 
        /// the metadata.
        /// </summary>
        /// <param name="destinUrl"> Destination url</param>
        /// <param name="contentType"> Content type</param>
        /// <param name="metadata"> Meta data</param>
        /// <param name="document">Document in bytes </param>
        /// <param name="site">Site in  </param>
        /// <returns> Return ID</returns>
        private string UploadFile(string destinUrl, string contentType, Dictionary<string, object> metadata, byte[] document, out string site)
        {
            using (new PerfLogger(typeof(AXASharepointHandler), "UploadFile"))
            { 
                string returnID = string.Empty;

                string claimReference = metadata[CLAIM_REFERENCE_FIELDNAME].ToString();
                string claimProductCode = metadata[PRODUCT_CODE_FIELDNAME].ToString();
                string lob = metadata[LOB_FIELDNAME].ToString();
                string entity = metadata[ENTITY_FIELDNAME].ToString();


                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug("Claim Product Code => " + claimProductCode);
                    _Logger.Debug("DestinUrl => " + destinUrl);
                    _Logger.Debug("ContentType => " + contentType);
                }

                string productSiteUrl = AXAClaimProductHelper.GetProductFolderURL(claimProductCode).Trim();
                _Logger.Debug("ProductSiteUrl => " + productSiteUrl);

                if (productSiteUrl == string.Empty)
                {
                    site = string.Empty;
                    return string.Empty;
                }

                string documentName = destinUrl;
                destinUrl = productSiteUrl + documentName;

                // Retrieve the Sharepoint site URL from the Product URL. 
                string sharePointSiteURL = productSiteUrl;
                if (productSiteUrl.EndsWith("/"))
                {
                    productSiteUrl = productSiteUrl.Substring(0, productSiteUrl.Length - 1);
                }

                sharePointSiteURL = productSiteUrl.Substring(0, productSiteUrl.LastIndexOf("/"));
                string library = productSiteUrl.Substring(productSiteUrl.LastIndexOf("/") + 1);
                                
                _Logger.Debug("sharePointSiteURL => " + sharePointSiteURL);
                _Logger.Debug("document library => " + library);
                string username = null;
                string domain = null;
                using (ClientContext clientContext = new ClientContext(sharePointSiteURL))
                {
                    if (this.isCredentialsProvided)
                    {
                        //// If explicit credentials are provided for SharePoint access, then connect via the given credentials.
                        Hashtable sharepointCredentials = (Hashtable)ConfigurationManager.GetSection("SharepointCredentials");
                        username = (string)sharepointCredentials["username"];
                        string password = (string)sharepointCredentials["password"];
                        domain = (string)sharepointCredentials["domain"];
                        clientContext.AuthenticationMode = ClientAuthenticationMode.Default;
                        clientContext.Credentials = new NetworkCredential(username, password, domain);
                    }

                    ////get the site object.
                    Web web = clientContext.Web;
                    List list = web.Lists.GetByTitle(library);

                    clientContext.Load(
                        web,
                        website => website.Title,
                        website => website.Created,
                        website => website.Description,
                        website => website.ServerRelativeUrl);
                    
                    site = sharePointSiteURL; 
                     
                    ////Create the Document set with metadata.
                    Folder documentSet = this.CreateDocumentSet(clientContext, claimReference, productSiteUrl, library, lob, entity);
                    if (documentSet == null)
                    {
                        if (_Logger.IsDebugEnabled)
                        {
                            _Logger.Debug(string.Format("Document set {0} does not exists.", claimReference));
                        }

                        return string.Empty;
                    }

                    var fileCreationInformation = new FileCreationInformation();

                    ////Assign to content byte[] i.e. documentStream
                    fileCreationInformation.Content = document;

                    ////Allow owerwrite of document
                    fileCreationInformation.Overwrite = true;

                    clientContext.Load(web);
                    clientContext.ExecuteQuery();

                    ////Upload URL
                    fileCreationInformation.Url = web.ServerRelativeUrl + "/" + library + "/" + claimReference + "/" + documentName;
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug("FileCreationInformation.Url =" + fileCreationInformation.Url);
                    }

                    Microsoft.SharePoint.Client.File uploadFile = documentSet.Files.Add(
                        fileCreationInformation);

                    clientContext.Load(documentSet);
                    clientContext.ExecuteQuery();

                    ////Get the file item object for accessing its metadata.
                    ListItem fileItem = uploadFile.ListItemAllFields;

                    clientContext.Load(fileItem, item => item["_dlc_DocId"]);
                    clientContext.Load(fileItem);
                    clientContext.Load(list.ContentTypes);
                    clientContext.ExecuteQuery();

                    ContentType type = null;
                    string contentValue;
                    this.contentTypes.TryGetValue(contentType, out contentValue);
                    // check if content type is present in site
                    foreach (var content in list.ContentTypes)
                    {
                        if (content.Name == contentValue)
                        {
                            type = content;
                            break;
                        }
                    }

                    if (type != null && metadata != null)
                    {
                        if (_Logger.IsDebugEnabled)
                        {
                            _Logger.Debug("Adding metadata to uploaded file");
                        }

                        // add metadata to the file
                        foreach (string key in metadata.Keys)
                        {
                            if (fileItem.FieldValues.ContainsKey(key))
                            {
                                fileItem[key] = metadata[key];
                            }
                            else if (key != CLAIM_REFERENCE_FIELDNAME && key != LOB_FIELDNAME && key != PRODUCT_CODE_FIELDNAME && key != ENTITY_FIELDNAME)
                            {
                                //// Do not throw Error in the properties of the Document Set
                                throw new ObjectNotFoundException(string.Format("Field {0} not configured in sharepoint.", key));
                            }
                        }

                        // set site content type
                        fileItem["ContentTypeId"] = type != null ? type.Id : null;
                        fileItem.Update();
                        clientContext.ExecuteQuery();
                    }

                    ////Get the Xiap user.
                    User user = null;
                    if (this.isCredentialsProvided)
                    {
                        user = web.EnsureUser(domain + @"\" + username);
                    }
                    else
                    {
                        user = web.EnsureUser(XiapIdentity.GetCurrentIdentity().Name);
                    }

                    clientContext.Load(user);
                    clientContext.ExecuteQuery();

                    ////Update the current user details.
                    fileItem["Author"] = user;
                    fileItem["Editor"] = user;
                    fileItem.Update();
                    clientContext.ExecuteQuery();

                    ////return document id
                    returnID = fileItem["_dlc_DocId"].ToString();
                }

                return returnID;
            }
        }

        /// <summary>
        /// This method updates the metadata information in the sharepoint.
        /// </summary>
        /// <param name="destinUrl">Destination url</param>
        /// <param name="documentReference"> Document Name</param>
        /// <param name="metadata">Meta data</param>
        public void UpdateMetadata(string destinUrl, string documentReference, Dictionary<string, object> metadata)
        {
            NetworkCredential networkCredentials = null;

            if (this.isCredentialsProvided)
            {
                //// If explicit credentials are provided for SharePoint access, then connect via the given credentials.
                Hashtable sharepointCredentials = (Hashtable)ConfigurationManager.GetSection("SharepointCredentials");
                string username = (string)sharepointCredentials["username"];
                string password = (string)sharepointCredentials["password"];
                string domain = (string)sharepointCredentials["domain"];
                networkCredentials = new NetworkCredential(username, password, domain);
            }

            destinUrl = this.GetRedirectURL(destinUrl, networkCredentials);

            string transportProtocol = "http://";
            if (destinUrl.StartsWith("https://"))
            {
                transportProtocol = "https://";
            }

            int siteIndex = destinUrl.Substring(transportProtocol.Length).IndexOf('/');
            string sharepointSite = destinUrl.Substring(0, transportProtocol.Length + siteIndex);
            using (ClientContext clientContext = new ClientContext(sharepointSite))
            {
                if (this.isCredentialsProvided)
                {
                    clientContext.AuthenticationMode = ClientAuthenticationMode.Default;
                    clientContext.Credentials = networkCredentials;
                }

                string urlToAccess = destinUrl.Replace(sharepointSite, string.Empty);
                Web web = this.GetSite(clientContext, urlToAccess);

                Microsoft.SharePoint.Client.File file = web.GetFileByServerRelativeUrl(urlToAccess);
                clientContext.Load(file, f => f.ListItemAllFields);
                clientContext.ExecuteQuery();

                ListItem fileItem = file.ListItemAllFields;
                bool fieldUpdated = false;
                foreach (string key in metadata.Keys)
                {
                    if (fileItem.FieldValues.ContainsKey(key))
                    {
                        fileItem[key] = metadata[key];
                        fieldUpdated = true;
                    }
                    else
                    {
                        //// Do not throw Error in the properties of the Document Set
                        if (key != CLAIM_REFERENCE_FIELDNAME && key != LOB_FIELDNAME && key != PRODUCT_CODE_FIELDNAME && key != ENTITY_FIELDNAME)
                        {
                            throw new ObjectNotFoundException(string.Format("Field {0} not configured in sharepoint.", key));
                        }
                    }
                }

                if (fieldUpdated)
                {
                    fileItem.Update();
                    clientContext.ExecuteQuery();
                }
            }
        }

        /// <summary>
        /// This method checks if a metadata update is required for the document in sharepoint.
        /// </summary>
        /// <param name="destinUrl">The url of the document.</param>
        /// <param name="metadata">Meta data</param>
        /// <param name="document">Document object</param>
        /// <returns>If a document metadata needs an update</returns>
        public bool CheckIfDocumentMetadataUpdateRequired(string destinUrl, Dictionary<string, object> metadata, IDocument document)
        {
            if ((document.PropertiesChanged != null && document.PropertiesChanged.Any()) ||
               (document.DirtyPropertyList != null && document.DirtyPropertyList.Count() > 0))
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// This method fetches the Absolute Uri of the document.
        /// </summary>
        /// <param name="documentUrl">Document Url</param>
        /// <param name="networkCredential">The network credentials to connect to sharepoint</param>
        /// <returns>The absolute Uri.</returns>
        private string GetRedirectURL(string documentUrl, NetworkCredential networkCredential)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(documentUrl);
            req.Method = "HEAD";
            req.AllowAutoRedirect = true;
            req.UseDefaultCredentials = false;
            req.Credentials = networkCredential;

            try
            {
                req.GetResponse();
            }
            catch
            {
            }

            return req.Address.AbsoluteUri;
        }

        /// <summary>
        /// This method will create a document set.
        /// </summary>
        /// <param name="clientContext"> The client context</param>
        /// <param name="claimReference">Reference of the Claim</param>
        /// <param name="library">library name</param>
        /// <param name="documentLibrary">document library name</param>
        /// <param name="lob">The lob</param>
        /// <param name="entity">The entity</param>
        /// <returns>The Folder created</returns>
        private Folder CreateDocumentSet(ClientContext clientContext, string claimReference, string library, string documentLibrary, string lob, string entity)
        {
            using (new PerfLogger(typeof(AXASharepointHandler), "CreateDocumentSet"))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug("CreateDocumentSet()");
                }

                // Check for the existence of the document set.
                Folder libFolder = clientContext.Web.GetFolderByServerRelativeUrl(library + "/" + claimReference);
                clientContext.Load(libFolder);
                bool exists = false;

                try
                {
                    clientContext.ExecuteQuery();
                    exists = true;
                }
                catch (Exception)
                { 
                }

                if (exists == false)
                {
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug(string.Format("CreateDocumentSet() -> creating {0} document set", claimReference));
                    }

                    List list = clientContext.Web.Lists.GetByTitle(documentLibrary);

                    // create the document set.
                    ContentTypeCollection listContentTypes = list.ContentTypes;
                    clientContext.Load(listContentTypes, types => types.Include(type => type.Id, type => type.Name,type => type.Parent));

                    var result = clientContext.LoadQuery(listContentTypes.Where(c => c.Name == DOCUMENTSET_CONTENTTYPE));

                    clientContext.ExecuteQuery();
                    ContentType targetDocumentSetContentType = result.FirstOrDefault();

                    ListItemCreationInformation newItemInfo = new ListItemCreationInformation();
                    newItemInfo.UnderlyingObjectType = FileSystemObjectType.Folder;
                    newItemInfo.LeafName = claimReference;

                    ListItem newListItem = list.AddItem(newItemInfo);
                    
                    ////adding metadata.
                    if (targetDocumentSetContentType != null)
                    {
                        newListItem["ContentTypeId"] = targetDocumentSetContentType.Id.ToString();
                    }

                    newListItem[TITLE_FIELDNAME] = claimReference;
                    newListItem[LOB_FIELDNAME] = lob;
                    newListItem[ENTITY_FIELDNAME] = entity;
                    newListItem[CLAIM_REFERENCE_FIELDNAME] = claimReference;
                    newListItem.Update();

                    clientContext.ExecuteQuery();

                    libFolder = clientContext.Web.GetFolderByServerRelativeUrl(library + "/" + claimReference);
                }

                return libFolder;
            }
        }

        ////Get the document set as folder from folders collection.
        private Folder GetDocumentSetFolder(FolderCollection folders,string folderName)
        {
            Folder returnValue= null;
            foreach (Folder folder in folders)
            {
                if (folder.Name == folderName)
                {
                    returnValue = folder;
                    break;
                }
            }

            return returnValue;
        }

        ////This method saves the bytes stream to file stream at specific location
        ////in case their is an error uploading the file.
        private void SaveBytesToStream(byte[] document,string documentName)
        {
            FileStream stream = new FileStream(@"c:\\" + documentName, FileMode.Create);
            stream.Write(document,0,document.Length);
            stream.Close();
        }

        public Stream GetDocument(string docURL)
        {
            return null;
        }


        public string UploadDocument(byte[] document, string documentName, string contentType, Dictionary<string, object> metadata, out bool isDocumentUploadDelayed)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method fetches the site in sharepoint for the specified url.
        /// </summary>
        /// <param name="clientContext">client context</param>
        /// <param name="urlToUpload">The url</param>
        /// <returns>The site in sharepoint</returns>
        private Web GetSite(ClientContext clientContext, string urlToUpload)
        {
            using (new PerfLogger(typeof(AXASharepointHandler), "GetSite"))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug("GetSite()");
                }

                urlToUpload = urlToUpload.StartsWith("/") ? urlToUpload.Substring(1) : urlToUpload;
                string[] strs = urlToUpload.Split('/');
                Web web = clientContext.Web;
                string relativeUrl = "/";
                foreach (string str in strs)
                {
                    var query = clientContext.LoadQuery(web.Webs.Where(p => p.ServerRelativeUrl == relativeUrl + str));
                    clientContext.ExecuteQuery();
                    Web temp = query.FirstOrDefault();
                    if (temp != null)
                    {
                        web = temp;
                        relativeUrl += str + "/";
                    }
                    else
                    {
                        break;
                    }
                }

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("GetSite() -> site location {0}", web.ServerRelativeUrl));
                }

                return web;
            }
        }
    }
}
