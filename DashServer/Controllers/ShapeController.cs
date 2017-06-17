using System;
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
    public class ShapeController : ApiController
    {

        private IDocumentRepository _documentRepository;

        public ShapeController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<IHttpActionResult> Post([FromBody]ShapeModel shapeModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return Ok(await _documentRepository.AddItemAsync(shapeModel));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }


        public async Task<IHttpActionResult> Put([FromBody]ShapeModel shapeModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return Ok(await _documentRepository.UpdateItemAsync(shapeModel));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }


        public async Task<IHttpActionResult> Get(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var item = (await _documentRepository.GetItemsAsync<ShapeModel>(i => i.Id == id)).FirstOrDefault();
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
