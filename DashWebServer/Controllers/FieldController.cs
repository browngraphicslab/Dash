using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class FieldController : Controller
    {

        private readonly IDocumentRepository _documentRepository;

        /// <summary>
        /// Constructs a new FieldController endpoint with a reference to the Document Repository.
        /// </summary>
        /// <param name="FieldRepository"></param>
        public FieldController(IDocumentRepository FieldRepository)
        {
            _documentRepository = FieldRepository;
        }
        
        /// GET api/Field/5, returns the Field with the given ID
        [HttpGet("{id}")]
        public async Task<FieldModelDTO> Get(string id)
        {
            return await _documentRepository.GetItemByIdAsync<FieldModelDTO>(id);
        }

        // POST api/Field, adds a Field with a given 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]FieldModelDTO fieldModelDto)
        {
            try
            {
                fieldModelDto = await _documentRepository.AddItemAsync(fieldModelDto);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(fieldModelDto);
        }

        // POST api/field/batch, adds the complete list of fields
        [HttpPost]
        [Route("batch")]
        public async Task<IActionResult> Post([FromBody]IEnumerable<FieldModelDTO> fieldModelDtOs)
        {
            try
            {
                fieldModelDtOs = await _documentRepository.AddItemsAsync(fieldModelDtOs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(fieldModelDtOs);
        }

        // PUT api/field/5, updates a given Field field
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]FieldModelDTO fieldModelDTO)
        {
            try
            {
                fieldModelDTO = await _documentRepository.UpdateItemAsync(fieldModelDTO);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(fieldModelDTO);
        }

        // DELETE api/field/5, sends OK on success
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _documentRepository.DeleteItemAsync(await _documentRepository.GetItemByIdAsync<FieldModelDTO>(id));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok();
        }
    }
}
