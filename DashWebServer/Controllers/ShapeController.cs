using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace DashWebServer.Controllers
{
    [Route("api/[controller]")]
    public class ShapeController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ShapeController> _logger;

        public ShapeController(IDocumentRepository documentRepository, ILogger<ShapeController> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
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

            _logger.LogInformation(LoggingEvents.CREATE_ITEM, "Creating item {ID}, at {RequestTime}", shapeModel.Id, DateTime.Now);


            try
            {
                // add the shape model to the documentRepository
                shapeModel = await _documentRepository.AddItemAsync(shapeModel);
            }
            catch (DocumentClientException e)
            {
                _logger.LogWarning(LoggingEvents.DOCUMENT_CLIENT_EXCPETION, e,
                    "Could Not Add Shape To Document Repository", shapeModel);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                _logger.LogError(LoggingEvents.UNHANDLED_EXCEPTION, e,
                    "An exception was throws that we do not handle", shapeModel);
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
