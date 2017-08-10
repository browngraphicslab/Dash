﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class DocumentController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly PushHandler _pushHandler;

        /// <summary>
        ///     Constructs a new DocumentController endpoint with a reference to the Document Repository.
        /// </summary>
        /// <param name="documentRepository"></param>
        public DocumentController(IDocumentRepository documentRepository, PushHandler pushHandler)
        {
            _documentRepository = documentRepository;
            _pushHandler = pushHandler;
        }


        // GET api/document/5, returns the document with the given ID
        [HttpGet("{id}")]
        public async Task<DocumentModelDTO> GetDocumentById(string id)
        {
            return await _documentRepository.GetItemByIdAsync<DocumentModelDTO>(id);
        }

        // GET api/document/type/5, returns a list of documents with type specified by the given id
        [HttpGet("type/{id}")]
        public async Task<IEnumerable<DocumentModelDTO>> GetDocumentsByType(string id)
        {
            return await _documentRepository.GetItemsAsync<DocumentModelDTO>(documentModel => documentModel.DocumentType.Id == id);
        }

        // POST api/document, adds a new document from the given docModel
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DocumentModelDTO docModel)
        {
            DocumentModelDTO DocModel;
            try
            {
                // add the shape model to the documentRepository
                DocModel = await _documentRepository.AddItemAsync(docModel);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _pushHandler.SendCreate(DocModel);

            return Ok(DocModel);
        }

        // PUT api/document, pushes updates of a given DocumentModel into the server?
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] DocumentModelDTO docModel)
        {
            DocumentModelDTO DocModel;
            try
            {
                DocModel = await _documentRepository.UpdateItemAsync(docModel);
            }
            catch (Exception e)
            {
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
                await _documentRepository.DeleteItemAsync(
                    await _documentRepository.GetItemByIdAsync<DocumentModelDTO>(id));
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