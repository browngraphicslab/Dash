using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            var docModel = await _documentRepository.GetItemByIdAsync<DocumentModel>(id);

            return await GetDocumentModelDtoFromDocumentModel(docModel);
        }
        
        // GET api/batch/document/5, returns the document with the given ID
        [HttpGet("batch/{ids}")]
        [HttpGet("batch")]
        public async Task<IEnumerable<DocumentModelDTO>> GetDocumentsByIds(IEnumerable<string> ids)
        {
            var docModels = new List<DocumentModel>();
            foreach (var docId in ids)
            {
                docModels.Add(await _documentRepository.GetItemByIdAsync<DocumentModel>(docId));
            }

            var docModelDtos = new List<DocumentModelDTO>();
            foreach (var docModel in docModels)
            {
                docModelDtos.Add(await GetDocumentModelDtoFromDocumentModel(docModel));
            }

            return docModelDtos;
        }

        // GET api/document/type/5, returns a list of documents with type specified by the given id
        [HttpGet("type/{id}")]
        public async Task<IEnumerable<DocumentModelDTO>> GetDocumentsByType(string id)
        {
            var docModels = await _documentRepository.GetItemsAsync<DocumentModel>(documentModel => documentModel.DocumentType.Id == id);
            var docModelDtos = new List<DocumentModelDTO>();

            foreach (var docModel in docModels)
            {
                docModelDtos.Add(await GetDocumentModelDtoFromDocumentModel(docModel));
            }

            return docModelDtos;
        }

        // private helper method to transform a doucmentmodel into a document model dto
        private async Task<DocumentModelDTO> GetDocumentModelDtoFromDocumentModel(DocumentModel docModel)
        {
            var fieldModelDtos = new List<FieldModelDTO>();
            var keyModels = new List<KeyModel>();

            foreach (var fieldId in docModel.Fields.Values)
            {
                var field = await _documentRepository.GetItemByIdAsync<FieldModelDTO>(fieldId);
                fieldModelDtos.Add(field);
            }

            foreach (var keyId in docModel.Fields.Keys)
            {
                var key = await _documentRepository.GetItemByIdAsync<KeyModel>(keyId);
                keyModels.Add(key);
            }

            return new DocumentModelDTO(fieldModelDtos, keyModels, docModel.DocumentType, docModel.Id);
        }

        // POST api/document, adds a new document from the given docModel
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DocumentModel docModel)
        {
            DocumentModel DocModel;
            try
            {
                // add the shape model to the documentRepository
                DocModel = await _documentRepository.AddItemAsync(docModel);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e);
            }

            _pushHandler.SendCreate(DocModel);

            return Ok(DocModel);
        }

        // PUT api/document, pushes updates of a given DocumentModel into the server?
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] DocumentModel docModel)
        {
            DocumentModel DocModel;
            try
            {
                DocModel = await _documentRepository.UpdateItemAsync(docModel);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e);
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
                    await _documentRepository.GetItemByIdAsync<DocumentModel>(id));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e);
            }

            _pushHandler.SendDelete(id);

            return Ok();
        }

        // DELETE api/document/, deletes all documents from the database
        [HttpDelete]
        public async Task<IActionResult> DeleteAllDocuments()
        {
            try
            {
                await _documentRepository.DeleteAllAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError, e);
            }

            return Ok();
        }
    }
}