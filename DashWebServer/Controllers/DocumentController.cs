﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class DocumentController : Controller
    {

        private readonly IDocumentRepository _documentRepository;
        private readonly PushHandler _pushHandler;

        /// <summary>
        /// Constructs a new DocumentController endpoint with a reference to the Document Repository.
        /// </summary>
        /// <param name="documentRepository"></param>
        public DocumentController(IDocumentRepository documentRepository, PushHandler pushHandler)
        {
            _documentRepository = documentRepository;
            _pushHandler = pushHandler;
        }


        // GET api/document/5, returns the document with the given ID
        [HttpGet("{id}")]
        public async Task<DocumentModel> Get(string id)
        {
            return await _documentRepository.GetItemByIdAsync<DocumentModel>(id);
        }

        // POST api/document, adds a new document from the given docModel
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]DocumentModel docModel)
        {
            DocumentModel DocModel;
            try {
                Debug.WriteLine(docModel);
                // add the shape model to the documentRepository
                DocModel = await _documentRepository.AddItemAsync(docModel);
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _pushHandler.SendCreate(DocModel);

            return Ok(DocModel);
        }

        // PUT api/document, pushes updates of a given DocumentModel into the server?
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]DocumentModel docModel)
        {
            DocumentModel DocModel;
            try {
                DocModel = await _documentRepository.UpdateItemAsync(docModel);
            }
            catch (Exception e) {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _pushHandler.SendUpdate(DocModel);

            return Ok(DocModel);
        }
        
        // DELETE api/document/5, deletes a document of the given id sends OK on success
        [HttpDelete("{id}")] 
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _documentRepository.DeleteItemAsync(await _documentRepository.GetItemByIdAsync<DocumentModel>(id));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _pushHandler.SendDelete(id);

            return Ok();
        }

    }
}
