﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class FieldController : Controller
    {

        private readonly IDocumentRepository _documentRepository;

        /// <summary>
        /// Constructs a new DocumentController endpoint with a reference to the Document Repository.
        /// </summary>
        /// <param name="documentRepository"></param>
        public FieldController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }
        

        // GET api/document/5, returns the document with the given ID
        [HttpGet("{id}")]
        public async Task<FieldModelDTO> Get(string id)
        {
            return await _documentRepository.GetItemByIdAsync<FieldModelDTO>(id);
        }

        // POST api/document, adds a document with a given 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]FieldModelDTO FieldModelDTO)
        {
            try
            {
                // add the shape model to the documentRepository
                FieldModelDTO = await _documentRepository.AddItemAsync(FieldModelDTO);
            }
            catch (DocumentClientException e)
            {
               // _logger.LogWarning(LoggingEvents.DOCUMENT_CLIENT_EXCPETION, e,
                //    "Could Not Add Shape To Document Repository", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
               // _logger.LogError(LoggingEvents.UNHANDLED_EXCEPTION, e,
               //     "An exception was throws that we do not handle", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(FieldModelDTO);
        }

        // PUT api/document/5, updates a given document field
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody]FieldModelDTO FieldModelDTO)
        {
            try
            {
                // add the shape model to the documentRepository
                FieldModelDTO = await _documentRepository.UpdateItemAsync(FieldModelDTO);
            }
            catch (DocumentClientException e) // TODO: verify this is the right error to check for
            {
                // _logger.LogWarning(LoggingEvents.DOCUMENT_CLIENT_EXCPETION, e,
                //    "Could Not Add Shape To Document Repository", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                // _logger.LogError(LoggingEvents.UNHANDLED_EXCEPTION, e,
                //     "An exception was throws that we do not handle", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(FieldModelDTO);
        }

        // DELETE api/document/5, sends OK on success
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(FieldModelDTO FieldModelDTO)
        {
            try
            {
               await _documentRepository.DeleteItemAsync<FieldModelDTO>(FieldModelDTO);
            }
            catch (DocumentClientException e)
            {
                // _logger.LogWarning(LoggingEvents.DOCUMENT_CLIENT_EXCPETION, e,
                //    "Could Not Add Shape To Document Repository", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                // _logger.LogError(LoggingEvents.UNHANDLED_EXCEPTION, e,
                //     "An exception was throws that we do not handle", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok();
        }
    }
}
