using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DashShared;
using Microsoft.Azure.Documents;

namespace DashServer.Controllers
{
    /// <summary>
    /// Api Endpoint for shapes
    /// </summary>
    public class ShapeController : ApiController
    {
        /// <summary>
        /// The document repository which stores documents we communicate wth
        /// </summary>
        private IDocumentRepository _documentRepository;

        /// <summary>
        /// Create a new shape controller this should never have to be done manually, its instantiated when needed
        /// </summary>
        /// <param name="documentRepository"></param>
        public ShapeController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Add a new ShapeModel to the server
        /// </summary>
        /// <param name="shapeModel">The model which is being added to the server</param>
        /// <returns>The model which was added otherwise returns an error</returns>
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

        /// <summary>
        /// Update an existing model on the server,
        /// </summary>
        /// <param name="shapeModel">The existing model on the server which needs to be updated</param>
        /// <returns>The model which was updated otherwise returns an error</returns>
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

        /// <summary>
        /// Get a shapeModel from the server by its id
        /// </summary>
        /// <param name="id">The id of the shapeModel which is being retrieved</param>
        /// <returns>The shape model which was searched for otherwise an error</returns>
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

        /// <summary>
        /// Deletes a shapeModel from the server by its id
        /// </summary>
        /// <param name="id">The id of the model which is going to be deleted</param>
        /// <returns>The deleted model otherwise an error</returns>
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
