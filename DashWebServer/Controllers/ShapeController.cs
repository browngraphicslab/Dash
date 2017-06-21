using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class ShapeController : Controller
    {
        private readonly IDocumentRepository _documentRepository;

        public ShapeController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Createa a new item on the server
        /// </summary>
        /// <returns>The newly created item</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // generate random shapes
            var random = new Random();
            
            // create a new shapeModel
            var shapeModel = new ShapeModel()
            {
                Height = random.Next(1000),
                Width = random.Next(1000),
                X = random.Next(1000),
                Y = random.Next(1000),
                Id = Guid.NewGuid().ToString(),
            };

            try
            {
                // add the shape model to the documentRepository
                shapeModel= await _documentRepository.AddItemAsync(shapeModel);
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok(shapeModel);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
