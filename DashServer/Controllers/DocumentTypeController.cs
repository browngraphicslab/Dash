﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DashShared;
using Microsoft.Azure.Documents;

namespace DashServer.Controllers
{
    /// <summary>
    /// Used to add and remove 
    /// </summary>
    public class DocumentTypeController : ApiController
    {

        private IDocumentRepository _documentRepository;

        public DocumentTypeController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Creates and returns a new Key with a unique id but the same name
        /// </summary>
        /// <param name="name">The name of the key which will be created</param>
        /// <returns>The new key with the given name and a unique id</returns>
        public async Task<IHttpActionResult> Post([FromBody]string type)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newKey = new Key(type);

            try
            {
                return Ok(await _documentRepository.AddItemAsync(newKey));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// Updates and returns a Key with a new name
        /// </summary>
        /// <param name="name">The name of the key which will be created</param>
        /// <returns>The new key with the given name and a unique id</returns>
        public async Task<IHttpActionResult> Put([FromBody]DocumentType type)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return Ok(await _documentRepository.UpdateItemAsync(type));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// Returns a key with the same Id
        /// </summary>
        /// <param name="id">The id of the key to be searched for</param>
        /// <returns>The key with the unique id that was passed in</returns>
        public async Task<IHttpActionResult> Get(string id)
        {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            try
            {
                var item = (await _documentRepository.GetItemsAsync<Key>(i => i.Id == id)).FirstOrDefault();
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

        /// <summary>
        /// Deletes the key with the passed in id from the database
        /// </summary>
        /// <param name="id">The id of the key which is to be deleted</param>
        /// <returns>The key which was deleted</returns>
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
