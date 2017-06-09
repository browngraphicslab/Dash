using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.Azure.Documents;

namespace DashServer.Controllers
{
    /// <summary>
    ///     Description
    /// </summary>
    [RoutePrefix("api/Product")]
    public class ProductsController : ApiController
    {


        private IDocumentRepository _documentRepository;

        public ProductsController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        ///     An example method description
        /// </summary>
        /// <returns></returns>
        // GET: api/Products
        public async Task<IEnumerable<Product>> Get()
        {
            return await _documentRepository.GetItemsAsync<Product>(i => i.Type == "Product");
        }

        /// <summary>
        ///     An example method description
        /// </summary>
        /// <param name="id">An example parameter description</param>
        /// <returns>An example return value description</returns>
        // GET: api/Products/5
        public string Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        // POST: api/Products
        public async Task<IHttpActionResult> Post([FromBody] Product value)
        {
            if (value == null)
            {
                return BadRequest("the passed in model was null");
            }

            try
            {
                return Ok(await _documentRepository.AddItemAsync(value));
            }
            catch (DocumentClientException e)
            {
                return InternalServerError(e);
            }
        }

        // PUT: api/Products/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/Products/5
        public void Delete(int id)
        {
        }
    }
}
