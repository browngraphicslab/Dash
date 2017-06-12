using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DashShared;
using Microsoft.Azure.Documents;

namespace DashServer.Controllers
{
    /// <summary>
    /// The controller for the workspace endpoint
    /// </summary>
    public class WorkspaceController : ApiController
    {
        /// <summary>
        /// The document repository represents a connection to a database which contains documents
        /// </summary>
        private IDocumentRepository _documentRepository;

        /// <summary>
        /// Creates a new instance of the workspace controller
        /// </summary>
        /// <param name="documentRepository">The connection to a document db satisfied through dependency injection</param>
        public WorkspaceController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Creates and returns a new workspace with a unique id but the same name
        /// </summary>
        /// <param name="name">The name of the workspace which will be created</param>
        /// <returns>The new workspace with the given name and a unique id</returns>
        public async Task<IHttpActionResult> Post([FromBody]string name)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newWorkSpace = new WorkspaceModel(name);

            try
            {
                return Ok(await _documentRepository.AddItemAsync(newWorkSpace));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// Updates and returns a Workspace with a new name
        /// </summary>
        /// <param name="workspace">The workspace model which is going to be replaced in the database</param>
        /// <returns>The updated workspace model</returns>
        public async Task<IHttpActionResult> Put([FromBody]WorkspaceModel workspace)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return Ok(await _documentRepository.UpdateItemAsync(workspace));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// Returns a Workspace with the same id
        /// </summary>
        /// <param name="id">The id of the workspace to be searched for</param>
        /// <returns>The workspace with the unique id that was passed in</returns>
        public async Task<IHttpActionResult> Get(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var item = (await _documentRepository.GetItemsAsync<WorkspaceModel>(i => i.Id == id)).FirstOrDefault();
                if (item == null)
                {
                    return NotFound();
                }

                return Ok(item);
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// Deletes the workspace with the passed in id from the database
        /// </summary>
        /// <param name="id">The id of the workspace which is to be deleted</param>
        /// <returns>The workspace which was deleted</returns>
        public async Task<IHttpActionResult> Delete(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var item = await _documentRepository.DeleteItemAsync(id);
                if (item == null)
                {
                    return NotFound();
                }
                return Ok(item);
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                return InternalServerError(e);
            }
        }
    }
}
