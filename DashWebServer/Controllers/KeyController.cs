using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DashShared;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class KeyController : Controller
    {
        private readonly IDocumentRepository _documentRepository;

        /// <summary>
        /// Constructs a new KeyController endpoint with a reference to the Document Repository.
        /// </summary>
        /// <param name="KeyRepository"></param>
        public KeyController(IDocumentRepository KeyRepository)
        {
            _documentRepository = KeyRepository;
        }

        /// GET api/Key/5, returns the Key with the given ID
        [HttpGet("{id}")]
        public async Task<Key> Get(string id)
        {
            return await _documentRepository.GetItemByIdAsync<Key>(id);
        }

        // POST api/Key, adds a Key with a given 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Key Key)
        {
            try
            {
                Key = await _documentRepository.AddItemAsync(Key);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(Key);
        }

        // PUT api/Key/5, updates a given Key Key
        [HttpPut()]
        public async Task<IActionResult> Put([FromBody]Key Key)
        {
            try
            {
                Key = await _documentRepository.UpdateItemAsync(Key);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok(Key);
        }

        // DELETE api/Key/5, sends OK on success
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _documentRepository.DeleteItemAsync(await _documentRepository.GetItemByIdAsync<Key>(id));
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return Ok();
        }
    }
}
