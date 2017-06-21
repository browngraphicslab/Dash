using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNetCore.Mvc;

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

        // GET: api/values
        [HttpGet]
        public async Task<ShapeModel> Get()
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

            // add the shape model to the documentRepository
            var model = await _documentRepository.AddItemAsync(shapeModel);

            model.X += 1;


            var model2 = await _documentRepository.UpdateItemAsync(model);

            await _documentRepository.DeleteItemAsync(model2);

            return model;
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
